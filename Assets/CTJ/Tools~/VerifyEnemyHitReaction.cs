using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // CTJ 테스트 씬의 런타임 복제본만 사용합니다. 저장/공용 에셋 변경 없음.
    public static class VerifyEnemyHitReaction
    {
        private const string Cat = "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab";
        private const string Ghost = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";

        public static object ValidatePrefabs() => new[] { Cat, Ghost }.Select(path =>
        {
            EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
            Check(enemy.GetComponents<EnemyHitReactionModule>().Length == 1, "Exactly one reaction module");
            foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script");
            SerializedObject so = new SerializedObject(enemy.GetComponentInChildren<EnemyHitReactionModule>());
            Check(so.FindProperty("enableKnockback").boolValue && so.FindProperty("enableBlink").boolValue, "Feedback defaults");
            Check(so.FindProperty("cancelAttackOnHit").boolValue, "Both enemies interrupt attacks");
            Check(Mathf.Approximately(so.FindProperty("knockbackSpeed").floatValue, 5.1f) &&
                Mathf.Approximately(so.FindProperty("knockbackDuration").floatValue, 0.18f), "1.7x knockback setting");
            Check(Mathf.Approximately(so.FindProperty("blinkDuration").floatValue, 0.35f) &&
                so.FindProperty("hitColor").colorValue == Color.red, "Solid red blink setting");
            Material material = (Material)so.FindProperty("hitMaterial").objectReferenceValue;
            Check(material != null && material.shader.name == "CTJ/Enemies/SolidHitFlash" &&
                !ShaderUtil.ShaderHasError(material.shader), "Flash shader/material reference or compile error");
            Check(so.FindProperty("sprites").arraySize == 1 &&
                so.FindProperty("sprites").GetArrayElementAtIndex(0).objectReferenceValue == enemy.GetComponentInChildren<SpriteRenderer>(), "Sprite reference");
            return new { path, valid = true };
        }).ToArray();

        public static async Task<object> RunScenarios()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path ==
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Use CTJ test Play session only");
            return await Task.WhenAll(
                Knockback(Cat, 8000f, 1f, false), Knockback(Ghost, 8100f, -1f, true),
                CancelGhost(8200f, 0), CancelGhost(8300f, 1), CancelGhost(8400f, 2),
                ReleasedShot(8500f), CancelCat(8600f, 0), CancelCat(9900f, 1), CancelCat(10000f, 2), CatOptOut(10100f),
                Options(8700f, false, false, false), Options(8800f, false, true, false),
                Options(8900f, true, false, false), Options(9000f, false, false, true),
                Cleanup(9100f, 0), Cleanup(9200f, 1), Cleanup(9300f, 2), Cleanup(9400f, 3),
                TerrainAndInvalidDamage(9500f), NoModule(9600f),
                DistanceRatio(Cat, 9700f), DistanceRatio(Ghost, 9800f));
        }

        private static async Task<object> Knockback(string path, float x, float direction, bool chase)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                EnemyDamageProbe target = Target(arena, x - direction * (chase ? 8f : 4f));
                enemy.SetTarget(target.transform);
                if (chase) { Set(enemy, "detectionRange", 10f); Set(enemy, "moveSpeed", 2f); }
                GameObject source = new GameObject("Damage dealer");
                source.transform.SetParent(arena.transform);
                source.transform.position = new Vector3(x - direction * 2f, 0f);
                ModuleOwner dealer = source.AddComponent<ModuleOwner>();
                await Wait(0.2f);
                SpriteRenderer sprite = enemy.GetComponentInChildren<SpriteRenderer>();
                sprite.color = new Color(0.3f, 0.5f, 0.7f, 0.6f);
                Material originalMaterial = sprite.sharedMaterial;
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                float start = body.position.x;
                float hp = enemy.GetModule<Lrw.Script.Agent.HealthSystem.IHealthModule>().CurrentHealth;
                float vy = body.linearVelocityY;
                Hit(enemy, 2f, -direction, dealer); // 전달 방향이 반대여도 가해자 반대쪽으로 밀려야 함.
                Check(enemy.IsHitReacting && body.linearVelocityX * direction > 0f, "Direction/lock");
                Check(Mathf.Approximately(body.linearVelocityY, vy), "Knockback changed vertical velocity");
                Check(!enemy.GetModule<EnemySkillModule>().TryUseAttack(), "New attack started while knocked back");
                Check(sprite.color == new Color(1f, 0f, 0f, 0.6f) && IsSolidFlash(sprite), "Immediate solid red with original alpha");
                await Wait(0.07f);
                Check(body.linearVelocityX * direction > 0f, "FSM overwrote knockback");
                Hit(enemy, 2f, -direction, dealer);
                Check(Mathf.Approximately(enemy.GetModule<Lrw.Script.Agent.HealthSystem.IHealthModule>().CurrentHealth, hp - 4f), "Blink must not grant invulnerability");
                Check(sprite.color == new Color(1f, 0f, 0f, 0.6f) && IsSolidFlash(sprite), "Repeated hit color/material");
                float lastHitTime = Time.time;
                bool sawOriginal = false, sawRed = false;
                await Until(() => !enemy.IsHitReacting, () =>
                {
                    sawOriginal |= sprite.color == new Color(0.3f, 0.5f, 0.7f, 0.6f) && sprite.sharedMaterial == originalMaterial;
                    sawRed |= sprite.color == new Color(1f, 0f, 0f, 0.6f) && IsSolidFlash(sprite);
                    Check(Mathf.Approximately(sprite.color.a, 0.6f), "Blink changed alpha");
                });
                float distance = (body.position.x - start) * direction;
                Check(distance > 0.2f && distance < 1.3f, "Expected short knockback after repeated hits: " + distance);
                enemy.SetTarget(null);
                float blinkEnd = (float)typeof(EnemyHitReactionModule).GetField("_blinkEnd", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(enemy.GetModule<EnemyHitReactionModule>());
                Check(Mathf.Abs(blinkEnd - lastHitTime - 0.35f) < 0.001f, "Blink must extend old 0.3s duration");
                await Wait(0.35f);
                Check(sawOriginal && sawRed && sprite.color == new Color(0.3f, 0.5f, 0.7f, 0.6f), "Blink / full color restore");
                Check(sprite.sharedMaterial == originalMaterial, "Original material not restored after repeated hits");
                Check(enemy.GetModule<EnemyMovementModule>().IsGrounded, "Ground collision lost");
                return new { scenario = "knockback-blink-repeat", path, direction, chase, distance, passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> CancelGhost(float x, int phase)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, x);
                EnemyDamageProbe target = Target(arena, x + 4f);
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                EnemySkillModule skills = enemy.GetModule<EnemySkillModule>();
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                Check(skills.TryUseAttack(), "Ghost preparation start");
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                if (phase == 1) await Until(() => ui.IsBlinking);
                if (phase == 2) await Until(() => shot.IsHoldingWarning);
                Check(shot.IsPreparing, "Must hit during windup");
                float cooldownStart = shot.CoolTimeStartTime;
                Hit(enemy);
                Check(!shot.IsUsing && !skills.IsBusy && !ui.IsVisible, "Ghost preparation/UI not canceled");
                enemy.OnAttackHit(); enemy.OnAttackFinished();
                await Wait(0.75f);
                Check(Shots(x).Length == 0 && target.ReceivedHitCount == 0, "Canceled attack fired later");
                Check(shot.CoolTimeStartTime == cooldownStart, "Canceled cooldown reset");
                await Until(() => shot.RemainingCoolTime <= 0f);
                Check(skills.TryUseAttack(), "Ghost did not recover after hit and cooldown");
                return new { scenario = "ghost-preparation-cancel", phase, passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> ReleasedShot(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, x);
                EnemyDamageProbe target = Target(arena, x + 6f);
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Shot start");
                await Until(() => Shots(x).Length == 1);
                EnemyProjectile projectile = Shots(x).Single();
                Hit(enemy);
                Check(projectile != null && enemy.GetComponentInChildren<EnemyProjectileSkill>().IsUsing, "Released attack incorrectly canceled");
                await Until(() => target.ReceivedHitCount == 1);
                return new { scenario = "released-projectile-survives-hit", passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> CancelCat(float x, int phase)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                EnemyDamageProbe target = Target(arena, x + 0.7f);
                enemy.SetTarget(target.transform);
                // 타격 시작 직후와 이미 맞힌 후를 확실히 나눠 검사합니다(런타임 복제본만).
                enemy.GetComponentInChildren<Animator>().speed = 0.1f;
                await Wait(0.15f);
                EnemySkillModule skills = enemy.GetModule<EnemySkillModule>();
                Check(skills.TryUseAttack(), "Cat preparation start");
                EnemyMeleeSkill skill = enemy.GetComponentInChildren<EnemyMeleeSkill>();
                if (phase >= 1) await Until(() => !skill.IsPreparing);
                if (phase == 2) await Until(() => target.ReceivedHitCount == 1);
                int hitsBefore = target.ReceivedHitCount;
                Check(hitsBefore == (phase == 2 ? 1 : 0), "Unexpected melee hit phase");
                float cooldownStart = skill.CoolTimeStartTime;
                Hit(enemy);
                Check(!skills.IsBusy && !skill.IsUsing, "Cat attack not canceled");
                enemy.OnAttackHit(); enemy.OnAttackFinished();
                await Wait(0.85f);
                Check(target.ReceivedHitCount == hitsBefore, "Canceled melee animation dealt delayed damage");
                Check(skill.CoolTimeStartTime == cooldownStart, "Melee cancellation reset cooldown");
                Check(enemy.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"), "Melee not returned to idle");
                Check(enemy.GetComponentsInChildren<Canvas>(true).Length == 0, "Melee UI returned");
                return new { scenario = "cat-attack-cancel-preparation-strike-recovery", phase, hitsBefore, passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> CatOptOut(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                Set(enemy.GetModule<EnemyHitReactionModule>(), "cancelAttackOnHit", false);
                enemy.SetTarget(Target(arena, x + 0.7f).transform);
                await Wait(0.15f);
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Cat opt-out attack start");
                Hit(enemy);
                Check(enemy.GetComponentInChildren<EnemyMeleeSkill>().IsPreparing, "Melee interruption opt-out ignored");
                return new { scenario = "cat-attack-cancel-opt-out", passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> Options(float x, bool knockback, bool blink, bool cancel)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, x);
                EnemyHitReactionModule reaction = enemy.GetModule<EnemyHitReactionModule>();
                Set(reaction, "enableKnockback", knockback); Set(reaction, "enableBlink", blink); Set(reaction, "cancelAttackOnHit", cancel);
                enemy.SetTarget(Target(arena, x + 4f).transform);
                await Wait(0.15f);
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Option attack start");
                int additionalEffects = 0;
                ((UnityEvent)typeof(Lrw.Script.Agent.Agent).GetField("OnHit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy))
                    .AddListener(() => additionalEffects++);
                Hit(enemy);
                Check(enemy.IsHitReacting == knockback, "Independent knockback toggle");
                Color color = enemy.GetComponentInChildren<SpriteRenderer>().color;
                Check((color == Color.red && IsSolidFlash(enemy.GetComponentInChildren<SpriteRenderer>())) == blink &&
                    Mathf.Approximately(color.a, 1f), "Independent solid blink toggle / alpha");
                Check(enemy.GetComponentInChildren<EnemyProjectileSkill>().IsUsing != cancel, "Independent interruption toggle");
                Check(enemy.GetModule<Lrw.Script.Agent.HealthSystem.IHealthModule>().CurrentHealth == 28f && additionalEffects == 1, "Damage/OnHit must work with effects off");
                return new { scenario = "independent-effect-toggles-and-OnHit", knockback, blink, cancel, passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> Cleanup(float x, int mode)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                await Wait(0.15f);
                SpriteRenderer sprite = enemy.GetComponentInChildren<SpriteRenderer>();
                Color original = new Color(0.4f, 0.6f, 0.8f, 0.7f);
                sprite.color = original;
                Material originalMaterial = sprite.sharedMaterial;
                Hit(enemy);
                if (mode == 0) Hit(enemy, 100f);
                if (mode == 1) enemy.gameObject.SetActive(false);
                if (mode == 2) enemy.enabled = false;
                if (mode == 3) enemy.GetModule<EnemyHitReactionModule>().enabled = false;
                Check(!enemy.IsHitReacting && sprite.color == original, "Cleanup lock/full color");
                Check(sprite.sharedMaterial == originalMaterial, "Cleanup material not restored");
                Check(Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocityX) < 0.001f, "Cleanup velocity");
                if (mode == 0)
                    Check(enemy.IsDead && !enemy.GetComponent<Rigidbody2D>().simulated, "Death still works");
                else
                {
                    enemy.gameObject.SetActive(true); enemy.enabled = true;
                    enemy.GetModule<EnemyHitReactionModule>().enabled = true;
                    await Wait(0.35f);
                    Check(sprite.color == original, "Re-enable inherited stale flash");
                    Check(sprite.sharedMaterial == originalMaterial, "Re-enable inherited flash material");
                    Hit(enemy);
                    Check(enemy.IsHitReacting, "Re-enabled feedback failed");
                }
                return new { scenario = "cleanup-death-disable", mode, passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> TerrainAndInvalidDamage(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                GameObject wall = new GameObject("Ground wall");
                wall.transform.SetParent(arena.transform); wall.layer = 6;
                wall.transform.position = new Vector3(x + 0.85f, 1.5f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 3f);
                await Wait(0.15f);
                foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) Hit(enemy, amount);
                Check(enemy.GetModule<Lrw.Script.Agent.HealthSystem.IHealthModule>().CurrentHealth == 40f && !enemy.IsHitReacting, "Invalid damage caused reaction");
                Hit(enemy, 2f, 1f);
                await Wait(0.4f);
                Check(enemy.transform.Find("TerrainBody").GetComponent<Collider2D>().bounds.max.x < x + 0.73f, "Knockback crossed wall");
                Hit(enemy, 2f, -1f);
                Check(enemy.GetComponent<Rigidbody2D>().linearVelocityX < 0f, "Fallback hit direction");
                return new { scenario = "terrain-invalid-damage-fallback-direction", passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> NoModule(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                Object.Destroy(enemy.GetModule<EnemyHitReactionModule>());
                await Wait(0.15f);
                Hit(enemy);
                Check(enemy.GetModule<Lrw.Script.Agent.HealthSystem.IHealthModule>().CurrentHealth == 38f && !enemy.IsHitReacting, "Optional module compatibility");
                return new { scenario = "optional-module-removed", passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static async Task<object> DistanceRatio(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase baseline = Spawn(path, arena, x);
                EnemyBase current = Spawn(path, arena, x + 10f);
                Set(baseline.GetModule<EnemyHitReactionModule>(), "knockbackSpeed", 3f);
                await Wait(0.15f);
                float beforeOld = baseline.transform.position.x, beforeNew = current.transform.position.x;
                Hit(baseline, 2f, 1f); Hit(current, 2f, 1f);
                await Until(() => !baseline.IsHitReacting && !current.IsHitReacting);
                await Wait(0.04f);
                float oldDistance = baseline.transform.position.x - beforeOld;
                float newDistance = current.transform.position.x - beforeNew;
                float ratio = newDistance / oldDistance;
                Check(oldDistance > 0.1f && ratio >= 1.5f && ratio <= 2f, "Knockback ratio outside request: " + ratio);
                return new { scenario = "single-hit-distance-vs-previous-settings", path, oldDistance, newDistance, ratio, passed = true };
            }
            finally { CleanupArena(arena, x); }
        }

        private static bool IsSolidFlash(SpriteRenderer sprite) => sprite.sharedMaterial != null &&
            sprite.sharedMaterial.shader.name == "CTJ/Enemies/SolidHitFlash";

        private static void Hit(EnemyBase enemy, float amount = 2f, float direction = -1f, ModuleOwner dealer = null) =>
            enemy.ApplyDamage(new DamageData { DamageAmount = amount, Dealer = dealer, DirectedKBForce = Vector2.right * direction * 3f },
                enemy.transform.position, Vector2.right * direction, Vector2.left * direction);

        private static void Set(Object target, string name, object value)
        {
            SerializedObject so = new SerializedObject(target);
            if (value is bool flag) so.FindProperty(name).boolValue = flag;
            else so.FindProperty(name).floatValue = Convert.ToSingle(value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static GameObject Arena(float x)
        {
            GameObject arena = new GameObject("CTJ Hit Reaction runtime " + x);
            GameObject ground = new GameObject("Ground"); ground.transform.SetParent(arena.transform);
            ground.layer = 6; ground.transform.position = new Vector3(x, -0.5f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(70f, 1f);
            return arena;
        }
        private static EnemyBase Spawn(string path, GameObject arena, float x)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(path), new Vector3(x, 0.05f), Quaternion.identity, arena.transform);
            Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f);
            return enemy;
        }
        private static EnemyDamageProbe Target(GameObject arena, float x)
        {
            GameObject go = new GameObject("Hit reaction target"); go.transform.SetParent(arena.transform);
            go.transform.position = new Vector3(x, 0.02f);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.8f); col.offset = new Vector2(0f, 0.4f);
            return go.AddComponent<EnemyDamageProbe>();
        }
        private static EnemyProjectile[] Shots(float x) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
            .Where(p => Mathf.Abs(p.transform.position.x - x) < 40f).ToArray();
        private static void CleanupArena(GameObject arena, float x)
        {
            foreach (EnemyProjectile projectile in Shots(x)) Object.Destroy(projectile.gameObject);
            Object.Destroy(arena);
        }
        private static Task Wait(float seconds) { float end = Time.time + seconds; return Until(() => Time.time >= end); }
        private static async Task Until(Func<bool> condition, Action observe = null)
        {
            DateTime timeout = DateTime.UtcNow.AddSeconds(12);
            while (!condition())
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < timeout, "Hit test timeout");
                observe?.Invoke();
                await Task.Yield();
            }
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
