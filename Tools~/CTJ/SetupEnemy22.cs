using System;
using System.IO;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupEnemy22
{
    private const string Name = "Enemy22_LoadingCrusher";
    private const string Sheets = "Assets/_Graphics/Assets/Enemy/Enemy22/SHADOW Series - Duskclaw/png/";
    private const string Animations = "Assets/_Graphics/Animations/Enemy/Enemy22";
    private const string Enemies = "Assets/_Prefabs/Enemies";
    private const string Prefabs = Enemies + "/Enemy/Enemy22";
    private const string Effects = Enemies + "/Effects";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string Skills = Scripts + "/EnemySkills/Enemy22";
    private const string Prefab = Prefabs + "/" + Name + ".prefab";
    private const float PixelsPerUnit = 25f;

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(Prefab) != null)
            throw new InvalidOperationException("Create Enemy22 in Edit Mode before its prefab exists.");

        MakeFolder(Animations);
        MakeFolder(Prefabs);
        MakeFolder(Skills);
        var idle = Slice("idle", 8, 8, 35);
        var walk = Slice("walk", 10, 10, 36);
        var attack = Slice("attack", 8, 0, 91);
        var toss = Slice("throw", 10, 0, 44);
        var death = Slice("death", 13, 8, 40);
        var controller = CreateController(idle, walk, attack, toss, death);
        var projectile = CreateScrapProjectile();
        var landingWarning = CreateWarning("Enemy22_ImpactWarning", Enemies + "/Enemy/Enemy18/Enemy18_LeapWarning.prefab",
            new Color(1f, 0.18f, 0.22f, 0.27f));
        var crushWarning = CreateWarning("Enemy22_CrushWarning", Effects + "/AttackTelegraphCanvas.prefab",
            new Color(1f, 0.73f, 0.30f, 0.24f));
        var root = PrefabUtility.LoadPrefabContents(Enemies + "/Enemy/Enemy17/Enemy17_IntakeBiter.prefab");
        try
        {
            Configure(root, controller, idle[0], projectile, landingWarning, crushWarning);
            Save(root, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = Prefab, states = controller.layers[0].stateMachine.states.Length };
    }

    private static Sprite[] Slice(string file, int count, int left, int right)
    {
        const int width = 176;
        const int height = 39;
        const int originX = 22;
        string path = Sheets + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if (sourceWidth != count * width || sourceHeight != height)
            throw new InvalidDataException("Unexpected Duskclaw sheet dimensions " + path);

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(sprite => sprite.name, sprite => sprite.spriteID);
        var frames = Enumerable.Range(0, count).Select(index =>
        {
            string name = file + "_" + index;
            return new SpriteRect
            {
                name = name,
                spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(index * width + left, 0, right - left, height),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((originX - left) / (float)(right - left), 0f)
            };
        }).ToArray();
        provider.SetSpriteRects(frames);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(frames.Select(frame => new SpriteNameFileIdPair(frame.name, frame.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        return frames.Select(frame => sprites[frame.name]).ToArray();
    }

    private static AnimatorController CreateController(Sprite[] idle, Sprite[] walk, Sprite[] attack, Sprite[] toss, Sprite[] death)
    {
        string path = Animations + "/" + Name + ".controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idleState = Add("Idle", idle, 0.10f, true);
        var moveState = Add("Move", walk, 0.085f, true);
        Add("CrushPreparation", attack.Take(2).ToArray(), 0.375f);
        Add("Crush", attack.Skip(2).ToArray(), 0.065f);
        Add("ThrowPreparation", toss.Take(3).ToArray(), 0.25f);
        Add("Throw", toss.Skip(3).ToArray(), 0.075f);
        Add("Recovery", new[] { idle[3], idle[1], idle[0] }, 0.14f);
        Add("Death", death, 0.075f);
        machine.defaultState = idleState;
        Transition(idleState, moveState, AnimatorConditionMode.If);
        Transition(moveState, idleState, AnimatorConditionMode.IfNot);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState Add(string name, Sprite[] frames, float interval, bool loop = false)
        {
            var clip = new AnimationClip { frameRate = 200f };
            var keys = frames.Select((frame, index) => Key(index * interval, frame)).ToList();
            keys.Add(Key(frames.Length * interval, loop ? frames[0] : frames.Last()));
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, $"{Animations}/{Name}_{name}.anim");
            var state = machine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = false;
            return state;
        }
    }

    private static ObjectReferenceKeyframe Key(float time, Object value) =>
        new ObjectReferenceKeyframe { time = time, value = value };

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(condition, 0f, "IsMoving");
    }

    public static EnemyLobProjectile CreateScrapProjectile()
    {
        const string path = Prefabs + "/Enemy22_Scrap.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root = existing != null ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Enemy22_Scrap");
        try
        {
            if (!root.TryGetComponent(out EnemyLobProjectile projectile))
                projectile = root.AddComponent<EnemyLobProjectile>();
            var visual = root.transform.Find("Metal Plate");
            if (visual == null)
            {
                visual = new GameObject("Metal Plate").transform;
                visual.SetParent(root.transform, false);
            }
            if (visual.TryGetComponent(out MeshFilter mesh)) Object.DestroyImmediate(mesh);
            if (visual.TryGetComponent(out MeshRenderer meshRenderer)) Object.DestroyImmediate(meshRenderer);
            if (!visual.TryGetComponent(out SpriteRenderer renderer))
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadScrapSprite();
            renderer.sortingOrder = 10;
            visual.localScale = new Vector3(1.3f, 1.3f, 1f);
            Set(projectile, "visual", visual);
            Set(projectile, "impactEffect", AssetDatabase.LoadAssetAtPath<ParticleSystem>(Effects + "/HitMetalFragments.prefab"));
            return Save(root, path).GetComponent<EnemyLobProjectile>();
        }
        finally
        {
            if (existing != null) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    private static Sprite LoadScrapSprite()
    {
        const string path = "Assets/_Graphics/Assets/Enemy/Enemy22/Enemy22_ScrapPixels.asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;
        string[] pixels = { "  ####   ", " #llll#  ", "#lssls#  ", "#lshss###", "#ssshssb#", "#bssssbb#", " #bbbb## ", "  ####   " };
        var texture = new Texture2D(pixels[0].Length, pixels.Length, TextureFormat.RGBA32, false)
        {
            name = "ScrapPixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
            texture.SetPixel(x, texture.height - 1 - y, pixels[y][x] switch
            {
                '#' => new Color32(46, 43, 42, 255),
                'l' => new Color32(188, 183, 168, 255),
                's' => new Color32(124, 123, 117, 255),
                'h' => new Color32(164, 144, 114, 255),
                'b' => new Color32(79, 78, 76, 255),
                _ => Color.clear
            });
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, PixelsPerUnit);
        sprite.name = "Scrap";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return sprite;
    }

    private static AttackTelegraphUI CreateWarning(string name, string source, Color color)
    {
        string path = Prefabs + "/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(path);
        if (existing != null) return existing;
        Copy(source, path);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = name;
            Set(root.GetComponent<AttackTelegraphUI>(), "areaColor", color);
            return Save(root, path).GetComponent<AttackTelegraphUI>();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Configure(GameObject root, AnimatorController controller, Sprite idle,
        EnemyLobProjectile projectile, AttackTelegraphUI landingWarning, AttackTelegraphUI crushWarning)
    {
        root.name = Name;
        var enemy = root.GetComponent<MeleeEnemy>();
        Set(enemy, "detectionRange", 10f);
        Set(enemy, "chaseReleaseRange", 14f);
        Set(enemy, "moveSpeed", 2.2f);
        var patrol = root.GetComponentInChildren<EnemyPatrolModule>();
        Set(patrol, "patrolSpeed", 2.2f);
        Set(patrol, "returnToSpawnBeforePatrol", true);

        var animator = root.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = controller;
        var renderer = animator.GetComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.flipX = false;
        Object.DestroyImmediate(animator.transform.Find("IntakeGrille").gameObject);
        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(0.88f, 1.52f);
        hurtbox.offset = Vector2.up * 0.76f;
        var movement = root.GetComponentInChildren<EnemyMovementModule>();
        var body = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        body.size = new Vector2(0.62f, 1.38f);
        body.offset = Vector2.up * 0.69f;
        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
        Set(reaction, "knockbackSpeed", new SerializedObject(reaction).FindProperty("knockbackSpeed").floatValue * 0.35f);
        Set(reaction, "knockbackDuration", 0.15f);
        Set(reaction, "hitStunDuration", 0.15f);
        Set(reaction, "hurtAnimator", null);
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(), "maxBonusAmount", 8);

        var particles = root.GetComponentInChildren<EnemyParticleModule>();
        particles.transform.localPosition = Vector3.up * 0.78f;
        var particleData = new SerializedObject(particles);
        var hit = particleData.FindProperty("hitEffects").GetArrayElementAtIndex(0);
        hit.FindPropertyRelative("label").stringValue = "Metal Fragments";
        hit.FindPropertyRelative("prefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>(Effects + "/HitMetalFragments.prefab");
        particleData.FindProperty("deathBurstPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>(Effects + "/DeathMetalBurst.prefab");
        particleData.ApplyModifiedPropertiesWithoutUndo();

        var stats = Copy(Scripts + "/EnemyStatGroups/Enemy17_IntakeBiter_Stats.asset", Scripts + $"/EnemyStatGroups/{Name}_Stats.asset");
        var statData = new SerializedObject(stats);
        var values = statData.FindProperty("<Stats>k__BackingField");
        float[] amounts = { 170f, 6f, 16f };
        for (int i = 0; i < amounts.Length; i++)
            values.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = amounts[i];
        statData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        var statModule = root.GetComponentsInChildren<MonoBehaviour>(true).Single(component => component.GetType().Name == "StatModule");
        Set(statModule, "baseStats", stats);

        var crush = root.GetComponentInChildren<EnemyTimedMeleeSkill>();
        crush.name = "ForwardCrush";
        var box = (BoxCollider2D)new SerializedObject(crush).FindProperty("attackHitbox").objectReferenceValue;
        box.size = new Vector2(1.7f, 1.3f);
        box.offset = new Vector2(0.85f, 0.65f);
        Set(crush, "damage", 20f);
        Set(crush, "activeDuration", 0.065f);
        Set(crush, "checkTerrainAtHit", true);
        Set(crush, "preparationStateName", "Base Layer.CrushPreparation");
        Set(crush, "attackStateName", "Base Layer.Crush");
        Set(crush, "recoveryStateName", "Base Layer.Recovery");
        ConfigureSkill(crush, "Crush", 2.8f, crushWarning);

        var skillModule = root.GetComponentInChildren<EnemySkillModule>();
        var throwObject = new GameObject("ScrapThrow") { layer = root.layer };
        throwObject.transform.SetParent(skillModule.transform, false);
        var toss = throwObject.AddComponent<EnemyLobSkill>();
        var throwPoint = new GameObject("ThrowPoint") { layer = root.layer };
        throwPoint.transform.SetParent(box.transform.parent, false);
        throwPoint.transform.localPosition = new Vector3(0.53f, 0.94f, 0f);
        Set(toss, "attackAnimator", animator);
        Set(toss, "throwPoint", throwPoint.transform);
        Set(toss, "projectilePrefab", projectile);
        ConfigureSkill(toss, "ScrapThrow", 4.6f, landingWarning);

        var skills = new SerializedObject(skillModule);
        var entries = skills.FindProperty("skills");
        entries.arraySize = 2;
        ConfigureSlot(entries.GetArrayElementAtIndex(0), crush, 0f, 1.7f, 0.55f, true);
        ConfigureSlot(entries.GetArrayElementAtIndex(1), toss, 2.5f, 6f, -1f, false);
        skills.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSkill(EnemyAttackSkill skill, string name, float cooldown, AttackTelegraphUI warning)
    {
        Set(skill, "<SkillSo>k__BackingField", Copy(Scripts + "/EnemySkills/Melee/Enemy17_IntakeBiter_Bite.asset", $"{Skills}/{Name}_{name}.asset"));
        Set(skill, "<BaseCoolTime>k__BackingField", cooldown);
        Set(skill, "fallbackAttackDelay", 0.75f);
        Set(skill, "postBlinkDelay", 0f);
        Set(skill, "blinkBeforeAttack", false);
        Set(skill, "recoveryTime", 1f);
        Set(skill, "telegraphPrefab", warning);
    }

    private static void ConfigureSlot(SerializedProperty slot, AbstractSkill skill, float min, float max, float height, bool clearPath)
    {
        slot.FindPropertyRelative("skill").objectReferenceValue = skill;
        slot.FindPropertyRelative("useSkill").boolValue = true;
        slot.FindPropertyRelative("useForAutomaticSelection").boolValue = true;
        slot.FindPropertyRelative("requireUsableToEnterAttack").boolValue = !clearPath;
        slot.FindPropertyRelative("minRange").floatValue = min;
        slot.FindPropertyRelative("maxRange").floatValue = max;
        slot.FindPropertyRelative("useHorizontalRange").boolValue = clearPath;
        slot.FindPropertyRelative("maxHeightDifference").floatValue = height;
        slot.FindPropertyRelative("requireGrounded").boolValue = true;
        slot.FindPropertyRelative("requireClearPath").boolValue = clearPath;
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new InvalidOperationException("Missing field " + name);
        if (value is float number) field.floatValue = number;
        else if (value is int count) field.intValue = count;
        else if (value is bool flag) field.boolValue = flag;
        else if (value is string label) field.stringValue = label;
        else if (value is Color color) field.colorValue = color;
        else field.objectReferenceValue = (Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject Save(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
        if (!saved) throw new IOException("Could not save " + path);
        return prefab;
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
