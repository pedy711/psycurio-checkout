using System.Linq;
using PsyCurio.Shop;
using PsyCurio.Shop.Interaction;
using PsyCurio.Shop.Ui;
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
        // Gate the WHOLE run on TMP: aborting one wiring step while the rest
        // continues, saves and logs success would hide a null balloon until
        // the first Say() throws in Play mode.
        if (!EditorAssets.TmpEssentialsPresent())
        {
            Debug.LogError("SceneWiring: TMP essential resources missing — nothing wired. "
                + "Import them via Window > TextMeshPro > Import TMP Essential Resources, or run "
                + "Unity with -importPackage \"<project>/Library/PackageCache/com.unity.ugui@*/"
                + "Package Resources/TMP Essential Resources.unitypackage\" (AssetDatabase."
                + "ImportPackage in a -quit batch run only queues the import and persists nothing).");
            return;
        }

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
        if (slots == null)
        {
            return;
        }
        var controller = WireShopController(slots);
        WireShelfItems(controller);
        WireCashierSpeech(camera);
        WireRegister(controller);
        WireControllerCashier(controller);
        EnsureEventSystem();
        BuildScreenUi(camera, controller);
        WireClickFeedback(camera, controller);
        WireTherapistLayer(camera);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("SceneWiring: interactions, speech, register, UI, feedback and therapist layer wired");
    }

    private static void WireTherapistLayer(Camera camera)
    {
        var cashier = Object.FindFirstObjectByType<Cashier>();
        if (cashier == null)
        {
            Debug.LogError("SceneWiring: no Cashier — therapist layer skipped");
            return;
        }

        var eyeContact = cashier.GetComponent<CashierEyeContact>();
        if (eyeContact == null)
        {
            eyeContact = cashier.gameObject.AddComponent<CashierEyeContact>();
        }
        var serializedEye = new SerializedObject(eyeContact);
        serializedEye.FindProperty("lookTarget").objectReferenceValue = camera.transform;
        serializedEye.ApplyModifiedPropertiesWithoutUndo();

        var bystandersObject = GameObject.Find("Bystanders");
        if (bystandersObject == null)
        {
            bystandersObject = new GameObject("Bystanders");
        }
        var spawner = bystandersObject.GetComponent<PsyCurio.Shop.Therapist.BystanderSpawner>();
        if (spawner == null)
        {
            spawner = bystandersObject.AddComponent<PsyCurio.Shop.Therapist.BystanderSpawner>();
        }
        var queueAnchorsObject = GameObject.Find("QueueAnchors");
        if (queueAnchorsObject == null)
        {
            Debug.LogError("SceneWiring: QueueAnchors not found — run PsyCurio > Rebuild Greybox Scene first");
            return;
        }
        var anchorsRoot = queueAnchorsObject.transform;
        var serializedSpawner = new SerializedObject(spawner);
        var anchorArray = serializedSpawner.FindProperty("queueAnchors");
        anchorArray.arraySize = anchorsRoot.childCount;
        for (var i = 0; i < anchorsRoot.childCount; i++)
        {
            anchorArray.GetArrayElementAtIndex(i).objectReferenceValue = anchorsRoot.Find($"Queue_{i}");
        }
        // Prefer the Mixamo character variants; the greybox mannequin remains
        // the fallback if none have been set up yet.
        var variantGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Bystanders" });
        // Ascending: the slim first character heads the line nearest the
        // camera (clearing the desk-corner sightline); the widest (The Boss)
        // stands deepest, where the widening frustum fits his full body.
        var variants = variantGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .ToArray();
        if (variants.Length == 0)
        {
            variants = new[] { ItemContentBuilder.BuildBystanderPrefab() };
        }
        var prefabArray = serializedSpawner.FindProperty("bystanderPrefabs");
        prefabArray.arraySize = variants.Length;
        for (var v = 0; v < variants.Length; v++)
        {
            prefabArray.GetArrayElementAtIndex(v).objectReferenceValue = variants[v];
        }
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

        var ambientObject = GameObject.Find("AmbientNoise");
        if (ambientObject == null)
        {
            ambientObject = new GameObject("AmbientNoise");
        }
        if (ambientObject.GetComponent<AudioSource>() == null)
        {
            ambientObject.AddComponent<AudioSource>();
        }
        var ambient = ambientObject.GetComponent<PsyCurio.Shop.Therapist.AmbientNoise>();
        if (ambient == null)
        {
            ambient = ambientObject.AddComponent<PsyCurio.Shop.Therapist.AmbientNoise>();
        }

        var sudsPrompt = SudsUiBuilder.Build(camera);
        TherapistUiBuilder.Build(camera, cashier, eyeContact, spawner, ambient, sudsPrompt);
        WireSessionLogger(sudsPrompt);
    }

    private static void WireSessionLogger(PsyCurio.Shop.Therapist.SudsPrompt sudsPrompt)
    {
        var controller = Object.FindFirstObjectByType<ShopController>();
        var panel = Object.FindFirstObjectByType<PsyCurio.Shop.Therapist.TherapistPanel>();
        var shop = controller.gameObject;

        var logger = shop.GetComponent<PsyCurio.Shop.Therapist.SessionLogger>();
        if (logger == null)
        {
            logger = shop.AddComponent<PsyCurio.Shop.Therapist.SessionLogger>();
        }
        var serialized = new SerializedObject(logger);
        serialized.FindProperty("therapistPanel").objectReferenceValue = panel;
        serialized.FindProperty("controller").objectReferenceValue = controller;
        serialized.FindProperty("sudsPrompt").objectReferenceValue = sudsPrompt;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireControllerCashier(ShopController controller)
    {
        var cashier = Object.FindFirstObjectByType<Cashier>();
        if (cashier == null)
        {
            // Loud, not silent: without this reference the refusal line and
            // the register narration cannot be spoken at runtime.
            Debug.LogError("SceneWiring: no Cashier in scene — the controller's spoken "
                + "feedback is unwired; run PsyCurio > Setup Cashier, then re-wire");
            return;
        }
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("cashier").objectReferenceValue = cashier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        // Active Input Handling is "Both", so the classic module works.
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    /// <summary>
    /// Screen UI: the first-run hint (top center, fades on first interaction)
    /// and the reset button (bottom right, quiet — present but not loud).
    /// </summary>
    private static void BuildScreenUi(Camera camera, ShopController controller)
    {
        var root = UiKit.ScreenCanvas("ScreenUi", camera, planeDistance: 1f, sortingOrder: 20);

        // First-run hint: dark pill, white text, top center.
        var hint = new GameObject("FirstRunHint", typeof(RectTransform));
        hint.transform.SetParent(root.transform, false);
        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -36f);
        hintRect.sizeDelta = new Vector2(880f, 64f);
        var hintImage = hint.AddComponent<UnityEngine.UI.Image>();
        hintImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        hintImage.type = UnityEngine.UI.Image.Type.Sliced;
        hintImage.color = new Color(0.08f, 0.09f, 0.11f, 0.72f);
        hint.AddComponent<CanvasGroup>();
        UiKit.FillLabel(hint, "Click an item on the shelf to put it on the counter.", 30f, Color.white);

        var hintComponent = hint.AddComponent<FirstRunHint>();
        var serializedHint = new SerializedObject(hintComponent);
        serializedHint.FindProperty("router").objectReferenceValue = camera.GetComponent<ClickRouter>();
        serializedHint.ApplyModifiedPropertiesWithoutUndo();

        var resetButton = UiKit.CornerChip(root, "ResetButton", new Vector2(1f, 0f),
            new Vector2(-28f, 28f), new Vector2(210f, 56f), "Reset counter");
        var resetComponent = resetButton.gameObject.AddComponent<ResetButton>();
        var serializedReset = new SerializedObject(resetComponent);
        serializedReset.FindProperty("controller").objectReferenceValue = controller;
        serializedReset.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireClickFeedback(Camera camera, ShopController controller)
    {
        var shop = controller.gameObject;
        var audioSource = shop.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = shop.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        var feedback = shop.GetComponent<ClickFeedback>();
        if (feedback == null)
        {
            feedback = shop.AddComponent<ClickFeedback>();
        }
        var serialized = new SerializedObject(feedback);
        serialized.FindProperty("router").objectReferenceValue = camera.GetComponent<ClickRouter>();
        serialized.FindProperty("controller").objectReferenceValue = controller;
        serialized.FindProperty("counterSlots").objectReferenceValue =
            Object.FindFirstObjectByType<CounterSlots>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Builds the cashier's speech balloon: world-space canvas above her head,
    /// sliced bubble, auto-height text. TMP presence is guaranteed — Apply()
    /// gates on it before any wiring runs. Idempotent — rebuilt from scratch
    /// on every run so tweaks land everywhere.
    /// </summary>
    private static void WireCashierSpeech(Camera camera)
    {
        var cashier = Object.FindFirstObjectByType<Cashier>();
        if (cashier == null)
        {
            Debug.LogError("SceneWiring: no Cashier in scene — run CashierSetup first");
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

        // Hidden in the serialized scene too, not only after runtime Awake —
        // otherwise an empty white strip floats over her head in edit mode.
        balloonObject.AddComponent<CanvasGroup>().alpha = 0f;

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
        text.font = EditorAssets.TmpFont();
        text.fontSize = 34f;
        text.color = new Color(0.09f, 0.09f, 0.11f);
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.textWrappingMode = TMPro.TextWrappingModes.Normal;
        text.text = "";

        var balloon = balloonObject.AddComponent<SpeechBalloon>();
        var serializedBalloon = new SerializedObject(balloon);
        serializedBalloon.FindProperty("messageText").objectReferenceValue = text;
        // Wired, not discovered: Camera.main at runtime is a tag lookup that
        // breaks silently if the tag ever changes — the balloon keeps it only
        // as an Awake fallback.
        serializedBalloon.FindProperty("viewCamera").objectReferenceValue = camera;
        serializedBalloon.ApplyModifiedPropertiesWithoutUndo();

        var serializedCashier = new SerializedObject(cashier);
        serializedCashier.FindProperty("balloon").objectReferenceValue = balloon;
        serializedCashier.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireRegister(ShopController controller)
    {
        var registerObject = GameObject.Find("CashRegister");
        if (registerObject == null)
        {
            Debug.LogError("SceneWiring: CashRegister missing — run PsyCurio > Rebuild Greybox Scene first");
            return;
        }

        var register = registerObject.GetComponent<CashRegister>();
        if (register == null)
        {
            register = registerObject.AddComponent<CashRegister>();
        }
        // Recreated, not kept: an existing component carries the serialized
        // defaults of whatever code version added it.
        var staleHighlight = registerObject.GetComponent<HoverHighlight>();
        if (staleHighlight != null)
        {
            Object.DestroyImmediate(staleHighlight);
        }
        registerObject.AddComponent<HoverHighlight>();

        // The register only forwards to the controller; the cashier reference
        // lives on the controller (the sole domain bridge).
        var serialized = new SerializedObject(register);
        serialized.FindProperty("controller").objectReferenceValue = controller;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CounterSlots WireCounterSlots()
    {
        var anchorsRoot = GameObject.Find("Counter/SlotAnchors");
        if (anchorsRoot == null)
        {
            Debug.LogError("SceneWiring: Counter/SlotAnchors not found — run PsyCurio > Rebuild Greybox Scene first");
            return null;
        }
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
        serialized.FindProperty("landingBurstPrefab").objectReferenceValue =
            ItemContentBuilder.BuildLandingBurstPrefab();
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
