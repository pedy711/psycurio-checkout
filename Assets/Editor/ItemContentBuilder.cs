using PsyCurio.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Generates the six grocery items — a material, a distinctly shaped and
/// colored greybox prefab, and a ShopItemDefinition asset each — and places
/// display instances on the shelf's ItemSpot anchors in Shop.unity. Committed
/// so the content pipeline is reviewable; re-runnable via the menu item
/// (idempotent: existing assets are updated, shelf displays are replaced).
/// </summary>
public static class ItemContentBuilder
{
    private const string ItemsFolder = "Assets/Items";
    private const string PrefabsFolder = "Assets/Prefabs/Items";
    private const string MaterialsFolder = "Assets/Materials/Items";
    private const string ScenePath = "Assets/Scenes/Shop.unity";

    private readonly struct ItemSpec
    {
        public readonly string Name;
        public readonly string DisplayName;
        public readonly float PriceEuros;
        public readonly Color Color;
        public readonly PrimitiveType Shape;
        public readonly Vector3 Size;

        public ItemSpec(string name, string displayName, float priceEuros,
            Color color, PrimitiveType shape, Vector3 size)
        {
            Name = name;
            DisplayName = displayName;
            PriceEuros = priceEuros;
            Color = color;
            Shape = shape;
            Size = size;
        }
    }

    // Shelf order: index 0..2 top board, 3..5 bottom board. Shapes and hues are
    // deliberately distinct so items are tellable apart at greybox fidelity.
    private static readonly ItemSpec[] Items =
    {
        new ItemSpec("coffee", "Coffee", 4.99f, new Color(0.36f, 0.25f, 0.18f), PrimitiveType.Cube, new Vector3(0.08f, 0.17f, 0.08f)),
        new ItemSpec("milk", "Milk", 1.09f, new Color(0.78f, 0.87f, 0.97f), PrimitiveType.Cube, new Vector3(0.07f, 0.16f, 0.07f)),
        new ItemSpec("cheese", "Cheese", 3.79f, new Color(0.89f, 0.78f, 0.26f), PrimitiveType.Cube, new Vector3(0.13f, 0.06f, 0.1f)),
        new ItemSpec("bread", "Bread", 2.49f, new Color(0.8f, 0.64f, 0.3f), PrimitiveType.Cube, new Vector3(0.19f, 0.09f, 0.09f)),
        new ItemSpec("apples", "Apples", 1.99f, new Color(0.76f, 0.22f, 0.17f), PrimitiveType.Sphere, new Vector3(0.11f, 0.11f, 0.11f)),
        new ItemSpec("chocolate", "Chocolate", 1.49f, new Color(0.5f, 0.25f, 0.6f), PrimitiveType.Cube, new Vector3(0.11f, 0.18f, 0.03f))
    };

    [MenuItem("PsyCurio/Rebuild Item Content")]
    public static void BuildAll()
    {
        EnsureFolders();

        for (var i = 0; i < Items.Length; i++)
        {
            var prefab = BuildPrefab(Items[i]);
            BuildDefinition(Items[i], prefab);
        }
        PlaceShelfDisplays();
        BuildBystanderPrefab();
        BuildLandingBurstPrefab();

        AssetDatabase.SaveAssets();
        Debug.Log("ItemContentBuilder: six items built and placed on the shelf");
    }

    /// <summary>
    /// Soft dust puff played where a flying item lands: a dozen small, short
    /// particles, self-destroying. Configured entirely in code.
    /// </summary>
    public static GameObject BuildLandingBurstPrefab()
    {
        const string path = "Assets/Prefabs/LandingBurst.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

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
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>(
            "Default-ParticleSystem.mat");

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>
    /// Greybox mannequin for the therapist panel's queue: capsule body plus
    /// sphere head, deliberately inert — colliders stripped so it can never
    /// take a click or a hover.
    /// </summary>
    public static GameObject BuildBystanderPrefab()
    {
        const string path = "Assets/Prefabs/Bystander.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

        var material = GetOrCreateMaterial(new ItemSpec(
            "bystander", "Bystander", 0f, new Color(0.47f, 0.51f, 0.58f),
            PrimitiveType.Capsule, Vector3.one));

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

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(ItemsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Items");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
        }
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Items");
        }
    }

    private static GameObject BuildPrefab(ItemSpec spec)
    {
        var material = GetOrCreateMaterial(spec);

        var temp = GameObject.CreatePrimitive(spec.Shape);
        temp.name = spec.Name;
        temp.transform.localScale = spec.Size;
        temp.GetComponent<Renderer>().sharedMaterial = material;
        temp.AddComponent<PsyCurio.Shop.Interaction.HoverHighlight>();

        // Fat-finger padding: replace the tight primitive collider with a box
        // guaranteeing ~10 cm of tappable extent per axis — the 3 cm-deep
        // chocolate bar was a needle to hit on a phone at 4 m camera distance.
        const float minTapExtent = 0.10f;
        Object.DestroyImmediate(temp.GetComponent<Collider>());
        var tapBox = temp.AddComponent<BoxCollider>();
        tapBox.size = new Vector3(
            Mathf.Max(1f, minTapExtent / spec.Size.x),
            Mathf.Max(1f, minTapExtent / spec.Size.y),
            Mathf.Max(1f, minTapExtent / spec.Size.z));

        var path = $"{PrefabsFolder}/{spec.Name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
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
            display.transform.localPosition = new Vector3(0f, Items[i].Size.y / 2f, 0f);
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static Material GetOrCreateMaterial(ItemSpec spec)
    {
        var path = $"{MaterialsFolder}/{spec.Name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", spec.Color);
        EditorUtility.SetDirty(material);
        return material;
    }
}
