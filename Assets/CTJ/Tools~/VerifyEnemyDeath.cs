// CTJ EnemyArtTest의 Play Mode에서만 실행합니다. 플레이어 원본과 씬 에셋을 저장/수정하지 않습니다.
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Agents.Players;
using CTJ.Enemies;
using CTJ.Testing;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyDeath
    {
        private const string PlayerPath = "Assets/GameModules/Player/Prefabs/Player.prefab";

        public static object CheckEditorDamageMenu()
        {
            if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity")
                throw new InvalidOperationException("Run only in CTJ EnemyArtTest Play Mode.");
            foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                MethodInfo menu = enemy.GetType().GetMethod("TestTakeDamage", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(menu != null && menu.GetCustomAttribute<ContextMenu>() != null, "Inherited context menu is missing.");
                float before = enemy.HealthModule.CurrentHealth;
                menu.Invoke(enemy, null);
                Require(enemy.HealthModule.CurrentHealth == before - 2f, "Editor menu did not deliver 2 damage.");
            }
            return "Both enemy types inherit the context menu and receive exactly 2 damage.";
        }

        public static async Task<object> Run()
        {
            if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity")
                throw new InvalidOperationException("Run only in the CTJ EnemyArtTest Play session.");
            MeleeEnemy cat = Object.FindFirstObjectByType<MeleeEnemy>();
            RangedEnemy ghost = Object.FindFirstObjectByType<RangedEnemy>();
            Require(cat != null && ghost != null, "Both live CTJ prototype enemies are required.");
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            PlayerMeleeSkillModule source = player.GetComponentInChildren<PlayerMeleeSkillModule>(true);
            // 공격 모듈의 실제 프리팹 설정/자식 Caster를 복제합니다. 전체 플레이어나 입력은 생성하지 않습니다.
            PlayerMeleeSkillModule attack = Object.Instantiate(source);
            ModuleOwner owner = new GameObject("CTJ Player Attack Verification").AddComponent<ModuleOwner>();
            GameObject observerEnemy = null;
            try
            {
                attack.gameObject.name = "CTJ Player Attack Module Verification";
                OverlapDamageCaster caster = attack.GetComponentInChildren<OverlapDamageCaster>();
                Require(caster != null, "Player's real OverlapDamageCaster is missing.");
                caster.InitCaster(owner);
                typeof(PlayerMeleeSkillModule).GetField("_damageCaster", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(attack, caster);
                MethodInfo cast = typeof(PlayerMeleeSkillModule).GetMethod("HandleDmgCast", BindingFlags.Instance | BindingFlags.NonPublic);
                float damage = new SerializedObject(attack).FindProperty("attackDmg").floatValue;
                Require(damage == 2f, "Expected current player prefab damage 2; update the test if it changes.");
                Require(cat.HealthModule.CurrentHealth == 6f && ghost.HealthModule.CurrentHealth == 8f, "Incorrect starting HP.");

                foreach (float invalid in new[] { 0f, -2f, float.NaN, float.PositiveInfinity })
                    ((IDamageable)cat).ApplyDamage(new DamageData { DamageAmount = invalid }, Vector2.zero, Vector2.right, Vector2.left);
                Require(cat.HealthModule.CurrentHealth == 6f, "Invalid damage changed HP.");

                CastAt(attack, caster, cast, cat);
                Require(cat.HealthModule.CurrentHealth == 4f && !cat.IsDead, "First player attack should cause exactly 2 damage.");
                CastAt(attack, caster, cast, cat);
                Require(cat.HealthModule.CurrentHealth == 2f && !cat.IsDead, "Second player attack should leave 2 HP.");
                await WaitUntil(() => !cat.IsAttackInProgress);
                await WaitUntil(() => cat.IsAttackInProgress);
                EnemyDamageProbe catTarget = cat.Target.GetComponent<EnemyDamageProbe>();
                CastAt(attack, caster, cast, cat);
                // 플레이어의 범위 공격은 옆의 테스트 블록도 맞힐 수 있으므로 그 이후를 기준으로 비교합니다.
                int targetHits = catTarget.ReceivedHitCount;
                CheckDead(cat);
                Require(!cat.IsAttackInProgress, "Death did not cancel Catto's attack.");
                cat.OnAttackHit();
                ((IDamageable)cat).ApplyDamage(new DamageData { DamageAmount = 2f }, Vector2.zero, Vector2.right, Vector2.left);
                await WaitDeathState(cat, "Catto_0");
                await WaitUntil(() => cat == null);
                Require(catTarget.ReceivedHitCount == targetHits, "Dead Catto delivered a late hit.");

                for (int i = 1; i <= 4; i++)
                {
                    CastAt(attack, caster, cast, ghost);
                    Require(ghost.HealthModule.CurrentHealth == 8f - i * 2f, "Mad Ghost damage was missing or duplicated.");
                }
                CheckDead(ghost);
                int[] existingProjectiles = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
                    .Select(p => p.GetInstanceID()).ToArray();
                await WaitDeathState(ghost, "MadGhost_0");
                await WaitUntil(() => ghost == null);
                Require(Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
                    .All(p => existingProjectiles.Contains(p.GetInstanceID())), "Dead Ghost spawned a new projectile.");

                GameObject catPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab");
                observerEnemy = Object.Instantiate(catPrefab, new Vector3(-20f, 0f, 0f), Quaternion.identity);
                EnemyBase observer = observerEnemy.GetComponent<EnemyBase>();
                // HealthModule을 직접 변경하는 다른 시스템도 동일한 사망 처리를 거칩니다.
                observer.HealthModule.CurrentHealth = 0f;
                CheckDead(observer);
                await WaitUntil(() => observer == null);

                return new
                {
                    source = PlayerPath,
                    route = "PlayerMeleeSkillModule.HandleDmgCast -> OverlapDamageCaster.CastDamage -> IDamageable.ApplyDamage",
                    playerDamage = damage,
                    catHealth = new[] { 6, 4, 2, 0 }, ghostHealth = new[] { 8, 6, 4, 2, 0 },
                    lateMeleeDamage = 0, collidersDisabled = true, deathAnimations = true,
                    despawned = true, noPostDeathProjectiles = true, directHealthDeath = true,
                    invalidDamageIgnored = true
                };
            }
            finally
            {
                if (attack != null) Object.Destroy(attack.gameObject);
                if (owner != null) Object.Destroy(owner.gameObject);
                if (observerEnemy != null) Object.Destroy(observerEnemy);
            }
        }

        private static void CastAt(PlayerMeleeSkillModule attack, OverlapDamageCaster caster, MethodInfo cast, EnemyBase enemy)
        {
            caster.transform.position = enemy.GetComponent<Collider2D>().bounds.center;
            Physics2D.SyncTransforms();
            cast.Invoke(attack, null);
        }

        private static void CheckDead(EnemyBase enemy)
        {
            Require(enemy.IsDead && !enemy.enabled && enemy.HealthModule.CurrentHealth == 0f, "Death/health flag is incorrect.");
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            Require(!body.simulated && body.linearVelocity == Vector2.zero, "Dead enemy still moves.");
            Require(enemy.GetComponentsInChildren<Collider2D>(true).All(c => !c.enabled), "Dead enemy collider is still enabled.");
        }

        private static async Task WaitDeathState(EnemyBase enemy, string spritePrefix)
        {
            Animator animator = enemy.GetComponentInChildren<Animator>();
            await WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"));
            Sprite sprite = enemy.GetComponentInChildren<SpriteRenderer>().sprite;
            int frame = int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1));
            int first = enemy is MeleeEnemy ? 26 : 22;
            Require(sprite.name.StartsWith(spritePrefix) && frame >= first && frame <= first + 8, "Wrong death sprite frame.");
        }

        private static async Task WaitUntil(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(6);
            while (!condition())
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline)
                    throw new InvalidOperationException("Death test condition timed out.");
                await Task.Yield();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
