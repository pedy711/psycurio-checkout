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
        // Validate every input before any destructive step (BuildIdleController
        // deletes the committed controller the existing prefabs reference).
        if (AssetImporter.GetAtPath(IdleClipPath) == null)
        {
            Debug.LogError($"BystanderSetup: missing {IdleClipPath} — run CashierSetup "
                + "after the Mixamo download first");
            return;
        }

        var controller = BuildIdleController();
        if (controller == null)
        {
            return;
        }
        EditorAssets.EnsureFolder(PrefabsFolder);

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

        // Name-prefixed so same-named materials from different FBXs sharing
        // this folder never overwrite each other.
        var characterName = System.IO.Path.GetFileNameWithoutExtension(path);
        MixamoImportUtil.RemapToUrpMaterials(path, Folder + "/Materials", characterName + "_");
    }

    private static AnimatorController BuildIdleController()
    {
        // The clip is resolved BEFORE the delete: destroying the committed
        // controller and then throwing would leave every bystander prefab
        // T-posed with a missing-controller reference.
        var idleClip = AssetDatabase.LoadAllAssetsAtPath(IdleClipPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.Contains("__preview__"));
        if (idleClip == null)
        {
            Debug.LogError($"BystanderSetup: no animation clip inside {IdleClipPath} — "
                + "is it imported as Humanoid (CashierSetup does this)?");
            return null;
        }

        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle");
        idle.motion = idleClip;
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
