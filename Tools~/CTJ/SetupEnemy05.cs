using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.StatSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupEnemy05
{
    public const string SheetPath = "Assets/_Graphics/Assets/Enemy/ai_created_shieldman.png";
    public const string PrefabPath = "Assets/_Prefabs/Enemies/Enemy/Enemy05/Enemy05_Shieldbearer.prefab";
    private const string SourcePrefab = "Assets/_Prefabs/Enemies/Enemy/Enemy01/Enemy01_TwistedArm.prefab";
    private const string StatsPath = "Assets/_Scripts/Agent/Enemies/EnemyStatGroups/Enemy05_Shieldbearer_Stats.asset";
    private const string SkillFolder = "Assets/_Scripts/Agent/Enemies/EnemySkills/Enemy05";
    private const string AnimationFolder = "Assets/_Graphics/Animations/Enemy/Enemy05";
    private const string ControllerPath = AnimationFolder + "/Enemy05_Shieldbearer.controller";

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new InvalidOperationException("Create Enemy05 in Edit Mode when the target prefab does not exist.");

        var sprites = SliceSheet();
        MakeFolder("Assets/_Prefabs/Enemies/Enemy/Enemy05");
        MakeFolder(SkillFolder);
        MakeFolder(AnimationFolder);
        var controller = CreateController(sprites);
        var root = PrefabUtility.LoadPrefabContents(SourcePrefab);
        try
        {
            ConfigurePrefab(root, controller, sprites[FrameName("Idle", 0)]);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new InvalidOperationException("Could not save Enemy05 prefab.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath, slices = sprites.Count, controller = ControllerPath };
    }

    private static Dictionary<string, Sprite> SliceSheet()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existingIds = provider.GetSpriteRects().ToDictionary(sprite => sprite.name, sprite => sprite.spriteID);
        var image = new Texture2D(2, 2);
        var rects = new List<SpriteRect>(29);
        try
        {
            if (!ImageConversion.LoadImage(image, File.ReadAllBytes(SheetPath)) || image.width != 1536 || image.height != 1024)
                throw new InvalidOperationException("Unexpected shieldbearer sheet dimensions.");
            Color32[] pixels = image.GetPixels32();
            // 좌표는 원본 PNG의 좌상단 기준입니다. 행마다 몸통 축과 발 기준선이 다릅니다.
            (string name, int top, int bottom, int startX, int step, int count, int anchor, int feet)[] rows =
            {
                ("Idle", 0, 213, 15, 300, 5, 173, 198),
                ("Walk", 213, 413, 25, 250, 6, 151, 399),
                ("Run", 413, 612, 25, 250, 6, 155, 589),
                ("Bash", 612, 813, 25, 250, 6, 147, 787),
                ("Death", 813, 1024, 0, 250, 6, 147, 994)
            };
            foreach (var row in rows)
            {
                for (int i = 0; i < row.count; i++)
                {
                    string name = FrameName(row.name, i);
                    int startX = row.startX + row.step * i;
                    int endX = startX + row.step;
                    var cell = new RectInt(startX, row.top, endX - startX, row.bottom - row.top);
                    RectInt crop = FindFrame(pixels, image.width, image.height, cell, row.feet);
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        spriteID = existingIds.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                        alignment = SpriteAlignment.Custom,
                        rect = new Rect(crop.x, image.height - crop.yMax, crop.width, crop.height),
                        pivot = new Vector2((row.anchor + row.step * i - crop.x) / (float)crop.width,
                            (crop.yMax - row.feet) / (float)crop.height)
                    });
                }
            }
        }
        finally { Object.DestroyImmediate(image); }

        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 86f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        if (sprites.Count != rects.Count || rects.Any(rect => !sprites.ContainsKey(rect.name)))
            throw new InvalidOperationException("The importer did not create all 29 shieldbearer sprites.");
        return sprites;
    }

    private static RectInt FindFrame(Color32[] pixels, int width, int height, RectInt cell, int feet)
    {
        int left = cell.xMax, right = cell.xMin, top = cell.yMax, bottom = cell.yMin;
        for (int y = cell.yMin; y < cell.yMax; y++)
        {
            for (int x = cell.xMin; x < cell.xMax; x++)
            {
                if (pixels[(height - 1 - y) * width + x].a < 96) continue;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x + 1);
                top = Mathf.Min(top, y);
                bottom = Mathf.Max(bottom, y + 1);
            }
        }
        if (right <= left || bottom <= top) throw new InvalidOperationException("An expected sprite cell is empty.");
        const int padding = 3;
        left = Mathf.Max(cell.xMin, left - padding);
        right = Mathf.Min(cell.xMax, right + padding);
        top = Mathf.Max(cell.yMin, top - padding);
        bottom = Mathf.Min(cell.yMax, Mathf.Max(bottom, feet) + padding);
        return new RectInt(left, top, right - left, bottom - top);
    }

    private static string FrameName(string row, int index) => $"Shieldbearer_{row}_{index + 1:D2}";

    private static AnimatorController CreateController(Dictionary<string, Sprite> sprites)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null) return existing;

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var states = controller.layers[0].stateMachine;
        var idle = AddState("Idle", "Idle", 6f, true, 0, 1, 2, 3, 4);
        var move = AddState("Move", "Walk", 9f, true, 0, 1, 2, 3, 4, 5);
        AddState("AttackPreparation", "Bash", 4f, false, 0, 1);
        AddState("Attack", "Bash", 20f, false, 2, 3, 4);
        AddState("ChargePreparation", "Bash", 4.285714f, false, 0, 1, 2);
        AddState("Charge", "Bash", 12f, true, 2, 3, 4);
        AddState("ChargeRecovery", "Bash", 6f, false, 4, 5);
        AddState("Death", "Death", 7f, false, 0, 1, 2, 3, 4, 5);
        states.defaultState = idle;
        AddMovementTransition(idle, move, AnimatorConditionMode.If);
        AddMovementTransition(move, idle, AnimatorConditionMode.IfNot);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState AddState(string stateName, string row, float frameRate, bool loop, params int[] indices)
        {
            string path = $"{AnimationFolder}/Enemy05_Shieldbearer_{stateName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { frameRate = frameRate };
                var frames = indices.Select(index => sprites[FrameName(row, index)]).ToArray();
                var keys = frames.Select((sprite, index) => new ObjectReferenceKeyframe
                    { time = index / frameRate, value = sprite }).ToArray();
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, path);
            }
            int index = states.states.Length;
            var state = states.AddState(stateName, new Vector3(140f + index % 4 * 220f, 90f + index / 4 * 90f));
            state.motion = clip;
            state.writeDefaultValues = false;
            return state;
        }
    }

    private static void AddMovementTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(condition, 0f, "IsMoving");
    }

    private static void ConfigurePrefab(GameObject root, AnimatorController controller, Sprite idle)
    {
        root.name = "Enemy05_Shieldbearer";
        root.transform.localScale = Vector3.one;
        root.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var oldEnemy = root.GetComponent<MeleeEnemy>();
        var enemy = root.AddComponent<EnemyShieldbearer>();
        EditorUtility.CopySerializedManagedFieldsOnly(oldEnemy, enemy);
        Set(root.GetComponentInChildren<EnemyLocomotionAnimator>(true), "enemy", enemy);
        Object.DestroyImmediate(root.GetComponentInChildren<EnemyAnimationEventRelay>(true));
        Object.DestroyImmediate(oldEnemy);
        Set(enemy, "detectionRange", 10f);
        Set(enemy, "chaseReleaseRange", 13f);
        Set(enemy, "moveSpeed", 2.8f);
        var turnDelay = new GameObject("TurnDelayModule");
        turnDelay.transform.SetParent(root.transform, false);
        turnDelay.AddComponent<EnemyTurnDelayModule>();

        var animator = root.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = controller;
        animator.transform.localPosition = Vector3.zero;
        animator.transform.localScale = Vector3.one;
        animator.GetComponent<SpriteRenderer>().sprite = idle;
        Set(enemy, "deathAnimator", animator);

        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(1.4f, 2f);
        hurtbox.offset = Vector2.up;
        var movement = root.GetComponentInChildren<EnemyMovementModule>(true);
        var terrainBody = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        terrainBody.size = new Vector2(1.05f, 2f);
        terrainBody.offset = Vector2.up;
        Set(movement, "jumpHeight", 0f);
        Set(root.GetComponentInChildren<EnemyPatrolModule>(true), "patrolSpeed", 2.8f);
        Set(root.GetComponentInChildren<EnemyPatrolModule>(true), "halfWidth", 3f);
        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>(true);
        float standardKnockback = new SerializedObject(reaction).FindProperty("knockbackSpeed").floatValue;
        Set(reaction, "knockbackSpeed", standardKnockback * 0.7f);
        Set(reaction, "hitStunDuration", 0.35f);
        root.GetComponentInChildren<EnemyParticleModule>(true).transform.localPosition = Vector3.up;
        var oldWarning = root.GetComponentInChildren<EnemyAttackWarningModule>(true);
        if (oldWarning != null) Object.DestroyImmediate(oldWarning.gameObject);

        var statModule = root.GetComponentsInChildren<MonoBehaviour>(true)
            .Single(component => component != null && component.GetType().Name == "StatModule");
        var health = root.GetComponentInChildren<NewHealthModule>(true);
        var defense = (StatData)new SerializedObject(health).FindProperty("defenseStatData").objectReferenceValue;
        Set(statModule, "baseStats", CreateStats(defense));

        var skills = root.GetComponentInChildren<EnemySkillModule>(true);
        var oldAttack = root.GetComponentInChildren<EnemyMeleeSkill>(true);
        var bashObject = oldAttack.gameObject;
        Object.DestroyImmediate(oldAttack);
        bashObject.name = "ShieldBash";
        var bash = bashObject.AddComponent<EnemyShieldBashSkill>();
        var hitbox = root.GetComponentsInChildren<BoxCollider2D>(true).Single(collider => collider.name == "AttackHitbox");
        hitbox.size = new Vector2(2f, 1.4f);
        hitbox.offset = new Vector2(1f, 1f);
        Set(bash, "attackHitbox", hitbox);
        Set(bash, "damage", 18f);
        Set(bash, "activeDuration", 0.15f);
        Set(bash, "recoveryTime", 0.7f);
        ConfigureSkill(bash, "ShieldBash", 2.5f, 0.5f, animator);

        var chargeObject = new GameObject("ShieldCharge");
        chargeObject.transform.SetParent(skills.transform, false);
        var charge = chargeObject.AddComponent<EnemyShieldChargeSkill>();
        ConfigureSkill(charge, "ShieldCharge", 5.5f, 0.7f, animator);
        var data = new SerializedObject(skills);
        var entries = data.FindProperty("skills");
        entries.arraySize = 2;
        ConfigureEntry(entries.GetArrayElementAtIndex(0), bash, 0f, 1.8f, false);
        ConfigureEntry(entries.GetArrayElementAtIndex(1), charge, 3f, 7f, true);
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object CreateStats(StatData defense)
    {
        const string source = "Assets/_Scripts/Agent/Enemies/EnemyStatGroups/Enemy01_TwistedArm_Stats.asset";
        var stats = CopyAsset(source, StatsPath);
        var data = new SerializedObject(stats);
        var overrides = data.FindProperty("<Stats>k__BackingField");
        overrides.GetArrayElementAtIndex(0).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 140f;
        var defenseEntry = overrides.GetArrayElementAtIndex(1);
        defenseEntry.FindPropertyRelative("<StatData>k__BackingField").objectReferenceValue = defense;
        defenseEntry.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 8f;
        overrides.GetArrayElementAtIndex(2).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 18f;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        return stats;
    }

    private static void ConfigureSkill(EnemyAttackSkill skill, string name, float cooldown, float preparation, Animator animator)
    {
        const string source = "Assets/_Scripts/Agent/Enemies/EnemySkills/Melee/Enemy01_TwistedArm_Melee.asset";
        Set(skill, "<SkillSo>k__BackingField", CopyAsset(source, $"{SkillFolder}/Enemy05_{name}.asset"));
        Set(skill, "<BaseCoolTime>k__BackingField", cooldown);
        Set(skill, "fallbackAttackDelay", preparation);
        Set(skill, "postBlinkDelay", 0f);
        Set(skill, "blinkBeforeAttack", false);
        Set(skill, "attackAnimator", animator);
    }

    private static void ConfigureEntry(SerializedProperty entry, EnemyAttackSkill skill, float min, float max, bool requireReady)
    {
        entry.FindPropertyRelative("skill").objectReferenceValue = skill;
        entry.FindPropertyRelative("useSkill").boolValue = true;
        entry.FindPropertyRelative("useForAutomaticSelection").boolValue = true;
        entry.FindPropertyRelative("requireUsableToEnterAttack").boolValue = requireReady;
        entry.FindPropertyRelative("minRange").floatValue = min;
        entry.FindPropertyRelative("maxRange").floatValue = max;
        entry.FindPropertyRelative("maxHeightDifference").floatValue = 0.55f;
        entry.FindPropertyRelative("requireGrounded").boolValue = true;
        entry.FindPropertyRelative("requireClearPath").boolValue = true;
    }

    private static void Set(Object target, string field, object value)
    {
        var data = new SerializedObject(target);
        var property = data.FindProperty(field) ?? throw new InvalidOperationException($"{target.name} has no {field}");
        if (value is float number) property.floatValue = number;
        else if (value is bool flag) property.boolValue = flag;
        else property.objectReferenceValue = (Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object CopyAsset(string source, string destination)
    {
        var existing = AssetDatabase.LoadMainAssetAtPath(destination);
        if (existing != null) return existing;
        if (!AssetDatabase.CopyAsset(source, destination)) throw new IOException($"Could not create {destination}");
        return AssetDatabase.LoadMainAssetAtPath(destination);
    }

    private static void MakeFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
