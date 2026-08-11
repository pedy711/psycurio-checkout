using PsyCurio.Shop.Therapist;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the patient-facing SUDS prompt: a dimmed backdrop that swallows
/// scene clicks and a large, high-contrast card — question, 0/100 anchors,
/// oversized live value, slider, confirm. Rebuilt on every wiring run.
/// </summary>
public static class SudsUiBuilder
{
    private static readonly Color Accent = new Color(0.2f, 0.42f, 0.72f);
    private static readonly Color CardColor = new Color(0.97f, 0.97f, 0.95f);
    private static readonly Color TextDark = new Color(0.1f, 0.11f, 0.13f);

    public static SudsPrompt Build(Camera camera)
    {
        var existing = GameObject.Find("SudsUi");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };

        var root = new GameObject("SudsUi");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 0.8f;
        canvas.sortingOrder = 40;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();

        // Full-screen backdrop: dims the scene and blocks every click behind it.
        var prompt = new GameObject("Prompt", typeof(RectTransform));
        prompt.transform.SetParent(root.transform, false);
        var promptRect = prompt.GetComponent<RectTransform>();
        promptRect.anchorMin = Vector2.zero;
        promptRect.anchorMax = Vector2.one;
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;
        var backdrop = prompt.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.05f, 0.62f);

        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(prompt.transform, false);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(780f, 460f);
        var cardImage = card.AddComponent<Image>();
        cardImage.sprite = resources.standard;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = CardColor;

        var layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        Label(card, font, "How distressed do you feel right now?", 40f, TextDark,
            FontStyles.Bold, TextAlignmentOptions.Center, 56f);
        Label(card, font, "0 = no distress at all      100 = the worst imaginable", 24f,
            new Color(0.42f, 0.44f, 0.48f), FontStyles.Normal, TextAlignmentOptions.Center, 32f);

        var valueLabel = Label(card, font, "50", 100f, Accent,
            FontStyles.Bold, TextAlignmentOptions.Center, 118f);

        var sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.transform.SetParent(card.transform, false);
        var sliderElement = sliderObject.AddComponent<LayoutElement>();
        sliderElement.minHeight = 44f;
        sliderElement.preferredHeight = 44f;
        sliderElement.flexibleHeight = 0f;
        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.value = 50f;
        sliderObject.transform.Find("Background").GetComponent<Image>().color =
            new Color(0.78f, 0.8f, 0.82f);
        sliderObject.transform.Find("Fill Area/Fill").GetComponent<Image>().color = Accent;
        var handle = sliderObject.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
        handle.color = Color.white;
        handle.rectTransform.sizeDelta = new Vector2(40f, 40f);

        Spacer(card, 10f);

        // Centered confirm button in a non-stretching row.
        var buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
        buttonRow.transform.SetParent(card.transform, false);
        var rowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        var rowElement = buttonRow.AddComponent<LayoutElement>();
        rowElement.minHeight = 68f;
        rowElement.preferredHeight = 68f;
        rowElement.flexibleHeight = 0f;

        var button = new GameObject("ConfirmButton", typeof(RectTransform));
        button.transform.SetParent(buttonRow.transform, false);
        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(280f, 64f);
        var buttonImage = button.AddComponent<Image>();
        buttonImage.sprite = resources.standard;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Accent;
        var confirmButton = button.AddComponent<Button>();
        confirmButton.targetGraphic = buttonImage;

        var buttonLabelObject = new GameObject("Label", typeof(RectTransform));
        buttonLabelObject.transform.SetParent(button.transform, false);
        var buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
        buttonLabelRect.anchorMin = Vector2.zero;
        buttonLabelRect.anchorMax = Vector2.one;
        buttonLabelRect.offsetMin = Vector2.zero;
        buttonLabelRect.offsetMax = Vector2.zero;
        var buttonLabel = buttonLabelObject.AddComponent<TextMeshProUGUI>();
        buttonLabel.font = font;
        buttonLabel.fontSize = 30f;
        buttonLabel.color = Color.white;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.text = "Confirm";

        prompt.SetActive(false);

        var promptComponent = root.AddComponent<SudsPrompt>();
        var serialized = new SerializedObject(promptComponent);
        serialized.FindProperty("promptRoot").objectReferenceValue = prompt;
        serialized.FindProperty("slider").objectReferenceValue = slider;
        serialized.FindProperty("valueLabel").objectReferenceValue = valueLabel;
        serialized.FindProperty("confirmButton").objectReferenceValue = confirmButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return promptComponent;
    }

    private static TextMeshProUGUI Label(GameObject parent, TMP_FontAsset font, string content,
        float size, Color color, FontStyles style, TextAlignmentOptions alignment, float height)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent.transform, false);
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.fontStyle = style;
        label.alignment = alignment;
        label.text = content;
        var element = labelObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        return label;
    }

    private static void Spacer(GameObject parent, float height)
    {
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent.transform, false);
        var element = spacer.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }
}
