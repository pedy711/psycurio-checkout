using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The builders' shared uGUI toolkit. SceneWiring's screen UI, the therapist
/// panel and the SUDS prompt are one visual family; building their pieces here
/// keeps them consistent and keeps each builder about layout, not plumbing.
/// </summary>
public static class UiKit
{
    /// <summary>Quiet dark chip shared by the Reset and Therapist buttons.</summary>
    public static readonly Color ChipColor = new Color(0.16f, 0.17f, 0.19f, 0.9f);

    public static DefaultControls.Resources StandardResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };
    }

    /// <summary>
    /// Recreates the named screen-space canvas from scratch. Screen Space -
    /// Camera rather than Overlay so the offscreen render requests used for
    /// verification include it; visually identical here.
    /// </summary>
    public static GameObject ScreenCanvas(string name, Camera camera, float planeDistance,
        int sortingOrder)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        var root = new GameObject(name);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = planeDistance;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();
        return root;
    }

    public static LayoutElement FixedHeight(GameObject target, float height)
    {
        var element = target.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        return element;
    }

    /// <summary>Label that participates in a layout group.</summary>
    public static TextMeshProUGUI LayoutLabel(GameObject parent, string content, float size,
        Color color, FontStyles style, TextAlignmentOptions alignment, float height,
        float flexibleWidth = 0f)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent.transform, false);
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = EditorAssets.TmpFont();
        label.fontSize = size;
        label.color = color;
        label.fontStyle = style;
        label.alignment = alignment;
        label.text = content;
        FixedHeight(labelObject, height).flexibleWidth = flexibleWidth;
        return label;
    }

    /// <summary>Centered label stretched over its parent's full rect (buttons,
    /// pills).</summary>
    public static TextMeshProUGUI FillLabel(GameObject parent, string content, float size,
        Color color)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent.transform, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = EditorAssets.TmpFont();
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.text = content;
        return label;
    }

    public static void Spacer(GameObject parent, float height)
    {
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent.transform, false);
        FixedHeight(spacer, height);
    }

    /// <summary>Sliced-sprite button with a centered white TMP label.</summary>
    public static Button SpriteButton(GameObject parent, string name, Color background,
        string labelText, float fontSize)
    {
        var button = new GameObject(name, typeof(RectTransform));
        button.transform.SetParent(parent.transform, false);
        var image = button.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = background;
        var component = button.AddComponent<Button>();
        component.targetGraphic = image;
        FillLabel(button, labelText, fontSize, Color.white);
        return component;
    }

    /// <summary>Quiet chip pinned to a screen corner (Reset, Therapist).
    /// corner is the 0..1 anchor corner, e.g. (1,0) for bottom right.</summary>
    public static Button CornerChip(GameObject canvasRoot, string name, Vector2 corner,
        Vector2 anchoredPosition, Vector2 size, string labelText)
    {
        var chip = SpriteButton(canvasRoot, name, ChipColor, labelText, 26f);
        var rect = (RectTransform)chip.transform;
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return chip;
    }

    /// <summary>DefaultControls slider restyled to the given palette.</summary>
    public static Slider StyledSlider(GameObject parent, DefaultControls.Resources resources,
        float min, float max, bool wholeNumbers, float height,
        Color background, Color fill, Color handle)
    {
        var sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.transform.SetParent(parent.transform, false);
        FixedHeight(sliderObject, height);
        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        sliderObject.transform.Find("Background").GetComponent<Image>().color = background;
        sliderObject.transform.Find("Fill Area/Fill").GetComponent<Image>().color = fill;
        sliderObject.transform.Find("Handle Slide Area/Handle").GetComponent<Image>().color = handle;
        return slider;
    }
}
