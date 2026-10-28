using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Agent.Players.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // 씬 저장 없이 CTJ 전용 Play 인스턴스에서만 실행합니다.
    public static class VerifyEnemyCombatRules
    {
        private const string Cat = "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab";
        private const string Ghost = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";
        private const string Delay = "Assets/CTJ/Prototypes/Stats/EnemyAttackDelay.asset";
        private const string Player = "Assets/Lrw/GameModule/Player/Character/PlayerTestPack Variant.prefab";

        private static float PlayerDamage()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(Player);
            PlayerMeleeAttackSkill skill = player.GetComponentInChildren<PlayerMeleeAttackSkill>(true);
            StatData damage = (StatData)new SerializedObject(skill).FindProperty("physicalDamage").objectReferenceValue;
            StatModule module = player.GetComponentInChildren<StatModule>(true);
            StatGroup group = (StatGroup)new SerializedObject(module).FindProperty("baseStats").objectReferenceValue;
            return group.Stats.First(entry => entry.StatData == damage).BaseValue;
        }

        public static object ValidatePrefabs()
        {
            float damage = PlayerDamage();
            return new { playerDamage = damage, enemies = new[] { Cat, Ghost }.Select(path =>
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + path);
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                SerializedObject serialized = new SerializedObject(skill);
                Check(serialized.FindProperty("attackDelayStat").objectReferenceValue == AssetDatabase.LoadAssetAtPath<StatData>(Delay), "Delay reference");
                AttackTelegraphUI ui = (AttackTelegraphUI)serialized.FindProperty("telegraphPrefab").objectReferenceValue;
                if (path == Cat) Check(ui == null, "Melee must not have a telegraph prefab");
                else
                {
                    Check(ui != null && ui.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "World Canvas reference");
                    Check(!ui.GetComponentInChildren<Image>().raycastTarget && ui.GetComponent<GraphicRaycaster>() == null, "UI blocks input");
                }
                Check(Mathf.Approximately(skill.PostBlinkDelay, 0.15f), "Post-blink hold must be 0.15 seconds");
                Check(serialized.FindProperty("blinkBeforeAttack").boolValue == (path == Ghost), "Enemy blink setting");
                Check(Mathf.Approximately(serialized.FindProperty("telegraphOpacity").floatValue, path == Cat ? 1f : 0.6f), "Enemy alpha multiplier");
                BoxCollider2D hurtbox = enemy.GetComponent<BoxCollider2D>();
                BoxCollider2D terrain = enemy.transform.Find("TerrainBody").GetComponent<BoxCollider2D>();
                Check(hurtbox.isTrigger && !terrain.isTrigger && terrain.includeLayers.value == (1 << 6) && terrain.excludeLayers.value == ~(1 << 6), "Trigger/terrain layer split");
                Check(hurtbox.offset == terrain.offset && hurtbox.size == terrain.size && terrain.gameObject.layer == 2, "Terrain collider geometry/layer");
                Check(new SerializedObject(enemy.GetComponentInChildren<EnemyMovementModule>()).FindProperty("bodyCollider").objectReferenceValue == terrain, "Movement collider reference");
                Check(!terrain.TryGetComponent<IDamageable>(out _), "Terrain collider must not receive duplicate damage");
                StatGroup group = (StatGroup)new SerializedObject(enemy.GetComponentInChildren<StatModule>()).FindProperty("baseStats").objectReferenceValue;
                float hp = group.Stats.First(s => s.StatData.name == "MaxHealth").BaseValue;
                float delay = group.Stats.First(s => s.StatData.name == "EnemyAttackDelay").BaseValue;
                Check(enemy.ChaseReleaseRange > enemy.DetectionRange && hp > damage && delay > 0f, "Enemy config values");
                if (path == Cat)
                {
                    AnimatorController controller = (AnimatorController)enemy.GetComponentInChildren<Animator>().runtimeAnimatorController;
                    AnimatorState preparation = controller.layers[0].stateMachine.states.Select(s => s.state).Single(s => s.name == "AttackPreparation");
                    AnimationClip clip = (AnimationClip)preparation.motion;
                    Check(preparation.transitions.Length == 0 && !clip.isLooping && clip.events.Length == 0, "Preparation must hold without auto-release/events");
                    Check(serialized.FindProperty("preparationStateName").stringValue == "Base Layer.AttackPreparation", "Preparation state reference");
                    AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/CTJ/Prototypes/Animations/Catto_Attack.anim");
                    Check(attack.events.Length == 2 && attack.events.Any(e => e.functionName == "OnAttackHit" && Mathf.Approximately(e.time, 0.05f)) &&
                        attack.events.Any(e => e.functionName == "OnAttackFinished" && Mathf.Approximately(e.time, 0.4f)), "Attack hit/recovery events");
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(attack, AnimationUtility.GetObjectReferenceCurveBindings(attack).Single());
                    Check(keys.Length == 6 && keys.All(k => k.value != null) && Mathf.Approximately(keys[4].time, 0.2f) &&
                        Mathf.Approximately(keys[5].time, 0.4f) && keys[4].value == keys[5].value, "Attack recovery keys/references");
                }
                return new { path, detection = enemy.DetectionRange, release = enemy.ChaseReleaseRange, hp, hitsToKill = Mathf.CeilToInt(hp / damage), delay, postBlinkDelay = skill.PostBlinkDelay,
                    hasTelegraph = ui != null, blink = serialized.FindProperty("blinkBeforeAttack").boolValue, alpha = path == Cat ? 0 : 30, triggerBody = hurtbox.isTrigger, referencesValid = true };
            }).ToArray() };
        }

        public static async Task<object> RunScenarios()
        {
            RequirePlay();
            return await Task.WhenAll(
                Chase(Cat, 4000f), Chase(Ghost, 4100f),
                Health(Cat, 4200f, 4), Health(Ghost, 4300f, 3),
                Melee(4400f, false), Melee(4500f, true),
                Ranged(4600f, false), Ranged(4700f, true),
                Cancellation(Cat, 4800f), Cancellation(Ghost, 4900f),
                ReleaseDuringAttack(Cat, 5000f), ReleaseDuringAttack(Ghost, 5100f),
                DelayStat(5200f));
        }

        public static async Task<object> RunCollisionScenarios()
        {
            RequirePlay();
            return await Task.WhenAll(
                WalkThrough(Cat, 6000f, 7), WalkThrough(Ghost, 6100f, 7),
                WalkThrough(Cat, 6200f, 0), WalkThrough(Ghost, 6300f, 0), WalkThrough(Cat, 6400f, 8),
                TerrainCollision(Cat, 6500f), TerrainCollision(Ghost, 6600f),
                PlayerCasterDamage(Cat, 6700f), PlayerCasterDamage(Ghost, 6800f), StopAtMeleeRange(6900f));
        }

        private static async Task<object> WalkThrough(string path, float x, int actorLayer)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                EnemyDamageProbe actor = SolidActor(arena, x - 3f, actorLayer);
                Rigidbody2D actorBody = actor.GetComponent<Rigidbody2D>();
                Collider2D actorCollider = actor.GetComponent<Collider2D>();
                Collider2D terrain = enemy.transform.Find("TerrainBody").GetComponent<Collider2D>();
                await Wait(0.2f);
                float enemyX = enemy.transform.position.x;
                bool overlapped = false;
                actorBody.linearVelocityX = 4f;
                float end = Time.time + 1.6f;
                await ObserveUntil(() => Time.time >= end, () =>
                {
                    overlapped |= enemy.GetComponent<Collider2D>().bounds.Intersects(actorCollider.bounds);
                    Check(!terrain.IsTouching(actorCollider), "Terrain body physically blocked actor");
                });
                Check(overlapped && actorBody.position.x > x + 2.5f, "Actor did not pass through enemy");
                Check(Mathf.Abs(enemy.transform.position.x - enemyX) < 0.02f && enemy.GetModule<EnemyMovementModule>().IsGrounded,
                    "Actor pushed enemy or enemy fell through ground");
                actorBody.linearVelocityX = -4f;
                await Wait(1.6f);
                Check(actorBody.position.x < x - 2.5f && Mathf.Abs(enemy.transform.position.x - enemyX) < 0.02f, "Reverse crossing pushed/blocked");
                return new { scenario = "body-pass-through-both-directions", prefab = path, actorLayer, passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> TerrainCollision(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                enemy.enabled = false; // 물리만 검사. FSM의 정지/점프 입력은 제외합니다.
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                Collider2D terrain = enemy.transform.Find("TerrainBody").GetComponent<Collider2D>();
                GameObject wall = new GameObject("Ground wall");
                wall.transform.SetParent(arena.transform);
                wall.layer = 6;
                wall.transform.position = new Vector3(x + 3f, 3f);
                BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
                wallCollider.size = new Vector2(0.5f, 6f);
                body.position = new Vector2(x, 2f);
                await Wait(0.8f);
                Check(Mathf.Abs(terrain.bounds.min.y) < 0.04f, "Trigger enemy fell through floor");
                body.linearVelocityX = 5f;
                await Wait(0.8f);
                Check(terrain.bounds.max.x <= wallCollider.bounds.min.x + 0.03f && terrain.IsTouching(wallCollider), "Trigger enemy passed through wall");
                enemy.enabled = true;
                Check(enemy.GetModule<EnemyMovementModule>().IsGrounded, "Movement ground detection lost terrain collider");
                return new { scenario = "trigger-body-ground-and-wall", prefab = path, passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> PlayerCasterDamage(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                await Wait(0.15f);
                // 플레이어 파일은 읽기만 합니다. 같은 Caster/필터 설정의 복제본으로 실제 피해 경로를 호출합니다.
                OverlapDamageCaster source = AssetDatabase.LoadAssetAtPath<GameObject>(Player).GetComponentInChildren<OverlapDamageCaster>(true);
                OverlapDamageCaster caster = Object.Instantiate(source, enemy.GetComponent<Collider2D>().bounds.center, Quaternion.identity, arena.transform);
                caster.InitCaster(null);
                Physics2D.SyncTransforms();
                float before = enemy.GetModule<Lrw.Script.Agent.HealthSystem.NewHealthSystem.INewHealthModule>().CurrentHealth;
                float damage = PlayerDamage();
                Check(caster.CastDamage(damage, Vector2.right, 0f), "Player caster failed to query trigger");
                Check(Mathf.Approximately(enemy.GetModule<Lrw.Script.Agent.HealthSystem.NewHealthSystem.INewHealthModule>().CurrentHealth, before - damage), "Trigger body missed or duplicated player damage");
                return new { scenario = "existing-player-caster-single-damage-to-trigger", prefab = path, passed = true, damage };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> StopAtMeleeRange(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x, false);
                EnemyDamageProbe target = SolidActor(arena, x + 4f, 7);
                enemy.SetTarget(target.transform);
                EnemyMeleeSkill skill = enemy.GetComponentInChildren<EnemyMeleeSkill>();
                Set(skill, "<BaseCoolTime>k__BackingField", 3f);
                await ObserveUntil(() => skill.IsPreparing, () => { });
                float stoppedX = enemy.transform.position.x;
                float distance = target.transform.position.x - stoppedX;
                Check(distance > 0.75f && distance <= enemy.AttackRange + 0.02f, "Melee did not stop before body contact");
                await ObserveUntil(() => !skill.IsUsing, () =>
                {
                    Check(Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocityX) < 0.001f && Mathf.Abs(enemy.transform.position.x - stoppedX) < 0.02f,
                        "Melee advanced during preparation/attack/recovery");
                });
                Check(target.ReceivedHitCount == 1, "Stationary melee did not hit once");
                await Wait(0.25f);
                Check(State(enemy) == "EnemyAttackState" && Mathf.Abs(enemy.transform.position.x - stoppedX) < 0.02f, "Melee moved during cooldown in range");
                target.GetComponent<Rigidbody2D>().position += Vector2.right * 3f;
                await Wait(0.15f);
                Check(State(enemy) == "EnemyChaseState" && enemy.GetComponent<Rigidbody2D>().linearVelocityX > 0.1f, "Melee did not resume chase outside range");
                return new { scenario = "melee-stops-without-contact-attacks-waits-cooldown-resumes-chase", passed = true, stoppedDistance = distance };
            }
            finally { Object.Destroy(arena); }
        }

        private static EnemyDamageProbe SolidActor(GameObject arena, float x, int layer)
        {
            EnemyDamageProbe actor = Target(arena, x);
            actor.gameObject.layer = layer;
            BoxCollider2D collider = actor.GetComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<EnemyBase>(Cat).GetComponent<Collider2D>().sharedMaterial;
            Rigidbody2D body = actor.gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            return actor;
        }

        public static async Task<object> RunTimingScenarios()
        {
            RequirePlay();
            float previousCaptureDelta = Time.captureDeltaTime;
            var results = new System.Collections.Generic.List<object>();
            try
            {
                // 성능 벤치마크가 아니라 30/60 Hz 시간 간격에서 순서와 최소 대기를 검사합니다.
                foreach (int rate in new[] { 30, 60 })
                {
                    Time.captureDeltaTime = 1f / rate;
                    results.Add(await Timing(Cat, 5300f, rate));
                    results.Add(await Timing(Ghost, 5400f, rate));
                }
                results.Add(await CancelDuringHold(Cat, 5500f));
                results.Add(await CancelDuringHold(Ghost, 5600f));
                results.Add(await LongMeleePreparation(5700f));
                results.Add(await ZeroDelayWarning(5800f));
                return results;
            }
            finally { Time.captureDeltaTime = previousCaptureDelta; }
        }

        private static async Task<object> Timing(string path, float x, int rate)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                EnemyDamageProbe target = Target(arena, x + (path == Cat ? 1f : 4f));
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                Check(module.TryUseAttack(), "Timing begin");
                float started = Time.time;
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                Check((path == Cat) == (ui == null), "Runtime telegraph presence differs from enemy configuration");
                Image area = ui != null ? ui.GetComponentInChildren<Image>() : null;
                float expectedAlpha = 30f / 255f;
                SpriteRenderer sprite = enemy.GetComponentInChildren<SpriteRenderer>();
                bool blink = false;
                await Wait(0.1f);
                if (path == Cat) Check(sprite.sprite.name == "Catto_019", "Preparation pose not displayed immediately");
                await ObserveUntil(() => skill.IsHoldingWarning, () =>
                {
                    blink |= ui != null && ui.IsBlinking;
                    if (path == Cat) CheckNoMeleeTelegraph(enemy);
                    Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0, "Attack before final hold");
                });
                float holdStarted = Time.time;
                Check(blink == (path == Ghost), "Unexpected blink setting");
                if (path == Ghost) Check(ui.IsVisible && !ui.IsBlinking && Mathf.Abs(area.color.a - expectedAlpha) < 0.001f, "Hold must preserve ranged alpha");
                enemy.OnAttackWarning(); enemy.OnAttackHit(); enemy.OnAttackFinished();
                Check(skill.IsHoldingWarning && (ui == null || !ui.IsBlinking) && target.ReceivedHitCount == 0, "Old animation event bypassed/restarted hold");
                await ObserveUntil(() => !skill.IsPreparing, () =>
                {
                    if (path == Cat) CheckNoMeleeTelegraph(enemy);
                    else Check(ui.IsVisible && !ui.IsBlinking && Mathf.Abs(area.color.a - expectedAlpha) < 0.001f, "Hold flickered/hidden");
                    Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0 && !module.TryUseAttack(), "Hold allowed early attack");
                    if (path == Cat) Check(sprite.sprite.name == "Catto_019", "Preparation pose released during hold");
                });
                float released = Time.time;
                Check(released - holdStarted >= skill.PostBlinkDelay - 0.001f, "Hold shorter than configured delay");
                float hitAt = released;
                float finalPoseHold = 0f;
                if (path == Cat)
                {
                    await ObserveUntil(() => target.ReceivedHitCount > 0, () => { });
                    hitAt = Time.time;
                    Check(hitAt - released <= 0.05f + 2f / rate, "Fast hit timing");
                    CheckNoMeleeTelegraph(enemy);
                    enemy.OnAttackHit(); enemy.OnAttackHit();
                    await ObserveUntil(() => sprite.sprite.name == "Catto_023", () => { });
                    float recoveryStarted = Time.time;
                    await ObserveUntil(() => !skill.IsUsing, () =>
                    {
                        Check(!module.TryUseAttack() && target.ReceivedHitCount == 1, "Recovery interrupted/damaged twice");
                        Check(sprite.sprite.name == "Catto_023", "Recovery pose changed before finishing");
                    });
                    finalPoseHold = Time.time - recoveryStarted;
                    Check(finalPoseHold >= 0.2f - 1.1f / rate, "Final pose recovery too short");
                    Check(Time.time - hitAt >= 0.35f - 1.1f / rate, "Melee finished before recovery");
                }
                else
                {
                    Check(Shots(enemy).Length == 1 && !ui.IsVisible, "Projectile not released after hold");
                    await Wait(0.7f);
                }
                Check(!skill.IsUsing && target.ReceivedHitCount == 1, "Timing damage/end");
                return new { scenario = path == Cat ? "melee-anticipation-hold-fast-hit-recovery" : "projectile-post-blink-hold", rate,
                    passed = true, releaseAfter = released - started, holdSeconds = released - holdStarted, hitAfter = hitAt - started, finalPoseHold };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> CancelDuringHold(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                EnemyDamageProbe target = Target(arena, x + 1f);
                enemy.SetTarget(target.transform);
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                Set(skill, "<BaseCoolTime>k__BackingField", 0f);
                await Wait(0.15f);
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                Check(module.TryUseAttack(), "Hold cancel begin");
                await ObserveUntil(() => skill.IsHoldingWarning, () => { });
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                enemy.SetTarget(null);
                Check(!skill.IsUsing && TelegraphHidden(ui), "Hold target loss did not cancel");
                await Wait(0.1f);
                if (path == Cat) Check(enemy.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"), "Cancelled preparation pose stuck");
                enemy.SetTarget(target.transform);
                Check(module.TryUseAttack(), "Hold cancellation reuse");
                await ObserveUntil(() => skill.IsHoldingWarning, () => { });
                ((Agent)enemy).ApplyDamage(new DamageData { DamageAmount = 999f }, Vector2.zero, Vector2.zero, Vector2.zero);
                Check(!skill.IsUsing && TelegraphHidden(ui), "Hold death did not cancel");
                await Wait(0.4f);
                Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0, "Cancelled hold caused damage");
                return new { scenario = path == Cat ? "melee-hold-cancel-reuse-death" : "projectile-hold-cancel-reuse-death", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> LongMeleePreparation(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                enemy.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Delay)).SetBaseValue(1f);
                EnemyDamageProbe target = Target(arena, x + 1f);
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Long preparation begin");
                await Wait(0.7f);
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                Check(skill.IsPreparing && target.ReceivedHitCount == 0 && enemy.GetComponentInChildren<SpriteRenderer>().sprite.name == "Catto_019", "Preparation clip auto-released or looped");
                await ObserveUntil(() => !skill.IsUsing, () => { });
                Check(target.ReceivedHitCount == 1, "Long preparation never released");
                return new { scenario = "melee-stat-extends-held-pose-not-strike-speed", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> ZeroDelayWarning(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, x);
                enemy.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Delay)).SetBaseValue(0f);
                enemy.SetTarget(Target(arena, x + 4f).transform);
                await Wait(0.15f);
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Zero delay begin");
                float started = Time.time;
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                await ObserveUntil(() => skill.IsHoldingWarning, () => Check(Shots(enemy).Length == 0, "Zero delay skipped blink"));
                float held = Time.time;
                await ObserveUntil(() => !skill.IsPreparing, () => Check(Shots(enemy).Length == 0 && !ui.IsBlinking, "Zero delay skipped hold"));
                Check(Time.time - held >= 0.149f && Time.time - started >= ui.BlinkDuration + 0.149f && Shots(enemy).Length == 1, "Zero delay warning minimum");
                return new { scenario = "zero-stat-still-preserves-blink-and-hold", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> Chase(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x, false);
                EnemyDamageProbe target = Target(arena, x + enemy.DetectionRange + 1f);
                enemy.SetTarget(target.transform);
                await Wait(0.2f);
                Check(State(enemy) == "EnemyIdleState", "Detected outside entry range");
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.DetectionRange - 1f);
                await Wait(0.2f);
                Check(State(enemy) == "EnemyChaseState" && enemy.GetComponent<Rigidbody2D>().linearVelocityX > 0.1f, "Detection did not chase");
                target.transform.position = enemy.transform.position + Vector3.right * ((enemy.DetectionRange + enemy.ChaseReleaseRange) * 0.5f);
                await Wait(0.15f);
                Check(State(enemy) == "EnemyChaseState", "Chase broke at detection radius");
                target.transform.position = enemy.transform.position + Vector3.right * enemy.ChaseReleaseRange;
                await Wait(0.1f);
                Check(State(enemy) == "EnemyChaseState", "Chase broke at exact release boundary");
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.ChaseReleaseRange + 1f);
                await Wait(0.1f);
                Check(State(enemy) == "EnemyIdleState", "Release did not idle");
                float stoppedX = enemy.transform.position.x;
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.DetectionRange + 0.5f);
                await Wait(0.25f);
                Check(State(enemy) == "EnemyIdleState" && Mathf.Abs(enemy.transform.position.x - stoppedX) < 0.02f && stoppedX > x + 0.1f,
                    "Enemy returned home or reacquired outside detection");
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.DetectionRange - 1f);
                await Wait(0.15f);
                Check(State(enemy) == "EnemyChaseState", "Reacquire failed");
                return new { scenario = path == Cat ? "cat-chase-hysteresis" : "ghost-chase-hysteresis", passed = true, noReturn = true, boundaryAndReacquire = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Health(string path, float x, int expectedHits)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                float damage = PlayerDamage();
                float initial = enemy.GetModule<Lrw.Script.Agent.HealthSystem.NewHealthSystem.INewHealthModule>().CurrentHealth;
                Check(initial == enemy.GetModule<Lrw.Script.Agent.HealthSystem.NewHealthSystem.INewHealthModule>().MaxHealth && initial == expectedHits * damage, "HP initialization");
                for (int i = 1; i <= expectedHits; i++)
                {
                    ((IDamageable)(Agent)enemy).ApplyDamage(new DamageData { DamageAmount = damage }, Vector2.zero, Vector2.zero, Vector2.zero);
                    Check(enemy.GetModule<Lrw.Script.Agent.HealthSystem.NewHealthSystem.INewHealthModule>().CurrentHealth == initial - damage * i && enemy.IsDead == (i == expectedHits), "HP/death threshold");
                }
                Check(!enemy.GetComponent<Rigidbody2D>().simulated && enemy.GetComponentsInChildren<Collider2D>().All(c => !c.enabled), "Death collision cleanup");
                await Wait(1.2f);
                Check(enemy == null, "Death despawn");
                return new { scenario = path == Cat ? "cat-4-player-hits" : "ghost-3-player-hits", passed = true, damage, initialHP = initial };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Melee(float x, bool dodge)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                if (dodge) enemy.transform.Find("Visual").localScale = new Vector3(-1f, 1f, 1f);
                EnemyDamageProbe target = Target(arena, x + (dodge ? -1f : 1f));
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                EnemyMeleeSkill skill = enemy.GetComponentInChildren<EnemyMeleeSkill>();
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                Check(module.TryUseAttack(), "Melee begin");
                CheckNoMeleeTelegraph(enemy);
                Check(skill.IsPreparing, "No-UI melee must still prepare");
                enemy.OnAttackHit(); enemy.OnAttackFinished();
                Check(target.ReceivedHitCount == 0 && skill.IsPreparing, "Early animation event bypassed delay");
                float start = Time.time;
                await Wait(0.2f);
                Check(skill.IsPreparing && target.ReceivedHitCount == 0, "Melee released before stat delay");
                if (dodge) target.transform.position += Vector3.up * 3f;
                await ObserveUntil(() => !skill.IsUsing, () => CheckNoMeleeTelegraph(enemy));
                CheckNoMeleeTelegraph(enemy);
                Check(target.ReceivedHitCount == (dodge ? 0 : 1), "Melee event contact damage");
                enemy.OnAttackHit(); enemy.OnAttackHit();
                Check(target.ReceivedHitCount == (dodge ? 0 : 1), "Late duplicate event");
                return new { scenario = dodge ? "left-melee-dodge-no-ui" : "melee-delay-event-no-ui", passed = true, duration = Time.time - start, hits = target.ReceivedHitCount };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Ranged(float x, bool dodge)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, x);
                EnemyDamageProbe target = Target(arena, x + 4f);
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                EnemyProjectileSkill skill = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Ranged begin");
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                Vector3 direction = ui.transform.right;
                Check(ui.IsVisible && skill.IsPreparing && Shots(enemy).Length == 0, "Ranged fired immediately");
                await Wait(0.25f);
                Check(skill.IsPreparing && Shots(enemy).Length == 0 && target.ReceivedHitCount == 0, "Ranged ignored delay");
                if (dodge) target.transform.position += Vector3.up * 3f;
                bool blink = false, off = false, on = false;
                await ObserveUntil(() => !skill.IsPreparing, () =>
                {
                    blink |= ui.IsBlinking;
                    if (ui.IsBlinking)
                    {
                        off |= ui.GetComponentInChildren<Image>().color.a < 0.001f;
                        on |= ui.GetComponentInChildren<Image>().color.a > 0.1f;
                    }
                });
                Check(blink && off && on && !ui.IsVisible, "Ranged flash/hide");
                EnemyProjectile projectile = Shots(enemy).Single();
                Check(Vector3.Dot(projectile.transform.right, direction) > 0.999f, "Shot changed advertised direction");
                await Wait(0.7f);
                Check(!skill.IsUsing && target.ReceivedHitCount == (dodge ? 0 : 1), "Ranged contact/recovery");
                return new { scenario = dodge ? "ranged-fixed-aim-dodge" : "ranged-delay-flash-projectile", passed = true, hits = target.ReceivedHitCount };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> Cancellation(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                EnemyDamageProbe target = Target(arena, x + 1f);
                enemy.SetTarget(target.transform);
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                Set(skill, "<BaseCoolTime>k__BackingField", 0f);
                await Wait(0.15f);
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                Check(module.TryUseAttack(), "Cancel begin");
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                enemy.SetTarget(null);
                Check(!skill.IsUsing && TelegraphHidden(ui), "Target switch retained UI");
                enemy.SetTarget(target.transform);
                Check(module.TryUseAttack(), "Disable begin");
                enemy.enabled = false;
                Check(!skill.IsUsing && TelegraphHidden(ui), "Disable retained UI");
                enemy.enabled = true;
                if (path == Cat)
                {
                    Check(module.TryUseAttack(), "Missing animator case begin");
                    Animator animator = enemy.GetComponentInChildren<Animator>();
                    animator.enabled = false;
                    await Wait(0.45f);
                    Check(!skill.IsUsing && TelegraphHidden(ui) && target.ReceivedHitCount == 0, "Disabled animator did not cancel release");
                    animator.enabled = true;
                }
                Check(module.TryUseAttack(), "Death begin");
                ((Agent)enemy).ApplyDamage(new DamageData { DamageAmount = 999f }, Vector2.zero, Vector2.zero, Vector2.zero);
                enemy.OnAttackWarning(); enemy.OnAttackHit();
                Check(!skill.IsUsing && TelegraphHidden(ui) && enemy.IsDead, "Death retained UI/skill");
                await Wait(0.8f);
                Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0, "Cancelled attack caused damage");
                return new { scenario = path == Cat ? "cat-cancel-cleanup" : "ghost-cancel-cleanup", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> ReleaseDuringAttack(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x, false);
                EnemyDamageProbe target = Target(arena, x + 1f);
                enemy.SetTarget(target.transform);
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                await ObserveUntil(() => skill.IsPreparing, () => { });
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.ChaseReleaseRange + 2f);
                await Wait(0.1f);
                Check(State(enemy) == "EnemyIdleState" && !skill.IsUsing && TelegraphHidden(enemy.GetComponentInChildren<AttackTelegraphUI>()),
                    "Attack preparation ignored chase release");
                await Wait(0.6f);
                Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0, "Attack released after losing pursuit");
                return new { scenario = path == Cat ? "cat-release-cancels-windup" : "ghost-release-cancels-windup", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> DelayStat(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase a = Spawn(Ghost, arena, x);
                EnemyBase b = Spawn(Ghost, arena, x + 10f);
                StatData data = AssetDatabase.LoadAssetAtPath<StatData>(Delay);
                a.GetModule<IStatModule>().GetStat(data).SetBaseValue(0.8f);
                EnemyAttackSkill skill = a.GetComponentInChildren<EnemyAttackSkill>();
                Check(Mathf.Approximately(skill.AttackDelay, 0.8f) && Mathf.Approximately(b.GetComponentInChildren<EnemyAttackSkill>().AttackDelay, 0.5f), "Stat isolation");
                EnemyDamageProbe target = Target(arena, x + 4f);
                a.SetTarget(target.transform);
                await Wait(0.15f);
                Check(a.GetModule<EnemySkillModule>().TryUseAttack(), "Stat begin");
                a.GetModule<IStatModule>().GetStat(data).SetBaseValue(0.1f);
                await Wait(0.5f);
                Check(skill.IsPreparing && Shots(a).Length == 0, "In-flight stat update changed cast deadline");
                a.GetModule<EnemySkillModule>().StopAttack();
                return new { scenario = "per-enemy-delay-stat-and-cast-snapshot", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        public static async Task<string> PrepareVisualPreview()
        {
            RequirePlay();
            foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                enemy.GetModule<EnemySkillModule>().StopAttack();
                Set(enemy.GetComponentInChildren<EnemyAttackSkill>(), "<BaseCoolTime>k__BackingField", 0f);
                enemy.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Delay)).SetBaseValue(10f);
            }
            await Wait(0.2f);
            return "Runtime-only preview: preparation extended to 10 seconds. Stop Play to restore original settings.";
        }

        private static void CheckNoMeleeTelegraph(EnemyBase enemy) => Check(
            enemy.GetComponentsInChildren<AttackTelegraphUI>(true).Length == 0 && enemy.GetComponentsInChildren<Canvas>(true).Length == 0,
            "Melee must not create an attack range Canvas, including hidden ones");
        private static bool TelegraphHidden(AttackTelegraphUI ui) => ui == null || !ui.IsVisible;

        private static EnemyProjectile[] Shots(EnemyBase enemy) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
            .Where(p => ReferenceEquals(Field(p, "_dealer"), enemy)).ToArray();
        private static void DestroyShots(float x)
        {
            foreach (EnemyProjectile p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                if (Mathf.Abs(p.transform.position.x - x) < 45f) Object.Destroy(p.gameObject);
        }
        private static string State(EnemyBase enemy) => Field(Field(enemy, "_stateMachine"), "_currentState").GetType().Name;
        private static object Field(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
            }
            throw new InvalidOperationException("Field not found: " + name);
        }
        private static void Set(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static GameObject Arena(float x)
        {
            GameObject arena = new GameObject("CTJ Combat Rules runtime " + x);
            GameObject ground = new GameObject("Ground");
            ground.transform.SetParent(arena.transform);
            ground.transform.position = new Vector3(x, -0.5f);
            ground.layer = 6;
            ground.AddComponent<BoxCollider2D>().size = new Vector2(70f, 1f);
            return arena;
        }
        private static EnemyBase Spawn(string path, GameObject arena, float x, bool manual = true)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(path), new Vector3(x, 0.05f), Quaternion.identity, arena.transform);
            if (manual) { Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f); }
            return enemy;
        }
        private static EnemyDamageProbe Target(GameObject arena, float x)
        {
            GameObject go = new GameObject("Combat target");
            go.transform.SetParent(arena.transform);
            go.transform.position = new Vector3(x, 0.02f);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 0.8f);
            collider.offset = new Vector2(0f, 0.4f);
            collider.isTrigger = true;
            return go.AddComponent<EnemyDamageProbe>();
        }
        private static Task Wait(float seconds)
        {
            float end = Time.time + seconds;
            return ObserveUntil(() => Time.time >= end, () => { });
        }
        private static async Task ObserveUntil(Func<bool> condition, Action observe)
        {
            DateTime timeout = DateTime.UtcNow.AddSeconds(12);
            while (!condition())
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < timeout, "Play test timeout");
                observe();
                await Task.Yield();
            }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path ==
            "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Run only in CTJ EnemyArtTest Play Mode");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
