using System;
using System.IO;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupEnemy19
{
    private const string Name = "Enemy19_OverheatedWorker";
    private const string Sheets = "Assets/_Graphics/Assets/Enemy/Enemy19/SHADOW Series - Hallokin/png/";
    private const string Animations = "Assets/_Graphics/Animations/Enemy/Enemy19";
    private const string Enemies = "Assets/_Prefabs/Enemies";
    private const string Prefabs = Enemies + "/Enemy/Enemy19";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string Skills = Scripts + "/EnemySkills/Enemy19";
    private const string PrefabPath = Prefabs + "/" + Name + ".prefab";

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
            throw new InvalidOperationException("Create Enemy19 in Edit Mode before its prefab exists.");
        MakeFolder(Animations);
        MakeFolder(Prefabs);
        MakeFolder(Skills);
        var idle = Slice("idle", 6, 47, 67, 54);
        var run = Slice("run", 4, 47, 67, 54);
        var front = Slice("attack-2", 7, 40, 90, 54);
        var up = Slice("attack up", 6, 45, 73, 30);
        var hit = Slice("hit", 1, 47, 67, 54);
        var death = Slice("death", 13, 37, 76, 48);
        var controller = CreateController(idle, run, front, up, hit[0], death);

        const string source = Enemies + "/Enemy/Enemy17/Enemy17_IntakeBiter.prefab";
        var root = PrefabUtility.LoadPrefabContents(source);
        try
        {
            Configure(root, controller, idle[0]);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath, clips = controller.animationClips.Length };
    }

    private static Sprite[] Slice(string file, int count, int left, int right, int top)
    {
        const int cellWidth = 192;
        const int cellHeight = 128;
        const int feet = 80;
        const int centerX = 57;
        string path = Sheets + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != cellWidth * count || height != cellHeight)
            throw new InvalidDataException("Unexpected Hallokin sheet dimensions " + path);
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
                rect = new Rect(index * cellWidth + left, cellHeight - feet, right - left, feet - top),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((centerX - left) / (float)(right - left), 0f)
            };
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 24f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        return rects.Select(rect => sprites[rect.name]).ToArray();
    }

    private static AnimatorController CreateController(Sprite[] idle, Sprite[] run, Sprite[] front, Sprite[] up, Sprite hurt, Sprite[] death)
    {
        string path = Animations + "/" + Name + ".controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var states = controller.layers[0].stateMachine;
        var idleState = AddState("Idle", idle, 0.12f, true);
        var move = AddState("Move", run, 0.09f, true);
        AddState("ForwardPreparation", front.Take(2).ToArray(), 0.15f);
        AddState("ForwardStrike", front.Skip(2).ToArray(), 0.07f);
        AddState("UpwardPreparation", up.Take(2).ToArray(), 0.15f);
        AddState("UpwardStrike", up.Skip(2).ToArray(), 0.07f);
        var recovery = states.AddState("Recovery");
        recovery.motion = idleState.motion;
        recovery.writeDefaultValues = false;
        var hurtState = AddState("Hurt", new[] { hurt }, 0.10f);
        AddState("Death", death, 0.075f);
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

        AnimatorState AddState(string name, Sprite[] frames, float interval, bool loop = false)
        {
            var clip = new AnimationClip { frameRate = 100f };
            var keys = frames.Select((sprite, i) => new ObjectReferenceKeyframe { time = i * interval, value = sprite }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = frames.Length * interval, value = loop ? frames[0] : frames.Last() });
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
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

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(condition, 0f, "IsMoving");
    }

    private static void Configure(GameObject root, AnimatorController controller, Sprite idle)
    {
        root.name = Name;
        var enemy = root.GetComponent<MeleeEnemy>();
        Set(enemy, "detectionRange", 8.5f);
        Set(enemy, "chaseReleaseRange", 12f);
        Set(enemy, "moveSpeed", 2.8f);
        var animator = root.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = controller;
        Object.DestroyImmediate(animator.transform.Find("IntakeGrille").gameObject);
        var renderer = animator.GetComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.flipX = false;
        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(0.54f, 0.95f);
        hurtbox.offset = Vector2.up * 0.475f;
        var movement = root.GetComponentInChildren<EnemyMovementModule>();
        var terrainBody = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        terrainBody.size = new Vector2(0.46f, 0.86f);
        terrainBody.offset = Vector2.up * 0.43f;
        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
        float standardKnockback = new SerializedObject(reaction).FindProperty("knockbackSpeed").floatValue;
        Set(reaction, "knockbackSpeed", standardKnockback * 0.85f);
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(), "maxBonusAmount", 4);

        var stats = Copy(Scripts + "/EnemyStatGroups/Enemy17_IntakeBiter_Stats.asset", Scripts + $"/EnemyStatGroups/{Name}_Stats.asset");
        var statData = new SerializedObject(stats);
        var values = statData.FindProperty("<Stats>k__BackingField");
        float[] amounts = { 70f, 3f, 6f };
        for (int i = 0; i < amounts.Length; i++)
            values.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = amounts[i];
        statData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        var statModule = root.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == "StatModule");
        Set(statModule, "baseStats", stats);

        var old = root.GetComponentInChildren<EnemyTimedMeleeSkill>();
        Transform skillParent = old.transform.parent;
        Object.DestroyImmediate(old.gameObject);
        var forwardBox = root.GetComponentsInChildren<BoxCollider2D>().Single(box => box.name == "AttackHitbox");
        forwardBox.name = "ForwardHitbox";
        forwardBox.offset = new Vector2(0.65f, 0.42f);
        forwardBox.size = new Vector2(1.3f, 0.84f);
        var upwardBox = Object.Instantiate(forwardBox.gameObject, forwardBox.transform.parent, false).GetComponent<BoxCollider2D>();
        upwardBox.name = "UpwardHitbox";
        upwardBox.offset = Vector2.up * 1.7f;
        upwardBox.size = new Vector2(1.3f, 1.5f);

        var upward = AddAttack("Upward", upwardBox, damage: 12f, cooldown: 2.4f, upward: true);
        var forward = AddAttack("Forward", forwardBox, damage: 10f, cooldown: 1.8f, upward: false);
        var skills = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>());
        var entries = skills.FindProperty("skills");
        entries.arraySize = 2;
        Entry(entries.GetArrayElementAtIndex(0), upward, 0.65f, 2.45f, true);
        Entry(entries.GetArrayElementAtIndex(1), forward, 1.3f, 0.5f, false);
        skills.ApplyModifiedPropertiesWithoutUndo();

        EnemyTimedMeleeSkill AddAttack(string name, BoxCollider2D box, float damage, float cooldown, bool upward)
        {
            var obj = new GameObject(name + "Strike") { layer = root.layer };
            obj.transform.SetParent(skillParent, false);
            var skill = obj.AddComponent<EnemyTimedMeleeSkill>();
            Set(skill, "<SkillSo>k__BackingField", Copy(Scripts + "/EnemySkills/Melee/Enemy17_IntakeBiter_Bite.asset",
                $"{Skills}/{Name}_{name}.asset"));
            Set(skill, "<BaseCoolTime>k__BackingField", cooldown);
            Set(skill, "attackAnimator", animator);
            Set(skill, "attackHitbox", box);
            Set(skill, "preparationStateName", $"Base Layer.{name}Preparation");
            Set(skill, "attackStateName", $"Base Layer.{name}Strike");
            Set(skill, "recoveryStateName", "Base Layer.Recovery");
            Set(skill, "fallbackAttackDelay", 0.30f);
            Set(skill, "postBlinkDelay", 0f);
            Set(skill, "blinkBeforeAttack", false);
            Set(skill, "damage", damage);
            Set(skill, "activeDuration", 0.08f);
            Set(skill, "recoveryTime", 0.65f);
            Set(skill, "cooldownFromStrike", true);
            Set(skill, "lockFacingDuringPreparation", true);
            Set(skill, "attackUpward", upward);
            Set(skill, "minimumHeightForUpwardAttack", 0.9f);
            Set(skill, "checkTerrainAtHit", true);
            return skill;
        }
    }

    private static void Entry(SerializedProperty entry, EnemyTimedMeleeSkill skill, float range, float height, bool requireReady)
    {
        entry.FindPropertyRelative("skill").objectReferenceValue = skill;
        entry.FindPropertyRelative("useSkill").boolValue = true;
        entry.FindPropertyRelative("useForAutomaticSelection").boolValue = true;
        entry.FindPropertyRelative("requireUsableToEnterAttack").boolValue = requireReady;
        entry.FindPropertyRelative("minRange").floatValue = 0f;
        entry.FindPropertyRelative("maxRange").floatValue = range;
        entry.FindPropertyRelative("useHorizontalRange").boolValue = true;
        entry.FindPropertyRelative("maxHeightDifference").floatValue = height;
        entry.FindPropertyRelative("requireGrounded").boolValue = true;
        entry.FindPropertyRelative("requireClearPath").boolValue = true;
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new InvalidOperationException("Missing field " + name);
        if (value is bool flag) field.boolValue = flag;
        else if (value is int count) field.intValue = count;
        else if (value is float number) field.floatValue = number;
        else if (value is string label) field.stringValue = label;
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
