using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Recreates the exact 32x32 cuts without editing PNG .meta files by hand.
public static class NpcSpriteSetup
{
    public static string SliceAll()
    {
        string[] files =
        {
            "Assets/Graphicks/Guard/Walk.png",
            "Assets/Graphicks/Guard/Attack.png",
            "Assets/Graphicks/Doctor/Walk.png",
            "Assets/Graphicks/Doctor/Idle.png"
        };

        var report = new List<string>();
        foreach (string file in files)
            report.Add(Slice(file));
        AssetDatabase.SaveAssets();
        return string.Join("\n", report);
    }

    private static string Slice(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture missing: " + path);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        CheckCapability(factory.GetSpriteEditorDataProviderFromObject(importer), path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        CheckCapability(provider, path);
        provider.InitSpriteEditorDataProvider();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || texture.width % 32 != 0 || texture.height % 32 != 0)
            throw new InvalidOperationException("Sheet is not aligned to 32 pixels: " + path);

        int columns = texture.width / 32;
        int rows = texture.height / 32;
        bool idle = path.EndsWith("/Idle.png", StringComparison.Ordinal);
        if (rows != (idle ? 1 : 5))
            throw new InvalidOperationException("Unexpected direction count: " + path);

        string[] directions = idle
            ? new[] { "Up" }
            : new[] { "Down", "DownSide", "Side", "UpSide", "Up" };

        var previous = new Dictionary<string, GUID>();
        foreach (var rect in provider.GetSpriteRects())
            previous[rect.name] = rect.spriteID;

        var cuts = new List<SpriteRect>();
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            string name = directions[row] + " " + (column + 1);
            cuts.Add(new SpriteRect
            {
                name = name,
                rect = new Rect(column * 32, texture.height - (row + 1) * 32, 32, 32),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = previous.TryGetValue(name, out var id) ? id : GUID.Generate()
            });
        }

        provider.SetSpriteRects(cuts.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var cut in cuts)
                pairs.Add(new SpriteNameFileIdPair(cut.name, cut.spriteID));
            names.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
        importer.SaveAndReimport();
        return path + ": " + cuts.Count + " sprites";
    }

    private static void CheckCapability(ISpriteEditorDataProvider provider, string path)
    {
        if (provider == null)
            throw new InvalidOperationException("Sprite Editor unavailable: " + path);
        provider.InitSpriteEditorDataProvider();
        var frameEdit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (frameEdit == null || !frameEdit.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
            throw new InvalidOperationException("Slicing unsupported by importer: " + path);
    }
}


