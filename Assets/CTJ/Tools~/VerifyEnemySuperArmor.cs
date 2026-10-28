using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // VerifyEnemy01.BeginPlay / StopPlay / RestoreScene으로 독립 씬에서 실행합니다.
    public static class VerifyEnemySuperArmor
    {
        private const string Melee = "Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab";
        private const string Ranged = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";
        private const string Boss = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
        private static int _checks;

        public static object RunEdit()
        {
            _checks = 0;
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            string[] paths = { Melee, Ranged, Boss,
                "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab",
                "Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab",
                "Assets/CTJ/Prototypes/Prefabs/Enemy04_MechanicArcher.prefab" };
            foreach (string path in paths)
            {
                EnemyBase prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                Check(prefab != null, "Prefab: " + path);
                Check(prefab.GetComponentInChildren<EnemySuperArmorModule>(true) == null, "Do not add armor to existing prefab");
                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing script: " + path);
                    SerializedProperty p = new SerializedObject(component).GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0,
                                "Broken reference: " + path + " / " + p.propertyPath);
                }
            }
            return new { passed = true, prefabs = paths.Length, checks = _checks };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay(); _checks = 0;
            foreach (bool armor in new[] { false, true })
                using (var f = new Fixture(Melee, armor)) Hit(f, false, "Idle");

            using (var f = new Fixture(Melee))
            {
                EnemyMeleeSkill skill = Start<EnemyMeleeSkill>(f);
                Hit(f, true, "Melee preparation");
                Check(skill.IsPreparing, "Preparation survives hit");
                Release(skill);
                Check(!skill.IsRecovering, "Before hit event remains active attack");
                Hit(f, true, "Melee active before event");
                Collider2D hitbox = (Collider2D)new SerializedObject(skill).FindProperty("attackHitbox").objectReferenceValue;
                f.Target.transform.position = hitbox.bounds.center - new Vector3(0f, 0.4f);
                skill.OnAttackHit();
                Check(f.Target.ReceivedHitCount == 1 && skill.IsRecovering, "Real melee damage / recovery after event");
                skill.OnAttackHit();
                Check(f.Target.ReceivedHitCount == 1, "Duplicate event must not hit twice");
                Hit(f, false, "Melee recovery");
                Check(!skill.IsUsing && !skill.IsRecovering, "Recovery hit cancels melee and resets phase");
            }

            using (var f = new Fixture(Ranged))
            {
                EnemyProjectileSkill skill = Start<EnemyProjectileSkill>(f);
                Hit(f, true, "Projectile preparation"); Release(skill);
                EnemyProjectile bullet = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
                    .FirstOrDefault(p => p.gameObject.activeInHierarchy);
                Check(bullet != null && skill.IsRecovering, "Projectile emitted / recovery starts");
                Hit(f, false, "Projectile recovery");
                Check(bullet != null && bullet.gameObject.activeSelf, "Hit after release must preserve projectile");
                f.Skills.StopAttack();
                Check(!f.Armor.IsProtected && !skill.IsRecovering, "Stopped skill clears phase");
            }

            using (var f = new Fixture(Boss))
            {
                BossRadialVolleySkill skill = Start<BossRadialVolleySkill>(f);
                Hit(f, true, "Volley preparation"); Release(skill);
                Check(skill.DidFire && skill.IsRecovering, "Volley recovery after emission");
                Hit(f, false, "Volley recovery");
            }
            using (var f = new Fixture(Boss))
            {
                BossLaserSkill skill = Start<BossLaserSkill>(f);
                Hit(f, true, "Laser preparation"); Release(skill);
                Check(skill.IsFiring && !skill.IsRecovering, "Continuous beam remains active attack");
                Hit(f, true, "Continuous laser");
                Check(skill.IsFiring, "Protected hit must not cancel beam");
                skill.FinishTraversal(); Hit(f, false, "Laser ended");
            }

            foreach (bool armor in new[] { false, true })
                using (var f = new Fixture(Melee, armor))
                {
                    EnemyMeleeSkill skill = Start<EnemyMeleeSkill>(f);
                    if (armor) f.Armor.enabled = false;
                    Hit(f, false, armor ? "Disabled module" : "No module");
                    Check(!skill.IsUsing, "Unprotected preparation must cancel");
                    if (armor) { f.Armor.enabled = true; Check(!f.Armor.IsProtected, "No stale protection on enable"); }
                }

            using (var f = new Fixture(Melee))
            {
                EnemyMeleeSkill skill = Start<EnemyMeleeSkill>(f);
                f.Enemy.SetTarget(null);
                Check(!skill.IsUsing && !f.Armor.IsProtected, "Target loss still cancels protected attack");
            }
            using (var f = new Fixture(Melee))
            {
                EnemyMeleeSkill skill = Start<EnemyMeleeSkill>(f);
                f.Enemy.gameObject.SetActive(false);
                Check(!skill.IsUsing && !f.Armor.IsProtected, "Disable clears protection");
                f.Enemy.gameObject.SetActive(true);
                Check(!f.Armor.IsProtected, "Reactivation does not restore stale armor");
            }
            using (var f = new Fixture(Melee))
            {
                EnemyMeleeSkill skill = Start<EnemyMeleeSkill>(f);
                f.Enemy.ApplyDamage(new DamageData { DamageAmount = 100000f }, f.Enemy.transform.position, Vector2.right, Vector2.left);
                Check(f.Enemy.IsDead && f.Health.CurrentHealth <= 0f && !skill.IsUsing && !f.Armor.IsProtected,
                    "Lethal damage bypasses armor and clears attack");
            }

            // 실제 FixedUpdate 이동, 애니메이션 이벤트, 쿨타임 이후 재시작까지 확인합니다.
            using (var f = new Fixture(Melee))
            {
                foreach (Animator animator in f.Enemy.GetComponentsInChildren<Animator>()) animator.speed = 1f;
                f.Target.transform.position = f.Enemy.transform.position + Vector3.right;
                await Until(() => f.Enemy.GetComponentInChildren<EnemyMeleeSkill>().IsPreparing, "Automatic preparation");
                Hit(f, true, "Automatic attack preparation");
                EnemyMeleeSkill skill = f.Enemy.GetComponentInChildren<EnemyMeleeSkill>();
                await Until(() => skill.IsRecovering, "Real animation hit event");
                Check(f.Target.ReceivedHitCount == 1, "Animated attack hits target once");
                float x = f.Enemy.transform.position.x;
                Hit(f, false, "Animated recovery");
                await Wait(0.08f);
                Check(f.Enemy.transform.position.x > x + 0.02f, "Unprotected hit actually moves body");
                await Until(() => !f.Reaction.IsReacting, "Stagger expires");
                Check(!f.Armor.IsProtected, "Cooldown has no protection");
                await Until(() => skill.IsPreparing, "Next attack");
                Check(f.Armor.IsProtected && !skill.IsRecovering, "Next cast resets recovery phase");
            }
            return new { passed = true, checks = _checks,
                scenarios = "idle / melee event / projectile / volley / sustained laser / recovery / damage / blink-scale-outline / opt-out / target loss / disable / death / real animation and knockback / recast" };
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject Root;
            public readonly EnemyBase Enemy;
            public readonly EnemyDamageProbe Target;
            public EnemySkillModule Skills => Enemy.GetModule<EnemySkillModule>();
            public EnemySuperArmorModule Armor => Enemy.GetModule<EnemySuperArmorModule>();
            public EnemyHitReactionModule Reaction => Enemy.GetModule<EnemyHitReactionModule>();
            public INewHealthModule Health => Enemy.GetModule<INewHealthModule>();

            public Fixture(string prefab, bool armor = true)
            {
                Root = new GameObject("Super armor runtime verification"); Root.SetActive(false);
                Enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(prefab), Root.transform);
                Enemy.transform.position = new Vector3(9000f, 0f);
                if (armor)
                {
                    GameObject child = new GameObject("SuperArmorModule"); child.transform.SetParent(Enemy.transform, false);
                    child.AddComponent<EnemySuperArmorModule>();
                }
                Enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                Enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                // 보스 원본은 상시 경직/넉백 비활성이므로 복제본만 켜서 슈퍼아머를 검증합니다.
                EnemyHitReactionModule reaction = Enemy.GetComponentInChildren<EnemyHitReactionModule>();
                Set(reaction, "hitStunDuration", 0.3f); Set(reaction, "enableKnockback", true); Set(reaction, "cancelAttackOnHit", true);
                foreach (Animator animator in Enemy.GetComponentsInChildren<Animator>()) animator.speed = 0f;
                GameObject target = new GameObject("Target"); target.transform.SetParent(Root.transform);
                target.transform.position = new Vector3(9001f, 0f);
                BoxCollider2D collider = target.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
                collider.size = new Vector2(0.5f, 0.8f); collider.offset = new Vector2(0f, 0.4f);
                Target = target.AddComponent<EnemyDamageProbe>();
                GameObject floor = new GameObject("Ground"); floor.transform.SetParent(Root.transform);
                floor.transform.position = new Vector3(9000f, -0.5f); floor.layer = 6;
                floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);
                Root.SetActive(true);
                Enemy.SetTarget(Target.transform);
                Physics2D.SyncTransforms();
            }

            public void Dispose()
            {
                foreach (EnemyProjectile projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                { projectile.gameObject.SetActive(false); Object.Destroy(projectile.gameObject); }
                Root.SetActive(false); Object.Destroy(Root);
            }
        }

        private static T Start<T>(Fixture f) where T : EnemyAttackSkill
        {
            T skill = f.Enemy.GetComponentInChildren<T>();
            Check(f.Skills.TryUseSpecifiedSkill(skill) && skill.IsUsing && skill.IsPreparing, "Start " + typeof(T).Name);
            return skill;
        }
        private static void Release(EnemyAttackSkill skill)
        {
            for (int i = 0; i < 3 && skill.IsPreparing; i++)
            {
                typeof(EnemyAttackSkill).GetField("_phaseEndTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(skill, -1f);
                skill.UpdateSkill();
            }
            Check(skill.IsUsing && !skill.IsPreparing, "Release " + skill.name);
        }
        private static void Hit(Fixture f, bool protectedHit, string label)
        {
            Check((f.Armor != null && f.Armor.IsProtected) == protectedHit, label + " protection");
            float hp = f.Health.CurrentHealth;
            Rigidbody2D body = f.Enemy.GetComponent<Rigidbody2D>(); body.linearVelocityX = 0f;
            f.Enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, f.Enemy.transform.position, Vector2.right, Vector2.left);
            Check(Mathf.Approximately(f.Health.CurrentHealth, hp - 1f), label + " HP must decrease");
            Check(f.Reaction.IsReacting != protectedHit && f.Reaction.IsStunned != protectedHit && f.Reaction.IsKnockedBack != protectedHit,
                label + " stagger / knockback");
            Check(protectedHit ? Mathf.Abs(body.linearVelocityX) < 0.001f : body.linearVelocityX > 1.9f, label + " velocity");
            Check(protectedHit ? f.Reaction.RemainingReactionTime == 0f : f.Reaction.RemainingReactionTime > 0f, label + " gauge");
            Check(f.Reaction.IsBlinking, label + " blink remains");
            Check(f.Enemy.GetModule<EnemyOutlineModule>().IsShowingHit, label + " outline remains");
            if (new SerializedObject(f.Reaction).FindProperty("enableScalePulse").boolValue)
                Check((bool)typeof(EnemyHitReactionModule).GetField("_scaling", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Reaction),
                    label + " scale pulse remains");
        }
        private static void Set(Object obj, string field, object value)
        {
            SerializedObject so = new SerializedObject(obj); SerializedProperty p = so.FindProperty(field);
            if (value is float number) p.floatValue = number; else p.boolValue = (bool)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Wait(float seconds)
        { float end = Time.time + seconds; await Until(() => Time.time >= end, "Wait"); }
        private static async Task Until(Func<bool> predicate, string label)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(12);
            while (!predicate()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Timeout: " + label); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
            SceneManager.GetActiveScene().path == "", "Use isolated verification scene");
        private static void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); _checks++; }
    }
}
