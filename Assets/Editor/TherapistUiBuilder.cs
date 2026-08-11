using PsyCurio.Shop;
using PsyCurio.Shop.Therapist;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the therapist control panel: a dark clinical side panel, visually
/// unmistakable as not-the-patient-view, toggled with T at runtime. Standard
/// uGUI controls come from DefaultControls (correct substructure for free),
/// restyled and labeled with TMP. Rebuilt from scratch on every wiring run.
/// </summary>
public static class TherapistUiBuilder
{
    private static readonly Color PanelBackground = new Color(0.07f, 0.08f, 0.1f, 1f);
    private static readonly Color Accent = new Color(0.44f, 0.66f, 0.92f);
    private static readonly Color LabelColor = new Color(0.88f, 0.9f, 0.93f);

    public static void Build(Camera camera, Cashier cashier, CashierEyeContact eyeContact,
        BystanderSpawner bystanders, AmbientNoise ambientNoise, SudsPrompt sudsPrompt)
    {
        var existing = GameObject.Find("TherapistUi");
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
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };

        var root = new GameObject("TherapistUi");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 0.9f;
        canvas.sortingOrder = 30;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(470f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImage = panel.AddComponent<Image>();
        panelImage.sprite = resources.background;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = PanelBackground;

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 30, 30);
        layout.spacing = 15;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        Label(panel, font, "THERAPIST CONTROLS", 30f, Accent, FontStyles.Bold, 44f);
        Label(panel, font, "Exposure intensity — press T or tap Therapist to close", 20f,
            new Color(0.55f, 0.58f, 0.63f), FontStyles.Normal, 26f);
        Spacer(panel, 8f);

        // Eye contact: toggle + inline label.
        var toggleRow = Row(panel, 40f);
        var toggle = BuildToggle(toggleRow, resources);
        Label(toggleRow, font, "Cashier eye contact", 24f, LabelColor, FontStyles.Normal, 40f);

        Spacer(panel, 6f);
        var delayLabel = Label(panel, font, "Response delay: 0.5 s", 24f, LabelColor, FontStyles.Normal, 30f);
        var delaySlider = BuildSlider(panel, resources, 0f, 5f, wholeNumbers: false);

        Spacer(panel, 6f);
        var bystanderLabel = Label(panel, font, "Bystanders: 0", 24f, LabelColor, FontStyles.Normal, 30f);
        var bystanderSlider = BuildSlider(panel, resources, 0f, 3f, wholeNumbers: true);

        Spacer(panel, 6f);
        var noiseLabel = Label(panel, font, "Ambient noise: 0 %", 24f, LabelColor, FontStyles.Normal, 30f);
        var noiseSlider = BuildSlider(panel, resources, 0f, 1f, wholeNumbers: false);

        Spacer(panel, 14f);
        var sudsButton = BuildSudsButton(panel, resources, font);

        var absorber = new GameObject("Spacer", typeof(RectTransform));
        absorber.transform.SetParent(panel.transform, false);
        absorber.AddComponent<LayoutElement>().flexibleHeight = 1f;

        panel.SetActive(false);

