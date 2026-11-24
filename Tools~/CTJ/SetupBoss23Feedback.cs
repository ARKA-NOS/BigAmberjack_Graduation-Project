using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.Players;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23Feedback
{
    private const string BossFolder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";
    private const string AnimFolder = "Assets/_Graphics/Animations/Enemy/Enemy23/";
    private const string Prefix = "Boss23_ProductionControlDrone";
    private const string Crouch = "Assets/_Graphics/Assets/Enemy/Enemy23/SHADOW Series - Voidrone Boss/Crouch/";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new Exception("Use Edit Mode");

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimFolder + Prefix + ".controller");
        var charge = Slice("transition", new[] { 42f, 45f, 54f, 67f, 67f });
        var fire = Slice("speedy shot", new[] { 69f, 69f, 69f, 69f });
        MakeClip(controller, "CrouchCharge", charge, 0.07f, new[] { 0f, -2f, -2f, -8f, -8f });
        MakeClip(controller, "CrouchFire", fire, 0.09f, new[] { -8f, -8f, -8f, -8f });

        // 플레이어 프리팹은 읽기만 하며, 적 전용 복사본에서 부드러운 알파 머티리얼을 사용합니다.
        string ghostPath = BossFolder + "Boss23_SmoothAfterImage.prefab";
        var ghost = PrefabUtility.LoadPrefabContents("Assets/_Prefabs/Player/AfterImageGhost.prefab");
        try
        {
            Set(ghost.GetComponent<AfterImageGhost>(), "customSpriteMaterial",
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Enemy/PrototypeSpriteUnlit.mat"));
            ghost.name = "Boss23_SmoothAfterImage";
            PrefabUtility.SaveAsPrefabAsset(ghost, ghostPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(ghost); }

        string path = BossFolder + Prefix + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Set(root.GetComponentInChildren<BossFlightModule>(true), "decelerateFromStart", true);
            var dash = root.GetComponentInChildren<DroneChargeSkill>(true);
            Set(dash, "distance", 6.5f);
            Set(root.GetComponentInChildren<DroneDownwardLaserSkill>(true), "repeatPause", 0.25f);

            var salvo = root.GetComponentInChildren<DroneSalvoSkill>(true);
            Set(salvo, "firingDuration", 1.5f);
            Set(salvo, "retreatSpeed", 4f);
            Transform facing = root.transform.Find("Visual");
            Transform point = facing.Find("GroundFirePoint");
            if (point == null)
            {
                point = new GameObject("GroundFirePoint").transform;
                point.SetParent(facing, false);
            }
            point.localPosition = new Vector3(24f / 28f, -4f / 28f, 0f);
            Set(salvo, "muzzle", point);
            Set(root.GetComponentInChildren<DroneBombSkill>(true), "lastStandInterval", 0.75f);

            var after = root.GetComponentInChildren<EnemyAfterImageModule>(true);
            Set(after, "ghostPrefab", AssetDatabase.LoadAssetAtPath<AfterImageGhost>(ghostPath));
            Set(after, "spawnInterval", 0.018f);
            Set(after, "lifetime", 0.22f);
            Set(after, "ghostColor", new Color(0.65f, 0.85f, 0.7f, 0.28f));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var bomb = PrefabUtility.LoadPrefabContents(BossFolder + "Boss23_Bomb.prefab");
        try
        {
            Set(bomb.GetComponent<EnemyLobProjectile>(), "arcHeight", 4.6f);
            Set(bomb.GetComponent<EnemyLobProjectile>(), "flightDuration", 0.96f);
            PrefabUtility.SaveAsPrefabAsset(bomb, BossFolder + "Boss23_Bomb.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(bomb); }

        var warning = PrefabUtility.LoadPrefabContents(BossFolder + "Boss23_TargetBombWarning.prefab");
        try
        {
            Set(warning.GetComponent<_Scripts.UI.Enemy.DroneBombTargetGraphic>(), "countdownRadius", 1f);
            PrefabUtility.SaveAsPrefabAsset(warning, BossFolder + "Boss23_TargetBombWarning.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(warning); }

        return new { dashDistance = 6.5f, barrageDuration = 1.5f, lastStandBombInterval = 0.75f,
            bombArc = 4.6f, bombFlight = 0.96f, smoothGhost = ghostPath };
    }

    private static Sprite[] Slice(string name, float[] originY)
    {
        string path = Crouch + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = originY.Select((pivotY, i) =>
        {
            string spriteName = name + "_" + i;
            return new SpriteRect { name = spriteName,
                spriteID = ids.TryGetValue(spriteName, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(i * 128 + 32, 9, 92, 68), alignment = SpriteAlignment.Custom,
                pivot = new Vector2(32f / 92f, (87f - pivotY) / 68f) };
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 28f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        return rects.Select(r => sprites[r.name]).ToArray();
    }

    private static void MakeClip(AnimatorController controller, string stateName, Sprite[] frames, float interval, float[] offsets)
    {
        string path = AnimFolder + Prefix + "_" + stateName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { frameRate = 200f };
            AssetDatabase.CreateAsset(clip, path);
        }

        var keys = frames.Select((s, i) => new ObjectReferenceKeyframe { time = i * interval, value = s }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = frames.Length * interval, value = frames.Last() });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());

        Mark("ProcessNumber", new Vector2(-15.5f, 10.5f) / 28f);
        Mark("AlarmLamp", new Vector2(-11.5f, 18.5f) / 28f);
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName) ?? machine.AddState(stateName);
        state.motion = clip;
        state.writeDefaultValues = false;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.SaveAssetIfDirty(controller);

        void Mark(string child, Vector2 anchor)
        {
            foreach (string axis in new[] { "x", "y" })
            {
                var curve = new AnimationCurve(keys.Select((k, i) => new Keyframe(k.time,
                    axis == "x" ? anchor.x : anchor.y + offsets[Mathf.Min(i, offsets.Length - 1)] / 28f)).ToArray());
                for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(child, typeof(Transform), "m_LocalPosition." + axis), curve);
            }
        }
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new Exception("Missing field " + name);
        if (field.propertyType == SerializedPropertyType.ObjectReference) field.objectReferenceValue = value as Object;
        else if (field.propertyType == SerializedPropertyType.Boolean) field.boolValue = (bool)value;
        else if (field.propertyType == SerializedPropertyType.Color) field.colorValue = (Color)value;
        else field.floatValue = Convert.ToSingle(value);
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
