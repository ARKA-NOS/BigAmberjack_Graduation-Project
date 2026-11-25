using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Audio;
using _Scripts.DevLib.BattleSystem;
using _Scripts.Testing.Enemy;
using _Scripts.UI.Enemy;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class VerifyBoss23Rework
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Path = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        var skills = root.GetComponentInChildren<EnemySkillModule>(true).Skills;
        Check(skills.Count == 6 && skills.All(e => e.Skill != null && e.Skill.SkillSo != null && !e.UseForAutomaticSelection) &&
            skills.Select(e => e.Skill.SkillSo).Distinct().Count() == 6, "Six unique specified skills required");
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, "Missing script: " + node.name);
        Check(root.GetComponentsInChildren<DroneSalvoSkill>(true).Length == 1 && root.GetComponentInChildren<EnemyAfterImageModule>(true) != null,
            "Obsolete strong salvo or missing afterimage module");
        var data = new SerializedObject(root.GetComponent<ProductionControlDrone>());
        Check(data.FindProperty("lastStandHealthRatio").floatValue == 0.35f, "Wrong Last Stand threshold");
        var bomb = new SerializedObject(root.GetComponentInChildren<DroneBombSkill>(true));
        Check(bomb.FindProperty("warningPrefab").objectReferenceValue != null && bomb.FindProperty("telegraphPrefab").objectReferenceValue == null,
            "Target warning missing or obsolete circle warning remains");
        Check(!new SerializedObject(root.GetComponentInChildren<BossFlightModule>(true)).FindProperty("decelerateFromStart").boolValue,
            "Smooth acceleration/deceleration profile not enabled");
        var after = new SerializedObject(root.GetComponentInChildren<EnemyAfterImageModule>(true));
        Check(AssetDatabase.GetAssetPath(after.FindProperty("ghostPrefab").objectReferenceValue).EndsWith("Boss23_SmoothAfterImage.prefab"),
            "Smooth enemy afterimage prefab not assigned");
        Check(root.GetComponentInChildren<Animator>(true).runtimeAnimatorController.animationClips.Count(c => c.name.Contains("Crouch")) == 3,
            "Missing crouched attack animation clips");
        var singleShot = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_SalvoSingleShot.anim");
        var shotFrames = AnimationUtility.GetObjectReferenceCurve(singleShot, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
        Check(!singleShot.isLooping && shotFrames.Count(k => k.value.name == "speedy shot_0") == 1 &&
            shotFrames.All(k => k.value.name == "speedy shot_0" || k.value.name == "movingt_0"),
            "Single-shot clip contains repeated or rear-barrel flashes");
        Check(new SerializedObject(root.GetComponentInChildren<DroneVisualModule>(true)).FindProperty("basicFlightExhaust").objectReferenceValue != null,
            "Flight exhaust reference missing");
        var shield = root.GetComponentInChildren<DroneShieldGraphic>(true);
        Check(shield != null && shield.GetComponent<CanvasRenderer>() != null, "Shield CanvasRenderer missing");
        var charge = new SerializedObject(root.GetComponentInChildren<DroneChargeSkill>(true));
        var warning = (AttackTelegraphUI)charge.FindProperty("telegraphPrefab").objectReferenceValue;
        Check(warning != null && warning.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length == 5,
            "Charge rectangle fill or four borders missing");
        Check(Mathf.Abs(data.FindProperty("airAttackAltitude").floatValue - 4.5f) < 0.001f, "Air attack altitude was not lowered");
        Check(Mathf.Abs(charge.FindProperty("damageStartRatio").floatValue - 0.5f) < 0.001f, "Charge rear safe half missing");
        foreach (var skill in skills.Select(e => e.Skill).Where(s => s is DroneSalvoSkill || s is DroneAirTransitionSkill))
            Check(new SerializedObject(skill).FindProperty("rearMuzzle").objectReferenceValue != null, "Rear launcher reference missing");

        foreach (string name in new[] { "SalvoRearShot", "AirShotFront", "AirShotRear" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_" + name + ".anim");
            Check(clip != null && !clip.isLooping, "Alternating launcher clip missing or looping: " + name);
        }
        return new { skills = skills.Select(e => e.Skill.name).ToArray(), dependencies = AssetDatabase.GetDependencies(Path).Length,
            lastStandAt = 0.35f, airHeight = data.FindProperty("airAttackAltitude").floatValue,
            active = root.activeSelf, scale = root.transform.localScale.ToString() };
    }

    public static Task<object> Run() => RunInternal(false);

    public static Task<object> RunGround() => RunInternal(false, 1);

    public static Task<object> RunAir() => RunInternal(false, 2);

    public static Task<object> RunFlow() => RunInternal(true, 3);

    public static Task<object> RunSpacing() => RunInternal(false, 4);

    public static Task<object> RunLaserSound() => RunInternal(false, 5);

    public static Task<object> RunEntryOnly() => RunInternal(true);

    public static Task<object> RunRecovery() => RunInternal(true, 6);

    public static Task<object> RunCenterWalls() => RunInternal(true, 7);

    public static Task<object> RunBombSound() => RunInternal(true, 8);

    public static object CheckRecoveryAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        var animator = root.GetComponentInChildren<Animator>(true);
        var body = animator.GetComponent<SpriteRenderer>();
        var feedback = root.GetComponentInChildren<DroneLastStandFeedbackModule>(true);
        var sparks = (ParticleSystem)new SerializedObject(feedback).FindProperty("malfunctionSparks").objectReferenceValue;
        var renderer = sparks.GetComponent<ParticleSystemRenderer>();
        Check(sparks.transform.parent.name == "ProcessNumber" && sparks.transform.localPosition == Vector3.zero &&
            renderer.sortingOrder == body.sortingOrder - 1 && renderer.sortingLayerID == body.sortingLayerID,
            "Spark anchor/sorting incorrect");
        Check(renderer.sharedMaterial.shader.name == "Enemy/DroneGroggySpark" &&
            !ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader), "Spark glow shader missing or failed");
        Check(sparks.emission.rateOverTime.constant == 110f, "Spark density unchanged");
        string prefix = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var numberY = EditorCurveBinding.FloatCurve("ProcessNumber", typeof(Transform), "m_LocalPosition.y");
        var hold = AssetDatabase.LoadAssetAtPath<AnimationClip>(prefix + "GroggyHold.anim");
        var recovery = AssetDatabase.LoadAssetAtPath<AnimationClip>(prefix + "GroggyRecover.anim");
        Check(hold != null && recovery != null && hold.isLooping && !recovery.isLooping, "Groggy clips missing or wrong loop mode");
        Check(AnimationUtility.GetObjectReferenceCurve(hold, binding).All(k => k.value.name == "transition_4"), "Wrong groggy pose");
        foreach (string name in new[] { "GroggyHold", "GroggyRecover" })
            Check(animator.runtimeAnimatorController.animationClips.Any(c => c.name.EndsWith("_" + name)), "Groggy state missing");
        foreach (string name in new[] { "CrouchFire", "GroundRush", "SalvoSingleShot", "SalvoRearShot" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(prefix + name + ".anim");
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var curve = AnimationUtility.GetEditorCurve(clip, numberY);
            foreach (var key in frames)
            {
                var sprite = (Sprite)key.value;
                float sourceY = sprite.texture.height - sprite.rect.y - sprite.pivot.y - curve.Evaluate(key.time) * 28f;
                Check(Mathf.Abs(sourceY - 64.5f) < 0.01f, "Number did not compensate crouched sprite pivot: " + name);
            }
        }
        return new { sparkAnchor = sparks.transform.parent.name, sparkRate = sparks.emission.rateOverTime.constant,
            sparkSorting = renderer.sortingOrder, bodySorting = body.sortingOrder, shaderError = false,
            crouchNumberSourceY = 64.5f, groggySprite = "transition_4", recoverySeconds = recovery.length };
    }

    private static async Task<object> RunInternal(bool entryOnly, int group = 0)
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene Play Mode");
        var originals = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(o => o.activeSelf && o.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        foreach (var original in originals) original.SetActive(false);
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var randomState = Random.state;
        Random.InitState(231135);
        var results = new List<object>();

        using (var rig = new Rig())
        {
            try
            {
                if (group == 7)
                {
                    await rig.NewBoss(false);
                    var arena = new SerializedObject(rig.Root.GetComponentInChildren<BossArenaModule>());
                    arena.FindProperty("halfWidth").floatValue = 8f;
                    arena.ApplyModifiedPropertiesWithoutUndo();
                    var walls = new GameObject("Center return test walls");
                    try
                    {
                        foreach (float side in new[] { -1f, 1f })
                        {
                            var wall = new GameObject("Arena wall") { layer = LayerMask.NameToLayer("Ground") };
                            wall.transform.SetParent(walls.transform, false);
                            wall.transform.position = new Vector3(side * 8.25f, 3f, 0f);
                            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 6f);
                        }
                        foreach (float side in new[] { -1f, 1f }) results.Add(await CenterReturn(rig, side, true));
                    }
                    finally { Object.Destroy(walls); }
                    return new { results };
                }

                if (group == 6)
                {
                    await rig.NewBoss(false);
                    results.Add(await LastStandEntry(rig));
                    await rig.NewBoss(false);
                    results.Add(await CenterReturn(rig, -1f));
                    results.Add(await CenterReturn(rig, 1f));
                    return new { results, preview = "Tools~/CTJ/Boss23GroggyPreview.png" };
                }

                if (group == 5 || group == 8)
                {
                    var previousSound = ServiceLocator.Get<ISoundManager>();
                    GameObject soundRoot = null;
                    var manager = previousSound as SoundManager;
                    if (manager == null || !manager.isActiveAndEnabled)
                    {
                        soundRoot = new GameObject("Laser sound verification");
                        soundRoot.SetActive(false);
                        manager = soundRoot.AddComponent<SoundManager>();
                        var config = new SerializedObject(manager);
                        config.FindProperty("_sfxCount").intValue = 4;
                        config.FindProperty("_uiCount").intValue = 1;
                        config.ApplyModifiedPropertiesWithoutUndo();
                        soundRoot.SetActive(true);
                        manager.Initialize();
                    }

                    try
                    {
                        if (group == 8)
                        {
                            foreach (bool enhanced in new[] { false, true })
                            {
                                await rig.NewBoss(enhanced);
                                results.Add(await BombImpactSound(rig, manager, enhanced));
                            }
                            return new { results };
                        }

                        foreach (bool enhanced in new[] { false, true })
                        {
                            await rig.NewBoss(enhanced);
                            var expected = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Sound/Enemy/yodguard-short-energy-beam-shot-4-482500.mp3");
                            int sameFrameStarts = 0;
                            var startStates = new List<object>();
                            bool validStarts = true;
                            Action<EnemyAttackSkill, Vector3> onAttack = (skill, point) =>
                            {
                                if (skill != rig.Laser) return;
                                var sources = manager.GetComponentsInChildren<AudioSource>();
                                bool beamOn = rig.Root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>().enabled;
                                bool playing = sources.Any(s => s.clip == expected && s.isPlaying && s.time < 0.1f);
                                validStarts &= beamOn && playing;
                                startStates.Add(new { beam = rig.Root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>().enabled,
                                    registered = ReferenceEquals(ServiceLocator.Get<ISoundManager>(), manager),
                                    sources = sources.Where(s => s.clip == expected).Select(s => new { clip = s.clip.name, playing = s.isPlaying, time = s.time }).ToArray() });
                                sameFrameStarts++;
                            };
                            var attackEvent = typeof(EnemyAttackSkill).GetEvent("AttackPerformed", Private);
                            attackEvent.GetAddMethod(true).Invoke(rig.Laser, new object[] { onAttack });
                            var result = await Laser(rig, enhanced);
                            attackEvent.GetRemoveMethod(true).Invoke(rig.Laser, new object[] { onAttack });
                            Check(sameFrameStarts == (enhanced ? 2 : 1), "Laser sound release count mismatch");
                            Check(validStarts, "Laser clip not playing at beam release");
                            results.Add(new { enhanced, sameFrameStarts, clip = expected.name, startStates,
                                playingAfterRelease = manager.GetComponentsInChildren<AudioSource>().Any(s => s.clip == expected && s.isPlaying), laser = result });
                        }
                    }
                    finally
                    {
                        if (soundRoot != null)
                        {
                            soundRoot.SetActive(false);
                            Object.Destroy(soundRoot);
                            ServiceLocator.Register<ISoundManager>(previousSound);
                        }
                    }

                    return new { results };
                }

                if (group == 4)
                {
                    await rig.NewBoss(false);
                    foreach (float direction in new[] { 1f, -1f })
                        foreach (float gap in new[] { 1.5f, 9f })
                            results.Add(await ChargeReach(rig, direction, gap));

                    foreach (string scenario in new[] { "far", "wall", "cross", "changeDistance" })
                        results.Add(await SalvoSpacing(rig, scenario));

                    await rig.NewBoss(true);
                    results.Add(await SalvoSpacing(rig, "cross"));

                    return new { results };
                }

                for (int mode = 0; !entryOnly && mode < 2; mode++)
                {
                    bool enhanced = mode == 1;
                    await rig.NewBoss(enhanced);
                    if (group != 2)
                    {
                        if (!enhanced) results.Add(await Motion(rig));
                        results.Add(await Charge(rig, enhanced));
                        results.Add(await Barrage(rig, enhanced, false));
                        if (enhanced) results.Add(await Barrage(rig, true, false, -1f));
                        if (!enhanced) results.Add(await Barrage(rig, false, true));
                        if (!enhanced) results.Add(await BlockedChargeWarning(rig));
                    }
                    if (group != 1)
                    {
                        results.Add(await Air(rig, enhanced));
                        results.Add(await Laser(rig, enhanced));
                        results.Add(await Bomb(rig, enhanced));
                        if (!enhanced) results.Add(await Descent(rig));
                    }
                }

                if (entryOnly || group == 0)
                {
                    await rig.NewBoss(false);
                    if (!entryOnly || group == 3) results.Add(RandomChoices(rig));
                    if (group == 3) results.Add(await GroundRest(rig));
                    if (group == 3) results.Add(await AirRest(rig));
                    if (group == 3) results.Add(await BombFlow(rig));
                    results.Add(await LastStandEntry(rig));
                    await rig.NewBoss(false);
                    results.Add(await CancelLastStandEntry(rig));
                }
                return new { results, preview = "Tools~/CTJ/Boss23TargetBombPreview.png" };
            }
            catch (Exception exception)
            {
                var health = rig.Root.GetComponentInChildren<NewHealthModule>();
                throw new Exception(exception.Message + " | HP=" + health.CurrentHealth.ToString("R") +
                    ", threshold=" + (health.MaxHealth * 0.35f).ToString("R") + ", phase=" + rig.Boss.IsLastStand +
                    ", state=" + Read(rig.Boss, "_state"), exception);
            }
            finally
            {
                Random.state = randomState;
                Application.runInBackground = background;
                foreach (var original in originals) if (original != null) original.SetActive(true);
            }
        }
    }

    private static async Task<object> Charge(Rig r, bool enhanced)
    {
        r.Prepare(false, 1.5f);
        int hits = r.Probe.ReceivedHitCount;
        float damage = r.Probe.TotalDamage, startedAt = Time.time;
        Check(r.Skills.TryUseSpecifiedSkill(r.Charge), "Charge did not start");
        var phases = new Dictionary<string, float>();
        bool resisted = false, crouchSeen = false, captured = false;
        bool idleDuringRecovery = false, fastDashAnimation = false, renderSmoothingSeen = false, warningSeen = false;
        int ghosts = 0;
        var body = r.Root.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
        float firstDash = enhanced ? 0.91f : 1.2f;

        await Until(() =>
        {
            float t = Time.time - startedAt;
            string step = Read(r.Charge, "_step").ToString();
            var warning = (AttackTelegraphUI)typeof(EnemyAttackSkill).GetProperty("Telegraph", Private).GetValue(r.Charge);
            bool shouldWarn = step == "Charging" || step == "Reaiming";
            Check(warning != null && warning.IsVisible == shouldWarn, "Charge warning lifecycle mismatch at " + step);
            if (shouldWarn)
            {
                CheckWarningBounds(r, warning);
                if (!enhanced && !warningSeen)
                    CaptureGround(r.Camera, warning.transform.position, 2.6f, "Tools~/CTJ/Boss23ChargeWarningPreview.png");

                warningSeen = true;
            }

            var animator = r.Root.GetComponentInChildren<Animator>();
            if (!phases.ContainsKey(step)) phases[step] = t;
            if ((step == "FirstDash" || step == "SecondDash") && animator.GetCurrentAnimatorStateInfo(0)
                .IsName("Base Layer.GroundRush"))
            {
                Check(Mathf.Abs(animator.speed - 2f) < 0.01f, "Dash clip was stretched to the movement duration");
                fastDashAnimation = true;
                float offset = Vector3.Distance(r.Root.transform.position, r.Root.transform.Find("Visual").position);
                renderSmoothingSeen |= offset > 0.01f && offset < 0.7f;
            }
            if ((step == "Recovering" || step == "Reaiming") && t > phases[step] + 0.03f)
            {
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"), "Charge kept the attack pose during the wait");
                if (step == "Recovering") idleDuringRecovery = true;
            }
            ghosts = Mathf.Max(ghosts, Object.FindObjectsByType<AfterImageGhost>(FindObjectsSortMode.None).Length);
            crouchSeen |= AssetDatabase.GetAssetPath(body.sprite).Contains("/Crouch/speedy shot.png");
            if (step == "Following" || step == "Charging")
                r.Player.transform.position = t < firstDash - 0.12f ? Vector3.left * 3f : Vector3.right * 1.5f;
            if (step == "FirstDash" && t > firstDash + 0.25f) r.Player.transform.position = Vector3.right * (enhanced ? 2f : -3f);
            if (step == "Reaiming") r.Player.transform.position = Vector3.right * 2f;
            if (step == "SecondDash" && t > phases["SecondDash"] + 0.5f) r.Player.transform.position = Vector3.right * 10f;

            float facing = r.Root.transform.Find("Visual").localScale.x;
            if (step == "Following" || step == "Charging" || step == "FirstDash") Check(facing > 0f, "First dash direction followed the player");
            if (step == "SecondDash") Check(facing < 0f, "Second dash changed direction after reaim");
            if (!resisted && step == "Charging")
            {
                CheckHitResistance(r);
                resisted = true;
            }
            if (!enhanced && !captured && step == "FirstDash" && t > firstDash + 0.2f)
            {
                CaptureGround(r.Camera, body.bounds.center);
                captured = true;
            }
            return !r.Charge.IsUsing;
        }, 5f, "Charge stalled");

        var events = r.Events.Where(e => e.skill == r.Charge).ToArray();
        Check(events.Length == (enhanced ? 2 : 1) && ghosts > 1 && crouchSeen && resisted && warningSeen, "Charge feedback or release count mismatch");
        Check(idleDuringRecovery && fastDashAnimation && renderSmoothingSeen, "Charge animation/idle recovery or render smoothing was not observed");
        Check(Mathf.Abs(events[0].time - startedAt - firstDash) < 0.07f, "First dash time mismatch");
        if (enhanced) Check(Mathf.Abs(events[1].time - startedAt - 1.8f) < 0.09f, "Second dash time mismatch");
        Check(Mathf.Abs(phases["Recovering"] - (enhanced ? 2.35f : 2f)) < 0.1f, "Charge recovery time mismatch");
        Check(Mathf.Abs(Time.time - startedAt - (enhanced ? 2.55f : 2.2f)) < 0.12f, "Charge total time mismatch");
        float end = r.Root.transform.position.x;
        float firstEnd = enhanced ? events[1].position.x : end;
        Check(Mathf.Abs(firstEnd - events[0].position.x - 6.5f) < 0.04f &&
            (!enhanced || Mathf.Abs(firstEnd - end - 6.5f) < 0.04f), "Dash distance changed");
        Check(r.Probe.ReceivedHitCount - hits == (enhanced ? 2 : 1) &&
            Mathf.Abs(r.Probe.TotalDamage - damage - (enhanced ? 44f : 18f)) < 0.01f, "Dash damage mismatch");
        return new { pattern = "Charge", enhanced, phases, releases = events.Select(e => e.time - startedAt).ToArray(),
            total = Time.time - startedAt, damage = r.Probe.TotalDamage - damage, directionLocked = true, hitResistance = resisted,
            idleDuringRecovery, dashAnimationSpeed = 2f, renderSmoothingSeen, warningSeen };
    }

    private static void CheckWarningBounds(Rig r, AttackTelegraphUI warning)
    {
        float direction = ReadFloat(r.Charge, "_direction");
        object[] args = { direction, ReadFloat(r.Charge, "distance"), Vector2.zero };

        typeof(ProductionControlDrone).GetMethod("TryPlanGroundMove", Private).Invoke(r.Boss, args);
        float offset = ((Vector2)args[2]).x - r.Root.transform.position.x;
        Bounds expected = (Bounds)typeof(ProductionControlDrone).GetProperty("ContactBounds", Private).GetValue(r.Boss);

        expected.SetMinMax(new Vector3(direction > 0f ? expected.center.x : expected.min.x, expected.min.y, 0f),
            new Vector3(direction > 0f ? expected.max.x : expected.center.x, expected.max.y, 0f));

        expected.center += Vector3.right * (offset * 0.5f);
        expected.size += Vector3.right * Mathf.Abs(offset);
        var corners = new Vector3[4];

        warning.GetComponent<RectTransform>().GetWorldCorners(corners);
        Check(Vector2.Distance(corners[0], expected.min) < 0.002f && Vector2.Distance(corners[2], expected.max) < 0.002f,
            "Warning rectangle does not match the constrained body sweep");
    }

    private static async Task<object> BlockedChargeWarning(Rig r)
    {
        r.Prepare(false, -4f);
        var wall = new GameObject("Charge warning wall") { layer = LayerMask.NameToLayer("Ground") };

        try
        {
            wall.transform.position = new Vector3(-3f, 2f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 4f);
            Physics2D.SyncTransforms();
            Check(r.Skills.TryUseSpecifiedSkill(r.Charge), "Blocked charge did not start");
            await Until(() => Read(r.Charge, "_step").ToString() == "Charging", 1f, "Charge never prepared");
            var warning = (AttackTelegraphUI)typeof(EnemyAttackSkill).GetProperty("Telegraph", Private).GetValue(r.Charge);

            Check(warning.IsVisible && ReadFloat(r.Charge, "_direction") < 0f, "Left charge warning missing");
            CheckWarningBounds(r, warning);
            float width = warning.GetComponent<RectTransform>().rect.width;
            Check(width < 6.5f, "Warning extended through a blocking wall");
            r.Skills.StopAttack();
            Check(!warning.IsVisible && !warning.GetComponent<Canvas>().enabled, "Cancelled charge retained its warning");

            return new { pattern = "BlockedChargeWarning", direction = "left", width, scaledBoundsMatch = true, hiddenOnCancel = true };
        }
        finally { Object.DestroyImmediate(wall); }
    }

    private static void CheckHitResistance(Rig r)
    {
        var reaction = r.Root.GetComponentInChildren<EnemyHitReactionModule>();
        var config = new SerializedObject(reaction);
        config.FindProperty("enableKnockback").boolValue = true;
        config.FindProperty("knockbackSpeed").floatValue = 20f;
        config.FindProperty("knockbackDuration").floatValue = 0.5f;
        config.FindProperty("hitStunDuration").floatValue = 0.5f;
        config.FindProperty("cancelAttackOnHit").boolValue = true;
        config.ApplyModifiedPropertiesWithoutUndo();
        var active = r.Skills.ActiveSkill;
        var health = r.Root.GetComponentInChildren<NewHealthModule>();
        float hp = health.CurrentHealth;
        r.Boss.ApplyDamage(new _Scripts.DevLib.BattleSystem.DamageData { DamageAmount = 5f },
            r.Root.transform.position, Vector2.left, Vector2.right);
        Check(!reaction.IsReacting && r.Skills.ActiveSkill == active && active.IsUsing &&
            health.CurrentHealth < hp, "Damage was blocked or hit control interrupted the boss");
    }

    private static Task<object> Motion(Rig r)
    {
        r.Prepare(false, 4f);
        r.Flight.MoveTo(new Vector2(3f, r.Root.transform.position.y), 6f);
        var samples = new List<(float time, float x)>();
        for (int i = 0; r.Flight.IsMoving && i < 200; i++)
        {
            r.Flight.Tick();
            samples.Add((i * Time.fixedDeltaTime, r.Root.transform.position.x));
        }

        var speeds = samples.Zip(samples.Skip(1), (a, b) => (b.x - a.x) / (b.time - a.time)).ToArray();
        float first = speeds.Take(6).Average(), last = speeds.Skip(speeds.Length - 6).Average();
        Check(first < 5f && last < 3f && speeds.Max() > 5.9f && Mathf.Abs(r.Root.transform.position.x - 3f) < 0.001f,
            "Movement did not accelerate and decelerate to the exact destination");
        return Task.FromResult<object>(new { pattern = "Movement", firstSpeed = first, peakSpeed = speeds.Max(), lastSpeed = last,
            smoothAcceleration = true, endX = r.Root.transform.position.x });
    }

    private static async Task<object> Barrage(Rig r, bool enhanced, bool wall, float direction = 1f)
    {
        r.Prepare(false, direction * 1.5f);
        GameObject blocker = null;
        if (wall)
        {
            Bounds body = (Bounds)typeof(ProductionControlDrone).GetProperty("ContactBounds", Private).GetValue(r.Boss);
            blocker = new GameObject("Retreat blocked test") { layer = LayerMask.NameToLayer("Ground") };
            blocker.transform.position = new Vector3(-direction * (body.extents.x * 0.95f + 0.11f), 1.5f, 0f);
            blocker.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 3f);
            Physics2D.SyncTransforms();
        }

        try
        {
            float start = Time.time, recoveryAt = -1f, idleAt = -1f;
            Check(r.Skills.TryUseSpecifiedSkill(r.Barrage), "Barrage did not start");
            bool chargeSeen = false, crouchSeen = false, resisted = false, wasBright = false;
            bool hoverSeen = false, exhaustSeen = false, hoverCaptured = false;
            float hoverMin = float.PositiveInfinity, hoverMax = float.NegativeInfinity;
            var visualModule = r.Root.GetComponentInChildren<DroneVisualModule>();
            var exhaust = r.Root.transform.Find("Visual/BasicFlightExhaust").GetComponent<ParticleSystem>();
            int flashes = 0;
            var launchers = new List<string>();
            await Until(() =>
            {
                float t = Time.time - start;
                var animator = r.Root.GetComponentInChildren<Animator>();
                chargeSeen |= animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Charge");
                crouchSeen |= AssetDatabase.GetAssetPath(animator.GetComponent<SpriteRenderer>().sprite).Contains("/Crouch/speedy shot.png");
                string spriteName = animator.GetComponent<SpriteRenderer>().sprite.name;
                bool bright = spriteName == "speedy shot_0" || spriteName == "speedy shot_1";
                if (bright && !wasBright) { flashes++; launchers.Add(spriteName); }
                wasBright = bright;
                if (r.Barrage.FiredVolleys == (enhanced ? 12 : 7) && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle") && idleAt < 0f)
                    idleAt = t;

                if (t > (enhanced ? 0.48f : 0.58f) + 0.22f && r.Flight.IsMoving && !r.Barrage.IsRecovering)
                {
                    float offset = r.Root.transform.Find("Visual").position.y - r.Root.transform.position.y -
                        ((Vector3)Read(visualModule, "_visualOffset")).y;

                    hoverMin = Mathf.Min(hoverMin, offset);
                    hoverMax = Mathf.Max(hoverMax, offset);
                    hoverSeen |= offset > 0.2f;
                    exhaustSeen |= exhaust.particleCount > 0;
                    Check(Mathf.Abs(r.Root.transform.position.y - 0.9f) < 0.001f,
                        "Hover moved the physics body off the floor");
                    if (!enhanced && !hoverCaptured && offset > 0.23f)
                    {
                        CaptureGround(r.Camera, r.Root.transform.position + Vector3.up * 0.35f, 2f,
                            "Tools~/CTJ/Boss23SalvoHoverPreview.png");
                        hoverCaptured = true;
                    }
                }

                Check(r.Root.transform.Find("Visual").localScale.x * direction > 0f, "Barrage changed facing");
                if (r.Barrage.IsRecovering)
                {
                    if (recoveryAt < 0f) recoveryAt = t;
                    if (!resisted) { CheckHitResistance(r); resisted = true; }
                }
                return !r.Barrage.IsUsing;
            }, 6f, "Barrage stalled");

            var events = r.Events.Where(e => e.skill == r.Barrage).ToArray();
            int count = enhanced ? 12 : 7;
            float interval = enhanced ? 0.144f : 0.32f;
            Check(chargeSeen && crouchSeen && resisted && events.Length == count &&
                r.Shots.Count == (enhanced ? 36 : 7), "Barrage pose, hit resistance or count mismatch");
            Check(Mathf.Abs(events[0].time - start - (enhanced ? 0.48f : 0.58f)) < 0.06f, "First shot time mismatch");
            for (int i = 1; i < events.Length; i++)
                Check(Mathf.Abs(events[i].time - events[i - 1].time - interval) < 0.04f, "Shot interval mismatch");
            Check(Mathf.Abs(recoveryAt - (enhanced ? 2.186f : 2.646f)) < 0.08f, "Barrage recovery start mismatch: " + recoveryAt);
            Check(flashes == count && launchers.Select((name, i) => name == (i % 2 == 0 ? "speedy shot_0" : "speedy shot_1")).All(x => x),
                "Barrage flashes did not alternate once per volley: " + string.Join(",", launchers));
            Check(idleAt > 0f && Mathf.Abs(idleAt - recoveryAt) < 0.04f, "Barrage did not return to Idle when the last motion ended");
            Check(r.Shots.All(s => s.damage == 10f && s.direction.x * direction > 0f), "Barrage damage or locked direction changed");
            float[] verticalOffsets = r.Shots.Select(s => s.origin.y - s.muzzleY).ToArray();
            Check(enhanced ? verticalOffsets.All(y => Mathf.Abs(y) < 0.0001f) :
                verticalOffsets.All(y => Mathf.Abs(y) <= 0.3001f) && verticalOffsets.Max() - verticalOffsets.Min() > 0.1f,
                "Enhanced random height remains or normal variation changed");
            Check(enhanced ? r.Shots.Select(s => Mathf.RoundToInt(Mathf.Atan2(s.direction.y, s.direction.x) * Mathf.Rad2Deg)).Distinct().Count() == 3 :
                r.Shots.All(s => Mathf.Abs(s.direction.y) < 0.001f), "Wrong forward directions");
            if (enhanced)
            {
                var angles = r.Shots.Select(s => Mathf.RoundToInt(Mathf.Atan2(s.direction.y, Mathf.Abs(s.direction.x)) * Mathf.Rad2Deg))
                    .Distinct().OrderBy(x => x).ToArray();
                Check(angles.SequenceEqual(new[] { 0, 12, 24 }), "Lowest enhanced ray was not horizontal");
            }

            float retreat = -r.Root.transform.position.x * direction;
            Check(wall ? Mathf.Abs(retreat) < 0.02f : retreat > 1.2f && retreat <= 1.51f, "Blocked retreat or preferred distance rule failed");
            if (!wall) Check(hoverSeen && exhaustSeen && hoverMax - hoverMin > 0.1f, "Moving salvo hover or exhaust was not visible");
            Check(ReadFloat(visualModule, "_salvoHoverBlend") < 0.01f, "Hover did not settle after movement ended");
            return new { pattern = "Barrage", enhanced, wall, direction, shots = r.Shots.Count, firstShot = events[0].time - start,
                interval, idleAt, recoveryAt, retreat, directionLocked = true, damagePerShot = 10f,
                flashes, launchers, hoverSeen, exhaustSeen, maxVerticalJitter = verticalOffsets.Max(y => Mathf.Abs(y)),
                hoverRange = wall ? null : new[] { hoverMin, hoverMax } };
        }
        finally { if (blocker != null) Object.DestroyImmediate(blocker); }
    }

    private static async Task<object> ChargeReach(Rig r, float direction, float gap)
    {
        r.Prepare(false, direction * gap);
        int hits = r.Probe.ReceivedHitCount;
        Check(r.Skills.TryUseSpecifiedSkill(r.Charge), "Fixed-distance charge did not start");
        await Until(() => Read(r.Charge, "_step").ToString() == "Charging", 1f, "Charge preparation missing");
        Bounds body = (Bounds)typeof(ProductionControlDrone).GetProperty("ContactBounds", Private).GetValue(r.Boss);
        var warning = (AttackTelegraphUI)typeof(EnemyAttackSkill).GetProperty("Telegraph", Private).GetValue(r.Charge);

        CheckWarningBounds(r, warning);
        r.Player.transform.position = new Vector3(body.center.x - direction * body.extents.x * 0.5f, 0f, 0f);
        await Until(() => Read(r.Charge, "_step").ToString() == "FirstDash", 1f, "Dash did not start");
        float dashStartX = r.Events.Single(e => e.skill == r.Charge).position.x;
        float dashAt = Time.time;
        await Until(() => Time.time >= dashAt + 0.2f, 0.5f, "Rear safe-half sample timed out");
        Check(r.Probe.ReceivedHitCount == hits, "Rear half inflicted charge damage");

        body = (Bounds)typeof(ProductionControlDrone).GetProperty("ContactBounds", Private).GetValue(r.Boss);
        r.Player.transform.position = new Vector3(body.center.x + direction * body.extents.x * 0.75f, 0f, 0f);
        Physics2D.SyncTransforms();
        await Until(() => r.Probe.ReceivedHitCount == hits + 1, 0.4f, "Front half did not inflict charge damage");
        float hitX = r.Root.transform.position.x;
        await Until(() => !r.Charge.IsUsing, 2f, "Charge stopped or stalled on player contact");
        float travel = (r.Root.transform.position.x - dashStartX) * direction;

        Check(Mathf.Abs(travel - 6.5f) < 0.025f, "Player distance changed the maximum dash length");
        Check((r.Root.transform.position.x - hitX) * direction > 1f, "Charge ended when damage was applied");
        Check(r.Probe.ReceivedHitCount == hits + 1, "Single dash inflicted duplicate damage");

        return new { pattern = "ChargeReach", direction, initialGap = gap, travel, rearSafe = true, continuedAfterHit = true };
    }

    private static async Task<object> SalvoSpacing(Rig r, string scenario)
    {
        r.Prepare(false, scenario == "changeDistance" ? 1.5f : 6f);
        GameObject wall = null;
        float frozenAt = float.NaN;
        bool crossed = false, returned = false, changed = false;
        float minX = 0f;

        try
        {
            if (scenario == "wall")
            {
                Bounds body = (Bounds)typeof(ProductionControlDrone).GetProperty("ContactBounds", Private).GetValue(r.Boss);
                wall = new GameObject("Forward spacing wall") { layer = LayerMask.NameToLayer("Ground") };
                wall.transform.position = new Vector3(body.extents.x * 0.95f + 0.11f, 1.5f, 0f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 3f);
                Physics2D.SyncTransforms();
            }

            Check(r.Skills.TryUseSpecifiedSkill(r.Barrage), "Spacing barrage did not start");
            await Until(() =>
            {
                minX = Mathf.Min(minX, r.Root.transform.position.x);
                Check(r.Root.transform.Find("Visual").localScale.x > 0f, "Spacing turned the firing direction");
                if (scenario == "cross" && !crossed && r.Barrage.FiredVolleys >= 2)
                {
                    r.Player.transform.position = r.Root.transform.position + Vector3.left * 2f;
                    crossed = true;
                }
                if (crossed && (bool)Read(r.Barrage, "_movementLocked"))
                {
                    if (float.IsNaN(frozenAt)) frozenAt = r.Root.transform.position.x;
                    Check(Mathf.Abs(r.Root.transform.position.x - frozenAt) < 0.01f, "Barrage moved after target crossed behind");
                    if (!returned && r.Barrage.FiredVolleys >= 4)
                    {
                        r.Player.transform.position = r.Root.transform.position + Vector3.right * 6f;
                        returned = true;
                    }
                }
                if (scenario == "changeDistance" && !changed && r.Barrage.FiredVolleys >= 2)
                {
                    r.Player.transform.position = r.Root.transform.position + Vector3.right * 6f;
                    changed = true;
                }

                return !r.Barrage.IsUsing;
            }, 5f, "Spacing barrage stalled");

            bool enhanced = r.Boss.IsLastStand;
            Check(r.Shots.Count == (enhanced ? 36 : 7) && r.Shots.All(s => s.direction.x > 0f),
                "Spacing interrupted shots or changed firing direction");
            float endX = r.Root.transform.position.x;

            if (scenario == "far") Check(Mathf.Abs(6f - endX - 3f) < 0.25f, "Far barrage did not approach preferred distance");
            if (scenario == "wall") Check(Mathf.Abs(endX) < 0.02f, "Blocked barrage advanced through wall");
            if (scenario == "cross") Check(crossed && returned && !float.IsNaN(frozenAt), "Crossing freeze was not exercised");
            if (scenario == "changeDistance") Check(minX < -0.05f && endX > 0.5f, "Barrage did not switch from retreat to approach");

            return new { pattern = "SalvoSpacing", enhanced, scenario, minX, endX, shots = r.Shots.Count, directionLocked = true };
        }
        finally { if (wall != null) Object.DestroyImmediate(wall); }
    }

    private static async Task<object> Air(Rig r, bool enhanced)
    {
        r.Prepare(false, 4f);
        float start = Time.time;
        Check(r.Skills.TryUseSpecifiedSkill(r.Air), "Air transition did not start");
        bool flyingSeen = false, shotSeen = false, idleAfterShot = false, jetSeen = false;
        var exhaust = r.Root.transform.Find("Visual/BasicFlightExhaust").GetComponent<ParticleSystem>();
        var launchers = new List<string>();
        bool wasBright = false, movedTarget = false;
        float arrivedAt = -1f;
        await Until(() =>
        {
            var currentSprite = r.Root.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>().sprite;
            string sprite = AssetDatabase.GetAssetPath(currentSprite);
            flyingSeen |= sprite.Contains("/flying up/moving.png");
            shotSeen |= sprite.Contains("/flying up/shot.png");
            bool bright = currentSprite.name == "shot_1" || currentSprite.name == "shot_3";
            if (bright && !wasBright) launchers.Add(currentSprite.name);
            wasBright = bright;
            jetSeen |= exhaust.particleCount > 0;
            if ((bool)Read(r.Air, "_moving") && !r.Flight.IsMoving && arrivedAt < 0f) arrivedAt = Time.time - start;
            if (r.Events.Count(e => e.skill == r.Air) > 0 && !movedTarget)
            {
                r.Player.transform.position = Vector3.left * 3f;
                movedTarget = true;
            }
            if ((bool)Read(r.Air, "_returnedToIdle") && Time.time > ReadFloat(r.Air, "_shotEndsAt") + 0.03f)
            {
                Check(r.Root.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.AirIdle"),
                    "Air shot did not return to the flight idle pose");
                idleAfterShot = true;
            }
            return !r.Air.IsUsing;
        }, 7f, "Ascent stalled");
        Check(r.Air.DidArrive && Mathf.Abs(r.Root.transform.position.y - 4.5f) < 0.02f &&
            r.Shots.Count == (enhanced ? 16 : 4) && r.Shots.All(s => s.damage == 10f), "Ascent position or damage mismatch");
        Check(flyingSeen && shotSeen, "Flying animation was not used");
        var events = r.Events.Where(e => e.skill == r.Air).ToArray();
        Check(events.Length == 4 && events.All(e => e.position.y > 4f), "Four volleys were not fired after ascent");
        Check(launchers.SequenceEqual(new[] { "shot_3", "shot_1", "shot_3", "shot_1" }),
            "Air launchers did not alternate for four firing motions: " + string.Join(",", launchers));
        for (int i = 0; i < 4; i++)
        {
            int count = enhanced ? 4 : 1;
            var offsets = r.Shots.Skip(i * count).Take(count).Select(s => Vector2.SignedAngle(r.AimDirections[i], s.direction))
                .OrderBy(x => x).ToArray();
            var expected = enhanced ? new[] { -12f, -4f, 4f, 12f } : new[] { 0f };
            Check(offsets.Zip(expected, (a, b) => Mathf.Abs(a - b) < 0.05f).All(x => x), "Air volley missed its aimed tight spread");
            if (i > 0) Check(Mathf.Abs(events[i].time - events[i - 1].time - 0.38f / (enhanced ? 1.2f : 1f)) < 0.05f,
                "Air volley interval mismatch");
        }
        Check(r.Shots.First().direction.x > 0f && r.Shots.Last().direction.x < 0f, "Air volleys did not re-aim at the moved target");
        Check(Mathf.Abs(ReadFloat(r.Flight, "_peakSpeed") - (enhanced ? 12f : 6.5f)) < 0.01f, "Ascent speed multiplier mismatch");
        Check(jetSeen, "Shared flight exhaust missing");
        Check(Mathf.Abs(events[0].time - start - arrivedAt) < 0.03f, "Air shot did not follow arrival");
        Check(idleAfterShot && Time.time - start < (enhanced ? 2.7f : 3.4f), "Air transition/recovery was too slow");
        return new { pattern = "AirTransition", enhanced, total = Time.time - start,
            shotsAt = events.Select(e => e.time - start).ToArray(), arrivedAt, volleys = events.Length, launchers, shots = r.Shots.Count, damagePerShot = 10f,
            flyingSeen, idleAfterShot, jetSeen, postAttackDelay = ReadFloat(r.Air, "_recoverUntil") - ReadFloat(r.Air, "_shotEndsAt") };
    }

    private static async Task<object> Descent(Rig r)
    {
        r.Prepare(true, 4f);
        var field = typeof(ProductionControlDrone).GetField("_state", Private);
        typeof(ProductionControlDrone).GetMethod("Change", Private).Invoke(r.Boss, new[] { Enum.Parse(field.FieldType, "Descend") });
        float start = Time.time;
        await Until(() => Read(r.Boss, "_state").ToString() == "Recover", 2f, "Normal descent was too slow");
        float duration = Time.time - start;
        Check(duration < 1.3f && Mathf.Abs(r.Root.transform.position.y - 0.9f) < 0.01f, "Descent did not land quickly");
        await Awaitable.NextFrameAsync();
        Check(r.Root.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"), "Descent recovery did not use Idle");
        return new { pattern = "Descent", duration, speed = 10f, idleAfterLanding = true };
    }

    private static async Task<object> Laser(Rig r, bool enhanced)
    {
        r.Prepare(true, 0f);
        int hits = r.Probe.ReceivedHitCount;
        float damageBefore = r.Probe.TotalDamage;
        Check(r.Skills.TryUseSpecifiedSkill(r.Laser), "Laser did not start");
        int legs = (int)Read(r.Laser, "_sweepCount");
        float speed = ReadFloat(r.Laser, "_startSpeed");
        Check(legs == (enhanced ? 2 : 1) && Mathf.Abs(speed - (enhanced ? 720f : 600f)) < 0.01f, "Laser enhancement mismatch");
        var shake = r.Laser.GetComponent<BossLaserShake>();
        bool continuousShake = false, repeatWarningSeen = false, pauseSeen = false;
        float repeatWarningAt = -1f;
        await Until(() =>
        {
            if (enhanced && (bool)Read(r.Laser, "_waitingForRepeat"))
            {
                Check(!shake.IsShaking && !r.Root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>().enabled,
                    "Beam or shake remained active during the pause");
                pauseSeen = true;
            }
            if (r.Laser.IsPreparing && r.Laser.DidFire)
            {
                var warning = (object)typeof(EnemyAttackSkill).GetProperty("Telegraph", Private).GetValue(r.Laser);
                Check((bool)warning.GetType().GetProperty("IsVisible").GetValue(warning), "Second warning did not appear");
                Check(r.Probe.ReceivedHitCount - hits == 1 && !shake.IsShaking, "Damage or shake happened during second warning");
                if (repeatWarningAt < 0f) repeatWarningAt = Time.time;
                repeatWarningSeen = true;
            }
            if (r.Laser.DidFire && !r.Laser.IsPreparing && !(bool)Read(r.Laser, "_waitingForRepeat"))
            {
                float elapsed = Time.time - ReadFloat(r.Laser, "_beamStartedAt");
                if (elapsed < ReadFloat(r.Laser, "_sweepDuration"))
                {
                    Check(shake.IsShaking && shake.Strength == 1f, "Laser shake stopped during a sweep");
                    continuousShake = true;
                }
            }
            return !r.Laser.IsUsing;
        }, 5f, "Laser stalled");
        Check(continuousShake && !shake.IsShaking, "Laser shake did not start or clean up");
        var events = r.Events.Where(e => e.skill == r.Laser).ToArray();
        Check(events.Length == (enhanced ? 2 : 1), "Wrong number of separate laser releases");
        if (enhanced)
        {
            Check(!repeatWarningSeen && pauseSeen, "Last Stand should pause without a second warning");
        }
        Check(r.Probe.ReceivedHitCount - hits == (enhanced ? 2 : 1) && Mathf.Abs(r.Probe.TotalDamage - damageBefore - (enhanced ? 56f : 24f)) < 0.01f, "Laser damage per crossing mismatch");
        float final = Mathf.DeltaAngle(0f, r.Root.transform.Find("LaserBeam").eulerAngles.z);
        Check(enhanced ? final < -55f : final > 55f, "Laser did not return on Last Stand");
        return new { pattern = "Laser", enhanced, crossings = legs, releases = events.Length, repeatWarningSeen, pauseSeen,
            speed, finalAngle = final, damage = r.Probe.TotalDamage - damageBefore, hits = r.Probe.ReceivedHitCount - hits };
    }

    private static async Task<object> Bomb(Rig r, bool enhanced)
    {
        r.Prepare(true, 4f);
        float startedAt = Time.time, damageBefore = r.Probe.TotalDamage, descentAt = -1f;
        float minX = r.Root.transform.position.x, maxX = minX;
        int hitsBefore = r.Probe.ReceivedHitCount;
        Check(r.Skills.TryUseSpecifiedSkill(r.Bomb), "Bomb did not start");
        var markers = new Dictionary<int, (DroneBombTargetGraphic graphic, Vector3 point, EnemyLobProjectile bomb, float born)>();
        bool ringShrank = false, impactHidden = false, predictionSeen = false;
        var impactDelays = new Dictionary<int, float>();
        var sheet = enhanced ? null : new Texture2D(1536, 384, TextureFormat.RGBA32, false);
        bool[] captured = new bool[3];
        DroneBombTargetGraphic firstMarker = null;
        float deadline = Time.realtimeSinceStartup + 9f;

        while (r.Bomb.IsUsing && Time.realtimeSinceStartup < deadline)
        {
            float elapsed = Time.time - startedAt;
            minX = Mathf.Min(minX, r.Root.transform.position.x);
            maxX = Mathf.Max(maxX, r.Root.transform.position.x);
            r.Player.transform.position = new Vector3(3f + elapsed * 0.35f, 0f, 0f);
            Physics2D.SyncTransforms();
            if ((bool)Read(r.Bomb, "_descending") && descentAt < 0f) descentAt = elapsed;
            foreach (var marker in Object.FindObjectsByType<DroneBombTargetGraphic>(FindObjectsSortMode.None))
            {
                if (markers.ContainsKey(marker.GetInstanceID())) continue;
                var projectile = marker.GetComponentInParent<EnemyLobProjectile>();
                markers[marker.GetInstanceID()] = (marker, marker.transform.position, projectile, Time.time);
                predictionSeen |= marker.transform.position.x > r.Player.transform.position.x + 0.03f;
                Check(Mathf.Abs(ReadFloat(projectile, "_radius") - 52.5f / 28f) < 0.001f &&
                    ReadFloat(projectile, "_damage") == (enhanced ? 26f : 22f), "Bomb radius or damage mismatch");
                if (firstMarker == null) firstMarker = marker;
                Canvas.ForceUpdateCanvases();
                Check(marker.canvasRenderer.GetMesh().vertexCount > 400, "Bomb warning mesh missing");
            }

            foreach (var entry in markers)
            {
                var marker = entry.Value;
                if (marker.graphic == null) continue;
                Check(Vector3.Distance(marker.graphic.transform.position, marker.point) < 0.001f, "Bomb warning followed player");
                ringShrank |= marker.graphic.CountdownProgress > 0.2f && marker.graphic.CountdownProgress < 0.95f;
                if (marker.bomb != null && !marker.bomb.IsInFlight && !marker.graphic.gameObject.activeSelf)
                {
                    Check(marker.graphic.CountdownProgress == 1f, "Warning did not end at impact");
                    impactHidden = true;
                    if (!impactDelays.ContainsKey(entry.Key)) impactDelays[entry.Key] = Time.time - marker.born;
                }
            }

            if (sheet != null && firstMarker != null && firstMarker.gameObject.activeSelf)
                for (int i = 0; i < 3; i++)
                    if (!captured[i] && firstMarker.CountdownProgress >= new[] { 0.025f, 0.5f, 0.98f }[i])
                    { Capture(r.Camera, sheet, i, firstMarker.transform.position); captured[i] = true; }

            await Awaitable.NextFrameAsync();
        }

        // 스킬이 마지막 착탄 Update에 종료되어도 경고 Graphic의 같은 프레임 정리를 관찰합니다.
        await Awaitable.NextFrameAsync();
        foreach (var entry in markers)
        {
            var marker = entry.Value;
            if (marker.bomb != null && !marker.bomb.IsInFlight && marker.graphic != null && !marker.graphic.gameObject.activeSelf)
            {
                impactHidden = true;
                if (!impactDelays.ContainsKey(entry.Key)) impactDelays[entry.Key] = Time.time - marker.born;
            }
        }

        Check(!r.Bomb.IsUsing && r.Bomb.DroppedCount == (enhanced ? 6 : 3) && markers.Count == r.Bomb.DroppedCount,
            "Bomb drop count or completion mismatch");
        var times = r.Events.Where(e => e.skill == r.Bomb).Select(e => e.time).ToArray();
        Check(Mathf.Abs(times[0] - startedAt - (enhanced ? 0.65f : 0.8f)) < 0.06f, "First bomb time mismatch");
        for (int i = 1; i < times.Length; i++)
            Check(Mathf.Abs(times[i] - times[i - 1] - (enhanced ? 0.508f : 1.05f)) < 0.04f, "Bomb interval mismatch");
        Check(impactDelays.Count == markers.Count && impactDelays.Values.All(t => Mathf.Abs(t - (enhanced ? 1.15f : 1.65f)) < 0.07f),
            "Marker to explosion delay mismatch: " + impactDelays.Count + "/" + markers.Count + " " + string.Join(",", impactDelays.Values));
        Check(descentAt >= times.Last() - startedAt && Time.time - startedAt < 6f &&
            r.Bomb.DidReturnToGround && Mathf.Abs(r.Root.transform.position.y - 0.9f) < 0.02f, "Bomb descent or total timeline mismatch");
        Check(ringShrank && impactHidden && predictionSeen, "Prediction, countdown or impact synchronization failed");
        Check(maxX - minX > 0.5f, "Shared bombardment movement missing");
        Check(r.Probe.ReceivedHitCount - hitsBefore == markers.Count &&
            Mathf.Abs(r.Probe.TotalDamage - damageBefore - markers.Count * (enhanced ? 26f : 22f)) < 0.01f, "Actual bomb splash damage mismatch");
        if (sheet != null)
        {
            Check(captured.All(x => x), "Warning preview timing missed");
            System.IO.File.WriteAllBytes("Tools~/CTJ/Boss23TargetBombPreview.png", sheet.EncodeToPNG());
            Object.Destroy(sheet);
        }
        return new { pattern = "Bomb", enhanced, drops = markers.Count, firstDrop = times[0] - startedAt,
            interval = enhanced ? 0.508f : 1.05f, warningDurations = impactDelays.Values.ToArray(),
            radius = 52.5f / 28f, damagePerBomb = enhanced ? 26 : 22,
            descentAt, total = Time.time - startedAt, predictionSeen, fixedMarkers = true, horizontalTravel = maxX - minX };
    }

    private static object RandomChoices(Rig r)
    {
        int[] ground = new int[3];
        for (int i = 0; i < 240; i++)
        {
            r.Prepare(false, 4f);
            Call(r.Boss, "ChooseGroundPattern");
            var active = r.Skills.ActiveSkill;
            Check(active == r.Charge || active == r.Barrage || active == r.Air, "Invalid ground candidate");
            ground[active == r.Charge ? 0 : active == r.Barrage ? 1 : 2]++;
        }
        int[] air = new int[2];
        for (int i = 0; i < 160; i++)
        {
            r.Prepare(true, 4f);
            Call(r.Boss, "ChooseAirPattern");
            var active = r.Skills.ActiveSkill;
            Check(active == r.Laser || active == r.Bomb, "Invalid air candidate");
            air[active == r.Laser ? 0 : 1]++;
        }
        Check(ground.All(x => x > 45 && x < 115) && air.All(x => x > 45 && x < 115), "Random groups are biased or unavailable");
        return new { pattern = "RandomSelection", ground, air };
    }

    private static async Task<object> GroundRest(Rig r)
    {
        r.Prepare(false, 1.5f);
        typeof(ProductionControlDrone).GetMethod("StartGround", Private).Invoke(r.Boss, new object[] { r.Charge });
        await Until(() => !r.Charge.IsUsing, 4f, "Ground attack did not complete");
        float rest = await CheckRest(r, false);
        return new { pattern = "GroundRest", duration = rest, idle = true, damageAccepted = true };
    }

    private static async Task<object> AirRest(Rig r)
    {
        r.Prepare(false, 4f);
        typeof(ProductionControlDrone).GetMethod("StartGround", Private).Invoke(r.Boss, new object[] { r.Air });
        await Until(() => !r.Air.IsUsing, 4f, "Air transition did not complete");
        float rest = await CheckRest(r, true);
        return new { pattern = "AirRest", duration = rest, flightIdle = true };
    }

    private static async Task<float> CheckRest(Rig r, bool airborne)
    {
        await Until(() => Read(r.Boss, "_state").ToString() == "Recover", 0.2f, "FSM did not enter pattern recovery");
        float start = Time.time;
        int events = r.Events.Count;
        Vector3 position = r.Root.transform.position;
        var health = r.Root.GetComponentInChildren<NewHealthModule>();
        float before = health.CurrentHealth;
        r.Boss.ApplyDamage(new _Scripts.DevLib.BattleSystem.DamageData { DamageAmount = 3f },
            r.Root.transform.position, Vector2.left, Vector2.right);
        Check(health.CurrentHealth < before, "Boss was invulnerable during the counterattack window");
        while (Time.time - start < 1.1f)
        {
            bool movement = airborne ? Vector3.Distance(r.Root.transform.position, position) < 0.01f :
                Mathf.Abs(r.Root.transform.position.x) <= Mathf.Abs(position.x) + 0.01f;
            Check(Read(r.Boss, "_state").ToString() == "Recover" && !r.Skills.IsBusy && r.Events.Count == events && movement,
                "Boss attacked or failed to return toward center during recovery");
            await Awaitable.NextFrameAsync();
        }
        Check(r.Root.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0)
            .IsName(airborne ? "Base Layer.AirIdle" : "Base Layer.Idle"), "Recovery did not keep the default pose");
        await Until(() => Read(r.Boss, "_state").ToString() != "Recover", 0.4f, "Pattern recovery stalled");
        float duration = Time.time - start;
        Check(duration >= 1.22f && duration < 1.33f, "Pattern recovery duration mismatch");
        return duration;
    }

    private static async Task<object> BombFlow(Rig r)
    {
        r.Prepare(true, 4f);
        typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime").SetValue(r.Laser, Time.time + 100f);
        r.SetState("ChooseAir");
        await Until(() => r.Bomb.IsUsing, 0.5f, "FSM did not choose the bomb");
        await Until(() => !r.Bomb.IsUsing, 8f, "FSM bomb did not finish");
        Check(r.Bomb.DidReturnToGround && Mathf.Abs(r.Root.transform.position.y - 0.9f) < 0.02f &&
            Read(r.Boss, "_state").ToString() != "Descend", "FSM repeated descent after the bomb");
        float rest = await CheckRest(r, false);
        return new { pattern = "BombFlow", returnedToGround = true, counterattackWindow = rest, repeatedDescent = false };
    }

    private static async Task<object> LastStandEntry(Rig r)
    {
        r.Prepare(false, 4f);
        r.Flight.Place(new Vector2(-3f, 0.9f));
        r.SetHealth(0.36f);
        await Awaitable.NextFrameAsync();
        Check(!r.Boss.IsLastStand, "Last Stand started above 35%");
        r.SetHealth(0.35f);
        await Until(() => r.Boss.IsLastStand, 0.3f, "Last Stand did not latch at 35%");
        Check((bool)Read(r.Boss, "_lastStandEntryPending"), "Entry reservation missing");
        var entry = r.Root.GetComponentInChildren<DroneLastStandEntrySkill>();
        var shield = r.Root.GetComponentInChildren<DroneShieldGraphic>(true);
        var feedback = r.Root.GetComponentInChildren<DroneLastStandFeedbackModule>();
        var sparks = (ParticleSystem)new SerializedObject(feedback).FindProperty("malfunctionSparks").objectReferenceValue;
        var health = r.Root.GetComponentInChildren<NewHealthModule>();
        var hit = new DamageData { DamageAmount = 10f };
        float before = health.CurrentHealth;
        r.Boss.ApplyDamage(hit, r.Root.transform.position, Vector2.right, Vector2.left);
        Check(r.Boss.IsInvulnerable && health.CurrentHealth == before, "Entry did not reject damage");
        Check(!shield.enabled, "Shield appeared before reaching aerial height");
        await Until(() => feedback.IsShieldForming, 2f, "Shield formation did not start after ascent");
        Check(shield.enabled && Mathf.Abs(r.Root.transform.position.y - 4.5f) < 0.02f && !r.Flight.IsMoving,
            "Shield started before arriving/stopping in the air");
        Check(!r.Air.IsUsing, "Normal air transformation was used");
        float formationStarted = Time.time;
        Vector3 formationPosition = r.Root.transform.position;
        float centerAt = -1f, innerAt = -1f, outerAt = -1f;
        bool fadedCenter = false, fadedInner = false, fadedOuter = false;
        while (!feedback.IsShieldReady && Time.time - formationStarted < 2f)
        {
            Check(r.Events.Count == 0 && !r.Flight.IsMoving && Vector3.Distance(formationPosition, r.Root.transform.position) < 0.001f,
                "Boss moved or attacked before shield completed");
            float center = ReadFloat(shield, "_centerOpacity");
            float inner = ReadFloat(shield, "_innerOpacity");
            float outer = ReadFloat(shield, "_outerOpacity");
            if (center > 0f && centerAt < 0f) centerAt = Time.time - formationStarted;
            if (inner > 0f && innerAt < 0f) innerAt = Time.time - formationStarted;
            if (outer > 0f && outerAt < 0f) outerAt = Time.time - formationStarted;
            fadedCenter |= center > 0.05f && center < 0.95f;
            fadedInner |= inner > 0.05f && inner < 0.95f;
            fadedOuter |= outer > 0.05f && outer < 0.95f;
            await Awaitable.NextFrameAsync();
        }
        Check(feedback.IsShieldReady && centerAt < innerAt && innerAt < outerAt && fadedCenter && fadedInner && fadedOuter,
            "Shield layers did not fade in sequentially");
        float formationDuration = Time.time - formationStarted;
        Check(ReadFloat(shield, "innerArcDegrees") < 359f && ReadFloat(shield, "outerArcDegrees") < 359f,
            "Shield lines are still full circles");
        float firstAngle = ReadFloat(shield, "_innerAngle");
        float angleAt = ReadFloat(feedback, "_shieldElapsed");
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        float angularSpeed = Mathf.DeltaAngle(firstAngle, ReadFloat(shield, "_innerAngle")) /
            (ReadFloat(feedback, "_shieldElapsed") - angleAt);
        Check(Mathf.Abs(angularSpeed - 42f) < 0.1f, "Shield arc did not rotate at constant configured speed: " + angularSpeed);
        var bar = Object.FindObjectsByType<BossHealthBarUI>(FindObjectsSortMode.None).First();
        var label = (UnityEngine.UI.Text)new SerializedObject(bar).FindProperty("invulnerableLabel").objectReferenceValue;
        Check(label.gameObject.activeInHierarchy, "Boss HP bar did not show invulnerability");
        CaptureLastStand(r, "Boss23LastStandShieldPreview.png");

        var observedBombs = new HashSet<int>();
        var landings = new Dictionary<int, Vector2>();
        int bombs = 0;
        bool safe = true;
        float started = Time.time;
        while (!r.Laser.IsPreparing && Time.time - started < 14f)
        {
            Check(!r.Boss.IsGroggy, "Groggy began in the air");
            safe &= r.Boss.IsInvulnerable && shield.enabled;
            r.Player.transform.position = new Vector3(4f + Mathf.Sin(Time.time * 2f), 0f, 0f);
            foreach (var projectile in Object.FindObjectsByType<EnemyLobProjectile>(FindObjectsSortMode.None))
            {
                int id = projectile.GetInstanceID();
                Vector2 landing = (Vector2)Read(projectile, "_landingPoint");
                if (observedBombs.Add(id))
                {
                    bombs++;
                    landings[id] = landing;
                    float flightTime = (int)Read(projectile, "_steps") * Time.fixedDeltaTime;
                    Check(ReadFloat(projectile, "_damage") == 26f && Mathf.Abs(flightTime - 1.15f) <= Time.fixedDeltaTime,
                        "Entry bomb did not use enhanced damage/speed");
                }
                Check(Vector2.Distance(landings[id], landing) < 0.001f, "Bomb target followed the player after warning");
            }
            await Awaitable.NextFrameAsync();
        }

        Check(r.Laser.IsPreparing && entry.DidComplete, "Entry did not return for the laser");
        CaptureLastStand(r, "Boss23LastStandShieldPreview.png");
        Check(safe && bombs == 4 && entry.DroppedBombs == 4 && entry.FiredVolleys == 3 && r.Shots.Count == 9,
            "Shield or entry counts failed: bombs=" + bombs + ", volleys=" + entry.FiredVolleys + ", shots=" + r.Shots.Count);
        for (int i = 0; i < 3; i++)
        {
            var directions = r.Shots.Skip(i * 3).Take(3).Select(s => s.direction).ToArray();
            float spread = directions.SelectMany(a => directions.Select(b => Vector2.Angle(a, b))).Max();
            Check(Mathf.Abs(spread - 70f) < 0.1f, "Entry shotgun spread was not wide");
        }
        Check(Mathf.Abs(r.Root.transform.position.x) < 0.02f && Mathf.Abs(r.Root.transform.position.y - 4.5f) < 0.02f,
            "Laser did not start at aerial center");
        Check((int)Read(r.Laser, "_sweepCount") == 2 && ReadFloat(r.Laser, "_currentDamage") == 28f,
            "Entry laser was not enhanced");
        string[] order = r.Events.Where(e => e.skill == entry || e.skill == r.Bomb)
            .Select(e => e.skill == entry ? "shotgun" : "bomb").ToArray();
        Check(string.Join(",", order) == "bomb,shotgun,bomb,shotgun,bomb,shotgun,bomb", "Wrong interleaving order");

        await Until(() => Read(r.Boss, "_state").ToString() == "EntryFall", 4f, "Laser did not lead into falling");
        await Awaitable.NextFrameAsync();
        Check(shield.enabled && label.gameObject.activeSelf && r.Boss.IsInvulnerable && !r.Boss.IsGroggy && r.Root.transform.position.y > 0.95f,
            "Shield/HP label or airborne groggy timing is incorrect");
        Check(r.Events.Count(e => e.skill == r.Laser) == 2, "Enhanced laser did not sweep twice");
        var listener = r.Camera.gameObject.AddComponent<CinemachineExternalImpulseListener>();
        listener.ChannelMask = BossLaserShake.ImpulseChannel;
        listener.Gain = 1f;
        listener.Use2DDistance = true;
        listener.ReactionSettings.AmplitudeGain = 0f;
        before = health.CurrentHealth;
        r.Boss.ApplyDamage(hit, r.Root.transform.position, Vector2.right, Vector2.left);
        float fallingDamage = before - health.CurrentHealth;
        Check(fallingDamage == 0f, "Falling boss should remain invulnerable until landing");
        var fallSamples = new List<float>();
        float fallDeadline = Time.time + 1.5f;
        while (!r.Boss.IsGroggy && Time.time < fallDeadline)
        {
            if (Read(r.Boss, "_state").ToString() == "EntryFall") fallSamples.Add(ReadFloat(r.Boss, "_entryFallVelocity"));
            await Awaitable.NextFrameAsync();
        }
        Check(r.Boss.IsGroggy && fallSamples.Count > 2 && fallSamples.Last() > fallSamples.First() * 2f,
            "Fall did not accelerate into landing");
        float landedAt = Time.time;
        Check(Mathf.Abs(r.Root.transform.position.y - 0.9f) < 0.02f && !r.Flight.IsMoving, "Groggy started before landing");
        float shakePeak = 0f;
        float receivedShakePeak = 0f;
        var shutdownAlphas = new List<float>();
        int flickerChanges = 0;
        int particleCount = 0;
        bool yellow = false, orange = false;
        var particles = new ParticleSystem.Particle[80];
        while (Time.time - landedAt < 0.35f)
        {
            CinemachineImpulseManager.Instance.GetImpulseAt(r.Root.transform.position, true, BossLaserShake.ImpulseChannel,
                out Vector3 shake, out _);
            shakePeak = Mathf.Max(shakePeak, shake.magnitude);
            receivedShakePeak = Mathf.Max(receivedShakePeak, ((Vector3)Read(listener, "m_ImpulsePosLastFrame")).magnitude);
            if (shield.enabled) shutdownAlphas.Add(ReadFloat(shield, "_centerOpacity"));
            int count = sparks.GetParticles(particles);
            particleCount = Mathf.Max(particleCount, count);
            for (int i = 0; i < count; i++)
            {
                Color c = particles[i].startColor;
                yellow |= c.r > 0.9f && c.g > 0.65f;
                orange |= c.r > 0.9f && c.g < 0.6f;
            }
            await Awaitable.NextFrameAsync();
        }
        Check(sparks.isPlaying, "Groggy sparks not playing");
        var bodyRenderer = r.Root.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
        var sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
        var number = bodyRenderer.transform.Find("ProcessNumber");
        Check(sparks.transform.parent == number && Vector3.Distance(sparks.transform.position, number.position) < 0.001f &&
            sparkRenderer.sortingLayerID == bodyRenderer.sortingLayerID && sparkRenderer.sortingOrder < bodyRenderer.sortingOrder,
            "Sparks did not follow the rear number or render behind the boss");
        Check(sparkRenderer.sharedMaterial.shader.name == "Enemy/DroneGroggySpark" && particleCount >= 25,
            "Spark glow/count was not increased");
        Check(bodyRenderer.sprite.name == "transition_4", "Groggy did not use the lowest crouched pose");
        for (int i = 1; i < shutdownAlphas.Count; i++)
            if (Mathf.Abs(shutdownAlphas[i] - shutdownAlphas[i - 1]) > 0.15f) flickerChanges++;
        Check(!r.Boss.IsInvulnerable && !shield.enabled && !label.gameObject.activeSelf && flickerChanges >= 2,
            "Shield did not flicker out after landing or invulnerability persisted");
        Check(shakePeak > 0.1f && receivedShakePeak > 0.1f && particleCount > 0 && yellow && orange,
            "Landing shake/listener or yellow/orange sparks missing");
        Vector3 cameraPosition = r.Camera.transform.position;
        float cameraSize = r.Camera.orthographicSize;
        r.Camera.transform.position = new Vector3(r.Root.transform.position.x, 1.1f, -10f);
        r.Camera.orthographicSize = 1.7f;
        CaptureLastStand(r, "Boss23GroggyPreview.png");
        r.Camera.transform.position = cameraPosition;
        r.Camera.orthographicSize = cameraSize;
        before = health.CurrentHealth;
        r.Boss.ApplyDamage(hit, r.Root.transform.position, Vector2.right, Vector2.left);
        float groggyDamage = before - health.CurrentHealth;
        Check(groggyDamage > 0f, "Groggy damage was blocked");
        Check(!r.Boss.IsHitReacting && !r.Flight.IsMoving, "Groggy hit caused knockback/stagger");
        await Until(() => !r.Boss.IsGroggy, 5.3f, "Groggy did not end");
        float duration = Time.time - landedAt;
        Check(duration >= 4.95f && duration < 5.15f, "Groggy duration incorrect: " + duration);
        await Awaitable.NextFrameAsync();
        Check(!sparks.isPlaying, "Groggy sparks did not stop");
        Check(Read(r.Boss, "_state").ToString() == "GroggyRecover", "Groggy skipped the getting-up animation");
        float gettingUpAt = Time.time;
        var gettingUpSprites = new HashSet<Sprite>();
        while (Read(r.Boss, "_state").ToString() == "GroggyRecover" && Time.time - gettingUpAt < 1.5f)
        {
            Check(!r.Skills.IsBusy && !r.Flight.IsMoving, "Boss moved or attacked while getting up");
            gettingUpSprites.Add(bodyRenderer.sprite);
            await Awaitable.NextFrameAsync();
        }
        float gettingUpDuration = Time.time - gettingUpAt;
        Check(Read(r.Boss, "_state").ToString() == "GroggyRest" && gettingUpSprites.Count >= 4,
            "Getting-up animation was not played through");
        float restAt = Time.time;
        while (Read(r.Boss, "_state").ToString() == "GroggyRest" && Time.time - restAt < 1f)
        {
            Check(!r.Skills.IsBusy && !r.Flight.IsMoving, "Boss did not pause after getting up");
            await Awaitable.NextFrameAsync();
        }
        float afterGroggyPause = Time.time - restAt;
        Check(afterGroggyPause >= 0.60f && afterGroggyPause < 0.8f, "Wrong pause after groggy recovery");
        before = health.CurrentHealth;
        r.Boss.ApplyDamage(hit, r.Root.transform.position, Vector2.right, Vector2.left);
        float normalDamage = before - health.CurrentHealth;
        Check(Mathf.Abs(normalDamage * 2f - groggyDamage) < 0.01f, "Damage multiplier persisted after groggy");
        await Until(() => r.Skills.IsBusy, 0.5f, "Enhanced random patterns did not resume");
        Check(r.Skills.ActiveSkill != entry, "Last Stand entry repeated");

        return new { pattern = "LastStandEntry", boundary = 0.35f, order, bombs, shots = r.Shots.Count,
            laserSweeps = 2, invulnerableDuringAttacks = safe, formationDuration, centerAt, innerAt, outerAt,
            angularSpeed, flickerChanges, fallingDamage, normalDamage, groggyDamage,
            groggyDuration = duration, fallStartSpeed = fallSamples.First(), fallEndSpeed = fallSamples.Last(),
            shakePeak, receivedShakePeak, particleCount, yellow, orange, gettingUpDuration, afterGroggyPause,
            resumed = r.Skills.ActiveSkill.GetType().Name };
    }

    private static async Task<object> BombImpactSound(Rig r, SoundManager manager, bool enhanced)
    {
        r.Prepare(true, 4f);
        var expected = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Sound/Enemy/PlazmaBoom.mp3");
        var projectile = (EnemyLobProjectile)typeof(DroneBombSkill).GetMethod("LaunchTargetBomb", Private)
            .Invoke(r.Bomb, new object[] { r.Player.transform, 0f, enhanced });
        Check(projectile != null && projectile.IsInFlight, "Bomb sound fixture did not launch");
        Vector2 landing = (Vector2)Read(projectile, "_landingPoint");
        float startedAt = Time.time;
        await Until(() => projectile == null || !projectile.IsInFlight, 2.5f, "Bomb did not impact");
        var source = manager.GetComponentsInChildren<AudioSource>().FirstOrDefault(s =>
            s.clip == expected && s.isPlaying && s.time < 0.1f &&
            Vector2.Distance(s.transform.position, landing) < 0.03f);
        Check(source != null, "PlazmaBoom did not start at the impact position/frame");
        return new { pattern = "BombImpactSound", enhanced, clip = source.clip.name,
            flightSeconds = Time.time - startedAt, audioTimeAtImpact = source.time,
            volume = source.volume, pitch = source.pitch, landing = landing.ToString("F2") };
    }

    private static async Task<object> CenterReturn(Rig r, float side, bool nearWall = false)
    {
        r.Prepare(false, side * (nearWall ? 7.5f : 9f));
        r.Flight.Place(new Vector2(side * (nearWall ? 6.6f : 7f), 0.9f));
        Physics2D.SyncTransforms();
        foreach (var entry in r.Skills.Skills)
            typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime").SetValue(entry.Skill, Time.time + 100f);
        r.SetState("ChooseGround");
        await Until(() => r.Flight.IsMoving, 0.4f, "Boss did not start returning to center while skills cooled down");
        float start = r.Root.transform.position.x;
        float until = Time.time + 0.45f;
        while (Time.time < until) await Awaitable.NextFrameAsync();
        float returnedX = r.Root.transform.position.x;
        Check(Mathf.Abs(returnedX) < Mathf.Abs(start) - 0.5f && !r.Skills.IsBusy, "Return moved toward player/wall instead of center");
        typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime").SetValue(r.Barrage, float.NegativeInfinity);
        await Until(() => r.Barrage.IsUsing, 0.4f, "Ready skill waited for the center return to finish");
        float attackX = r.Root.transform.position.x;
        Check(Mathf.Abs(attackX) > 4f, "Skill started only after reaching the center");
        return new { pattern = "CenterReturn", nearWall, side, start, returnedX, attackX, skill = "Barrage", interruptedReturn = true };
    }

    private static async Task<object> CancelLastStandEntry(Rig r)
    {
        r.Prepare(false, 4f);
        r.SetHealth(0.34f);
        var entry = r.Root.GetComponentInChildren<DroneLastStandEntrySkill>();
        var shield = r.Root.GetComponentInChildren<DroneShieldGraphic>(true);
        await Until(() => entry.IsUsing && shield.enabled, 2f, "Cancellation fixture failed");
        r.Boss.SetTarget(null);
        await Awaitable.NextFrameAsync();
        Check(!r.Boss.IsInvulnerable && !r.Skills.IsBusy && !shield.enabled && !r.Boss.IsGroggy,
            "Interrupted entry left shield, invulnerability or skill active");
        return new { pattern = "EntryTargetLostCleanup", shield = false, invulnerable = false, skillActive = false };
    }

    private static void CaptureLastStand(Rig r, string file)
    {
        var target = RenderTexture.GetTemporary(800, 480, 24);
        var previous = RenderTexture.active;
        var previousTarget = r.Camera.targetTexture;
        var image = new Texture2D(800, 480, TextureFormat.RGB24, false);
        try
        {
            Canvas.ForceUpdateCanvases();
            r.Camera.targetTexture = target;
            r.Camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 800, 480), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes("Tools~/CTJ/" + file, image.EncodeToPNG());
        }
        finally
        {
            r.Camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(image);
        }
    }

    private static void Capture(Camera camera, Texture2D sheet, int index, Vector3 point)
    {
        Vector3 previousPosition = camera.transform.position;
        float previousSize = camera.orthographicSize;
        var target = RenderTexture.GetTemporary(512, 384, 24);
        var previous = RenderTexture.active;
        try
        {
            camera.transform.position = point + new Vector3(0f, 0.7f, -10f);
            camera.orthographicSize = 2.6f;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            sheet.ReadPixels(new Rect(0, 0, 512, 384), index * 512, 0); sheet.Apply();
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            camera.transform.position = previousPosition; camera.orthographicSize = previousSize;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    private static void CaptureGround(Camera camera, Vector3 point, float size = 1.6f,
        string output = "Tools~/CTJ/Boss23GroundAttackPreview.png")
    {
        var target = RenderTexture.GetTemporary(1024, 384, 24);
        var previousTarget = RenderTexture.active;
        Vector3 previousPosition = camera.transform.position;
        float previousSize = camera.orthographicSize;
        var image = new Texture2D(1024, 384, TextureFormat.RGBA32, false);
        try
        {
            camera.transform.position = point + new Vector3(-0.7f, 0f, -10f);
            camera.orthographicSize = size;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1024, 384), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes(output, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            camera.transform.position = previousPosition;
            camera.orthographicSize = previousSize;
            RenderTexture.active = previousTarget;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(image);
        }
    }

    private sealed class Rig : IDisposable
    {
        public GameObject Root;
        public readonly GameObject Player;
        public readonly Camera Camera;
        public readonly EnemyDamageProbe Probe;
        public ProductionControlDrone Boss;
        public EnemySkillModule Skills;
        public BossFlightModule Flight;
        public DroneChargeSkill Charge;
        public DroneSalvoSkill Barrage;
        public DroneAirTransitionSkill Air;
        public DroneDownwardLaserSkill Laser;
        public DroneBombSkill Bomb;
        public readonly List<(EnemyAttackSkill skill, Vector3 position, float time)> Events = new();
        public readonly List<(Vector3 origin, Vector2 direction, float muzzleY, float damage)> Shots = new();
        public readonly List<Vector2> AimDirections = new();
        private readonly HashSet<int> _seenShots = new();
        private readonly GameObject _floor;
        private readonly Sprite _white;

        public Rig()
        {
            _floor = new GameObject("Rework test floor") { layer = LayerMask.NameToLayer("Ground") };
            _floor.transform.position = Vector3.down * 0.1f;
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 0.2f);
            _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            var floorVisual = new GameObject("Floor visual");
            floorVisual.transform.SetParent(_floor.transform, false);
            floorVisual.transform.localScale = new Vector3(40f, 0.2f, 1f);
            var floorRenderer = floorVisual.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = _white; floorRenderer.color = new Color(0.2f, 0.25f, 0.28f); floorRenderer.sortingOrder = -1;

            Player = new GameObject("Rework player probe");
            var collider = Player.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; collider.size = new Vector2(0.35f, 1.35f); collider.offset = Vector2.up * 0.675f;
            Probe = Player.AddComponent<EnemyDamageProbe>();
            var cameraRoot = new GameObject("Rework camera");
            Camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.AddComponent<AudioListener>();
            Camera.orthographic = true; Camera.orthographicSize = 4.8f;
            cameraRoot.transform.position = new Vector3(0f, 3.5f, -10f);
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(0.05f, 0.065f, 0.08f);
        }

        public async Task NewBoss(bool enhanced)
        {
            ClearProjectiles();
            if (Root != null) { Root.SetActive(false); Object.Destroy(Root); await Awaitable.NextFrameAsync(); }
            Root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path), Vector3.up * 1.05f, Quaternion.identity);
            Root.SetActive(true);
            Boss = Root.GetComponent<ProductionControlDrone>();
            var arena = new SerializedObject(Root.GetComponentInChildren<BossArenaModule>());
            arena.FindProperty("halfWidth").floatValue = 14f;
            arena.ApplyModifiedPropertiesWithoutUndo();
            Skills = Root.GetComponentInChildren<EnemySkillModule>(); Flight = Root.GetComponentInChildren<BossFlightModule>();
            Charge = Root.GetComponentInChildren<DroneChargeSkill>(); Barrage = Root.GetComponentInChildren<DroneSalvoSkill>();
            Air = Root.GetComponentInChildren<DroneAirTransitionSkill>(); Laser = Root.GetComponentInChildren<DroneDownwardLaserSkill>();
            Bomb = Root.GetComponentInChildren<DroneBombSkill>();
            Boss.SetTarget(Player.transform);
            SetState(null);
            foreach (var entry in Skills.Skills)
                typeof(EnemyAttackSkill).GetEvent("AttackPerformed", Private).GetAddMethod(true).Invoke(entry.Skill,
                    new object[] { new Action<EnemyAttackSkill, Vector3>(OnAttack) });
            if (enhanced)
            {
                SetHealth(0.34f);
                await Until(() => Boss.IsLastStand, 0.3f, "Enhanced fixture did not latch phase");
                typeof(ProductionControlDrone).GetField("_lastStandEntryPending", Private).SetValue(Boss, false);
                Skills.StopAttack();
                typeof(BossBase).GetProperty("IsInvulnerable").SetValue(Boss, false);
                SetState(null);
            }
            else await Awaitable.NextFrameAsync();
            Check(Boss.IsLastStand == enhanced, "Controlled phase setup failed");
        }

        public void Prepare(bool airborne, float playerX)
        {
            Skills.StopAttack(); ClearProjectiles();
            SetState(null);
            float y = new SerializedObject(Boss).FindProperty(airborne ? "airAttackAltitude" : "lowAltitude").floatValue;
            Flight.Place(new Vector2(0f, y));
            Player.transform.position = Vector3.right * playerX;
            foreach (var entry in Skills.Skills)
                typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime").SetValue(entry.Skill, float.NegativeInfinity);
            Events.Clear(); Shots.Clear(); AimDirections.Clear(); _seenShots.Clear();
            Physics2D.SyncTransforms();
        }

        public void SetState(string state)
        {
            var field = typeof(ProductionControlDrone).GetField("_state", Private);
            field.SetValue(Boss, state == null ? Enum.ToObject(field.FieldType, -1) : Enum.Parse(field.FieldType, state));
        }

        public void SetHealth(float ratio)
        {
            var health = Root.GetComponentInChildren<NewHealthModule>();
            health.ApplyHeal(health.MaxHealth);
            health.ApplyDamage(health.MaxHealth * 0.9f);
            health.ApplyHeal(health.MaxHealth * ratio - health.CurrentHealth);
        }

        private void OnAttack(EnemyAttackSkill skill, Vector3 position)
        {
            Events.Add((skill, position, Time.time));
            int volley = Events.Count(e => e.skill == skill);
            if (skill is DroneAirTransitionSkill)
                AimDirections.Add(((Vector2)Player.GetComponent<Collider2D>().bounds.center - (Vector2)position).normalized);
            float muzzleY = skill is DroneSalvoSkill || skill is DroneAirTransitionSkill
                ? ((Transform)new SerializedObject(skill).FindProperty(volley % 2 == 0 ? "rearMuzzle" : "muzzle").objectReferenceValue).position.y : position.y;
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
            {
                if (!ReferenceEquals(Read(projectile, "_dealer"), Boss) || !_seenShots.Add(projectile.GetInstanceID())) continue;
                Shots.Add((projectile.transform.position, (Vector2)Read(projectile, "_direction"), muzzleY, ReadFloat(projectile, "_damage")));
            }
        }

        private void ClearProjectiles()
        {
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                if (ReferenceEquals(Read(projectile, "_dealer"), Boss)) Object.DestroyImmediate(projectile.gameObject);
            foreach (var projectile in Object.FindObjectsByType<EnemyLobProjectile>(FindObjectsSortMode.None))
                if (ReferenceEquals(Read(projectile, "_dealer"), Boss)) Object.DestroyImmediate(projectile.gameObject);
        }

        public void Dispose()
        {
            ClearProjectiles();
            if (Root != null) Object.Destroy(Root);
            Object.Destroy(Player); Object.Destroy(Camera.gameObject); Object.Destroy(_floor); Object.Destroy(_white);
        }
    }

    private static object Read(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static float ReadFloat(object target, string field) => (float)Read(target, field);
    private static object Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);

    private static async Task Until(Func<bool> condition, float seconds, string message)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
