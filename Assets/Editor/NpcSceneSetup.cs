using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds editable clips and places the three NPCs in the existing laboratory scene.
public static class NpcSceneSetup
{
    private static readonly string[] Directions =
        { "Down", "DownSide", "Side", "UpSide", "Up" };

    public static string Build()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Leave Play mode before building NPCs.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Laboratory.unity")
            throw new InvalidOperationException("Open Laboratory.unity before building NPCs.");

        GameObject map = GameObject.Find("Refined Laboratory");
        GameObject player = GameObject.Find("Player");
        if (map == null || player == null)
            throw new InvalidOperationException("Laboratory map or player is missing.");

        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets/Animations", "Guard");
        EnsureFolder("Assets/Animations", "Doctor");

        var guardWalk = LoadSprites("Assets/Graphicks/Guard/Walk.png", 20);
        var guardAttack = LoadSprites("Assets/Graphicks/Guard/Attack.png", 35);
        var doctorWalk = LoadSprites("Assets/Graphicks/Doctor/Walk.png", 20);
        var doctorIdle = LoadSprites("Assets/Graphicks/Doctor/Idle.png", 4);

        AnimatorController guardController = MakeController(
            "Guard", guardWalk, guardAttack, null);
        AnimatorController doctorController = MakeController(
            "Doctor", doctorWalk, null, doctorIdle);

        Transform guardRoute = EnsureChild(map.transform, "Guard Route");
        Transform[] guardPoints = MakePoints(guardRoute, new[]
        {
            new Vector2(5f, 10f), new Vector2(5f, 30f),
            new Vector2(64.5f, 30f), new Vector2(64.5f, 10f)
        });

        Transform doctorRoute = EnsureChild(map.transform, "Doctor Route");
        Transform[] doctorPoints = MakePoints(doctorRoute, new[]
        {
            new Vector2(34.5f, 18f), new Vector2(36.5f, 18f),
            new Vector2(36.5f, 21f), new Vector2(34.5f, 21f)
        });

        MakeNpc(map.transform, player, "Guard One", guardController,
            guardWalk["Down 1"], guardPoints, 0, 2f, 0.2f, false, true);
        MakeNpc(map.transform, player, "Guard Two", guardController,
            guardWalk["Down 1"], guardPoints, 2, 2f, 0.2f, false, true);
        MakeNpc(map.transform, player, "Doctor", doctorController,
            doctorIdle["Up 1"], doctorPoints, 0, 1f, 0.7f, true, false);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "25 animation clips, 2 guards, 1 doctor, 2 patrol routes saved";
    }

    private static Dictionary<string, Sprite> LoadSprites(string path, int count)
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
            {
                if (sprite.rect.width != 32 || sprite.rect.height != 32 ||
                    sprite.pixelsPerUnit != 16)
                    throw new InvalidOperationException("Invalid 32x32 cut: " + sprite.name);
                sprites.Add(sprite.name, sprite);
            }
        }
        if (sprites.Count != count)
            throw new InvalidOperationException("Wrong sprite count in " + path);
        return sprites;
    }

    private static AnimatorController MakeController(
        string actor, Dictionary<string, Sprite> walk,
        Dictionary<string, Sprite> attack, Dictionary<string, Sprite> idle)
    {
        string folder = "Assets/Animations/" + actor;
        string path = folder + "/" + actor + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        foreach (string direction in Directions)
        {
            Sprite[] walkFrames = Frames(walk, direction, 4);
            Sprite[] idleFrames = idle != null && direction == "Up"
                ? Frames(idle, "Up", 4)
                : new[] { walkFrames[0] };

            var idleState = State(machine, "Idle " + direction);
            idleState.motion = MakeClip(folder, "Idle " + direction,
                idleFrames, 5f, true);
            State(machine, "Walk " + direction).motion = MakeClip(
                folder, "Walk " + direction, walkFrames, 8f, true);

            if (attack != null)
                State(machine, "Attack " + direction).motion = MakeClip(
                    folder, "Attack " + direction,
                    Frames(attack, direction, 7), 10f, false);

            if (direction == (actor == "Doctor" ? "Up" : "Down"))
                machine.defaultState = idleState;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Sprite[] Frames(Dictionary<string, Sprite> sprites,
        string direction, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = sprites[direction + " " + (i + 1)];
        return frames;
    }

    private static AnimationClip MakeClip(string folder, string name,
        Sprite[] frames, float fps, bool loop)
    {
        string path = folder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = fps;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe
                { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe
            { time = frames.Length / fps,
              value = loop ? frames[0] : frames[frames.Length - 1] };

        var binding = new EditorCurveBinding
            { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var serialized = new SerializedObject(clip);
        var loopProperty = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopProperty == null)
            throw new InvalidOperationException("Animation loop setting unavailable: " + name);
        loopProperty.boolValue = loop;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState child in machine.states)
            if (child.state.name == name) return child.state;
        return machine.AddState(name);
    }

    private static Transform[] MakePoints(Transform route, Vector2[] positions)
    {
        var points = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            points[i] = EnsureChild(route, "Point " + (i + 1));
            points[i].position = new Vector3(positions[i].x, positions[i].y, -3f);
        }
        return points;
    }

    private static void MakeNpc(Transform parent, GameObject player,
        string name, AnimatorController controller, Sprite initialSprite,
        Transform[] points, int firstPoint, float speed, float pause,
        bool faceUp, bool hasAttack)
    {
        Transform npc = EnsureChild(parent, name);
        npc.position = points[firstPoint].position;
        npc.gameObject.layer = player.layer;

        var renderer = GetOrAdd<SpriteRenderer>(npc.gameObject);
        var playerRenderer = player.GetComponent<SpriteRenderer>();
        renderer.sprite = initialSprite;
        renderer.sortingLayerID = playerRenderer.sortingLayerID;
        renderer.sortingOrder = playerRenderer.sortingOrder;

        GetOrAdd<Animator>(npc.gameObject).runtimeAnimatorController = controller;

        var body = GetOrAdd<Rigidbody2D>(npc.gameObject);
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var collider = GetOrAdd<CapsuleCollider2D>(npc.gameObject);
        collider.isTrigger = false;
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = new Vector2(0.7f, 1.1f);
        collider.offset = new Vector2(0f, -0.06f);

        var animation = GetOrAdd<NpcAnimation>(npc.gameObject);
        var animationData = new SerializedObject(animation);
        animationData.FindProperty("initialFacing").enumValueIndex = faceUp ? 4 : 0;
        animationData.FindProperty("hasAttack").boolValue = hasAttack;
        animationData.ApplyModifiedPropertiesWithoutUndo();

        var patrol = GetOrAdd<NpcPatrol>(npc.gameObject);
        var patrolData = new SerializedObject(patrol);
        var pointData = patrolData.FindProperty("points");
        pointData.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
            pointData.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        patrolData.FindProperty("firstPoint").intValue = firstPoint;
        patrolData.FindProperty("speed").floatValue = speed;
        patrolData.FindProperty("waitSeconds").floatValue = pause;
        patrolData.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(npc.gameObject);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        return gameObject.transform;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
