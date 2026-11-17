using System;
using System.IO;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.UI.Enemy;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23
{
    private const string Name = "Boss23_ProductionControlDrone";
    private const string Art = "Assets/_Graphics/Assets/Enemy/Enemy23";
    private const string Sheets = Art + "/SHADOW Series - Voidrone Boss/";
    private const string Animations = "Assets/_Graphics/Animations/Enemy/Enemy23";
    private const string Enemies = "Assets/_Prefabs/Enemies";
    private const string Prefabs = Enemies + "/Bosses/Enemy23";
    private const string Scripts = "Assets/_Scripts/Agent/Enemies";
    private const string Skills = Scripts + "/EnemySkills/Enemy23";
    private const string EnemyTemplate = Enemies + "/Enemy/Enemy17/Enemy17_IntakeBiter.prefab";
    private const string Effects = Enemies + "/Effects";
    private const float PixelsPerUnit = 28f;
    private static readonly Vector2 NumberPosition = new Vector2(-17.5f, 10.5f) / PixelsPerUnit;
    private static readonly Vector2 LampPosition = new Vector2(-11.5f, 18.5f) / PixelsPerUnit;

    public static object Create()
    {
        string path = Prefabs + "/" + Name + ".prefab";
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadMainAssetAtPath(path) != null)
            throw new InvalidOperationException("Create Boss23 in Edit Mode before its prefab exists.");
        Folder(Prefabs);
        Folder(Animations);
        Folder(Skills);
        var idle = Slice("normal state/idle", 128, 96, 36, 20, 92, 64, 64, 46);
        var controller = Controller(idle);
        BakeBodyMarkings(controller);
        const string bombPath = "Spetial Attacks/shotting bomb/";
        var bombSprite = Slice(bombPath + "bomb", 11, 11, 0, 0, 11, 11, 5.5f, 5.5f)[0];
        var blast = ImportBombExplosion();
        var bomb = CreateBomb(bombSprite, blast);
        var round = CreateRound();
        var shapes = CreateArt();
        var circleWarning = CreateCircleWarning();
        var root = PrefabUtility.LoadPrefabContents(EnemyTemplate);
        try
        {
            Configure(root, controller, idle[0], round, bomb, shapes, circleWarning);
            Save(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = path, states = controller.layers[0].stateMachine.states.Length };
    }

    private static Sprite[] ImportBombExplosion() =>
        Slice("Spetial Attacks/shotting bomb/explosion", 86, 22, 0, 0, 86, 22, 43f, 21f);

    private static Sprite[] Slice(string file, int cellWidth, int cellHeight, int left, int top, int right, int bottom,
        float originX, float originY)
    {
        string path = Sheets + file + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int w, out int h);
        if (w % cellWidth != 0 || h != cellHeight) throw new IOException("Unexpected sheet size " + path);
        string prefix = Path.GetFileName(file);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        var rects = Enumerable.Range(0, w / cellWidth).Select(index =>
        {
            string name = $"{prefix}_{index}";
            return new SpriteRect
            {
                name = name,
                spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(index * cellWidth + left, h - bottom, right - left, bottom - top),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((originX - left) / (right - left), (bottom - originY) / (bottom - top))
            };
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(w));
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        return rects.Select(rect => sprites[rect.name]).ToArray();
    }

    private static AnimatorController Controller(Sprite[] idle)
    {
        string path = Animations + "/" + Name + ".controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        machine.defaultState = Add("Idle", idle, 0.095f, true);
        Add("Charge", Slice("normal state/charging", 128, 96, 35, 24, 98, 64, 64, 46), 0.06f);
        Add("Shot", Slice("normal state/shot", 128, 96, 32, 24, 123, 64, 64, 46), 0.08f);
        Add("RapidShot", Slice("normal state/speedy shots", 128, 96, 32, 24, 123, 64, 64, 46), 0.09f);
        Add("Rise", Slice("flying up/transition", 128, 96, 35, 19, 93, 65, 64, 46), 0.09f);
        Add("Descend", Slice("flying up/transition back", 128, 96, 35, 19, 93, 65, 64, 46), 0.09f);
        var laser = Slice("Spetial Attacks/laser/robot only", 256, 96, 32, 22, 98, 70, 64, 46);
        Add("LaserCharge", laser.Take(9).ToArray(), 0.1f);
        Add("Laser", laser.Skip(9).ToArray(), 0.05f);
        var bomb = Slice("Spetial Attacks/shotting bomb/robot only", 256, 96, 34, 19, 98, 67, 64, 46);
        Add("BombCharge", bomb.Take(9).ToArray(), 0.085f);
        Add("Bomb", bomb.Skip(9).ToArray(), 0.06f);
        Add("Death", Slice("normal state/death", 467, 129, 1, 25, 119, 102, 64, 72).Take(11).ToArray(), 0.09f);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        return controller;

        AnimatorState Add(string name, Sprite[] frames, float time, bool loop = false)
        {
            var clip = new AnimationClip { frameRate = 200f };
            SetSpriteFrames(clip, frames, time, loop);
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

    private static void SetSpriteFrames(AnimationClip clip, Sprite[] frames, float interval, bool loop = false)
    {
        var keys = frames.Select((frame, i) => new ObjectReferenceKeyframe { time = i * interval, value = frame }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = frames.Length * interval, value = loop ? frames[0] : frames.Last() });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
            keys.ToArray());
    }

    public static object UpdateAttackAnimations()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Update the drone animation assets in Edit Mode.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{Animations}/{Name}.controller");
        RestoreAttackTail("Laser", "Spetial Attacks/laser/robot only.png", 11, 0.05f);
        RestoreAttackTail("Bomb", "Spetial Attacks/shotting bomb/robot only.png", 12, 0.06f);
        BakeBodyMarkings(controller);

        string path = $"{Prefabs}/{Name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform facing = root.transform.Find("Visual");
            Transform body = facing.Find("HitVisual");
            Place("ProcessNumber", NumberPosition);
            Place("AlarmLamp", LampPosition);
            Save(root, path);

            void Place(string name, Vector2 position)
            {
                Transform marking = body.Find(name) ?? facing.Find(name);
                marking.SetParent(body, false);
                marking.localPosition = position;
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = path, animatedClips = controller.animationClips.Length - 1 };

        void RestoreAttackTail(string state, string sheet, int count, float interval)
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(Sheets + sheet).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Animations}/{Name}_{state}.anim");
            SetSpriteFrames(clip, Enumerable.Range(9, count).Select(i => frames[$"robot only_{i}"]).ToArray(), interval);
        }
    }

    private static void BakeBodyMarkings(AnimatorController controller)
    {
        // Offsets follow the head cel in each source frame, in source pixels (+Y is down).
        Vector2[] idle = Offsets(0, 2, 5, 5, 4, -2, -5, -3);
        Vector2[] charge = Offsets(0, 1, 2, 3, 3, 4, 6, 8, 7);
        var poses = new System.Collections.Generic.Dictionary<string, Vector2[]>
        {
            ["Idle"] = idle,
            ["Charge"] = charge,
            ["Shot"] = new Vector2[5],
            ["RapidShot"] = new Vector2[4],
            ["Rise"] = Offsets(2, 5, 6, 0, -5, -5),
            ["Descend"] = Offsets(-5, -5, 0, 6, 5, 2),
            ["LaserCharge"] = charge,
            ["Laser"] = new[] { new Vector2(-2, 0), new Vector2(-1, 0), new Vector2(0, 2),
                new Vector2(1, 3), new Vector2(0, -1), new Vector2(0, 5), new Vector2(0, 9),
                new Vector2(-1, 9), new Vector2(0, 3), new Vector2(0, -2), Vector2.zero },
            ["BombCharge"] = charge,
            ["Bomb"] = new[] { new Vector2(-2, -1), new Vector2(-3, -2), new Vector2(-2, -1), Vector2.zero }
                .Concat(idle).ToArray()
        };

        foreach (var state in controller.layers[0].stateMachine.states)
        {
            if (!poses.TryGetValue(state.state.name, out Vector2[] offsets)) continue;
            var clip = (AnimationClip)state.state.motion;
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
            if (frames.Length != offsets.Length + 1)
                throw new InvalidOperationException($"Body marking keys do not match {clip.name}.");
            AddKeys("ProcessNumber", NumberPosition);
            AddKeys("AlarmLamp", LampPosition);
            AssetDatabase.SaveAssetIfDirty(clip);

            void AddKeys(string path, Vector2 anchor)
            {
                var positions = frames.Select((frame, i) =>
                {
                    int index = i < offsets.Length ? i : clip.isLooping ? 0 : offsets.Length - 1;
                    return anchor + new Vector2(offsets[index].x, -offsets[index].y) / PixelsPerUnit;
                }).ToArray();
                SetCurve("m_LocalPosition.x", positions.Select(position => position.x));
                SetCurve("m_LocalPosition.y", positions.Select(position => position.y));

                void SetCurve(string property, System.Collections.Generic.IEnumerable<float> values)
                {
                    var curve = new AnimationCurve(values.Select((value, i) => new Keyframe(frames[i].time, value)).ToArray());
                    for (int i = 0; i < curve.length; i++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                        AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                    }
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
                }
            }
        }

        static Vector2[] Offsets(params int[] y) => y.Select(value => new Vector2(0f, value)).ToArray();
    }

    private static void Configure(GameObject root, AnimatorController controller, Sprite idle, EnemyProjectile round,
        EnemyLobProjectile bomb, Sprite[] art, AttackTelegraphUI circleWarning)
    {
        root.name = Name;
        Object.DestroyImmediate(root.GetComponent<MeleeEnemy>());
        Object.DestroyImmediate(root.GetComponentInChildren<EnemyMovementModule>().gameObject);
        Object.DestroyImmediate(root.GetComponentInChildren<EnemyPatrolModule>().gameObject);
        Object.DestroyImmediate(root.transform.Find("TerrainBody").gameObject);
        Object.DestroyImmediate(root.GetComponentsInChildren<BoxCollider2D>(true).Single(box => box.name == "AttackHitbox").gameObject);
        var module = root.GetComponentInChildren<EnemySkillModule>();
        Object.DestroyImmediate(module.GetComponentInChildren<EnemyTimedMeleeSkill>().gameObject);

        var drone = root.AddComponent<ProductionControlDrone>();
        Transform facing = root.transform.Find("Visual");
        var body = facing.Find("HitVisual").GetComponent<SpriteRenderer>();
        Object.DestroyImmediate(body.GetComponent<EnemyLocomotionAnimator>());
        Object.DestroyImmediate(body.transform.Find("IntakeGrille").gameObject);
        // Clear the old Sprite first. Different sheets can share the same imported sprite local ID.
        body.sprite = null;
        body.sprite = idle;
        body.flipX = false;
        var animator = body.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var hitbox = root.GetComponent<BoxCollider2D>();
        hitbox.size = new Vector2(1.88f, 1.31f);
        hitbox.offset = Vector2.zero;
        var rigidbody = root.GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;

        Set(drone, "displayName", "공정 통제 드론");
        Set(drone, "detectionRange", 12f);
        Set(drone, "chaseReleaseRange", 16f);
        Set(drone, "moveSpeed", 2.4f);
        Set(drone, "facingRoot", facing);
        Set(drone, "deathAnimator", animator);
        Child(root.transform, "FlightModule").gameObject.AddComponent<BossFlightModule>();
        var arena = Child(root.transform, "ArenaModule").gameObject.AddComponent<BossArenaModule>();
        Set(arena, "detectFloor", true);
        Set(arena, "floorDistance", 1.05f);
        Set(arena, "laserHeight", 3.5f);
        var healthBar = Child(root.transform, "HealthBarModule").gameObject.AddComponent<BossHealthBarModule>();
        Set(healthBar, "healthBarPrefab", AssetDatabase.LoadAssetAtPath<BossHealthBarUI>(
            Enemies + "/Bosses/EyeBoss/BossHealthBarCanvas.prefab"));
        var visual = Child(root.transform, "DroneVisualModule").gameObject.AddComponent<DroneVisualModule>();
        Set(visual, "animator", animator);
        Set(visual, "shadow", Renderer(root.transform, "FloorShadow", art[0], new Vector2(0f, -1f), 7));
        Set(visual, "alarmLamp", Renderer(body.transform, "AlarmLamp", art[1], LampPosition, 11));
        var number = Renderer(body.transform, "ProcessNumber", art[2], NumberPosition, 11);
        Set(visual, "processNumber", number);
        var flash = Renderer(facing, "GunFlash", art[3], new Vector2(0.96f, -0.20f), 11);
        flash.enabled = false;
        Set(visual, "gunFlash", flash);
        Set(drone, "visual", visual);

        var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
        Set(reaction, "enableKnockback", false);
        Set(reaction, "knockbackSpeed", 0f);
        Set(reaction, "knockbackDuration", 0f);
        Set(reaction, "hitStunDuration", 0f);
        Set(reaction, "cancelAttackOnHit", false);
        Set(reaction, "enableScalePulse", false);
        Set(reaction, "hurtAnimator", null);
        var particles = root.GetComponentInChildren<EnemyParticleModule>();
        particles.transform.localPosition = Vector3.zero;
        var particleData = new SerializedObject(particles);
        var hit = particleData.FindProperty("hitEffects").GetArrayElementAtIndex(0);
        hit.FindPropertyRelative("label").stringValue = "Metal Fragments";
        hit.FindPropertyRelative("prefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>(Effects + "/HitMetalFragments.prefab");
        particleData.FindProperty("deathBurstPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ParticleSystem>(Effects + "/DeathMetalBurst.prefab");
        particleData.ApplyModifiedPropertiesWithoutUndo();
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(), "maxBonusAmount", 20);
        Set(root.GetComponentInChildren<EnemyCurrencyRewardModule>(), "spawnOffset", new Vector2(0f, -0.28f));
        var stats = Copy(Scripts + "/EnemyStatGroups/Enemy17_IntakeBiter_Stats.asset", $"{Scripts}/EnemyStatGroups/{Name}_Stats.asset");
        var statObject = new SerializedObject(stats);
        var entries = statObject.FindProperty("<Stats>k__BackingField");
        float[] values = { 550f, 8f, 45f };
        for (int i = 0; i < values.Length; i++)
            entries.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = values[i];
        statObject.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        Set(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "StatModule"), "baseStats", stats);

        var shotPoint = Child(facing, "ShotPoint", new Vector2(1.0f, -0.2f));
        var normal = Salvo("RegularSalvo", "Shot", 10f, 14f, 1);
        var rapid = Salvo("RapidSalvo", "RapidShot", 15f, 8f, 3);
        var down = Child(module.transform, "DownwardLaser").gameObject.AddComponent<DroneDownwardLaserSkill>();
        var beam = Renderer(root.transform, "LaserBeam", art[4], Vector2.zero, 12);
        beam.enabled = false;
        Set(down, "beam", beam);
        Set(down, "visual", visual);
        Set(down, "muzzle", Child(root.transform, "LaserPoint", new Vector2(0f, -0.15f)));
        SetupSkill(down, "Laser", 0.9f, 5.5f, AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(Effects + "/AttackTelegraphCanvas.prefab"));

        var bombSkill = Child(module.transform, "UpwardBomb").gameObject.AddComponent<DroneBombSkill>();
        Set(bombSkill, "visual", visual);
        Set(bombSkill, "bombPrefab", bomb);
        var bombMuzzle = Child(root.transform, "BombPoint", Vector2.up * 0.56f);
        var preview = Renderer(bombMuzzle, "BombPreview", art[5], Vector2.zero, 12);
        preview.enabled = false;
        Set(bombSkill, "muzzle", bombMuzzle);
        Set(bombSkill, "preview", preview);
        SetupSkill(bombSkill, "Bomb", 0.8f, 4.8f, circleWarning);

        Set(drone, "regularSalvo", normal);
        Set(drone, "rapidSalvo", rapid);
        Set(drone, "laser", down);
        Set(drone, "bomb", bombSkill);
        var skillData = new SerializedObject(module);
        var slots = skillData.FindProperty("skills");
        EnemyAttackSkill[] patterns = { normal, rapid, down, bombSkill };
        slots.arraySize = patterns.Length;
        for (int i = 0; i < slots.arraySize; i++)
        {
            var slot = slots.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("skill").objectReferenceValue = patterns[i];
            slot.FindPropertyRelative("useSkill").boolValue = true;
            slot.FindPropertyRelative("useForAutomaticSelection").boolValue = false;
        }
        skillData.ApplyModifiedPropertiesWithoutUndo();

        DroneSalvoSkill Salvo(string name, string fireState, float speed, float damage, int count)
        {
            var salvo = Child(module.transform, name).gameObject.AddComponent<DroneSalvoSkill>();
            Set(salvo, "visual", visual);
            Set(salvo, "muzzle", shotPoint);
            Set(salvo, "projectilePrefab", round);
            Set(salvo, "fireState", fireState);
            Set(salvo, "projectileSpeed", speed);
            Set(salvo, "damage", damage);
            Set(salvo, "projectilesPerSalvo", count);
            SetupSkill(salvo, name, 0.55f, 2.4f, null);
            return salvo;
        }
    }

    private static void SetupSkill(EnemyAttackSkill skill, string name, float delay, float cooldown, AttackTelegraphUI warning)
    {
        Set(skill, "<SkillSo>k__BackingField", Copy(Scripts + "/EnemySkills/Melee/Enemy17_IntakeBiter_Bite.asset", $"{Skills}/{Name}_{name}.asset"));
        Set(skill, "<BaseCoolTime>k__BackingField", cooldown);
        Set(skill, "fallbackAttackDelay", delay);
        Set(skill, "postBlinkDelay", 0f);
        Set(skill, "blinkBeforeAttack", false);
        Set(skill, "telegraphPrefab", warning);
    }

    private static EnemyProjectile CreateRound()
    {
        const string path = Prefabs + "/Boss23_Round.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(path);
        if (existing != null) return existing;
        var root = new GameObject("Boss23_Round");
        try
        {
            var projectile = root.AddComponent<EnemyProjectile>();
            root.GetComponent<CircleCollider2D>().radius = 0.09f;
            var sprite = root.AddComponent<SpriteRenderer>();
            sprite.sprite = Pixel("Round", new[] { "   ggg  ", "gggwwwwg", "gggwwwwg", "   ggg  " }, new Color32(81, 230, 158, 255));
            sprite.sortingOrder = 12;
            return Save(root, path).GetComponent<EnemyProjectile>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static EnemyLobProjectile CreateBomb(Sprite sprite, Sprite[] explosion)
    {
        const string path = Prefabs + "/Boss23_Bomb.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyLobProjectile>(path);
        if (existing != null) return existing;
        var root = new GameObject("Boss23_Bomb");
        try
        {
            var bomb = root.AddComponent<EnemyLobProjectile>();
            var visual = Renderer(root.transform, "BombVisual", sprite, Vector2.zero, 12);
            visual.transform.localScale = Vector3.one * 1.65f;
            Set(bomb, "visual", visual.transform);
            Set(bomb, "flightDuration", 0.96f);
            Set(bomb, "arcHeight", 4.6f);
            Set(bomb, "spinSpeed", 440f);
            Set(bomb, "impactFrames", explosion);
            Set(bomb, "impactFramesPerSecond", 14f);
            Set(bomb, "impactEffect", AssetDatabase.LoadAssetAtPath<ParticleSystem>(Effects + "/HitMetalFragments.prefab"));
            Set(bomb, "impactEffectScale", 0.95f);
            return Save(root, path).GetComponent<EnemyLobProjectile>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static AttackTelegraphUI CreateCircleWarning()
    {
        const string path = Prefabs + "/Boss23_BombWarning.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(path);
        if (existing != null) return existing;
        Copy(Enemies + "/Enemy/Enemy18/Enemy18_LeapWarning.prefab", path);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = "Boss23_BombWarning";
            return Save(root, path).GetComponent<AttackTelegraphUI>();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Sprite[] CreateArt()
    {
        var shadow = Pixel("Shadow", new[] { "  gggggggggg  ", "gggggggggggggg", "gggggggggggggg", "  gggggggggg  " }, Color.white);
        var lamp = Pixel("Lamp", new[] { "  gg  ", " gwwg ", " gggg ", " bbbb " }, new Color32(251, 106, 45, 255));
        var number = Pixel("Process23", new[] { "ggg ggg", "  g   g", "ggg ggg", "g     g", "ggg ggg" }, new Color32(194, 221, 187, 255));
        var flash = Pixel("Muzzle", new[] { "  g  ", " ggg ", "ggwgg", " ggg ", "  g  " }, new Color32(114, 255, 171, 255));
        var beam = Pixel("Beam", Enumerable.Repeat("gggwwwwwggg", 16).ToArray(), new Color32(98, 255, 165, 152));
        var preview = Pixel("BombOutline", new[] { "   gggg   ", "  g    g  ", " g      g ", "g        g", "g        g", "g        g", "g        g", " g      g ", "  g    g  ", "   gggg   " }, new Color32(186, 255, 190, 255));
        return new[] { shadow, lamp, number, flash, beam, preview };
    }

    private static Sprite Pixel(string name, string[] rows, Color tint)
    {
        string path = $"{Art}/Boss23_{name}.asset";
        var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;
        var texture = new Texture2D(rows[0].Length, rows.Length, TextureFormat.RGBA32, false)
        {
            name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
            texture.SetPixel(x, texture.height - y - 1, rows[y][x] switch
            {
                'g' => tint, 'w' => new Color32(222, 255, 231, 255), 'b' => new Color32(66, 64, 56, 255), _ => Color.clear
            });
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, PixelsPerUnit);
        sprite.name = name;
        AssetDatabase.CreateAsset(texture, path);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssetIfDirty(texture);
        return sprite;
    }

    private static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, Vector2 position, int order)
    {
        var renderer = Child(parent, name, position).gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Transform Child(Transform parent, string name, Vector2 position = default)
    {
        var child = new GameObject(name) { layer = parent.gameObject.layer };
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        return child.transform;
    }

    private static Object Copy(string from, string to)
    {
        var existing = AssetDatabase.LoadMainAssetAtPath(to);
        if (existing != null) return existing;
        if (!AssetDatabase.CopyAsset(from, to)) throw new IOException("Could not copy " + from);
        return AssetDatabase.LoadMainAssetAtPath(to);
    }

    private static GameObject Save(GameObject root, string path)
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
        if (!saved) throw new IOException("Could not save " + path);
        return asset;
    }

    private static void Set(Object target, string name, object value)
    {
        var data = new SerializedObject(target);
        var field = data.FindProperty(name) ?? throw new InvalidOperationException("Missing field " + name);
        switch (value)
        {
            case bool flag: field.boolValue = flag; break;
            case int count: field.intValue = count; break;
            case float number: field.floatValue = number; break;
            case string label: field.stringValue = label; break;
            case Vector2 position: field.vector2Value = position; break;
            case Sprite[] frames:
                field.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                break;
            default: field.objectReferenceValue = (Object)value; break;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
