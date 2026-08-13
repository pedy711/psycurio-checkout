using PsyCurio.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Generates the six grocery items — modeled from primitive parts and one
/// custom mesh, skinned with generated textures — plus their
/// ShopItemDefinition assets, and stocks the shelf. Item prefabs follow a
/// bottom-pivot convention: y = 0 is the resting base, so placement code
/// simply parents them to an anchor at local zero. Committed and idempotent;
/// re-runnable via PsyCurio > Rebuild Item Content.
/// </summary>
public static class ItemContentBuilder
{
    private const string ItemsFolder = "Assets/Items";
    private const string PrefabsFolder = "Assets/Prefabs/Items";
    private const string MaterialsFolder = "Assets/Materials/Items";
    private const string GeneratedFolder = "Assets/Art/Generated";
    private const string ScenePath = "Assets/Scenes/Shop.unity";

    private readonly struct ItemSpec
    {
        public readonly string Name;
        public readonly string DisplayName;
        public readonly float PriceEuros;

        public ItemSpec(string name, string displayName, float priceEuros)
        {
            Name = name;
            DisplayName = displayName;
            PriceEuros = priceEuros;
        }
    }

    // Shelf order: index 0..2 lower board, 3..5 upper board.
    private static readonly ItemSpec[] Items =
    {
        new ItemSpec("coffee", "Coffee", 4.99f),
        new ItemSpec("milk", "Milk", 1.09f),
        new ItemSpec("cheese", "Cheese", 3.79f),
        new ItemSpec("bread", "Bread", 2.49f),
        new ItemSpec("apples", "Apples", 1.99f),
        new ItemSpec("chocolate", "Chocolate", 1.49f)
    };

    [MenuItem("PsyCurio/Rebuild Item Content")]
    public static void BuildAll()
    {
        EnsureFolders();

        foreach (var spec in Items)
        {
            var prefab = BuildPrefab(spec);
            BuildDefinition(spec, prefab);
        }
        PlaceShelfDisplays();
        BuildBystanderPrefab();
        BuildLandingBurstPrefab();

        AssetDatabase.SaveAssets();
        Debug.Log("ItemContentBuilder: six items built and placed on the shelf");
    }

    private static GameObject BuildPrefab(ItemSpec spec)
    {
        var root = new GameObject(spec.Name);
        BuildVisual(spec.Name, root.transform);
        // Real-world grocery dimensions read too small from the fixed camera
        // 4 m away; scaling the root keeps the bottom pivot.
        root.transform.localScale = Vector3.one * 1.35f;
        root.AddComponent<PsyCurio.Shop.Interaction.HoverHighlight>();

        // Strip part colliders; one padded box on the root is the tap target.
        foreach (var collider in root.GetComponentsInChildren<Collider>())
        {
            Object.DestroyImmediate(collider);
        }
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }
        // Tap targets extend ~35% beyond the visual bounds with a per-axis
        // floor (device-validated — do not shrink). Renderer bounds are
        // world-space, BoxCollider fields local: divide by the root scale.
        const float tapPadScale = 1.35f;
        const float minTapExtent = 0.135f;
        var rootScale = root.transform.localScale.x;
        var tapBox = root.AddComponent<BoxCollider>();
        tapBox.center = bounds.center / rootScale;
        tapBox.size = new Vector3(
            Mathf.Max(bounds.size.x * tapPadScale, minTapExtent),
            Mathf.Max(bounds.size.y * tapPadScale, minTapExtent),
            Mathf.Max(bounds.size.z * tapPadScale, minTapExtent)) / rootScale;

        var path = $"{PrefabsFolder}/{spec.Name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildVisual(string name, Transform parent)
    {
        switch (name)
        {
            case "coffee": BuildCoffee(parent); break;
            case "milk": BuildMilk(parent); break;
            case "cheese": BuildCheese(parent); break;
            case "bread": BuildBread(parent); break;
            case "apples": BuildApple(parent); break;
            case "chocolate": BuildChocolate(parent); break;
            default: throw new System.ArgumentException($"No visual builder for '{name}'");
        }
    }