        var panelComponent = root.AddComponent<TherapistPanel>();
        var serialized = new SerializedObject(panelComponent);
        serialized.FindProperty("panelRoot").objectReferenceValue = panel;
        serialized.FindProperty("eyeContactToggle").objectReferenceValue = toggle;
        serialized.FindProperty("delaySlider").objectReferenceValue = delaySlider;
        serialized.FindProperty("delayLabel").objectReferenceValue = delayLabel;
        serialized.FindProperty("bystanderSlider").objectReferenceValue = bystanderSlider;
        serialized.FindProperty("bystanderLabel").objectReferenceValue = bystanderLabel;
        serialized.FindProperty("noiseSlider").objectReferenceValue = noiseSlider;
        serialized.FindProperty("noiseLabel").objectReferenceValue = noiseLabel;
        serialized.FindProperty("eyeContact").objectReferenceValue = eyeContact;
        serialized.FindProperty("cashier").objectReferenceValue = cashier;
        serialized.FindProperty("bystanders").objectReferenceValue = bystanders;
        serialized.FindProperty("ambientNoise").objectReferenceValue = ambientNoise;
        serialized.FindProperty("sudsButton").objectReferenceValue = sudsButton;
        serialized.FindProperty("sudsPrompt").objectReferenceValue = sudsPrompt;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        BuildToggleChip(root, resources, font, panelComponent);
    }

    /// <summary>
    /// Quiet bottom-left chip that toggles the panel — the touch-device
    /// equivalent of the T key, styled to match the Reset chip. Wired as a
    /// persistent listener so it serializes into the scene.
    /// </summary>
    private static void BuildToggleChip(GameObject canvasRoot, DefaultControls.Resources resources,
        TMP_FontAsset font, TherapistPanel panelComponent)
    {
        var chip = new GameObject("TherapistToggle", typeof(RectTransform));
        chip.transform.SetParent(canvasRoot.transform, false);
        var chipRect = chip.GetComponent<RectTransform>();
        chipRect.anchorMin = new Vector2(0f, 0f);
        chipRect.anchorMax = new Vector2(0f, 0f);
        chipRect.pivot = new Vector2(0f, 0f);
        chipRect.anchoredPosition = new Vector2(28f, 28f);
        chipRect.sizeDelta = new Vector2(180f, 56f);
        var chipImage = chip.AddComponent<Image>();
        chipImage.sprite = resources.standard;
        chipImage.type = Image.Type.Sliced;
        chipImage.color = new Color(0.16f, 0.17f, 0.19f, 0.9f);
        var chipButton = chip.AddComponent<Button>();
        chipButton.targetGraphic = chipImage;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(chip.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 26f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.text = "Therapist";

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            chipButton.onClick, panelComponent.TogglePanel);
    }

    private static Button BuildSudsButton(GameObject parent, DefaultControls.Resources resources,
        TMP_FontAsset font)
    {
        var button = new GameObject("SudsButton", typeof(RectTransform));
        button.transform.SetParent(parent.transform, false);
        var buttonImage = button.AddComponent<Image>();
        buttonImage.sprite = resources.standard;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = Accent;
        var buttonComponent = button.AddComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;
        var element = button.AddComponent<LayoutElement>();
        element.minHeight = 56f;
        element.preferredHeight = 56f;
        element.flexibleHeight = 0f;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(button.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 26f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.text = "Request SUDS rating";
        return buttonComponent;
    }

    private static GameObject Row(GameObject parent, float height)
    {
        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        var element = row.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        return row;
    }

    private static TextMeshProUGUI Label(GameObject parent, TMP_FontAsset font, string content,
        float size, Color color, FontStyles style, float height)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent.transform, false);
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = content;
        var element = labelObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        element.flexibleWidth = 1f;
        return label;
    }

    private static void Spacer(GameObject parent, float height)
    {
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(parent.transform, false);
        var spacerElement = spacer.AddComponent<LayoutElement>();
        spacerElement.minHeight = height;
        spacerElement.preferredHeight = height;
        spacerElement.flexibleHeight = 0f;
    }

    private static Toggle BuildToggle(GameObject parent, DefaultControls.Resources resources)
    {
        var toggleObject = DefaultControls.CreateToggle(resources);
        toggleObject.transform.SetParent(parent.transform, false);

        // DefaultControls ships a legacy Text label; the row has its own TMP one.
        var legacyLabel = toggleObject.transform.Find("Label");
        if (legacyLabel != null)
        {
            Object.DestroyImmediate(legacyLabel.gameObject);
        }

        var element = toggleObject.AddComponent<LayoutElement>();
        element.minWidth = 34f;
        element.preferredWidth = 34f;
        element.minHeight = 34f;
        element.preferredHeight = 34f;
        element.flexibleHeight = 0f;
        element.flexibleWidth = 0f;

        var background = toggleObject.transform.Find("Background").GetComponent<Image>();
        background.color = new Color(0.2f, 0.22f, 0.26f);
        background.rectTransform.sizeDelta = new Vector2(34f, 34f);
        var checkmark = background.transform.Find("Checkmark").GetComponent<Image>();
        checkmark.color = Accent;

        return toggleObject.GetComponent<Toggle>();
    }

    private static Slider BuildSlider(GameObject parent, DefaultControls.Resources resources,
        float min, float max, bool wholeNumbers)
    {
        var sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.transform.SetParent(parent.transform, false);
        var element = sliderObject.AddComponent<LayoutElement>();
        element.minHeight = 28f;
        element.preferredHeight = 28f;
        element.flexibleHeight = 0f;

        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;

        sliderObject.transform.Find("Background").GetComponent<Image>().color =
            new Color(0.2f, 0.22f, 0.26f);
        sliderObject.transform.Find("Fill Area/Fill").GetComponent<Image>().color = Accent;
        sliderObject.transform.Find("Handle Slide Area/Handle").GetComponent<Image>().color =
            new Color(0.85f, 0.88f, 0.92f);

        return slider;
    }
}
