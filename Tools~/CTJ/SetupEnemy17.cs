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

public static class SetupEnemy17
{
    private const string Name = "Enemy17_IntakeBiter";
    private const string Graphics = "Assets/_Graphics/Assets/Enemy/Enemy17";
    private const string Sheets = Graphics + "/SHADOW mini - Night Fang/png";
    private const string AnimationFolder = "Assets/_Graphics/Animations/Enemy/Enemy17";
    private const string PrefabFolder = "Assets/_Prefabs/Enemies/Enemy/Enemy17";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string Legacy = Scripts + "/Legacy";
    private const string StatsFolder = Scripts + "/EnemyStatGroups";
    private const string SkillFolder = Scripts + "/EnemySkills/Melee";
    private const float PixelsPerUnit = 24f;
    public const string PrefabPath = PrefabFolder + "/" + Name + ".prefab";

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
            throw new InvalidOperationException("Create Enemy17 in Edit Mode before its prefab exists.");

        foreach (string path in new[] { AnimationFolder, PrefabFolder, StatsFolder, SkillFolder }) MakeFolder(path);
        var idle = Slice("idle", 6, 53, 89);
        var walk = Slice("walk", 6, 53, 89);
        // The original attack sheet has a ranged beam starting at cell 12.
        var attack = Slice("attack", 21, 53, 89);
        var hurt = Slice("damaged", 1, 53, 89);
        var death = Slice("death", 8, 44, 94);
        var grille = CreateGrille();
        var controller = CreateController(idle, walk, attack, hurt[0], death, grille);

        const string source = "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy01/Enemy01_TwistedArm.prefab";
        var root = PrefabUtility.LoadPrefabContents(source);
        try
        {
            ConfigurePrefab(root, controller, idle[0], grille);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException($"Could not save {PrefabPath}");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { prefab = PrefabPath, animations = controller.animationClips.Length };
    }

