using System;
using System.Linq;
using System.Threading.Tasks;
using Lrw.Script.Agent;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using DevLib.BattleSystem;
using Lrw.Script.Agent.SkillSystem;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // run_script로만 실행. 런타임 복제본을 검사하며 씬/프리팹/공용 SO를 저장하지 않습니다.
    public static class VerifyEnemySkills
    {
        private const string Cat = "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab";
        private const string Ghost = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";

        public static string ExitBatchEditor(int expectedProcessId)
        {
            Check(Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode &&
                System.Diagnostics.Process.GetCurrentProcess().Id == expectedProcessId,
                "Only the batch editor started for verification may be closed.");
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
            return "Closing the verification batch editor without saving a scene.";
        }

        public static object ValidatePrefabs()
        {
            return new[] { Cat, Ghost }.Select(path =>
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                Check(enemy != null, "Enemy prefab missing: " + path);
                foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + path);
                EnemySkillModule module = enemy.GetComponent<EnemySkillModule>();
                AbstractCoolTimeSkill skill = enemy.GetComponent<AbstractCoolTimeSkill>();
                Check(module != null && skill != null && module.Skills.Count == 1 && module.Skills[0].Skill == skill,
                    "Skill/module reference: " + path);
                Check(skill.SkillSo != null, "Skill SO missing: " + path);
                SerializedObject serialized = new SerializedObject(skill);
                Check(serialized.FindProperty("attackAnimator").objectReferenceValue != null, "Animator missing: " + path);
                Check(serialized.FindProperty(path == Cat ? "attackHitbox" : "firePoint").objectReferenceValue != null,
                    "Hitbox/fire point missing: " + path);
                if (path == Ghost) Check(serialized.FindProperty("projectilePrefab").objectReferenceValue != null, "Projectile missing");
                return new { path, skill = skill.GetType().Name, key = skill.SkillSo.name, cooldown = skill.CoolTime,
                    range = module.Skills[0].MaxRange, referencesValid = true };
            }).ToArray();
        }

        public static async Task<object> RunScenarios()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path ==
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Use only the CTJ EnemyArtTest Play session.");
            return await Task.WhenAll(CooldownAndSingleHit(), DodgeDisableDeath(), ComposeTwoSkills(), RangedDamage(), ChargeInput());
        }

        private static async Task<object> CooldownAndSingleHit()
        {
            GameObject arena = Arena(3000f);
            try
            {
                EnemyBase a = Spawn(Cat, arena, 3000f);
                EnemyBase b = Spawn(Cat, arena, 3004f);
                EnemyDamageProbe targetA = Target(arena, 3001f);
                EnemyDamageProbe targetB = Target(arena, 3005f);
                a.SetTarget(targetA.transform); b.SetTarget(targetB.transform);
                EnemyMeleeSkill sa = a.GetComponent<EnemyMeleeSkill>();
                EnemyMeleeSkill sb = b.GetComponent<EnemyMeleeSkill>();
                Set(sa, "<BaseCoolTime>k__BackingField", 2.2f);
                await Wait(0.15f);
                EnemySkillModule ma = a.GetModule<EnemySkillModule>();
                Check(ma.CanUseSkill(sa.SkillSo) && sa.RemainingCoolTime == 0f, "First use must be ready");
                Check(ma.TryUseAttack(), "First attack failed");
                float first = sa.CoolTimeStartTime;
                Check(sa.IsUsing && !ma.TryUseAttack(), "Attack overlap guard failed");
                Check(sa.SkillSo == sb.SkillSo && sb.RemainingCoolTime == 0f && b.GetModule<EnemySkillModule>().TryUseAttack(),
                    "Shared SO must not share cooldown state");
                a.OnAttackHit();
                Check(targetA.ReceivedHitCount == 0, "Hit event must not bypass preparation");
                await Until(() => !sa.IsPreparing);
                a.OnAttackHit(); a.OnAttackHit();
                Check(targetA.ReceivedHitCount == 1, "Duplicate hit event must damage once");
                await Wait(0.65f);
                Check(!ma.IsBusy && targetA.ReceivedHitCount == 1, "Animation end / duplicate animation hit failed");
                Check(sa.RemainingCoolTime > 1f && !ma.CanUseSkill(sa.SkillSo), "Cooldown used normalized value instead of seconds");
                await Until(() => Time.time - first >= 2.25f);
                Check(ma.CanUseSkill(sa.SkillSo) && ma.TryUseAttack(), "Cooldown did not recover");
                return new { scenario = "first-use / 2.2s cooldown / per-instance / duplicate-hit / animation-end", passed = true,
                    elapsed = sa.CoolTimeStartTime - first, firstAttackHits = targetA.ReceivedHitCount };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> DodgeDisableDeath()
        {
            GameObject arena = Arena(3100f);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, 3100f);
                EnemyDamageProbe probe = Target(arena, 3101f);
                enemy.SetTarget(probe.transform);
                EnemyMeleeSkill skill = enemy.GetComponent<EnemyMeleeSkill>();
                Set(skill, "<BaseCoolTime>k__BackingField", 0f);
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                await Wait(0.15f);
                Check(module.TryUseAttack(), "Dodge attack failed to start");
                probe.transform.position += Vector3.up * 3f;
                await Wait(1f);
                Check(probe.ReceivedHitCount == 0 && !module.IsBusy, "Dodged attack caused damage");
                probe.transform.position -= Vector3.up * 3f;
                Physics2D.SyncTransforms();
                Check(module.TryUseAttack(), "Disable case failed to start");
                enemy.enabled = false;
                enemy.OnAttackHit();
                Check(!skill.IsUsing && !module.IsBusy && probe.ReceivedHitCount == 0, "Disabled enemy retained attack");
                enemy.enabled = true;
                Check(module.TryUseAttack(), "Target switch case failed to start");
                enemy.SetTarget(null);
                enemy.OnAttackHit();
                Check(!module.IsBusy && probe.ReceivedHitCount == 0, "Changed target retained attack");
                enemy.SetTarget(probe.transform);
                Check(module.TryUseAttack(), "Death case failed to start");
                ((Agent)enemy).ApplyDamage(new DamageData { DamageAmount = 999f }, Vector2.zero, Vector2.zero, Vector2.zero);
                enemy.OnAttackHit();
                Check(enemy.IsDead && !skill.IsUsing && !module.IsBusy && probe.ReceivedHitCount == 0, "Death did not cancel attack");
                return new { scenario = "dodge / disable / target-switch / Agent damage death cancellation", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> ComposeTwoSkills()
        {
            GameObject arena = Arena(3200f);
            GameObject inactive = new GameObject("Inactive composition staging");
            inactive.SetActive(false);
            try
            {
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Cat), inactive.transform);
                enemy.transform.position = new Vector3(3200f, 0.05f, 0f);
                EnemyMeleeSkill melee = enemy.GetComponent<EnemyMeleeSkill>();
                EnemyProjectileSkill shot = enemy.gameObject.AddComponent<EnemyProjectileSkill>();
                EnemyProjectileSkill source = AssetDatabase.LoadAssetAtPath<EnemyBase>(Ghost).GetComponent<EnemyProjectileSkill>();
                Set(shot, "<SkillSo>k__BackingField", source.SkillSo);
                Set(shot, "<BaseCoolTime>k__BackingField", 2f);
                Set(shot, "projectilePrefab", new SerializedObject(source).FindProperty("projectilePrefab").objectReferenceValue);
                GameObject muzzle = new GameObject("Composed FirePoint");
                muzzle.transform.SetParent(enemy.transform.Find("Visual"), false);
                muzzle.transform.localPosition = new Vector3(0.45f, 0.3f, 0f);
                Set(shot, "firePoint", muzzle.transform);
                Set(shot, "recoveryTime", 0.1f);
                Set(melee, "<BaseCoolTime>k__BackingField", 5f);
                EnemySkillModule module = enemy.GetComponent<EnemySkillModule>();
                SerializedObject sm = new SerializedObject(module);
                SerializedProperty entries = sm.FindProperty("skills");
                entries.arraySize = 2;
                SerializedProperty second = entries.GetArrayElementAtIndex(1);
                second.FindPropertyRelative("skill").objectReferenceValue = shot;
                second.FindPropertyRelative("maxRange").floatValue = 6f;
                second.FindPropertyRelative("maxHeightDifference").floatValue = -1f;
                sm.ApplyModifiedPropertiesWithoutUndo();
                Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f);
                enemy.transform.SetParent(arena.transform, true); // 완성된 모듈 트리로 Awake를 한 번만 실행
                EnemyDamageProbe probe = Target(arena, 3201f);
                enemy.SetTarget(probe.transform);
                await Wait(0.15f);
                Check(module.Skills.Count == 2 && ReferenceEquals(enemy.GetModule<ISkillModule>(), module), "Shared module lookup failed");
                Check(module.TryUseAttack() && module.ActiveSkill == melee, "Higher-priority melee was not selected");
                await Wait(0.95f);
                Check(melee.RemainingCoolTime > 3.9f && module.TryUseAttack() && module.ActiveSkill == shot,
                    "Cooldown fallback to second skill failed");
                await Wait(0.85f);
                Check(probe.ReceivedHitCount >= 2, "Composed projectile did not damage target");
                module.StopAttack();
                probe.transform.position = new Vector3(3204f, 0.05f, 0f);
                await Until(() => shot.RemainingCoolTime <= 0f);
                Check(module.TryUseAttack() && module.ActiveSkill == shot, "Range selection failed");
                return new { scenario = "two-skills / priority / cooldown fallback / range / shared ISkillModule lookup", passed = true };
            }
            finally { Object.Destroy(arena); Object.Destroy(inactive); }
        }

        private static async Task<object> RangedDamage()
        {
            GameObject arena = Arena(3300f);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, 3300f);
                EnemyDamageProbe probe = Target(arena, 3304f);
                enemy.SetTarget(probe.transform);
                EnemyProjectileSkill skill = enemy.GetComponent<EnemyProjectileSkill>();
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                await Wait(0.15f);
                Check(module.TryUseAttack() && module.IsBusy, "Ranged did not start");
                await Wait(1.05f);
                Check(!module.IsBusy && probe.ReceivedHitCount == 1 && probe.TotalDamage == 5f, "Ranged damage/recovery failed");
                Check(!module.CanUseSkill(skill.SkillSo), "Ranged 1.2s cooldown ended early");
                await Until(() => skill.RemainingCoolTime <= 0f);
                Check(module.TryUseAttack(), "Ranged repeat failed");
                return new { scenario = "ranged projectile damage / recovery / repeat cooldown", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> ChargeInput()
        {
            GameObject arena = Arena(3400f);
            GameObject inactive = new GameObject("Inactive charge staging");
            inactive.SetActive(false);
            SkillSO key = ScriptableObject.CreateInstance<SkillSO>();
            try
            {
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Cat), inactive.transform);
                enemy.transform.position = new Vector3(3400f, 0.05f, 0f);
                VerificationChargeSkill charge = enemy.gameObject.AddComponent<VerificationChargeSkill>();
                Set(charge, "<SkillSo>k__BackingField", key);
                Set(charge, "<BaseCoolTime>k__BackingField", 0.5f);
                EnemySkillModule module = enemy.GetComponent<EnemySkillModule>();
                SerializedObject sm = new SerializedObject(module);
                SerializedProperty entry = sm.FindProperty("skills").GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("skill").objectReferenceValue = charge;
                entry.FindPropertyRelative("chargeDuration").floatValue = 0.3f;
                sm.ApplyModifiedPropertiesWithoutUndo();
                Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f);
                enemy.transform.SetParent(arena.transform, true);
                EnemyDamageProbe probe = Target(arena, 3401f);
                enemy.SetTarget(probe.transform);
                await Wait(0.15f);
                Check(module.TryUseAttack() && charge.IsCharging && module.IsBusy, "Charge press failed");
                await Wait(0.15f);
                Check(charge.IsCharging && charge.Uses == 0, "Charge released early");
                await Wait(0.2f);
                Check(!charge.IsCharging && charge.Uses == 1 && charge.RemainingCoolTime > 0f, "Charge release failed");
                await Until(() => charge.RemainingCoolTime <= 0f);
                Check(module.TryUseAttack() && charge.IsCharging, "Charge reuse failed");
                module.enabled = false;
                Check(!charge.IsCharging && !module.IsBusy && charge.Cancels == 1 && charge.RemainingCoolTime == 0f,
                    "Module disable must cancel charge without cooldown");
                return new { scenario = "shared charge start / timed release / cooldown / disable cancellation", passed = true };
            }
            finally { Object.Destroy(arena); Object.Destroy(inactive); Object.Destroy(key); }
        }

        private static GameObject Arena(float x)
        {
            GameObject root = new GameObject("CTJ Skills runtime arena " + x);
            GameObject ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform);
            ground.transform.position = new Vector3(x, -0.5f, 0f);
            ground.layer = 6;
            ground.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
            return root;
        }

        private static EnemyBase Spawn(string path, GameObject arena, float x)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(path),
                new Vector3(x, 0.05f, 0f), Quaternion.identity, arena.transform);
            Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f);
            return enemy;
        }

        private static EnemyDamageProbe Target(GameObject arena, float x)
        {
            GameObject go = new GameObject("Damage target");
            go.transform.SetParent(arena.transform);
            go.transform.position = new Vector3(x, 0.05f, 0f);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.5f, 0.8f); box.offset = new Vector2(0f, 0.4f); box.isTrigger = true;
            return go.AddComponent<EnemyDamageProbe>();
        }

        private static void Set(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Wait(float seconds)
        {
            float end = Time.time + seconds;
            await Until(() => Time.time >= end);
        }
        private static async Task Until(Func<bool> condition)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(10);
            while (!condition())
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < end, "Timed out");
                await Task.Yield();
            }
        }
        private static void Check(bool condition, string error)
        {
            if (!condition) throw new InvalidOperationException(error);
        }
    }

    // 검증용이며 Tools~ 안에 있어 게임 어셈블리/빌드에는 들어가지 않습니다.
    public sealed class VerificationChargeSkill : AbstractChargeSkill
    {
        public int Uses { get; private set; }
        public int Cancels { get; private set; }
        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            Uses++;
            EndSkill();
        }
        protected override void OnChargeCancel() => Cancels++;
    }
}
