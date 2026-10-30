using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Imports the heroine's original fall frames without changing her walk art.
public static class PlayerFallSetup
{
    private const string Sheet = "Assets/Graphicks/Player/Fall.png";
    private const string Folder = "Assets/Animations/Player/";
    private static readonly string[] SourceRows =
        { "Down", "Left_Down", "Left_Up", "Up", "Right_Up", "Right_Down" };
    // The existing walk controller names its diagonal states in the opposite
    // order, so these labels keep the fall facing consistent with walking.
    private static readonly string[] FacingStates =
        { "Down", "Right_Down", "Right_Up", "Up", "Left_Up", "Left_Down" };

    public static string Build()
    {
        var importer = AssetImporter.GetAtPath(Sheet) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Fall sheet is missing.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
        if (texture == null || texture.width != 384 || texture.height != 384)
            throw new InvalidOperationException("Expected six rows of eight 48x64 frames.");

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new InvalidOperationException("Sprite Editor unavailable.");
        provider.InitSpriteEditorDataProvider();
        var capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (capability == null ||
            !capability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
            throw new InvalidOperationException("Fall sheet does not support slicing.");

        var oldIds = new Dictionary<string, GUID>();
        foreach (var old in provider.GetSpriteRects()) oldIds[old.name] = old.spriteID;
        var cuts = new List<SpriteRect>();
        for (int row = 0; row < SourceRows.Length; row++)
        for (int frame = 0; frame < 8; frame++)
        {
            string name = SourceRows[row] + " " + (frame + 1);
            cuts.Add(new SpriteRect
            {
                name = name,
                rect = new Rect(frame * 48, 384 - (row + 1) * 64, 48, 64),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = oldIds.TryGetValue(name, out GUID id) ? id : GUID.Generate()
            });
        }
        provider.SetSpriteRects(cuts.ToArray());
        var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var cut in cuts) pairs.Add(new SpriteNameFileIdPair(cut.name, cut.spriteID));
            nameProvider.SetNameFileIdPairs(pairs);
        }
        provider.Apply();
        importer.SaveAndReimport();

        var sprites = new Dictionary<string, Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(Sheet))
            if (asset is Sprite sprite) sprites.Add(sprite.name, sprite);
        if (sprites.Count != 48) throw new InvalidOperationException("Incorrect fall sprite count.");

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            Folder + "Idle_0.controller");
        if (controller == null) throw new InvalidOperationException("Player Animator is missing.");
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        for (int row = 0; row < SourceRows.Length; row++)
        {
            string direction = FacingStates[row];
            string stateName = "Player_Fall_" + direction;
            string path = Folder + "Fall " + direction + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = 8f;
            clip.wrapMode = WrapMode.Once;
            var keys = new ObjectReferenceKeyframe[9];
            // The source's straight-back death row ends upright. The nearby
            // diagonal row has a readable lying pose for that direction.
            string source = direction == "Up" ? "Left_Up" : SourceRows[row];
            for (int i = 0; i < 8; i++)
                keys[i] = new ObjectReferenceKeyframe
                    { time = i / 8f, value = sprites[source + " " + (i + 1)] };
            keys[8] = new ObjectReferenceKeyframe { time = 1f, value = keys[7].value };
            AnimationUtility.SetObjectReferenceCurve(clip,
                new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" },
                keys);
            var serialized = new SerializedObject(clip);
            serialized.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clip);

            AnimatorState state = null;
            foreach (ChildAnimatorState child in machine.states)
                if (child.state.name == stateName) state = child.state;
            if (state == null) state = machine.AddState(stateName);
            state.motion = clip;
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return "48 fall sprites sliced at 48x64; 6 non-looping animations";
    }
}
