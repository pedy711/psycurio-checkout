using System.Linq;
using PsyCurio.Shop;
using PsyCurio.Shop.Interaction;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Turns the three raw Mixamo downloads in Assets/Art/Mixamo (Cashier.fbx,
/// Idle.fbx, Wave.fbx) into the working scene character, entirely in code so
/// none of the error-prone import clicks are manual: Humanoid rig with the
/// avatar shared into both animation files, looped idle with baked root
/// motion, URP materials rebuilt from the embedded textures, an Animator
/// controller (Idle default, Wave on trigger), and the wired scene instance
/// at CashierAnchor. Idempotent — safe to re-run after re-downloading a file.
/// </summary>
public static class CashierSetup
{
    private const string Folder = "Assets/Art/Mixamo";
    private const string CharacterPath = Folder + "/Cashier.fbx";
    private const string IdlePath = Folder + "/Idle.fbx";
    private const string WavePath = Folder + "/Wave.fbx";
    private const string ControllerPath = Folder + "/CashierController.controller";
    private const string ScenePath = "Assets/Scenes/Shop.unity";

    [MenuItem("PsyCurio/Setup Cashier (after Mixamo import)")]
    public static void Apply()
    {
        foreach (var path in new[] { CharacterPath, IdlePath, WavePath })
        {
            if (AssetImporter.GetAtPath(path) == null)
            {
                Debug.LogError($"CashierSetup: missing {path} — download from Mixamo first");
                return;
            }
        }

        ConfigureCharacterImport();
        ConfigureAnimationImport(IdlePath, loop: true);
        ConfigureAnimationImport(WavePath, loop: false);
        BuildMaterials();
        var controller = BuildAnimatorController();
        PlaceInScene(controller);

        AssetDatabase.SaveAssets();
        Debug.Log("CashierSetup: cashier imported, animated and wired");
    }

    private static void ConfigureCharacterImport()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(CharacterPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.ExtractTextures(Folder + "/Textures");
        importer.SaveAndReimport();
    }

    private static void ConfigureAnimationImport(string path, bool loop)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath)
            .OfType<Avatar>().FirstOrDefault();

        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = loop;
            clip.loopPose = loop;
            // Bake root motion into the pose so the character never drifts or
            // spins away from her anchor while idling or waving.
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Mixamo materials import as Standard shader and render pink under URP
    /// (and Unity 6.3 removed the quick Edit-menu converter), so build URP Lit
    /// materials from the extracted textures and remap the importer to them.
    /// </summary>
    private static void BuildMaterials()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(CharacterPath);
        var materialsFolder = Folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            AssetDatabase.CreateFolder(Folder, "Materials");
        }

        var character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        foreach (var renderer in character.GetComponentsInChildren<Renderer>())
        {
            foreach (var sourceMaterial in renderer.sharedMaterials)
            {
                if (sourceMaterial == null)
                {
                    continue;
                }

                var path = $"{materialsFolder}/{sourceMaterial.name}.mat";
                var urpMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (urpMaterial == null)
                {
                    urpMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(urpMaterial, path);
                }
                urpMaterial.SetTexture("_BaseMap", sourceMaterial.mainTexture);

                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterial.name),
                    urpMaterial);
            }
        }
        importer.SaveAndReimport();
    }

    private static AnimatorController BuildAnimatorController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);

        // IK pass on for the therapist panel's eye-contact control later.
        var layers = controller.layers;
        layers[0].iKPass = true;
        controller.layers = layers;

        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle");
        idle.motion = FirstClip(IdlePath);
        machine.defaultState = idle;

        var wave = machine.AddState("Wave");
        wave.motion = FirstClip(WavePath);

        var toWave = idle.AddTransition(wave);
        toWave.AddCondition(AnimatorConditionMode.If, 0f, "Wave");
        toWave.hasExitTime = false;
        toWave.duration = 0.15f;

        var toIdle = wave.AddTransition(idle);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 0.9f;
        toIdle.duration = 0.25f;

        return controller;
    }

    private static void PlaceInScene(AnimatorController controller)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var anchor = GameObject.Find("CashierAnchor");

        for (var i = anchor.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(anchor.transform.GetChild(i).gameObject);
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Cashier";
        instance.transform.SetParent(anchor.transform, false);

        var animator = instance.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var collider = instance.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = instance.AddComponent<CapsuleCollider>();
        }
        collider.center = new Vector3(0f, 0.85f, 0f);
        collider.height = 1.7f;
        collider.radius = 0.3f;

        if (instance.GetComponent<Cashier>() == null)
        {
            instance.AddComponent<Cashier>();
        }
        if (instance.GetComponent<HoverHighlight>() == null)
        {
            instance.AddComponent<HoverHighlight>();
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static AnimationClip FirstClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.Contains("__preview__"));
    }
}
