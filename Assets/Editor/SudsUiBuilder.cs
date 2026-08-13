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
        var resources = UiKit.StandardResources();
        var root = UiKit.ScreenCanvas("SudsUi", camera, planeDistance: 0.8f, sortingOrder: 40);

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

        UiKit.LayoutLabel(card, "How distressed do you feel right now?", 40f, TextDark,
            FontStyles.Bold, TextAlignmentOptions.Center, 56f);
        UiKit.LayoutLabel(card, "0 = no distress at all      100 = the worst imaginable", 24f,
            new Color(0.42f, 0.44f, 0.48f), FontStyles.Normal, TextAlignmentOptions.Center, 32f);

        var valueLabel = UiKit.LayoutLabel(card, "50", 100f, Accent,
            FontStyles.Bold, TextAlignmentOptions.Center, 118f);

        var slider = UiKit.StyledSlider(card, resources, 0f, 100f, wholeNumbers: true, height: 44f,
            background: new Color(0.78f, 0.8f, 0.82f), fill: Accent, handle: Color.white);
        slider.value = 50f;
        // An oversized handle: the patient drags this on a phone screen.
        slider.handleRect.sizeDelta = new Vector2(40f, 40f);

        UiKit.Spacer(card, 10f);

        // Centered confirm button in a non-stretching row.
        var buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
        buttonRow.transform.SetParent(card.transform, false);
        var rowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        UiKit.FixedHeight(buttonRow, 68f);

        var confirmButton = UiKit.SpriteButton(buttonRow, "ConfirmButton", Accent, "Confirm", 30f);
        ((RectTransform)confirmButton.transform).sizeDelta = new Vector2(280f, 64f);

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
}
