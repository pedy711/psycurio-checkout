using PsyCurio.Shop.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Attaches scene-level interaction components (currently: ClickRouter on the
/// fixed camera). Idempotent; separate from the greybox builder so re-wiring
/// never regenerates geometry.
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

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("SceneWiring: ClickRouter present on Main Camera");
    }
}
