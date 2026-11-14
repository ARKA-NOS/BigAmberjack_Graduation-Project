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

public static class SetupEnemy21
{
    private const string Name = "Enemy21_CutterAutomaton";
    private const string Art = "Assets/_Graphics/Assets/Enemy/Enemy21";
    private const string Sheets = Art + "/SHADOW Series - Noctislicer/png/";
    private const string Animations = "Assets/_Graphics/Animations/Enemy/Enemy21";
    private const string Enemies = "Assets/_Prefabs/Enemies";
    private const string Prefabs = Enemies + "/Enemy/Enemy21";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string Skills = Scripts + "/EnemySkills/Enemy21";
    private const string PrefabPath = Prefabs + "/" + Name + ".prefab";
    private const float PixelsPerUnit = 28f;

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
            throw new InvalidOperationException("Create Enemy21 in Edit Mode before its prefab exists.");

        MakeFolder(Animations);
        MakeFolder(Prefabs);
        MakeFolder(Skills);
        var idle = Slice("idle", 6, 47, 82);
        var move = Slice("move", 6, 47, 82);
        var attack = Slice("attack", 8, 46, 117);
        var death = Slice("death", 11, 47, 97);
        var armor = CreateArmor();
        var controller = CreateController(idle, move, attack, death, armor);
        var root = PrefabUtility.LoadPrefabContents(Enemies + "/Enemy/Enemy17/Enemy17_IntakeBiter.prefab");
        try
        {
            Configure(root, controller, idle[0], armor);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new IOException("Could not save " + PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath, states = controller.layers[0].stateMachine.states.Length };
    }

    private static Sprite[] Slice(string file, int count, int left, int right)
    {
        const int cellWidth = 128;
        const int cellHeight = 64;
        const int feet = 60;
        const int center = 64;
        const int top = 14;
        string path = Sheets + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != count * cellWidth || height != cellHeight)
            throw new InvalidDataException("Unexpected Noctislicer sheet dimensions " + path);

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(sprite => sprite.name, sprite => sprite.spriteID);
        var rects = Enumerable.Range(0, count).Select(index =>
        {
            string name = file + "_" + index;
            return new SpriteRect
            {
                name = name,
                spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(index * cellWidth + left, cellHeight - feet, right - left, feet - top),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((center - left) / (float)(right - left), 0f)
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

    private static Sprite CreateArmor()
    {
        string path = Art + "/Enemy21_WorkArmor.asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;
        string[] rows = { "   ####     ", "  #ssss##   ", " #hvssvsh#  ", " #sssssss#  ", " #ssbbbss#  ",
            "  #ssvss#   ", "   #sss#    ", "    ###     " };
        var texture = new Texture2D(rows[0].Length, rows.Length, TextureFormat.RGBA32, false)
        {
            name = "WorkArmorPixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < texture.width; x++)
            texture.SetPixel(x, texture.height - y - 1, rows[y][x] switch
            {
                '#' => new Color32(28, 30, 34, 255),
                'h' => new Color32(148, 133, 107, 255),
                's' => new Color32(94, 84, 72, 255),
                'b' => new Color32(65, 60, 55, 255),
                'v' => new Color32(185, 166, 128, 255),
                _ => Color.clear
            });
        texture.Apply();
        var armor = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, PixelsPerUnit);
        armor.name = "CutterWorkArmor";
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(armor, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return armor;
    }

    private static AnimatorController CreateController(Sprite[] idle, Sprite[] move, Sprite[] attack, Sprite[] death, Sprite armor)
    {
        string path = Animations + "/" + Name + ".controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var states = controller.layers[0].stateMachine;
        var idleState = Add("Idle", idle, 0.11f, true);
        var moveState = Add("Move", move, 0.085f, true);
        Add("DashPreparation", attack.Take(3).ToArray(), 0.20f);
        Add("Dash", move, 0.055f, true);
        Add("Slash", attack.Skip(3).ToArray(), 0.07f);
        var recovery = states.AddState("Recovery");
        recovery.motion = idleState.motion;
        recovery.writeDefaultValues = false;
        Add("Death", death, 0.08f);
        states.defaultState = idleState;
        Transition(idleState, moveState, AnimatorConditionMode.If);
        Transition(moveState, idleState, AnimatorConditionMode.IfNot);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState Add(string stateName, Sprite[] frames, float interval, bool loop = false)
        {
            var clip = new AnimationClip { frameRate = 100f };
            var keys = frames.Select((sprite, index) => Key(index * interval, sprite)).ToList();
            keys.Add(Key(frames.Length * interval, loop ? frames[0] : frames.Last()));
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                keys.ToArray());
            var plate = stateName == "Death" ? new[] { Key(0f, armor), Key(0.24f, null) } : new[] { Key(0f, armor) };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("WorkArmor", typeof(SpriteRenderer), "m_Sprite"), plate);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, $"{Animations}/{Name}_{stateName}.anim");
            var state = states.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = false;
            return state;
        }
    }

    private static ObjectReferenceKeyframe Key(float time, Sprite sprite) =>
        new ObjectReferenceKeyframe { time = time, value = sprite };

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(condition, 0f, "IsMoving");
    }

