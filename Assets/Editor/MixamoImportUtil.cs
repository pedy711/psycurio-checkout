using UnityEditor;
using UnityEngine;

/// <summary>
/// The one Mixamo-to-URP material recipe — extracted textures into URP Lit
/// .mat assets, remapped on the model importer — shared by CashierSetup and
/// BystanderSetup so a fix lands on every character. Mixamo materials import
/// as Standard and render pink under URP, and Unity 6.3 removed the quick
/// Edit-menu converter.
/// </summary>
public static class MixamoImportUtil
{
    /// <param name="materialNamePrefix">Distinguishes same-named source
    /// materials when several FBXs share one materials folder (the bystanders);
    /// empty for the cashier, whose original asset paths predate the prefix.</param>
    public static void RemapToUrpMaterials(string characterPath, string materialsFolder,
        string materialNamePrefix)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(characterPath);
        var character = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
        foreach (var renderer in character.GetComponentsInChildren<Renderer>())
        {
            foreach (var sourceMaterial in renderer.sharedMaterials)
            {
                if (sourceMaterial == null)
                {
                    continue;
                }
                var materialPath = $"{materialsFolder}/{materialNamePrefix}{sourceMaterial.name}.mat";
                var urpMaterial = EditorAssets.GetOrCreateUrpLit(materialPath, material =>
                    material.SetTexture("_BaseMap", sourceMaterial.mainTexture));
                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterial.name),
                    urpMaterial);
            }
        }
        importer.SaveAndReimport();
    }
}
