using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Dresses the greybox: procedural wood/tile/paint textures on the
/// environment, a properly modeled register, a baseboard, a three-light rig
/// and URP post-processing. Runs after GreyboxSceneBuilder in the pipeline
/// (the greybox stays the structural source of truth; this layer restyles
/// it). Idempotent — safe to re-run.
/// </summary>
public static class EnvironmentArtBuilder
{
    private const string ScenePath = "Assets/Scenes/Shop.unity";
    private const string MaterialsFolder = "Assets/Materials";

    [MenuItem("PsyCurio/Apply Environment Art")]
    public static void ApplyAll()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ApplySurfaces();
        BuildBaseboard();
        BuildRegisterModel();
        BuildLightRig();
        BuildPostProcessing();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("EnvironmentArtBuilder: environment dressed");
    }

    private static void ApplySurfaces()
    {
        var floorTexture = ProceduralTextures.Tiles("floor-tiles", 512, new Color(0.78f, 0.76f, 0.72f), 8, 11);
        var wallTexture = ProceduralTextures.Paint("wall-paint", 512, new Color(0.91f, 0.88f, 0.82f), 5);
        var shelfTexture = ProceduralTextures.Wood("shelf-wood", 512, new Color(0.55f, 0.4f, 0.27f), 6, 23);
        var counterTopTexture = ProceduralTextures.Wood("counter-wood", 512, new Color(0.62f, 0.47f, 0.32f), 8, 31);
        var counterBodyTexture = ProceduralTextures.Paint("counter-paint", 512, new Color(0.42f, 0.48f, 0.47f), 9);

        Assign("Floor", TexturedMaterial("FloorArt", floorTexture, new Vector2(6.5f, 4.5f), 0.1f));
        Assign("BackWall", TexturedMaterial("WallArt", wallTexture, new Vector2(5f, 1.2f), 0.02f));

        // Direct children only — GetComponentsInChildren would also repaint
        // the item displays under the shelf's ItemSpot anchors.
        var shelfMaterial = TexturedMaterial("ShelfArt", shelfTexture, new Vector2(1.6f, 1.6f), 0.15f);
        foreach (Transform child in GameObject.Find("Shelf").transform)
        {
            var childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.sharedMaterial = shelfMaterial;
            }
        }

        Assign("Counter/Top", TexturedMaterial("CounterTopArt", counterTopTexture, new Vector2(1.8f, 0.7f), 0.3f));
        Assign("Counter/Body", TexturedMaterial("CounterBodyArt", counterBodyTexture, new Vector2(1.8f, 1f), 0.08f));
    }

    private static void BuildBaseboard()
    {
        var existing = GameObject.Find("Baseboard");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
        var baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseboard.name = "Baseboard";
        baseboard.transform.position = new Vector3(0.5f, 0.065f, 2.37f);
        baseboard.transform.localScale = new Vector3(13f, 0.13f, 0.05f);
        Object.DestroyImmediate(baseboard.GetComponent<Collider>());
        baseboard.GetComponent<Renderer>().sharedMaterial =
            PlainMaterial("BaseboardArt", new Color(0.32f, 0.3f, 0.28f), 0.2f);
    }

    /// <summary>
    /// Replaces the two greybox boxes with a small model: angled body, tilted
    /// display with a generated screen texture, keypad, paper roll. Children
    /// only — the CashRegister component and its wiring live on the parent.
    /// </summary>
    private static void BuildRegisterModel()
    {
        var register = GameObject.Find("CashRegister");
        for (var i = register.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(register.transform.GetChild(i).gameObject);
        }

        var bodyMaterial = PlainMaterial("RegisterBodyArt", new Color(0.24f, 0.26f, 0.29f), 0.45f);
        var darkMaterial = PlainMaterial("RegisterDarkArt", new Color(0.12f, 0.13f, 0.15f), 0.3f);

        Part("Body", PrimitiveType.Cube, register.transform,
            new Vector3(0f, 0.07f, 0f), Vector3.zero, new Vector3(0.34f, 0.14f, 0.32f),
            bodyMaterial);

        var keypad = Part("Keypad", PrimitiveType.Cube, register.transform,
            new Vector3(0f, 0.15f, -0.06f), new Vector3(12f, 0f, 0f), new Vector3(0.3f, 0.02f, 0.16f),
            darkMaterial);

        // Key caps: a 4x3 grid of small light cubes on the sloped plate.
        var keyMaterial = PlainMaterial("RegisterKeyArt", new Color(0.82f, 0.83f, 0.85f), 0.15f);
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                Part($"Key_{row}{column}", PrimitiveType.Cube, keypad.transform,
                    new Vector3(-0.3f + column * 0.2f, 0.8f, -0.28f + row * 0.28f),
                    Vector3.zero, new Vector3(0.14f, 0.9f, 0.2f), keyMaterial);
            }
        }

        Part("Stand", PrimitiveType.Cube, register.transform,
            new Vector3(0f, 0.2f, 0.1f), Vector3.zero, new Vector3(0.05f, 0.14f, 0.03f),
            darkMaterial);

        var displayTexture = ProceduralTextures.Display("register-display", 128, 96);
        var screenMaterial = EditorAssets.GetOrCreateUrpLit(
            $"{MaterialsFolder}/RegisterScreenArt.mat", material =>
            {
                material.SetTexture("_BaseMap", displayTexture);
                material.SetColor("_BaseColor", Color.white);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.16f, 0.35f, 0.2f));
                material.SetTexture("_EmissionMap", displayTexture);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
        Part("Screen", PrimitiveType.Cube, register.transform,
            new Vector3(0f, 0.31f, 0.11f), new Vector3(-14f, 180f, 0f), new Vector3(0.26f, 0.17f, 0.02f),
            screenMaterial);

        Part("PaperRoll", PrimitiveType.Cylinder, register.transform,
            new Vector3(-0.1f, 0.16f, 0.1f), new Vector3(0f, 0f, 90f), new Vector3(0.05f, 0.06f, 0.05f),
            PlainMaterial("PaperArt", new Color(0.94f, 0.93f, 0.9f), 0.05f));

        // One padded tap box over the whole model instead of per-part
        // colliders: those leave dead gaps inside the register's visual
        // silhouette (a tap between monitor and drawer must still count —
        // found by driven touch testing on device).
        var modelBounds = new Bounds(register.transform.position, Vector3.zero);
        foreach (var partRenderer in register.GetComponentsInChildren<Renderer>())
        {
            modelBounds.Encapsulate(partRenderer.bounds);
        }
        const float tapPadScale = 1.2f;
        const float minTapExtent = 0.135f;
        var tapBox = register.GetComponent<BoxCollider>();
        if (tapBox == null)
        {
            tapBox = register.AddComponent<BoxCollider>();
        }
        tapBox.center = register.transform.InverseTransformPoint(modelBounds.center);
        tapBox.size = new Vector3(
            Mathf.Max(modelBounds.size.x * tapPadScale, minTapExtent),
            Mathf.Max(modelBounds.size.y * tapPadScale, minTapExtent),
            Mathf.Max(modelBounds.size.z * tapPadScale, minTapExtent));
    }

    private static GameObject Part(string name, PrimitiveType type, Transform parent,
        Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material,
        bool keepCollider = false)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        return part;
    }

    private static void BuildLightRig()
    {
        var sun = GameObject.Find("Directional Light").GetComponent<Light>();
        sun.color = new Color(1f, 0.93f, 0.82f);
        sun.intensity = 1.05f;

        var fill = EnsureLight("FillLight");
        fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(40f, 205f, 0f);
        fill.color = new Color(0.75f, 0.82f, 0.95f);
        fill.intensity = 0.3f;
        fill.shadows = LightShadows.None;

        var lampPositions = new[] { new Vector3(0.55f, 2.4f, 0.4f), new Vector3(-1.5f, 2.4f, 1.0f) };
        for (var i = 0; i < lampPositions.Length; i++)
        {
            var lamp = EnsureLight($"ShopLamp_{i}");
            lamp.type = LightType.Point;
            lamp.transform.position = lampPositions[i];
            lamp.color = new Color(1f, 0.9f, 0.75f);
            lamp.intensity = 0.65f;
            lamp.range = 4.5f;
            lamp.shadows = LightShadows.None;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.46f);
    }

    private static void BuildPostProcessing()
    {
        const string profilePath = "Assets/Settings/PostProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);
            AssetDatabase.AddObjectToAsset(tonemapping, profile);

            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.05f);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.18f);
            AssetDatabase.AddObjectToAsset(vignette, profile);

            var colors = profile.Add<ColorAdjustments>();
            colors.postExposure.Override(0.15f);
            colors.saturation.Override(8f);
            AssetDatabase.AddObjectToAsset(colors, profile);
        }

        var volumeObject = Ensure("PostVolume");
        var volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
        {
            volume = volumeObject.AddComponent<Volume>();
        }
        volume.isGlobal = true;
        volume.sharedProfile = profile;

        var cameraData = Camera.main.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
    }

    private static void Assign(string path, Material material)
    {
        GameObject.Find(path).GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material TexturedMaterial(string name, Texture2D texture, Vector2 tiling, float smoothness)
    {
        return EditorAssets.GetOrCreateUrpLit($"{MaterialsFolder}/{name}.mat", material =>
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
        });
    }

    private static Material PlainMaterial(string name, Color color, float smoothness)
    {
        return EditorAssets.GetOrCreateUrpLit($"{MaterialsFolder}/{name}.mat", material =>
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
        });
    }

    private static GameObject Ensure(string name)
    {
        var existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    private static Light EnsureLight(string name)
    {
        var host = Ensure(name);
        var light = host.GetComponent<Light>();
        // Unity's fake-null makes ?? unreliable; only == is overloaded.
        return light != null ? light : host.AddComponent<Light>();
    }
}