    private static Sprite[] Slice(string file, int count, int left, int right)
    {
        const int cellWidth = 94;
        const int cellHeight = 25;
        const int pivotX = 70;
        string path = $"{Sheets}/{file}.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != cellWidth * count || height != cellHeight)
            throw new InvalidDataException($"Unexpected cell dimensions in {path}");

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(sprite => sprite.name, sprite => sprite.spriteID);
        var rects = new SpriteRect[count];
        for (int i = 0; i < count; i++)
        {
            string name = $"{file}_{i}";
            int rectLeft = file == "attack" && i >= 12 ? 0 : left;
            rects[i] = new SpriteRect
            {
                name = name,
                spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                alignment = SpriteAlignment.Custom,
                rect = new Rect(cellWidth * i + rectLeft, 0, right - rectLeft, cellHeight),
                pivot = new Vector2((pivotX - rectLeft) / (float)(right - rectLeft), 0f)
            };
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        return rects.Select(rect => sprites[rect.name]).ToArray();
    }

    private static AnimatorController CreateController(Sprite[] idle, Sprite[] walk, Sprite[] attack, Sprite hurt,
        Sprite[] death, Sprite grille)
    {
        string path = $"{AnimationFolder}/{Name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return controller;

        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var states = controller.layers[0].stateMachine;
        var idleState = AddState("Idle", 0.72f, true, Sequence(idle, 0.12f));
        var moveState = AddState("Move", 0.48f, true, Sequence(walk, 0.08f));
        AddState("AttackPreparation", 0.55f, false, new[]
        {
            (0f, attack[6]), (0.08f, attack[8]), (0.16f, attack[11])
        });
        AddState("Attack", 0.73f, false, new[] { (0f, attack[10]), (0.1f, attack[9]), (0.2f, attack[20]) });
        var hurtState = AddState("Hurt", 0.12f, false, new[] { (0f, hurt) }, null);
        AddState("Death", 0.68f, false, Sequence(death, 0.085f), 0.255f);
        states.defaultState = idleState;
        AddMovementTransition(idleState, moveState, AnimatorConditionMode.If);
        AddMovementTransition(moveState, idleState, AnimatorConditionMode.IfNot);
        var hurtExit = hurtState.AddTransition(idleState);
        hurtExit.hasExitTime = true;
        hurtExit.exitTime = 1f;
        hurtExit.duration = 0f;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState AddState(string stateName, float length, bool loop, (float time, Sprite sprite)[] frames,
            float? grilleHideTime = float.PositiveInfinity)
        {
            var clip = new AnimationClip { frameRate = 100f };
            var keys = frames.Select(frame => new ObjectReferenceKeyframe { time = frame.time, value = frame.sprite }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = length, value = frames[loop ? 0 : frames.Length - 1].sprite });
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            SetGrilleCurve(clip, grille, grilleHideTime);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, $"{AnimationFolder}/{Name}_{stateName}.anim");

            int index = states.states.Length;
            var state = states.AddState(stateName, new Vector3(140f + index % 3 * 215f, 90f + index / 3 * 100f));
            state.motion = clip;
            state.writeDefaultValues = false;
            return state;
        }
    }

    private static void SetGrilleCurve(AnimationClip clip, Sprite grille, float? hideTime)
    {
        var frames = new List<ObjectReferenceKeyframe>
        {
            new ObjectReferenceKeyframe { time = 0f, value = hideTime.HasValue ? grille : null }
        };
        if (hideTime < clip.length) frames.Add(new ObjectReferenceKeyframe { time = hideTime.Value, value = null });
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("IntakeGrille", typeof(SpriteRenderer), "m_Sprite"), frames.ToArray());
    }

    private static (float time, Sprite sprite)[] Sequence(Sprite[] sprites, float frameTime) =>
        sprites.Select((sprite, index) => (index * frameTime, sprite)).ToArray();

    private static void AddMovementTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(condition, 0f, "IsMoving");
    }

    private static Sprite CreateGrille()
    {
        const string path = Graphics + "/Enemy17_IntakeGrille.asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;

        var texture = new Texture2D(8, 4, TextureFormat.RGBA32, false)
        {
            name = "IntakeGrillePixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        string[] rows = { " ###### ", "#ababab#", "#ababab#", " ###### " };
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
            texture.SetPixel(x, texture.height - y - 1, rows[y][x] switch
            {
                '#' => new Color32(58, 46, 65, 255),
                'a' => new Color32(94, 76, 100, 255),
                'b' => new Color32(26, 20, 32, 255),
                _ => new Color32(0, 0, 0, 0)
            });
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 4), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        sprite.name = "IntakeGrille";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return sprite;
    }

    private static void ConfigurePrefab(GameObject root, AnimatorController controller, Sprite idle, Sprite grille)
    {
        root.name = Name;
        root.transform.localScale = Vector3.one;
        root.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var enemy = root.GetComponent<MeleeEnemy>();
        Set(enemy, "detectionRange", 7f);
        Set(enemy, "chaseReleaseRange", 10f);
        Set(enemy, "moveSpeed", 2.8f);
        Object.DestroyImmediate(root.GetComponentInChildren<EnemyAnimationEventRelay>(true));
        Object.DestroyImmediate(root.GetComponentInChildren<EnemyAttackWarningModule>(true).gameObject);

        var animator = root.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = controller;
        animator.transform.localPosition = Vector3.zero;
        animator.transform.localScale = Vector3.one;
        var renderer = animator.GetComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.flipX = true;
        var grilleObject = new GameObject("IntakeGrille") { layer = root.layer };
        grilleObject.transform.SetParent(animator.transform, false);
        grilleObject.transform.localPosition = new Vector3(-7f, 17f) / PixelsPerUnit;
        var grilleRenderer = grilleObject.AddComponent<SpriteRenderer>();
        grilleRenderer.sprite = grille;
        grilleRenderer.sharedMaterial = renderer.sharedMaterial;
        grilleRenderer.sortingLayerID = renderer.sortingLayerID;
        grilleRenderer.sortingOrder = renderer.sortingOrder + 1;

        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(1.16f, 0.83f);
        hurtbox.offset = Vector2.up * 0.415f;
        var movement = root.GetComponentInChildren<EnemyMovementModule>(true);
        var terrainBody = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        terrainBody.size = new Vector2(0.82f, 0.75f);
        terrainBody.offset = Vector2.up * 0.375f;
        var patrol = root.GetComponentInChildren<EnemyPatrolModule>(true);
        Set(patrol, "patrolSpeed", 2.8f);
        Set(patrol, "halfWidth", 3f);
        Set(patrol, "returnToSpawnBeforePatrol", true);
        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>(true);
        Set(reaction, "hitStunDuration", 0.25f);
        Set(reaction, "hurtAnimator", animator);
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(true), "maxBonusAmount", 2);
        root.GetComponentInChildren<EnemyParticleModule>(true).transform.localPosition = Vector3.up * 0.4f;

        var statModule = root.GetComponentsInChildren<MonoBehaviour>(true)
            .Single(component => component != null && component.GetType().Name == "StatModule");
        var stats = CopyAsset(Legacy + "/EnemyStatGroups/Enemy05_Shieldbearer_Stats.asset", $"{StatsFolder}/{Name}_Stats.asset");
        var statData = new SerializedObject(stats);
        var statEntries = statData.FindProperty("<Stats>k__BackingField");
        float[] values = { 45f, 1f, 3f };
        for (int i = 0; i < values.Length; i++)
            statEntries.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = values[i];
        statData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        Set(statModule, "baseStats", stats);

        var oldAttack = root.GetComponentInChildren<EnemyMeleeSkill>(true);
        var biteObject = oldAttack.gameObject;
        Object.DestroyImmediate(oldAttack);
        biteObject.name = "Bite";
        var bite = biteObject.AddComponent<EnemyTimedMeleeSkill>();
        var hitbox = root.GetComponentsInChildren<BoxCollider2D>(true).Single(collider => collider.name == "AttackHitbox");
        hitbox.size = new Vector2(0.97f, 0.68f);
        hitbox.offset = new Vector2(0.715f, 0.34f);
        Set(bite, "attackHitbox", hitbox);
        Set(bite, "damage", 8f);
        Set(bite, "activeDuration", 0.08f);
        Set(bite, "recoveryTime", 0.65f);
        Set(bite, "lockFacingDuringPreparation", true);
        Set(bite, "cooldownFromStrike", true);
        Set(bite, "<SkillSo>k__BackingField", CopyAsset(Legacy + "/EnemySkills/Melee/Enemy01_TwistedArm_Melee.asset", $"{SkillFolder}/{Name}_Bite.asset"));
        Set(bite, "<BaseCoolTime>k__BackingField", 1.6f);
        Set(bite, "fallbackAttackDelay", 0.55f);
        Set(bite, "postBlinkDelay", 0f);
        Set(bite, "blinkBeforeAttack", false);
        Set(bite, "attackAnimator", animator);

        var skills = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>(true));
        var entries = skills.FindProperty("skills");
        entries.arraySize = 1;
        var entry = entries.GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("skill").objectReferenceValue = bite;
        entry.FindPropertyRelative("useSkill").boolValue = true;
        entry.FindPropertyRelative("useForAutomaticSelection").boolValue = true;
        entry.FindPropertyRelative("requireUsableToEnterAttack").boolValue = false;
        entry.FindPropertyRelative("minRange").floatValue = 0f;
        entry.FindPropertyRelative("maxRange").floatValue = 1.2f;
        entry.FindPropertyRelative("maxHeightDifference").floatValue = 0.55f;
        entry.FindPropertyRelative("requireGrounded").boolValue = true;
        entry.FindPropertyRelative("requireClearPath").boolValue = true;
        skills.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string field, object value)
    {
        var data = new SerializedObject(target);
        var property = data.FindProperty(field) ?? throw new InvalidOperationException($"{target.name} has no {field}");
        if (value is float number) property.floatValue = number;
        else if (value is int integer) property.intValue = integer;
        else if (value is bool flag) property.boolValue = flag;
        else property.objectReferenceValue = (Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object CopyAsset(string source, string target)
    {
        var existing = AssetDatabase.LoadMainAssetAtPath(target);
        if (existing != null) return existing;
        if (!AssetDatabase.CopyAsset(source, target)) throw new IOException($"Could not copy to {target}");
        return AssetDatabase.LoadMainAssetAtPath(target);
    }

    private static void MakeFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        MakeFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