    private static void BuildCoffee(Transform parent)
    {
        var glass = ItemMaterial("CoffeeGlass", new Color(0.23f, 0.15f, 0.1f), 0.65f);
        Part(parent, PrimitiveType.Cylinder, "Body",
            new Vector3(0f, 0.055f, 0f), new Vector3(0.085f, 0.055f, 0.085f), glass);

        var labelTexture = ProceduralTextures.Label("coffee-label",
            new Color(0.9f, 0.84f, 0.72f), new Color(0.45f, 0.27f, 0.15f), 5);
        Part(parent, PrimitiveType.Cylinder, "Label",
            new Vector3(0f, 0.052f, 0f), new Vector3(0.088f, 0.024f, 0.088f),
            ItemMaterial("CoffeeLabel", Color.white, 0.3f, labelTexture));

        Part(parent, PrimitiveType.Cylinder, "Lid",
            new Vector3(0f, 0.121f, 0f), new Vector3(0.086f, 0.012f, 0.086f),
            ItemMaterial("CoffeeLid", new Color(0.12f, 0.1f, 0.09f), 0.5f));
    }

    private static void BuildMilk(Transform parent)
    {
        var cartonTexture = ProceduralTextures.Label("milk-carton",
            new Color(0.93f, 0.95f, 0.97f), new Color(0.25f, 0.45f, 0.8f), 9);
        var carton = ItemMaterial("MilkCarton", Color.white, 0.15f, cartonTexture);
        Part(parent, PrimitiveType.Cube, "Body",
            new Vector3(0f, 0.065f, 0f), new Vector3(0.07f, 0.13f, 0.07f), carton);

        // Gable top: a 45°-rotated cube sunk halfway into the body top.
        var gable = Part(parent, PrimitiveType.Cube, "Gable",
            new Vector3(0f, 0.148f, 0f), new Vector3(0.05f, 0.05f, 0.068f),
            ItemMaterial("MilkGable", new Color(0.93f, 0.95f, 0.97f), 0.15f));
        gable.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void BuildCheese(Transform parent)
    {
        var wedge = new GameObject("Wedge");
        wedge.transform.SetParent(parent, false);
        wedge.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
        wedge.AddComponent<MeshFilter>().sharedMesh = WedgeMesh();
        wedge.AddComponent<MeshRenderer>().sharedMaterial =
            ItemMaterial("CheeseWedge", new Color(0.9f, 0.76f, 0.32f), 0.25f);
    }

    private static void BuildBread(Transform parent)
    {
        var loaf = Part(parent, PrimitiveType.Sphere, "Loaf",
            new Vector3(0f, 0.045f, 0f), new Vector3(0.19f, 0.1f, 0.11f),
            ItemMaterial("BreadCrust", Color.white, 0.1f, ProceduralTextures.Crust("bread-crust")));
        loaf.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
    }

    private static void BuildApple(Transform parent)
    {
        Part(parent, PrimitiveType.Sphere, "Fruit",
            new Vector3(0f, 0.052f, 0f), new Vector3(0.11f, 0.105f, 0.11f),
            ItemMaterial("AppleSkin", Color.white, 0.4f, ProceduralTextures.AppleSkin("apple-skin")));

        var stem = Part(parent, PrimitiveType.Cylinder, "Stem",
            new Vector3(0f, 0.112f, 0f), new Vector3(0.008f, 0.012f, 0.008f),
            ItemMaterial("AppleStem", new Color(0.35f, 0.24f, 0.12f), 0.2f));
        stem.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

        Part(parent, PrimitiveType.Sphere, "Leaf",
            new Vector3(0.014f, 0.115f, 0f), new Vector3(0.032f, 0.008f, 0.016f),
            ItemMaterial("AppleLeaf", new Color(0.3f, 0.55f, 0.22f), 0.3f));
    }

    private static void BuildChocolate(Transform parent)
    {
        var wrapperTexture = ProceduralTextures.Wrapper("chocolate-wrap",
            new Color(0.42f, 0.2f, 0.55f), new Color(0.85f, 0.7f, 0.3f));
        Part(parent, PrimitiveType.Cube, "Bar",
            new Vector3(0f, 0.09f, 0f), new Vector3(0.11f, 0.18f, 0.028f),
            ItemMaterial("ChocolateWrap", Color.white, 0.35f, wrapperTexture));
    }

    /// <summary>Flat-shaded triangular prism — the cheese wedge.</summary>
    private static Mesh WedgeMesh()
    {
        const string path = GeneratedFolder + "/cheese-wedge.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }

        const float width = 0.13f;
        const float depth = 0.1f;
        const float height = 0.07f;
        var a = new Vector3(-width / 2f, 0f, -depth / 2f);
        var b = new Vector3(width / 2f, 0f, -depth / 2f);
        var c = new Vector3(0.02f, 0f, depth / 2f);
        var up = Vector3.up * height;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        void Face(params Vector3[] corners)
        {
            var start = vertices.Count;
            vertices.AddRange(corners);
            for (var i = 2; i < corners.Length; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i - 1);
                triangles.Add(start + i);
            }
        }

        Face(a, b, c);                              // bottom (faces down)
        Face(a + up, c + up, b + up);               // top
        Face(a, a + up, b + up, b);                 // front side
        Face(b, b + up, c + up, c);                 // cut side
        Face(c, c + up, a + up, a);                 // other cut side

        var mesh = new Mesh { name = "cheese-wedge" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static GameObject Part(Transform parent, PrimitiveType type, string name,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static void BuildDefinition(ItemSpec spec, GameObject prefab)
    {
        var path = $"{ItemsFolder}/{spec.Name}.asset";
        var definition = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ShopItemDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        var serialized = new SerializedObject(definition);
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("priceEuros").floatValue = spec.PriceEuros;
        serialized.FindProperty("counterPrefab").objectReferenceValue = prefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
    }

    private static void PlaceShelfDisplays()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        for (var i = 0; i < Items.Length; i++)
        {
            var spot = GameObject.Find($"ItemSpot_{i}");
            if (spot == null)
            {
                Debug.LogError($"ItemContentBuilder: ItemSpot_{i} not found in {ScenePath}");
                continue;
            }

            for (var c = spot.transform.childCount - 1; c >= 0; c--)
            {
                Object.DestroyImmediate(spot.transform.GetChild(c).gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/{Items[i].Name}.prefab");
            var display = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            display.name = $"ShelfDisplay_{Items[i].Name}";
            display.transform.SetParent(spot.transform, false);
            // Bottom pivot: the anchor sits on the board surface.
            display.transform.localPosition = Vector3.zero;
        }

        if (Object.FindFirstObjectByType<ShopController>() != null)
        {
            Debug.LogWarning("ItemContentBuilder: shelf displays were rebuilt without their "
                + "ShelfItem wiring — run PsyCurio > Wire Scene Interactions to restore clickability.");
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    /// <summary>
    /// Greybox mannequin for the therapist panel's queue: capsule body plus
    /// sphere head, deliberately inert — colliders stripped so it can never
    /// take a click or a hover.
    /// </summary>
    public static GameObject BuildBystanderPrefab()
    {
        // Always rebuilt so the asset converges on this code; SaveAsPrefabAsset
        // overwrites in place, keeping the GUID and every scene reference.
        const string path = "Assets/Prefabs/Bystander.prefab";
        EditorAssets.EnsureFolder("Assets/Prefabs");

        var material = ItemMaterial("Bystander", new Color(0.47f, 0.51f, 0.58f), 0.2f);

        var root = new GameObject("Bystander");
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
        body.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
        head.transform.localScale = Vector3.one * 0.3f;
        head.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(head.GetComponent<Collider>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>
    /// Soft dust puff played where a flying item lands: a dozen small, short
    /// particles, self-destroying. Configured entirely in code.
    /// </summary>
    public static GameObject BuildLandingBurstPrefab()
    {
        // Always rebuilt, same reasoning as BuildBystanderPrefab.
        const string path = "Assets/Prefabs/LandingBurst.prefab";
        EditorAssets.EnsureFolder("Assets/Prefabs");

        var root = new GameObject("LandingBurst");
        var particles = root.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 0.35f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = new Color(0.92f, 0.9f, 0.85f, 0.85f);
        main.gravityModifier = 0.35f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.03f;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = root.GetComponent<ParticleSystemRenderer>();
        // Not the built-in Default-ParticleSystem.mat — its shader belongs to
        // the built-in pipeline and renders magenta under URP.
        renderer.sharedMaterial = EditorAssets.GetOrCreateMaterial(
            "Assets/Materials/LandingBurst.mat",
            "Universal Render Pipeline/Particles/Unlit",
            material =>
            {
                material.SetTexture("_BaseMap",
                    AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Surface", 1f); // transparent
                material.SetFloat("_Blend", 0f);   // alpha blend
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            });

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Material ItemMaterial(string name, Color color, float smoothness,
        Texture2D texture = null)
    {
        return EditorAssets.GetOrCreateUrpLit($"{MaterialsFolder}/{name}.mat", material =>
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
            }
        });
    }

    private static void EnsureFolders()
    {
        EditorAssets.EnsureFolder(ItemsFolder);
        EditorAssets.EnsureFolder(PrefabsFolder);
        EditorAssets.EnsureFolder(MaterialsFolder);
    }
}
