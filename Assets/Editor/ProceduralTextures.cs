using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates every texture in the project — wood, floor tiles, wall paint,
/// labels — and saves them as PNG assets. "Own textures" in the most literal
/// sense: reproducible from this file, no licensing questions anywhere.
/// </summary>
public static class ProceduralTextures
{
    private const string Folder = "Assets/Art/Generated";

    /// <summary>Vertical planks with grain streaks and dark seams.</summary>
    public static Texture2D Wood(string name, int size, Color baseColor, int planks, int seed)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var random = new System.Random(seed);
        var plankTints = new float[planks];
        for (var p = 0; p < planks; p++)
        {
            plankTints[p] = 0.88f + (float)random.NextDouble() * 0.24f;
        }

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var plank = x * planks / size;
                var withinPlank = (x * planks) % size / (float)size;
                var seam = withinPlank < 0.03f ? 0.55f : 1f;
                // Grain: stretched noise running along the plank.
                var grain = Mathf.PerlinNoise(x * 0.9f + seed * 17f, y * 0.045f + plank * 31f);
                var streak = Mathf.PerlinNoise(x * 0.15f, y * 0.008f + plank * 7f);
                var shade = plankTints[plank] * seam
                            * (0.9f + grain * 0.14f)
                            * (0.94f + streak * 0.1f);
                texture.SetPixel(x, y, new Color(
                    baseColor.r * shade, baseColor.g * shade, baseColor.b * shade));
            }
        }
        return Save(texture, name);
    }

    /// <summary>Square tiles with grout lines and per-tile variation.</summary>
    public static Texture2D Tiles(string name, int size, Color baseColor, int tiles, int seed)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var random = new System.Random(seed);
        var tints = new float[tiles, tiles];
        for (var ty = 0; ty < tiles; ty++)
        {
            for (var tx = 0; tx < tiles; tx++)
            {
                tints[tx, ty] = 0.92f + (float)random.NextDouble() * 0.16f;
            }
        }

        var tileSize = size / (float)tiles;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var tx = Mathf.Min((int)(x / tileSize), tiles - 1);
                var ty = Mathf.Min((int)(y / tileSize), tiles - 1);
                var groutX = (x % tileSize) / tileSize;
                var groutY = (y % tileSize) / tileSize;
                var grout = groutX < 0.025f || groutY < 0.025f ? 0.62f : 1f;
                var speck = Mathf.PerlinNoise(x * 0.25f + seed, y * 0.25f) * 0.07f;
                var shade = tints[tx, ty] * grout * (0.96f + speck);
                texture.SetPixel(x, y, new Color(
                    baseColor.r * shade, baseColor.g * shade, baseColor.b * shade));
            }
        }
        return Save(texture, name);
    }

    /// <summary>Painted surface: soft large-scale mottling plus fine grain.</summary>
    public static Texture2D Paint(string name, int size, Color baseColor, int seed)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var soft = Mathf.PerlinNoise(x * 0.012f + seed, y * 0.012f) * 0.08f;
                var fine = Mathf.PerlinNoise(x * 0.6f + seed * 3f, y * 0.6f) * 0.035f;
                var shade = 0.94f + soft + fine;
                texture.SetPixel(x, y, new Color(
                    baseColor.r * shade, baseColor.g * shade, baseColor.b * shade));
            }
        }
        return Save(texture, name);
    }

    /// <summary>Dark display screen with lighter text-like rows.</summary>
    public static Texture2D Display(string name, int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var background = new Color(0.04f, 0.09f, 0.06f);
        var glow = new Color(0.35f, 0.85f, 0.45f);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, background);
            }
        }
        var random = new System.Random(42);
        for (var row = 0; row < 4; row++)
        {
            var rowY = height - 14 - row * 12;
            var length = width / 3 + random.Next(width / 3);
            for (var x = 8; x < 8 + length; x++)
            {
                // Broken into character-ish clumps.
                if (x % 7 < 5)
                {
                    for (var y = rowY; y < rowY + 5; y++)
                    {
                        texture.SetPixel(x, y, glow);
                    }
                }
            }
        }
        return Save(texture, name);
    }

    private static Texture2D Save(Texture2D texture, string name)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Art", "Generated");
        }
        texture.Apply();
        var path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
