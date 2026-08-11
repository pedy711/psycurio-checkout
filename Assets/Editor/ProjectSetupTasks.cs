using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// One-time scripted project configuration for the task brief, executed headless
/// via -executeMethod so every setting change is reviewable in version control
/// rather than hand-clicked. Safe to re-run.
/// </summary>
public static class ProjectSetupTasks
{
    public static void Configure()
    {
        PlayerSettings.companyName = "Pedram Khoshdani";
        PlayerSettings.productName = "PsyCurio Checkout";
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Android, "com.pedramkhoshdani.psycuriocheckout");

        // Brief: IL2CPP + ARM64 only. The ARM64 flag requires IL2CPP to be set first.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Brief: Active Input Handling = "Both". No public API exists for this
        // setting; 0 = old Input Manager, 1 = Input System package, 2 = Both.
        var projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
        var serialized = new SerializedObject(projectSettings);
        serialized.FindProperty("activeInputHandler").intValue = 2;
        serialized.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        Debug.Log("ProjectSetupTasks.Configure completed");
    }
}
