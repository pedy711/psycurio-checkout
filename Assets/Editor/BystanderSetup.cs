using System.Linq;
using PsyCurio.Shop.Therapist;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Turns the raw Mixamo character downloads in Assets/Art/Mixamo/Bystanders
/// (Bystander_0..N.fbx) into inert queue-character prefabs: Humanoid rig,
/// URP materials rebuilt from embedded textures (the cashier's proven
/// recipe), a shared idle-only Animator retargeting the existing Idle clip,
/// a random idle-phase offset, and no colliders — bystanders must never take
/// a click. Idempotent.
/// </summary>
public static class BystanderSetup
{
    private const string Folder = "Assets/Art/Mixamo/Bystanders";
    private const string PrefabsFolder = "Assets/Prefabs/Bystanders";
    private const string ControllerPath = Folder + "/BystanderIdle.controller";
    private const string IdleClipPath = "Assets/Art/Mixamo/Idle.fbx";

    [MenuItem("PsyCurio/Setup Bystanders (after Mixamo import)")]
    public static void Apply()
    {
        var characterPaths = AssetDatabase.FindAssets("t:Model", new[] { Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx"))
            .OrderBy(path => path)
            .ToArray();
        if (characterPaths.Length == 0)
        {
            Debug.LogError($"BystanderSetup: no character FBX files in {Folder}");
            return;
        }

        var controller = BuildIdleController();
        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Bystanders");
        }

        foreach (var path in characterPaths)
        {
            ConfigureImport(path);
            BuildPrefab(path, controller);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"BystanderSetup: {characterPaths.Length} bystander prefabs ready");
    }

    private static void ConfigureImport(string path)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.ExtractTextures(Folder + "/Textures");
        importer.SaveAndReimport();

        // URP materials from the embedded textures, remapped on the importer.
        var materialsFolder = Folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            AssetDatabase.CreateFolder(Folder, "Materials");
        }
        var character = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var characterName = System.IO.Path.GetFileNameWithoutExtension(path);
        foreach (var renderer in character.GetComponentsInChildren<Renderer>())
        {
            foreach (var sourceMaterial in renderer.sharedMaterials)
            {
                if (sourceMaterial == null)
                {
                    continue;
                }
                var materialPath = $"{materialsFolder}/{characterName}_{sourceMaterial.name}.mat";
                var urpMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (urpMaterial == null)
                {
                    urpMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(urpMaterial, materialPath);
                }
                urpMaterial.SetTexture("_BaseMap", sourceMaterial.mainTexture);

                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterial.name),
                    urpMaterial);
            }
        }
        importer.SaveAndReimport();
    }

    private static AnimatorController BuildIdleController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle");
        idle.motion = AssetDatabase.LoadAllAssetsAtPath(IdleClipPath)
            .OfType<AnimationClip>()
            .First(clip => !clip.name.Contains("__preview__"));
        machine.defaultState = idle;
        return controller;
    }

    private static void BuildPrefab(string characterPath, AnimatorController controller)
    {
        var characterName = System.IO.Path.GetFileNameWithoutExtension(characterPath);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = characterName;

        var animator = instance.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        instance.AddComponent<IdleOffset>();

        foreach (var collider in instance.GetComponentsInChildren<Collider>())
        {
            Object.DestroyImmediate(collider);
        }

        var prefabPath = $"{PrefabsFolder}/{characterName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }
}
