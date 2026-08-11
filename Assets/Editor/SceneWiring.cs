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

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("SceneWiring: router, controller, slots and shelf items wired");
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
