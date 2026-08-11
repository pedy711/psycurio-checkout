using PsyCurio.Shop;
using PsyCurio.Shop.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Attaches and cross-references scene-level components: ClickRouter on the
/// fixed camera, ShopController, CounterSlots with the five named anchors,
/// and a ShelfItem per shelf display. Idempotent; separate from the greybox
/// builder so re-wiring never regenerates geometry.
/// </summary>
public static class SceneWiring
{
    private const string ScenePath = "Assets/Scenes/Shop.unity";

    [MenuItem("PsyCurio/Wire Scene Interactions")]
    public static void Apply()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("SceneWiring: no Main Camera in Shop.unity");
            return;
        }

        if (camera.GetComponent<ClickRouter>() == null)
        {
            camera.gameObject.AddComponent<ClickRouter>();
        }

        var slots = WireCounterSlots();
        var controller = WireShopController(slots);
        WireShelfItems(controller);
        WireCashierSpeech();
        WireRegister(controller);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("SceneWiring: router, controller, slots, shelf items, balloon and register wired");
    }

    /// <summary>
    /// Ensures TMP essential resources exist (imported non-interactively from
    /// the uGUI package) and builds the cashier's speech balloon: world-space
    /// canvas above her head, sliced bubble, auto-height text. Idempotent —
    /// rebuilt from scratch on every run so tweaks land everywhere.
    /// </summary>
    private static void WireCashierSpeech()
    {
        var cashier = Object.FindFirstObjectByType<Cashier>();
        if (cashier == null)
        {
            Debug.LogError("SceneWiring: no Cashier in scene — run CashierSetup first");
            return;
        }

        if (!TmpEssentialsPresent())
        {
            return;
        }

        var existing = cashier.transform.Find("SpeechBalloon");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var balloonObject = new GameObject("SpeechBalloon", typeof(RectTransform));
        balloonObject.transform.SetParent(cashier.transform, false);
        balloonObject.transform.localPosition = new Vector3(0f, 1.92f, 0.25f);
        // 560 px * 0.0022 ≈ 1.2 m — speech-bubble sized, not billboard sized.
        balloonObject.transform.localScale = Vector3.one * 0.0022f;

        var canvas = balloonObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        var canvasRect = balloonObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(640f, 60f);
        canvasRect.pivot = new Vector2(0.5f, 0f);

        balloonObject.AddComponent<CanvasGroup>();

        var bubble = new GameObject("Bubble", typeof(RectTransform));
        bubble.transform.SetParent(balloonObject.transform, false);
        var bubbleRect = bubble.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.5f, 0f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0f);
        bubbleRect.pivot = new Vector2(0.5f, 0f);
        bubbleRect.sizeDelta = new Vector2(640f, 120f);

        var bubbleImage = bubble.AddComponent<UnityEngine.UI.Image>();
        bubbleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bubbleImage.type = UnityEngine.UI.Image.Type.Sliced;
        bubbleImage.color = new Color(0.98f, 0.98f, 0.96f, 0.97f);

        var layout = bubble.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 20, 20);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;

        var fitter = bubble.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(bubble.transform, false);
        var text = textObject.AddComponent<TMPro.TextMeshProUGUI>();
        // Explicit font: a script-created TMP component serializes font=null,
        // which renders as no text at all — in editor and in player alike.
        // Loaded straight from the asset path; TMP_Settings.defaultFontAsset
        // is avoided because its getter itself NREs before settings load.
        text.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.fontSize = 34f;
        text.color = new Color(0.09f, 0.09f, 0.11f);
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.textWrappingMode = TMPro.TextWrappingModes.Normal;
        text.text = "";

        var balloon = balloonObject.AddComponent<SpeechBalloon>();
        var serializedBalloon = new SerializedObject(balloon);
        serializedBalloon.FindProperty("messageText").objectReferenceValue = text;
        serializedBalloon.ApplyModifiedPropertiesWithoutUndo();

        var serializedCashier = new SerializedObject(cashier);
        serializedCashier.FindProperty("balloon").objectReferenceValue = balloon;
        serializedCashier.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// TMP essentials must already be on disk. AssetDatabase.ImportPackage in
    /// a -quit batch run only queues the import and nothing persists, so the
    /// import has to happen via Unity's synchronous -importPackage CLI
    /// argument (or the editor dialog) before this wiring runs.
    /// </summary>
    private static bool TmpEssentialsPresent()
    {
        if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null)
        {
            return true;
        }

        Debug.LogError("SceneWiring: TMP essential resources missing — run Unity with "
            + "-importPackage \"<project>/Library/PackageCache/com.unity.ugui@*/Package Resources/"
            + "TMP Essential Resources.unitypackage\" first (or Window > TextMeshPro > Import TMP Essential Resources)");
        return false;
    }

    private static void WireRegister(ShopController controller)
    {
        var registerObject = GameObject.Find("CashRegister");
        var cashier = Object.FindFirstObjectByType<Cashier>();
        if (registerObject == null || cashier == null)
        {
            Debug.LogError("SceneWiring: register or cashier missing — cannot wire checkout");
            return;
        }

        var register = registerObject.GetComponent<CashRegister>();
        if (register == null)
        {
            register = registerObject.AddComponent<CashRegister>();
        }
        if (registerObject.GetComponent<HoverHighlight>() == null)
        {
            registerObject.AddComponent<HoverHighlight>();
        }

        var serialized = new SerializedObject(register);
        serialized.FindProperty("controller").objectReferenceValue = controller;
        serialized.FindProperty("cashier").objectReferenceValue = cashier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CounterSlots WireCounterSlots()
    {
        var anchorsRoot = GameObject.Find("Counter/SlotAnchors");
        var slots = anchorsRoot.GetComponent<CounterSlots>();
        if (slots == null)
        {
            slots = anchorsRoot.AddComponent<CounterSlots>();
        }

        var anchors = new Transform[anchorsRoot.transform.childCount];
        for (var i = 0; i < anchors.Length; i++)
        {
            anchors[i] = anchorsRoot.transform.Find($"Slot_{i}");
        }

        var serialized = new SerializedObject(slots);
        var array = serialized.FindProperty("anchors");
        array.arraySize = anchors.Length;
        for (var i = 0; i < anchors.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return slots;
    }

    private static ShopController WireShopController(CounterSlots slots)
    {
        var shop = GameObject.Find("Shop");
        if (shop == null)
        {
            shop = new GameObject("Shop");
        }

        var controller = shop.GetComponent<ShopController>();
        if (controller == null)
        {
            controller = shop.AddComponent<ShopController>();
        }

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("counterSlots").objectReferenceValue = slots;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static void WireShelfItems(ShopController controller)
    {
        for (var i = 0; ; i++)
        {
            var spot = GameObject.Find($"ItemSpot_{i}");
            if (spot == null)
            {
                break;
            }

            var display = spot.transform.childCount > 0 ? spot.transform.GetChild(0).gameObject : null;
            if (display == null)
            {
                Debug.LogError($"SceneWiring: ItemSpot_{i} has no shelf display — run Rebuild Item Content first");
                continue;
            }

            var itemName = display.name.Replace("ShelfDisplay_", "");
            var definition = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>($"Assets/Items/{itemName}.asset");
            if (definition == null)
            {
                Debug.LogError($"SceneWiring: no ShopItemDefinition asset for '{itemName}'");
                continue;
            }

            var shelfItem = display.GetComponent<ShelfItem>();
            if (shelfItem == null)
            {
                shelfItem = display.AddComponent<ShelfItem>();
            }

            var serialized = new SerializedObject(shelfItem);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