    private static void Configure(GameObject root, AnimatorController controller, Sprite idle, Sprite armor)
    {
        root.name = Name;
        var enemy = root.GetComponent<MeleeEnemy>();
        Set(enemy, "detectionRange", 9f);
        Set(enemy, "chaseReleaseRange", 13f);
        Set(enemy, "moveSpeed", 3.4f);
        Set(root.GetComponentInChildren<EnemyPatrolModule>(), "patrolSpeed", 3.4f);
        var animator = root.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = controller;
        var renderer = animator.GetComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.flipX = false;
        var plate = animator.transform.Find("IntakeGrille").GetComponent<SpriteRenderer>();
        plate.name = "WorkArmor";
        plate.sprite = armor;
        plate.transform.localPosition = new Vector3(-3f, 30f, 0f) / PixelsPerUnit;

        var hurtbox = root.GetComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(0.88f, 1.17f);
        hurtbox.offset = Vector2.up * 0.71f;
        var movement = root.GetComponentInChildren<EnemyMovementModule>();
        var body = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
        body.size = new Vector2(0.62f, 1.04f);
        body.offset = Vector2.up * 0.52f;
        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
        float knockback = new SerializedObject(reaction).FindProperty("knockbackSpeed").floatValue;
        Set(reaction, "knockbackSpeed", knockback * 0.65f);
        Set(reaction, "hitStunDuration", 0.20f);
        Set(reaction, "hurtAnimator", null);
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(), "maxBonusAmount", 5);
        var particles = root.GetComponentInChildren<EnemyParticleModule>();
        var effects = new SerializedObject(particles);
        var hit = effects.FindProperty("hitEffects").GetArrayElementAtIndex(0);
        hit.FindPropertyRelative("label").stringValue = "Metal Fragments";
        hit.FindPropertyRelative("prefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>(Enemies + "/Effects/HitMetalFragments.prefab");
        effects.FindProperty("deathBurstPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>(Enemies + "/Effects/DeathMetalBurst.prefab");
        effects.ApplyModifiedPropertiesWithoutUndo();
        particles.transform.localPosition = Vector3.up * 0.7f;

        var stats = Copy(Scripts + "/EnemyStatGroups/Enemy17_IntakeBiter_Stats.asset", Scripts + $"/EnemyStatGroups/{Name}_Stats.asset");
        var statData = new SerializedObject(stats);
        var entries = statData.FindProperty("<Stats>k__BackingField");
        float[] amounts = { 100f, 4f, 9f };
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
        skillObject.name = "GroundDashSlash";
        var dash = skillObject.AddComponent<EnemyGroundDashSlashSkill>();
        Set(dash, "<SkillSo>k__BackingField", Copy(Scripts + "/EnemySkills/Melee/Enemy17_IntakeBiter_Bite.asset", $"{Skills}/{Name}_DashSlash.asset"));
        Set(dash, "<BaseCoolTime>k__BackingField", 3.4f);
        Set(dash, "attackAnimator", animator);
        Set(dash, "fallbackAttackDelay", 0.65f);
        Set(dash, "postBlinkDelay", 0f);
        Set(dash, "blinkBeforeAttack", false);
        Set(dash, "telegraphPrefab", AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(Enemies + "/Effects/AttackTelegraphCanvas.prefab"));
        var skills = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>());
        var slot = skills.FindProperty("skills").GetArrayElementAtIndex(0);
        slot.FindPropertyRelative("skill").objectReferenceValue = dash;
        slot.FindPropertyRelative("maxRange").floatValue = 3.8f;
        slot.FindPropertyRelative("useHorizontalRange").boolValue = true;
        slot.FindPropertyRelative("maxHeightDifference").floatValue = 0.55f;
        slot.FindPropertyRelative("requireUsableToEnterAttack").boolValue = true;
        slot.FindPropertyRelative("requireGrounded").boolValue = true;
        slot.FindPropertyRelative("requireClearPath").boolValue = true;
        skills.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new InvalidOperationException("Missing field " + name);
        if (value is float number) field.floatValue = number;
        else if (value is int count) field.intValue = count;
        else if (value is bool flag) field.boolValue = flag;
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
