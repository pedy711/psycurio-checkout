using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the greybox shop scene programmatically so that layout, naming and
/// camera framing are reviewable code instead of opaque hand-edits, and the
/// scene can be regenerated with one click (PsyCurio > Rebuild Greybox Scene).
///
/// This is the step-4 bootstrap: later steps edit Shop.unity directly, so
/// rebuilding after that discards their changes — the menu item warns for that
/// reason. Every world position lives in the constants below; nothing else in
/// the project hard-codes scene coordinates (interaction code goes through the
/// named anchors this builder creates: Counter/SlotAnchors/Slot_0..4,
/// Shelf/ItemSpot_0..5, CashierAnchor, QueueAnchors/Queue_0..2).
/// </summary>
public static class GreyboxSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Shop.unity";
    private const string MaterialsFolder = "Assets/Materials";

    // Camera pose is the patient's fixed point of view; verified by rendering
    // CaptureFraming() and inspecting the image, not by eyeballing the editor.
    private static readonly Vector3 CameraPosition = new Vector3(0.15f, 1.45f, -2.7f);
    private static readonly Vector3 CameraLookTarget = new Vector3(0.15f, 0.85f, 0.8f);
    private const float CameraFieldOfView = 55f;

    // Aim the sun from high behind the camera into the scene so every face the
    // fixed view shows gets direct light (yaw is the light's forward direction,
    // not its origin). Flat ambient keeps shadowed faces readable without a
    // lighting bake, which batch-built scenes don't have.
    private static readonly Vector3 SunEulerAngles = new Vector3(35f, 15f, 0f);
    private const float SunIntensity = 1.15f;
    private static readonly Color AmbientColor = new Color(0.45f, 0.46f, 0.48f);

    // Counter (top surface at CounterHeight) with the register on its right end.
    private static readonly Vector3 CounterCenter = new Vector3(0.55f, 0f, 0.45f);
    private const float CounterWidth = 1.9f;
    private const float CounterDepth = 0.65f;
    private const float CounterHeight = 0.95f;

    // Five placement slots on the countertop, left of the register.
    private const int SlotCount = 5;
    private const float SlotSpacing = 0.25f;
    private static readonly Vector3 FirstSlotLocal = new Vector3(-0.65f, 0f, 0.42f);

    // Shelf to the camera's left, face-on so items read clearly from the fixed view.
    private static readonly Vector3 ShelfCenter = new Vector3(-1.55f, 0f, 1.1f);
    private const float ShelfWidth = 1.7f;
    private const float ShelfDepth = 0.32f;
    private static readonly float[] ShelfBoardHeights = { 0.75f, 1.25f };
    private const int SpotsPerBoard = 3;

    private static readonly Vector3 RegisterLocal = new Vector3(0.7f, 0f, 0.05f);
    private static readonly Vector3 CashierStanding = new Vector3(0.55f, 0f, 1.05f);

    // Trails back-right toward the wall: the frustum widens with distance, so
    // all three stay inside the fixed view (the first placement receded toward
    // the camera and walked the second and third right out of the frame).
    // Clear of the counter's right end (x = 1.5) and spaced so the figures
    // separate on screen instead of stacking along the view ray.
    // Chosen from an inverse-projection grid of the floor actually visible
    // right of the counter (the wedge is far smaller than frustum intuition
    // suggests): distinct screen columns, >0.5 m body separation, clear of
    // the counter's right end.
    // All three sit in the far depth band (z 1.3–1.8): only there do figures
    // render person-scale from the fixed camera — nearer floor makes them
    // loom huge, and the visible wedge allows nothing further left.
    private static readonly Vector3[] QueuePositions =
    {
        new Vector3(1.76f, 0f, 1.76f),
        new Vector3(2.14f, 0f, 1.34f),
        new Vector3(2.63f, 0f, 1.76f)
    };

    [MenuItem("PsyCurio/Rebuild Greybox Scene")]
    public static void RebuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Rebuild greybox scene?",
                "This regenerates Shop.unity from scratch and discards any changes made to the scene after the greybox step.",
                "Rebuild", "Cancel"))
        {
            return;
        }
        BuildShopScene();
    }

    public static void BuildShopScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        BuildEnvironment();
        BuildShelf();
        BuildCounterWithSlots();
        BuildRegister();
        CreateEmpty("CashierAnchor", CashierStanding, faceCamera: true);
        BuildQueueAnchors();
        ConfigureCamera();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("GreyboxSceneBuilder: Shop.unity built and set as the only build scene");
    }

    /// <summary>
    /// Renders the fixed camera to a PNG (path from SHOP_SCREENSHOT_PATH, else
    /// Library/framing.png) so framing can be verified from the command line.
    /// </summary>
    public static void CaptureFraming()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("GreyboxSceneBuilder: no Main Camera in scene");
            return;
        }

        const int width = 1600;
        const int height = 900;
        var renderTexture = new RenderTexture(width, height, 24);

        // Camera.Render() is a legacy path URP does not fully support (it can
        // render with stale per-camera light data, silently dropping the main
        // light); SubmitRenderRequest is the supported offscreen render API.
        var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
        if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera, request))
        {
            request.destination = renderTexture;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
        }
        else
        {
            camera.targetTexture = renderTexture;
            camera.Render();
        }

        RenderTexture.active = renderTexture;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = null;
        camera.targetTexture = null;

        var path = System.Environment.GetEnvironmentVariable("SHOP_SCREENSHOT_PATH");
        if (string.IsNullOrEmpty(path))
        {
            path = "Library/framing.png";
        }
        File.WriteAllBytes(path, image.EncodeToPNG());
        Debug.Log($"GreyboxSceneBuilder: framing screenshot written to {path}");
    }

    private static void BuildEnvironment()
    {
        // The room is ~4 m deep; the URP assets' default ~50 m shadow distance
        // spreads the shadow map so thin that everything self-shadows into
        // darkness (verified by A/B renders). 12 m is generous for this scene
        // and the right call for mobile regardless.
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                AssetDatabase.GUIDToAssetPath(guid));
            asset.shadowDistance = 12f;
            EditorUtility.SetDirty(asset);
        }

        var floor = Primitive("Floor", PrimitiveType.Cube, null,
            new Vector3(0.5f, -0.05f, 0.6f), new Vector3(13f, 0.1f, 9f), "Floor", new Color(0.72f, 0.72f, 0.71f));
        floor.isStatic = true;

        var backWall = Primitive("BackWall", PrimitiveType.Cube, null,
            new Vector3(0.5f, 1.8f, 2.45f), new Vector3(13f, 3.6f, 0.1f), "Wall", new Color(0.85f, 0.84f, 0.82f));
        backWall.isStatic = true;

        // Neither can shadow anything the camera sees, and as casters their
        // size wrecks shadow-map precision for everything else.
        floor.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backWall.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(SunEulerAngles);
            sun.GetComponent<Light>().intensity = SunIntensity;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;
    }

    private static void BuildShelf()
    {
        var shelf = CreateEmpty("Shelf", ShelfCenter, faceCamera: false);
        var shelfColor = new Color(0.49f, 0.5f, 0.51f);

        foreach (var side in new[] { -1f, 1f })
        {
            Primitive("SidePanel", PrimitiveType.Cube, shelf.transform,
                new Vector3(side * (ShelfWidth / 2f + 0.02f), 0.8f, 0f),
                new Vector3(0.04f, 1.6f, ShelfDepth), "Shelf", shelfColor);
        }
        Primitive("BackPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(0f, 0.8f, ShelfDepth / 2f - 0.01f),
            new Vector3(ShelfWidth + 0.08f, 1.6f, 0.02f), "Shelf", shelfColor);

        var spotIndex = 0;
        foreach (var boardHeight in ShelfBoardHeights)
        {
            Primitive("Board", PrimitiveType.Cube, shelf.transform,
                new Vector3(0f, boardHeight, 0f),
                new Vector3(ShelfWidth, 0.04f, ShelfDepth), "Shelf", shelfColor);

            for (var i = 0; i < SpotsPerBoard; i++)
            {
                var x = (i - (SpotsPerBoard - 1) / 2f) * (ShelfWidth / SpotsPerBoard);
                var spot = new GameObject($"ItemSpot_{spotIndex}");
                spot.transform.SetParent(shelf.transform, false);
                spot.transform.localPosition = new Vector3(x, boardHeight + 0.02f, -0.04f);
                spotIndex++;
            }
        }
    }

    private static void BuildCounterWithSlots()
    {
        var counter = CreateEmpty("Counter", CounterCenter, faceCamera: false);
        var counterColor = new Color(0.56f, 0.57f, 0.57f);

        Primitive("Body", PrimitiveType.Cube, counter.transform,
            new Vector3(0f, (CounterHeight - 0.05f) / 2f, 0f),
            new Vector3(CounterWidth - 0.08f, CounterHeight - 0.05f, CounterDepth - 0.06f),
            "Counter", counterColor * 0.92f);
        Primitive("Top", PrimitiveType.Cube, counter.transform,
            new Vector3(0f, CounterHeight - 0.025f, 0f),
            new Vector3(CounterWidth, 0.05f, CounterDepth), "Counter", counterColor);

        var anchors = CreateEmpty("SlotAnchors", Vector3.zero, faceCamera: false);
        anchors.transform.SetParent(counter.transform, false);

        for (var i = 0; i < SlotCount; i++)
        {
            var slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(anchors.transform, false);
            slot.transform.localPosition = FirstSlotLocal
                + new Vector3(i * SlotSpacing, CounterHeight, -0.28f);

            // Subtle visible marker: shows capacity at a glance and gives the
            // limit-refusal pulse (step 10) something to animate.
            Primitive("Marker", PrimitiveType.Cube, slot.transform,
                new Vector3(0f, 0.003f, 0f), new Vector3(0.18f, 0.006f, 0.14f),
                "SlotMarker", new Color(0.42f, 0.43f, 0.44f));
        }
    }

    private static void BuildRegister()
    {
        var register = CreateEmpty("CashRegister", Vector3.zero, faceCamera: false);
        register.transform.SetParent(GameObject.Find("Counter").transform, false);
        register.transform.localPosition = RegisterLocal + new Vector3(0f, CounterHeight, 0f);

        var bodyColor = new Color(0.29f, 0.3f, 0.31f);
        Primitive("Body", PrimitiveType.Cube, register.transform,
            new Vector3(0f, 0.09f, 0f), new Vector3(0.34f, 0.18f, 0.32f), "Register", bodyColor);
        var screen = Primitive("Screen", PrimitiveType.Cube, register.transform,
            new Vector3(0f, 0.27f, 0.05f), new Vector3(0.3f, 0.18f, 0.02f), "RegisterScreen",
            new Color(0.17f, 0.2f, 0.22f));
        screen.transform.localRotation = Quaternion.Euler(-12f, 180f, 0f);
    }

    private static void BuildQueueAnchors()
    {
        var root = CreateEmpty("QueueAnchors", Vector3.zero, faceCamera: false);
        for (var i = 0; i < QueuePositions.Length; i++)
        {
            var anchor = new GameObject($"Queue_{i}");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.position = QueuePositions[i];
            anchor.transform.LookAt(new Vector3(CounterCenter.x, 0f, CounterCenter.z));
        }
    }

    private static void ConfigureCamera()
    {
        var camera = Camera.main;
        camera.transform.position = CameraPosition;
        camera.transform.LookAt(CameraLookTarget);
        camera.fieldOfView = CameraFieldOfView;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
    }

    private static GameObject CreateEmpty(string name, Vector3 position, bool faceCamera)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.position = position;
        if (faceCamera)
        {
            gameObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        return gameObject;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPosition, Vector3 localScale, string materialName, Color color)
    {
        var gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        gameObject.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(materialName, color);
        return gameObject;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        var path = $"{MaterialsFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}
