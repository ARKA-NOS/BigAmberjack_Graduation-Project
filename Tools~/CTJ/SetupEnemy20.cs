using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupEnemy20
{
    private const string Name = "Enemy20_CeilingPatrol";
    private const string Art = "Assets/_Graphics/Assets/Enemy/Enemy20";
    private const string Sheets = Art + "/SAHDOW Series - Floating Shade/PNG's/";
    private const string Animations = "Assets/_Graphics/Animations/Enemy/Enemy20";
    private const string Enemies = "Assets/_Prefabs/Enemies";
    private const string Prefabs = Enemies + "/Enemy/Enemy20";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string Skills = Scripts + "/EnemySkills/Enemy20";
    private const string PrefabPath = Prefabs + "/" + Name + ".prefab";
    private const float PixelsPerUnit = 32f;
    private static readonly Color PreparationOutlineColor = new Color(1f, 0.78f, 0.25f, 1f);

    public static object UpdateAttackReadability()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Update the Enemy20 prefab in Edit Mode.");

        // The outside outline shader expands the four vertices of a Full Rect sprite.
        var importer = (TextureImporter)AssetImporter.GetAtPath(Sheets + "attack-fly.png");
        if (UseFullRect(importer)) importer.SaveAndReimport();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureAttackReadability(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath };
    }

    private static void ConfigureAttackReadability(GameObject root)
    {
        var outline = root.GetComponentInChildren<EnemyOutlineModule>();
        Set(outline, "showDuringAttackPreparation", true);
        Set(outline, "preparationColor", PreparationOutlineColor);
        Set(outline, "preparationWidth", 3f);
        if (root.GetComponentInChildren<EnemySuperArmorModule>() != null) return;
        var armor = new GameObject("SuperArmorModule") { layer = root.layer };
        armor.transform.SetParent(root.transform, false);
        armor.AddComponent<EnemySuperArmorModule>();
    }

    private static bool UseFullRect(TextureImporter importer)
    {
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteMeshType == SpriteMeshType.FullRect) return false;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        return true;
    }

    public static object UpdatePreparationShake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Update the Enemy20 animation clips in Edit Mode.");
        string path = Animations + "/" + Name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var clips = controller.animationClips.Distinct().ToArray();
        foreach (var clip in clips)
        {
            SetPreparationOffset(clip, clip.name == Name + "_DashPreparation");
            AssetDatabase.SaveAssetIfDirty(clip);
        }
        return new { clips = clips.Length };
    }

    private static void SetPreparationOffset(AnimationClip clip, bool shake)
    {
        const float duration = 0.60f;
        const int steps = 20;
        var keys = new List<Keyframe> { new Keyframe(0f, 0f) };
        if (shake)
        {
            for (int i = 1; i < steps; i++)
            {
                float pixels = Mathf.Lerp(0.75f, 2f, i / (float)(steps - 1));
                float direction = i % 2 == 0 ? -1f : 1f;
                keys.Add(new Keyframe(i * duration / steps, direction * pixels / PixelsPerUnit));
            }
            keys.Add(new Keyframe(duration, 0f));
        }

        var offset = new AnimationCurve(keys.ToArray());
        for (int i = 0; i < offset.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(offset, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(offset, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), offset);
    }

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
            throw new InvalidOperationException("Create Enemy20 in Edit Mode before its prefab exists.");
        MakeFolder(Animations);
        MakeFolder(Prefabs);
        MakeFolder(Skills);
        var idle = Slice("idle-fly", 7, 35, 84, 5, 60);
        var flight = Slice("side-fly", 4, 35, 84, 7, 56);
        var attack = Slice("attack-fly", 9, 0, 123, 8, 63, fullRect: true);
        var hurt = Slice("hiz-fly", 1, 38, 83, 10, 61);
        var death = Slice("death-fly", 12, 35, 84, 0, 64).Take(7).ToArray();
        var sensor = CreateSensor();
        var controller = CreateController(idle, flight, attack, hurt[0], death, sensor);

        var root = PrefabUtility.LoadPrefabContents(Enemies + "/Enemy/Enemy17/Enemy17_IntakeBiter.prefab");
        try
        {
            Configure(root, controller, idle[0], sensor);
        ConfigureAttackReadability(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath, states = controller.layers[0].stateMachine.states.Length };
    }

    private static Sprite[] Slice(string file, int count, int left, int right, int top, int bottom, bool fullRect = false)
    {
        const int cellWidth = 123;
        const int cellHeight = 72;
        const int centerX = 59;
        const int centerY = 35;
        string path = Sheets + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != cellWidth * count || height != cellHeight)
            throw new InvalidDataException("Unexpected Floating Shade sheet dimensions " + path);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        var rects = Enumerable.Range(0, count).Select(index =>
        {
            string name = file + "_" + index;
            return new SpriteRect
            {
                name = name,
                spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(index * cellWidth + left, cellHeight - bottom, right - left, bottom - top),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((centerX - left) / (float)(right - left), (bottom - centerY) / (float)(bottom - top))
            };
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (fullRect) UseFullRect(importer);
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        return rects.Select(rect => sprites[rect.name]).ToArray();
    }

    private static Sprite CreateSensor()
    {
        string path = Art + "/Enemy20_Sensor.asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;
        var texture = new Texture2D(7, 5, TextureFormat.RGBA32, false)
        {
            name = "SensorPixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        string[] rows = { " ###   ", "##gg## ", "#gwhgg#", "##gg## ", " ###   " };
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
            texture.SetPixel(x, texture.height - y - 1, rows[y][x] switch
            {
                '#' => new Color32(38, 53, 51, 255),
                'g' => new Color32(98, 211, 152, 255),
                'h' => new Color32(170, 250, 193, 255),
                'w' => new Color32(237, 255, 216, 255),
                _ => Color.clear
            });
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, PixelsPerUnit);
        sprite.name = "CeilingPatrolSensor";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return sprite;
    }

    private static AnimatorController CreateController(Sprite[] idle, Sprite[] flight, Sprite[] attack,
        Sprite hurt, Sprite[] death, Sprite sensor)
    {
        string path = Animations + "/" + Name + ".controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var states = controller.layers[0].stateMachine;
        var idleState = Add("Idle", idle, 0.11f, true);
        var move = Add("Move", flight, 0.09f, true);
        Add("DashPreparation", attack.Take(3).ToArray(), 0.20f);
        Add("Dash", attack.Skip(3).ToArray(), 0.05f);
        var recovery = states.AddState("Recovery");
        recovery.motion = idleState.motion;
        recovery.writeDefaultValues = false;
        var hurtState = Add("Hurt", new[] { hurt }, 0.12f);
        Add("Death", death, 0.09f);
        states.defaultState = idleState;
        Transition(idleState, move, AnimatorConditionMode.If);
        Transition(move, idleState, AnimatorConditionMode.IfNot);
        var exit = hurtState.AddTransition(idleState);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.duration = 0f;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState Add(string name, Sprite[] frames, float interval, bool loop = false)
        {
            var clip = new AnimationClip { frameRate = 100f };
            var keys = frames.Select((sprite, index) => Frame(index * interval, sprite)).ToList();
            keys.Add(Frame(frames.Length * interval, loop ? frames[0] : frames.Last()));
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            // 준비 중 밝은 센서 스프라이트를 교대로 표시합니다. 공격 중에는 어두워지고 사망 때 사라집니다.
            ObjectReferenceKeyframe[] sensorFrames = name switch
            {
                "DashPreparation" => Enumerable.Range(0, 8)
                    .Select(index => Frame(index * 0.075f, index % 2 == 0 ? sensor : null)).ToArray(),
                "Dash" or "Hurt" or "Death" => new[] { Frame(0f, null) },
                _ => new[] { Frame(0f, sensor) }
            };
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("Sensor", typeof(SpriteRenderer), "m_Sprite"), sensorFrames);
            SetPreparationOffset(clip, name == "DashPreparation");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, $"{Animations}/{Name}_{name}.anim");
            var state = states.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = false;
            return state;
        }
    }

    private static ObjectReferenceKeyframe Frame(float time, Sprite sprite) =>
        new ObjectReferenceKeyframe { time = time, value = sprite };

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(condition, 0f, "IsMoving");
    }

    private static void Configure(GameObject root, AnimatorController controller, Sprite idle, Sprite sensor)
    {
        root.name = Name;
        var enemy = root.GetComponent<MeleeEnemy>();
        Set(enemy, "detectionRange", 9f);
        Set(enemy, "chaseReleaseRange", 12f);
        Set(enemy, "moveSpeed", 3.1f);
        root.GetComponent<Rigidbody2D>().gravityScale = 0f;
        var animator = root.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = controller;
        var renderer = animator.GetComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.flipX = false;
        var sensorRenderer = animator.transform.Find("IntakeGrille").GetComponent<SpriteRenderer>();
        sensorRenderer.name = "Sensor";
        sensorRenderer.sprite = sensor;
        sensorRenderer.transform.localPosition = new Vector3(6f, 10f, 0f) / PixelsPerUnit;

        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(0.68f, 1.12f);
        hurtbox.offset = Vector2.zero;
        var movement = root.GetComponentInChildren<EnemyMovementModule>();
        var terrain = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        terrain.size = new Vector2(0.60f, 0.92f);
        terrain.offset = Vector2.zero;
        var moveObject = movement.gameObject;
        Object.DestroyImmediate(movement);
        moveObject.name = "FlightModule";
        var flight = moveObject.AddComponent<EnemyFlightModule>();
        Set(flight, "bodyCollider", terrain);
        Object.DestroyImmediate(root.GetComponentInChildren<EnemyPatrolModule>().gameObject);
        root.GetComponentInChildren<EnemyParticleModule>().transform.localPosition = Vector3.zero;
        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
        float standardKnockback = new SerializedObject(reaction).FindProperty("knockbackSpeed").floatValue;
        Set(reaction, "knockbackSpeed", standardKnockback * 0.90f);
        Set(reaction, "hitStunDuration", 0.20f);
        var rewards = root.GetComponentInChildren<EnemyCurrencyRewardModule>();
        Set(rewards, "maxBonusAmount", 3);

        var stats = Copy(Scripts + "/EnemyStatGroups/Enemy17_IntakeBiter_Stats.asset", Scripts + $"/EnemyStatGroups/{Name}_Stats.asset");
        var statData = new SerializedObject(stats);
        var entries = statData.FindProperty("<Stats>k__BackingField");
        float[] amounts = { 55f, 1f, 5f };
        for (int i = 0; i < amounts.Length; i++)
            entries.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = amounts[i];
        statData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        var statModule = root.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == "StatModule");
        Set(statModule, "baseStats", stats);

        var old = root.GetComponentInChildren<EnemyTimedMeleeSkill>();
        var skillObject = old.gameObject;
        Object.DestroyImmediate(old);
        Object.DestroyImmediate(root.GetComponentsInChildren<BoxCollider2D>().Single(box => box.name == "AttackHitbox").gameObject);
        skillObject.name = "FlightDash";
        var dash = skillObject.AddComponent<EnemyFlightDashSkill>();
        Set(dash, "<SkillSo>k__BackingField", Copy(Scripts + "/EnemySkills/Melee/Enemy17_IntakeBiter_Bite.asset",
            $"{Skills}/{Name}_FlightDash.asset"));
        Set(dash, "<BaseCoolTime>k__BackingField", 2.6f);
        Set(dash, "attackAnimator", animator);
        Set(dash, "fallbackAttackDelay", 0.60f);
        Set(dash, "postBlinkDelay", 0f);
        Set(dash, "blinkBeforeAttack", false);
        var skills = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>());
        var slot = skills.FindProperty("skills").GetArrayElementAtIndex(0);
        slot.FindPropertyRelative("skill").objectReferenceValue = dash;
        slot.FindPropertyRelative("maxRange").floatValue = 2.4f;
        slot.FindPropertyRelative("useHorizontalRange").boolValue = true;
        slot.FindPropertyRelative("maxHeightDifference").floatValue = 0.6f;
        slot.FindPropertyRelative("requireGrounded").boolValue = false;
        slot.FindPropertyRelative("requireClearPath").boolValue = true;
        skills.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new InvalidOperationException("Missing field " + name);
        if (value is int count) field.intValue = count;
        else if (value is float number) field.floatValue = number;
        else if (value is bool flag) field.boolValue = flag;
        else if (value is Color color) field.colorValue = color;
        else field.objectReferenceValue = (Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object Copy(string source, string destination)
    {
        var existing = AssetDatabase.LoadMainAssetAtPath(destination);
        if (existing != null) return existing;
        if (!AssetDatabase.CopyAsset(source, destination)) throw new IOException("Could not copy " + source);
        return AssetDatabase.LoadMainAssetAtPath(destination);
    }

    private static void MakeFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        MakeFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
