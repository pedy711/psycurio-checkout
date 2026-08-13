using System;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared editor-side asset plumbing for the builders. One rule everything
/// here follows: get-or-create ALWAYS re-applies the configuration, so editing
/// a builder constant and re-running converges the assets on the code — a
/// create-only helper would silently keep stale values forever.
/// </summary>
public static class EditorAssets
{
    /// <summary>
    /// TMP's default font, loaded directly by path: TMP_Settings.defaultFontAsset
    /// NREs before settings load, and a script-created TMP component serializes
    /// font=null (which renders as no text at all).
    /// </summary>
    public const string TmpFontPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    public static TMP_FontAsset TmpFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
    }

    public static bool TmpEssentialsPresent()
    {
        return TmpFont() != null;
    }

    /// <summary>
    /// Creates every missing segment of an Assets/... folder path —
    /// AssetDatabase.CreateFolder does not create parents, and a missing parent
    /// fails with an empty GUID rather than an exception.
    /// </summary>
    public static void EnsureFolder(string folder)
    {
        // "Assets" always validates, ending the recursion; the null/empty
        // check is the backstop for malformed paths.
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }
        var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }

    public static Material GetOrCreateUrpLit(string path, Action<Material> configure)
    {
        return GetOrCreateMaterial(path, "Universal Render Pipeline/Lit", configure);
    }

    public static Material GetOrCreateMaterial(string path, string shaderName,
        Action<Material> configure)
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader.name != shaderName)
        {
            material.shader = Shader.Find(shaderName);
        }
        configure?.Invoke(material);
        EditorUtility.SetDirty(material);
        return material;
    }
}
