using System;
using System.IO;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupEnemy18
{
    private const string Name = "Enemy18_EffluentLeaper";
    private const string Art = "Assets/_Graphics/Assets/Enemy/Enemy18";
    private const string Sheets = Art + "/SHADOW Mini - Toxcrawler/png/";
    private const string Animations = "Assets/_Graphics/Animations/Enemy/Enemy18";
    private const string Enemies = "Assets/_Prefabs/Enemies";
    private const string Prefabs = Enemies + "/Enemy/Enemy18";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string PrefabPath = Prefabs + "/" + Name + ".prefab";
    private const string WarningPath = Prefabs + "/Enemy18_LeapWarning.prefab";
    private const float MinimumLeapDistance = 2f;
    private const float PixelsPerUnit = 24f;

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
            throw new InvalidOperationException("Create Enemy18 in Edit Mode before its prefab exists.");
        MakeFolder(Animations);
        MakeFolder(Prefabs);
        MakeFolder(Scripts + "/EnemySkills/Enemy18");

        var idle = Slice("idle", 6);
        var run = Slice("run", 4);
        var jump = Slice("jump", 3);
        var fall = Slice("fall", 3);
        var attack = Slice("attack 2", 7, 56);
        var death = Slice("death", 8);
        var tank = CreateTank();
        var controller = CreateController(idle, run, jump, fall, attack, death, tank);
        var splash = CreateSplash();
        const string source = Enemies + "/Enemy/Enemy17/Enemy17_IntakeBiter.prefab";
        var root = PrefabUtility.LoadPrefabContents(source);
        try
        {
            Configure(root, controller, idle[0], tank, splash);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath, animations = controller.animationClips.Length, splash = AssetDatabase.GetAssetPath(splash) };
    }

    private static Sprite[] Slice(string file, int frameCount, int right = 60)
    {
        const int cellWidth = 149;
        const int cellHeight = 48;
        const int left = 18;
        const int pivotX = 36;
        string path = Sheets + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != cellWidth * frameCount || height != cellHeight)
            throw new InvalidDataException("Unexpected Toxcrawler sheet size " + path);

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        var rects = Enumerable.Range(0, frameCount).Select(index =>
        {
            string name = file + "_" + index;
            return new SpriteRect
            {
                name = name,
                spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(index * cellWidth + left, 0, right - left, 30),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((pivotX - left) / (float)(right - left), 0f)
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
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        return rects.Select(rect => sprites[rect.name]).ToArray();
    }

    private static AnimatorController CreateController(Sprite[] idle, Sprite[] run, Sprite[] jump, Sprite[] fall,
        Sprite[] attack, Sprite[] death, Sprite tank)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Animations + "/" + Name + ".controller");
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var states = controller.layers[0].stateMachine;
        var idleState = Add("Idle", idle, 0.10f, true);
        var runState = Add("Move", run, 0.085f, true);
        Add("LeapPreparation", new[] { attack[0], attack[1] }, 0.10f);
        Add("LeapRise", jump, 0.09f);
        Add("LeapFall", fall, 0.075f);
        Add("LeapLand", new[] { attack[2], attack[1], attack[0] }, 0.12f);
        Add("Death", death, 0.095f);
        states.defaultState = idleState;
        Transition(idleState, runState, AnimatorConditionMode.If);
        Transition(runState, idleState, AnimatorConditionMode.IfNot);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState Add(string name, Sprite[] frames, float interval, bool loop = false)
        {
            var clip = new AnimationClip { frameRate = 100f };
            var keys = frames.Select((sprite, i) => new ObjectReferenceKeyframe { time = i * interval, value = sprite }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time = frames.Length * interval, value = loop ? frames[0] : frames.Last() });
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                keys.ToArray());
            var tankKeys = name == "Death"
                ? new[] { new ObjectReferenceKeyframe { time = 0f, value = tank }, new ObjectReferenceKeyframe { time = 2f * interval, value = null } }
                : new[] { new ObjectReferenceKeyframe { time = 0f, value = tank } };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("EffluentTank", typeof(SpriteRenderer), "m_Sprite"),
                tankKeys);
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

    private static Sprite CreateTank()
    {
        string path = Art + "/Enemy18_EffluentTank.asset";
        var texture = new Texture2D(9, 8, TextureFormat.RGBA32, false)
        {
            name = "EffluentTankPixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        string[] rows = { "  ####   ", " #hmmh#  ", " #lggl## ", " #gggg#p#", " #gddg#p#", " #mddm#p#", "  #### # ", "       # " };
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < texture.width; x++)
            texture.SetPixel(x, texture.height - 1 - y, rows[y][x] switch
            {
                '#' => new Color32(16, 22, 23, 255),
                'h' => new Color32(120, 159, 150, 255),
                'm' => new Color32(55, 84, 77, 255),
                'l' => new Color32(147, 233, 185, 255),
                'g' => new Color32(71, 169, 141, 255),
                'd' => new Color32(33, 104, 95, 255),
                'p' => new Color32(70, 123, 117, 255),
                _ => Color.clear
            });
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, PixelsPerUnit);
        sprite.name = "EffluentTank";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return sprite;
    }

    private static ParticleSystem CreateSplash()
    {
        string path = Prefabs + "/Enemy18_EffluentSplash.prefab";
        var root = new GameObject("Enemy18_EffluentSplash");
        try
        {
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color32(27, 129, 129, 255), new Color32(148, 237, 168, 255));
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.gravityModifier = 1f;
            main.maxParticles = 28;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.62f, 0.07f, 0f);
            shape.position = Vector3.up * 0.1f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-1.65f, 1.65f);
            velocity.y = new ParticleSystem.MinMaxCurve(1.15f, 2.75f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.36f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Enemy/HitFeedback/BloodPolygon.mat");
            renderer.sortingOrder = 12;
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<ParticleSystem>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Configure(GameObject root, AnimatorController controller, Sprite idle, Sprite tank, ParticleSystem splash)
    {
        root.name = Name;
        var enemy = root.GetComponent<MeleeEnemy>();
        Set(enemy, "detectionRange", 8f);
        Set(enemy, "chaseReleaseRange", 11f);
        Set(enemy, "moveSpeed", 3.4f);
        var animator = root.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = controller;
        var renderer = animator.GetComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.flipX = false;
        var tankRenderer = animator.transform.Find("IntakeGrille").GetComponent<SpriteRenderer>();
        tankRenderer.name = "EffluentTank";
        tankRenderer.transform.localPosition = new Vector3(-5f, 17f, 0f) / PixelsPerUnit;
        tankRenderer.sprite = tank;
        var pose = new GameObject("LeapPose") { layer = root.layer };
        pose.transform.SetParent(animator.transform.parent, false);
        animator.transform.SetParent(pose.transform, false);

        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(0.60f, 0.79f);
        hurtbox.offset = Vector2.up * 0.395f;
        var movement = root.GetComponentInChildren<EnemyMovementModule>(true);
        var body = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        body.size = new Vector2(0.53f, 0.70f);
        body.offset = Vector2.up * 0.35f;
        Set(root.GetComponentInChildren<EnemyPatrolModule>(true), "patrolSpeed", 3.4f);
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(true), "maxBonusAmount", 3);
        Set(root.GetComponentInChildren<EnemyHitReactionModule>(true), "hurtAnimator", null);

        var stats = Copy(Scripts + "/EnemyStatGroups/Enemy17_IntakeBiter_Stats.asset", Scripts + $"/EnemyStatGroups/{Name}_Stats.asset");
        var values = new SerializedObject(stats);
        var overrides = values.FindProperty("<Stats>k__BackingField");
        float[] amounts = { 65f, 2f, 5f };
        for (int i = 0; i < amounts.Length; i++)
            overrides.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = amounts[i];
        values.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        var statModule = root.GetComponentsInChildren<MonoBehaviour>(true).Single(item => item.GetType().Name == "StatModule");
        Set(statModule, "baseStats", stats);

        var bite = root.GetComponentInChildren<EnemyTimedMeleeSkill>(true);
        var leapObject = bite.gameObject;
        Object.DestroyImmediate(bite);
        var unusedHitbox = root.GetComponentsInChildren<BoxCollider2D>(true).Single(collider => collider.name == "AttackHitbox");
        Object.DestroyImmediate(unusedHitbox.gameObject);
        leapObject.name = "EffluentLeap";
        var leap = leapObject.AddComponent<EnemyLeapSkill>();
        Set(leap, "<SkillSo>k__BackingField", Copy(Scripts + "/Legacy/EnemySkills/Enemy03/Enemy03_Leap.asset",
            Scripts + $"/EnemySkills/Enemy18/{Name}_Leap.asset"));
        Set(leap, "<BaseCoolTime>k__BackingField", 3f);
        Set(leap, "bodyCollider", body);
        Set(leap, "maximumLandingHeightDifference", 0.8f);
        Set(leap, "useTargetHeightForLanding", true);
        Set(leap, "cooldownFromLanding", true);
        Set(leap, "fallbackAttackDelay", 0.6f);
        Set(leap, "postBlinkDelay", 0f);
        Set(leap, "blinkBeforeAttack", false);
        Set(leap, "leapHeight", 1.25f);
        Set(leap, "riseDuration", 0.28f);
        Set(leap, "diveDuration", 0.23f);
        Set(leap, "horizontalAcceleration", 0.32f);
        Set(leap, "damage", 12f);
        Set(leap, "landingRadius", 0.85f);
        Set(leap, "activeDuration", 0f);
        Set(leap, "recoveryTime", 0.75f);
        Set(leap, "landingParticlePrefab", splash);
        Set(leap, "poseRoot", pose.transform);
        Set(leap, "preparationShake", 0f);
        Set(leap, "attackAnimator", animator);
        Set(leap, "preparationStateName", "Base Layer.LeapPreparation");
        Set(leap, "flightStateName", "Base Layer.LeapRise");
        Set(leap, "fallStateName", "Base Layer.LeapFall");
        Set(leap, "recoveryStateName", "Base Layer.LeapLand");

        var skills = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>(true));
        var entry = skills.FindProperty("skills").GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("skill").objectReferenceValue = leap;
        entry.FindPropertyRelative("requireUsableToEnterAttack").boolValue = true;
        entry.FindPropertyRelative("maxRange").floatValue = 3.5f;
        entry.FindPropertyRelative("maxHeightDifference").floatValue = 0.8f;
        entry.FindPropertyRelative("requireClearPath").boolValue = false; // 도약 경로 검사가 벽과 천장을 확인합니다.
        skills.ApplyModifiedPropertiesWithoutUndo();
        ConfigureLeapWarningAndRetreat(root);
    }

    public static object ApplyLeapWarningAndRetreat()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureLeapWarningAndRetreat(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException("Could not update " + PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { enemy = PrefabPath, warning = WarningPath };
    }

    private static void ConfigureLeapWarningAndRetreat(GameObject root)
    {
        var leap = root.GetComponentInChildren<EnemyLeapSkill>();
        var warning = AssetDatabase.LoadAssetAtPath<GameObject>(WarningPath);
        if (warning == null)
        {
            const string source = Enemies + "/Legacy/Enemy/Enemy03/Enemy03_LeapWarning.prefab";
            Copy(source, WarningPath);
            var canvas = PrefabUtility.LoadPrefabContents(WarningPath);
            try
            {
                canvas.name = "Enemy18_LeapWarning";
                PrefabUtility.SaveAsPrefabAsset(canvas, WarningPath, out bool saved);
                if (!saved) throw new IOException("Could not save " + WarningPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(canvas); }
            warning = AssetDatabase.LoadAssetAtPath<GameObject>(WarningPath);
        }
        Set(leap, "useTargetHeightForLanding", true);
        Set(leap, "showWarningUntilLanding", true);
        Set(leap, "telegraphPrefab", warning.GetComponent<AttackTelegraphUI>());

        var skills = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>(true));
        var entry = skills.FindProperty("skills").GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("minRange").floatValue = MinimumLeapDistance;
        entry.FindPropertyRelative("allowCloseAttackWhenRetreatBlocked").boolValue = false;
        skills.ApplyModifiedPropertiesWithoutUndo();

        var retreat = root.GetComponentInChildren<EnemyKeepDistanceModule>(true);
        if (retreat == null)
        {
            var retreatObject = new GameObject("KeepDistanceModule") { layer = root.layer };
            retreatObject.transform.SetParent(root.transform, false);
            retreat = retreatObject.AddComponent<EnemyKeepDistanceModule>();
        }
        Set(retreat, "retreatDuringCooldownOf", leap);
        Set(retreat, "minimumDistance", MinimumLeapDistance);
        Set(retreat, "cooldownRetreatDistance", 8f);
        Set(retreat, "cooldownRetreatSpeedMultiplier", 1.25f);
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new InvalidOperationException("Missing field " + name);
        if (value is bool flag) field.boolValue = flag;
        else if (value is int integer) field.intValue = integer;
        else if (value is float number) field.floatValue = number;
        else if (value is string label) field.stringValue = label;
        else field.objectReferenceValue = (Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object Copy(string source, string destination)
    {
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
