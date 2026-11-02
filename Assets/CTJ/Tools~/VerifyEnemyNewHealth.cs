using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Currency;
using Lrw.Script._Core._ServiceLocator;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyNewHealth
    {
        private static readonly string[] Paths = {
            "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
            "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss"
        };
        private static readonly float[] MaxHp = { 40f, 30f, 50f, 30f, 55f, 2500f };

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Use isolated verification scene");
            var errors = new List<string>(); int knownAgentAssertions = 0;
            Application.LogCallback capture = (message, trace, type) =>
            {
                if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
                if (type == LogType.Assert && message.EndsWith("] IHealthModule is not found", StringComparison.Ordinal) && trace.Contains("Lrw.Script.Agent.Agent:Awake"))
                    knownAgentAssertions++;
                else errors.Add(message);
            };
            Application.logMessageReceived += capture;
            GameObject arena = new GameObject("New health migration verification");
            ICurrencyManager previous = ServiceLocator.Get<ICurrencyManager>();
            CurrencyManager currency = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CurrencyManager>("Assets/Lrw/GameModule/Currency/CurrencyManager.prefab"));
            currency.Initialize(); // GameEnd/저장을 호출하지 않는 런타임 보상 검사
            EnemyKillCountManager counter = new GameObject("Runtime counter").AddComponent<EnemyKillCountManager>();
            var result = new List<object>();
            try
            {
                CurrencyData gold = currency.GetCurrency(AssetDatabase.LoadAssetAtPath<CurrencySO>("Assets/Lrw/GameModule/Currency/Gold.asset"));
                for (int i = 0; i < Paths.Length; i++)
                {
                    EnemyBase enemy = Spawn(arena, Paths[i], i * 40f + 10000f, true);
                    INewHealthModule health = enemy.GetModule<INewHealthModule>();
                    Check(health != null && health.CurrentHealth == MaxHp[i] && health.MaxHealth == MaxHp[i] && !health.IsDead,
                        "Initial HP: " + Paths[i]);
                    EnemyTemporaryStatusUI ui = enemy.GetComponentInChildren<EnemyTemporaryStatusUI>();
                    Image fill = (Image)new SerializedObject(ui).FindProperty("healthFill").objectReferenceValue;
                    int deadEvents = 0, changes = 0; float delta = 0f;
                    health.OnDead += () => deadEvents++;
                    health.OnHealthChanged += (current, change, max) => { changes++; delta = change; };
                    await Frames(2);
                    Check(fill.fillAmount == 1f && ui.GetComponent<Canvas>().enabled, "Initial health bar");
                    foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) Hit(enemy, invalid);
                    Check(changes == 0 && health.CurrentHealth == MaxHp[i], "Invalid damage blocked");
                    Hit(enemy, 2f);
                    Check(health.CurrentHealth == MaxHp[i] - 2f && changes == 1 && delta == -2f && !enemy.IsDead, "Enemy damage path");
                    Check(Mathf.Abs(fill.fillAmount - health.CurrentHealth / health.MaxHealth) < 0.001f, "Damage UI event");
                    Check(enemy.GetModule<EnemyOutlineModule>().IsShowingHit && enemy.GetModule<EnemyHitReactionModule>().IsBlinking,
                        "Outline / hit flash still respond");
                    enemy.ApplyHeal(new HealData { HealAmount = 1f });
                    Check(health.CurrentHealth == MaxHp[i] - 1f && changes == 2 && delta == 1f, "Agent heal uses same module");
                    Check(Mathf.Abs(fill.fillAmount - health.CurrentHealth / health.MaxHealth) < 0.001f, "Heal UI event");
                    health.ApplyHeal(10000f);
                    Check(health.CurrentHealth == MaxHp[i], "Heal clamps to core maximum");
                    int beforeGold = gold.Count, beforeKills = counter.GetKillCount();
                    StatData rewardStat = (StatData)new SerializedObject(enemy.GetModule<EnemyCurrencyRewardModule>())
                        .FindProperty("rewardAmountStat").objectReferenceValue;
                    int reward = Mathf.FloorToInt(enemy.GetModule<IStatModule>().GetStat(rewardStat).Value);
                    // 모듈을 직접 호출하는 외부 시스템에서도 기존 사망/보상이 이어져야 합니다.
                    if (i % 2 == 0) Hit(enemy, health.CurrentHealth + 1f);
                    else health.ApplyDamage(health.CurrentHealth + 1f);
                    Check(enemy.IsDead && health.IsDead && health.CurrentHealth == 0f && deadEvents == 1, "Death event / state");
                    Check(gold.Count == beforeGold && counter.GetKillCount() == beforeKills + 1, "Kill now, coin reward later");
                    Hit(enemy, 99f); health.ApplyDamage(99f); health.ApplyHeal(99f);
                    Check(health.CurrentHealth == 0f && deadEvents == 1 && gold.Count == beforeGold &&
                        counter.GetKillCount() == beforeKills + 1, "No revival / duplicate reward");
                    Check(!enemy.GetComponent<Rigidbody2D>().simulated && !enemy.IsAttackInProgress &&
                        enemy.GetComponentsInChildren<Collider2D>(true).All(c => !c.enabled), "Death cleanup");
                    await Frames(2);
                    Check(!ui.GetComponent<Canvas>().enabled, "Dead health bar hidden");
                    float rewardDeadline = Time.time + 4.5f;
                    while (Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Length > 0 && Time.time < rewardDeadline)
                        await Task.Yield();
                    Check(gold.Count == beforeGold + reward, "Exactly one reward after coins vanish (no-floor fallback)");
                    result.Add(new { enemy = Paths[i], maxHp = MaxHp[i], reward, passed = true });
                }

                RobotEyeBoss boss = (RobotEyeBoss)Spawn(arena, Paths[5], 11000f, false);
                boss.GetModule<EnemyCurrencyRewardModule>().enabled = false;
                boss.GetModule<EnemyDeathModule>().enabled = false;
                Agent target = CreateTarget(arena, boss.transform.position + Vector3.right);
                boss.SetTarget(target.transform);
                Hit(boss, 1499f);
                Check(boss.GetModule<INewHealthModule>().CurrentHealth == 1001f && !boss.HasUsedLaserPhase, "Boss above threshold");
                Hit(boss, 1f);
                Check(boss.GetModule<INewHealthModule>().CurrentHealth == 1000f && boss.HasUsedLaserPhase && boss.IsInvulnerable,
                    "Boss 1000 threshold still triggers from health event");
                Hit(boss, 10f);
                Check(boss.GetModule<INewHealthModule>().CurrentHealth == 1000f, "Existing boss phase immunity");
                target.GetModule<INewHealthModule>().ApplyDamage(10000f);
                Check(!boss.HasCombatTarget, "Boss recognizes target death through new interface");

                RobotEyeBoss lethal = (RobotEyeBoss)Spawn(arena, Paths[5], 11100f, false);
                lethal.GetModule<EnemyCurrencyRewardModule>().enabled = false;
                lethal.GetModule<EnemyDeathModule>().enabled = false;
                Agent alive = CreateTarget(arena, lethal.transform.position + Vector3.right);
                lethal.SetTarget(alive.transform); Hit(lethal, 3000f);
                Check(lethal.IsDead && !lethal.HasUsedLaserPhase && !lethal.IsInvulnerable, "Lethal damage takes priority over phase");
                Check(errors.Count == 0, "Unexpected runtime errors: " + string.Join(" | ", errors));
                return new { passed = true, enemies = result, knownSharedAgentAssertions = knownAgentAssertions,
                    unexpectedErrors = errors, extra = "temporary HP UI / Agent heal / outline / direct module damage / currency / kill count / boss threshold / new-health target death / lethal priority" };
            }
            finally
            {
                arena.SetActive(false); Object.Destroy(arena);
                Object.Destroy(counter.gameObject); Object.Destroy(currency.gameObject);
                ServiceLocator.Register<ICurrencyManager>(previous);
                Application.logMessageReceived -= capture;
            }
        }

        private static EnemyBase Spawn(GameObject arena, string name, float x, bool ui)
        {
            GameObject stage = new GameObject("Spawn staging"); stage.SetActive(false); stage.transform.SetParent(arena.transform);
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + name + ".prefab"),
                new Vector3(x, 0f), Quaternion.identity, stage.transform);
            enemy.GetComponent<Rigidbody2D>().gravityScale = 0f;
            if (ui && enemy.GetComponentInChildren<EnemyTemporaryStatusUI>(true) == null)
                Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyTemporaryStatusUI>("Assets/CTJ/Prototypes/Prefabs/EnemyTemporaryStatusUI.prefab"), enemy.transform);
            stage.SetActive(true); enemy.SetTarget(null);
            return enemy;
        }
        private static Agent CreateTarget(GameObject arena, Vector3 position)
        {
            GameObject target = new GameObject("Agent new-health target"); target.SetActive(false); target.transform.SetParent(arena.transform);
            target.transform.position = position;
            EnemyBase source = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab");
            Object.Instantiate(source.GetComponentInChildren<StatModule>(), target.transform);
            Object.Instantiate(source.GetComponentInChildren<NewHealthModule>(), target.transform);
            Agent agent = target.AddComponent<Agent>(); target.SetActive(true); return agent;
        }
        private static void Hit(EnemyBase enemy, float amount) => enemy.ApplyDamage(new DamageData { DamageAmount = amount },
            enemy.transform.position, Vector2.right, Vector2.left);
        private static async Task Frames(int count) { for (int i = 0; i < count; i++) await Task.Yield(); }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
