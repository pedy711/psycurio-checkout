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
/// restyled and labeled with TMP via the shared UiKit. Rebuilt from scratch on
/// every wiring run.
/// </summary>
public static class TherapistUiBuilder
{
    private static readonly Color PanelBackground = new Color(0.07f, 0.08f, 0.1f, 1f);
    private static readonly Color Accent = new Color(0.44f, 0.66f, 0.92f);
    private static readonly Color LabelColor = new Color(0.88f, 0.9f, 0.93f);
    private static readonly Color ControlBackground = new Color(0.2f, 0.22f, 0.26f);
    private static readonly Color HandleColor = new Color(0.85f, 0.88f, 0.92f);

    public static void Build(Camera camera, Cashier cashier, CashierEyeContact eyeContact,
        BystanderSpawner bystanders, AmbientNoise ambientNoise, SudsPrompt sudsPrompt)
    {
        var resources = UiKit.StandardResources();
        var root = UiKit.ScreenCanvas("TherapistUi", camera, planeDistance: 0.9f, sortingOrder: 30);

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

        Label(panel, "THERAPIST CONTROLS", 30f, Accent, FontStyles.Bold, 44f);
        Label(panel, "Exposure intensity — press T or tap Therapist to close", 20f,
            new Color(0.55f, 0.58f, 0.63f), FontStyles.Normal, 26f);
        UiKit.Spacer(panel, 8f);

        // Eye contact: toggle + inline label.
        var toggleRow = Row(panel, 40f);
        var toggle = BuildToggle(toggleRow, resources);
        Label(toggleRow, "Cashier eye contact", 24f, LabelColor, FontStyles.Normal, 40f);

        UiKit.Spacer(panel, 6f);
        var delayLabel = Label(panel, "Response delay: 0.5 s", 24f, LabelColor, FontStyles.Normal, 30f);
        var delaySlider = PanelSlider(panel, resources, 0f, 5f, wholeNumbers: false);

        UiKit.Spacer(panel, 6f);
        var bystanderLabel = Label(panel, "Bystanders: 0", 24f, LabelColor, FontStyles.Normal, 30f);
        var bystanderSlider = PanelSlider(panel, resources, 0f, 3f, wholeNumbers: true);

        UiKit.Spacer(panel, 6f);
        var noiseLabel = Label(panel, "Ambient noise: 0 %", 24f, LabelColor, FontStyles.Normal, 30f);
        var noiseSlider = PanelSlider(panel, resources, 0f, 1f, wholeNumbers: false);

        UiKit.Spacer(panel, 14f);
        var sudsButton = UiKit.SpriteButton(panel, "SudsButton", Accent, "Request SUDS rating", 26f);
        UiKit.FixedHeight(sudsButton.gameObject, 56f);

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

        // Quiet bottom-left chip, styled to match the Reset chip — the
        // touch-device equivalent of the T key. A persistent listener so the
        // binding serializes into the scene.
        var chip = UiKit.CornerChip(root, "TherapistToggle", Vector2.zero,
            new Vector2(28f, 28f), new Vector2(180f, 56f), "Therapist");
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            chip.onClick, panelComponent.TogglePanel);
    }

    private static TextMeshProUGUI Label(GameObject parent, string content, float size,
        Color color, FontStyles style, float height)
    {
        return UiKit.LayoutLabel(parent, content, size, color, style,
            TextAlignmentOptions.MidlineLeft, height, flexibleWidth: 1f);
    }

    private static Slider PanelSlider(GameObject parent, DefaultControls.Resources resources,
        float min, float max, bool wholeNumbers)
    {
        return UiKit.StyledSlider(parent, resources, min, max, wholeNumbers, 28f,
            ControlBackground, Accent, HandleColor);
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
        UiKit.FixedHeight(row, height);
        return row;
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

        var element = UiKit.FixedHeight(toggleObject, 34f);
        element.minWidth = 34f;
        element.preferredWidth = 34f;
        element.flexibleWidth = 0f;

        var background = toggleObject.transform.Find("Background").GetComponent<Image>();
        background.color = ControlBackground;
        background.rectTransform.sizeDelta = new Vector2(34f, 34f);
        var checkmark = background.transform.Find("Checkmark").GetComponent<Image>();
        checkmark.color = Accent;

        return toggleObject.GetComponent<Toggle>();
    }
}
