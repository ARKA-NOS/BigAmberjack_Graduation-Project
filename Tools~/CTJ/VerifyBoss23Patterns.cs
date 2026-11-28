using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Testing.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class VerifyBoss23Patterns
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string BossPath = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene Play Mode");
        var originals = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(r => r.activeSelf && r.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        foreach (var original in originals) original.SetActive(false);
        Random.State randomState = Random.state;
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var floor = new GameObject("Pattern test floor") { layer = LayerMask.NameToLayer("Ground") };
        floor.transform.position = Vector3.down * 0.1f;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 0.2f);
        var listener = new GameObject("Pattern test audio listener");
        listener.AddComponent<AudioListener>();
        var player = new GameObject("Pattern test target");
        player.transform.position = Vector3.right * 4f;
        var collider = player.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.35f, 1.35f);
        collider.offset = Vector2.up * 0.675f;
        var probe = player.AddComponent<EnemyDamageProbe>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BossPath), Vector3.up * 1.05f, Quaternion.identity);
        root.SetActive(true);
        try
        {
            var boss = root.GetComponent<ProductionControlDrone>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            var flight = root.GetComponentInChildren<BossFlightModule>();
            var normal = root.GetComponentsInChildren<DroneSalvoSkill>().Single(s => s.name == "RegularSalvo");
            var rapid = root.GetComponentsInChildren<DroneSalvoSkill>().Single(s => s.name == "RapidSalvo");
            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>();
            var bomb = root.GetComponentInChildren<DroneBombSkill>();
            var allSkills = new AbstractCoolTimeSkill[] { normal, rapid, laser, bomb };
            var stateField = typeof(ProductionControlDrone).GetField("_state", Private);
            Vector2 home = root.transform.position;
            Check(Mathf.Abs(home.y - 1.05f) < 0.01f, "Test boss must spawn above the floor");
            boss.SetTarget(player.transform);

            await Until(() => normal.IsPreparing, 2f, "Strong salvo did not start first");
            Check(!boss.IsAirPhase && !laser.IsUsing && !bomb.IsUsing, "Air pattern before half HP");
            await Until(() => normal.DidFire, 1.5f, "Strong salvo did not fire");
            var strongRound = LiveRound();
            Vector2 strongSize = strongRound.GetComponent<SpriteRenderer>().bounds.size;
            float strongDiameter = strongRound.Diameter;
            await Until(() => rapid.IsPreparing, 4.5f, "Strong/weak salvos did not alternate");
            await Until(() => rapid.DidFire, 1.5f, "Weak salvo did not fire");
            var weakRound = LiveRound();
            Vector2 weakSize = weakRound.GetComponent<SpriteRenderer>().bounds.size;
            float weakDiameter = weakRound.Diameter;
            Check(Mathf.Abs(strongSize.x / weakSize.x - 1.35f) < 0.01f &&
                Mathf.Abs(strongSize.y / weakSize.y - 1.35f) < 0.01f &&
                Mathf.Abs(strongDiameter / weakDiameter - 1.35f) < 0.01f, "Strong salvo size or collision scale mismatch");

            // 방어 계산의 부동소수점 오차를 제외하고, 같은 프레임 안에서 정확한 절반으로 맞춥니다.
            health.ApplyDamage(330f);
            health.ApplyHeal(health.MaxHealth * 0.5f - health.CurrentHealth);
            Check(health.CurrentHealth == health.MaxHealth * 0.5f, "Half HP boundary not exact: " + health.CurrentHealth.ToString("R"));
            float ascentStartedAt = -1f;
            await Until(() =>
            {
                Check(skills.ActiveSkill == null || skills.ActiveSkill == rapid || skills.ActiveSkill == laser,
                    "Random attack started before forced transition laser");
                if (State() == "Ascend" && ascentStartedAt < 0f)
                {
                    Check(!rapid.IsUsing, "Transition interrupted an active salvo");
                    ascentStartedAt = Time.time;
                }
                return laser.IsPreparing;
            }, 5f, "Forced laser did not prepare at half HP");
            float laserAscentSeconds = Time.time - ascentStartedAt;
            Check(boss.IsAirPhase && Pending() && !laser.DidFire && laserAscentSeconds < 1.35f,
                "Transition reservation or faster laser ascent failed");
            Check(Mathf.Abs(ReadFloat(flight, "_peakSpeed") - 6f) < 0.01f, "Laser ascent does not use Ascent Speed");
            Check(probe.ReceivedHitCount == 4 && probe.TotalDamage == 38f, "Initial salvo count or damage changed");
            await Until(() => laser.DidFire, 1.5f, "Forced laser did not release");
            await Until(() => !Pending(), 0.3f, "Transition reservation did not clear on release");
            await Until(() => !laser.IsUsing, 2f, "Forced laser did not finish");

            Random.InitState(231124);
            var readyCounts = Draw(300, false);
            Check(readyCounts.Values.All(count => count >= 65 && count <= 135), "Three ready pattern groups are not uniformly selected");
            var coolingCounts = Draw(100, true);
            Check(coolingCounts["Laser"] == 0 && coolingCounts["Salvo"] > 0 && coolingCounts["Bomb"] > 0,
                "Cooldown candidate filtering failed");

            ResetSelection();
            Cooldown(laser, Time.time + 60f);
            Cooldown(bomb, Time.time + 60f);
            await Until(() => normal.IsPreparing, 1f, "Post-transition strong salvo unavailable");
            await Until(() => rapid.IsPreparing, 4.5f, "Post-transition salvos did not keep alternating");
            Check(boss.IsAirPhase && !Pending(), "Random phase reset or retriggered transition");

            ResetSelection();
            Cooldown(normal, Time.time + 60f);
            Cooldown(rapid, Time.time + 60f);
            Cooldown(laser, Time.time + 60f);
            float bombAscentStartedAt = Time.time;
            await Until(() => bomb.IsPreparing, 1.5f, "Bomb group did not ascend");
            float bombAscentSeconds = Time.time - bombAscentStartedAt;
            Check(bombAscentSeconds < 1.1f && Mathf.Abs(ReadFloat(flight, "_peakSpeed") - 6f) < 0.01f,
                "Faster bomb ascent failed");
            await Until(() => bomb.CoolTimeStartTime > bombAscentStartedAt, 1.4f, "Bomb group did not release");
            Check(Object.FindObjectsByType<EnemyLobProjectile>(FindObjectsSortMode.None).Length > 0, "No plasma bomb projectile");

            return new { strongSize = strongSize.ToString(), weakSize = weakSize.ToString(), strongDiameter, weakDiameter,
                forcedAtHalfHP = true, laserAscentSeconds, bombAscentSeconds, readyCounts, coolingCounts,
                postTransitionSalvoAlternation = true };

            string State() => stateField.GetValue(boss).ToString();
            bool Pending() => (bool)typeof(ProductionControlDrone).GetField("_phaseLaserPending", Private).GetValue(boss);

            void ResetSelection()
            {
                skills.StopAttack();
                flight.Place(home);
                stateField.SetValue(boss, Enum.Parse(stateField.FieldType, "Engage"));
                typeof(ProductionControlDrone).GetField("_nextRapid", Private).SetValue(boss, false);
                typeof(ProductionControlDrone).GetField("_nextSalvoAt", Private).SetValue(boss, float.NegativeInfinity);
                foreach (var skill in allSkills) Cooldown(skill, float.NegativeInfinity);
                Physics2D.SyncTransforms();
            }

            Dictionary<string, int> Draw(int count, bool coolingLaser)
            {
                var counts = new Dictionary<string, int> { ["Salvo"] = 0, ["Laser"] = 0, ["Bomb"] = 0 };
                for (int i = 0; i < count; i++)
                {
                    ResetSelection();
                    if (coolingLaser) Cooldown(laser, Time.time);
                    typeof(ProductionControlDrone).GetMethod("SelectAttack", Private).Invoke(boss, null);
                    string selected = State() == "GroundAttack" ? "Salvo" :
                        ReferenceEquals(typeof(ProductionControlDrone).GetField("_airSkill", Private).GetValue(boss), laser) ? "Laser" : "Bomb";
                    Check(State() == "GroundAttack" || State() == "Ascend", "No random pattern selected");
                    counts[selected]++;
                }
                return counts;
            }
        }
        catch (Exception exception)
        {
            var health = root.GetComponentInChildren<NewHealthModule>();
            var boss = root.GetComponent<ProductionControlDrone>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            throw new Exception(exception.Message + " | HP=" + health.CurrentHealth.ToString("R") + "/" + health.MaxHealth +
                ", state=" + typeof(ProductionControlDrone).GetField("_state", Private).GetValue(boss) +
                ", pending=" + typeof(ProductionControlDrone).GetField("_phaseLaserPending", Private).GetValue(boss) +
                ", phase=" + boss.IsAirPhase + ", position=" + root.transform.position +
                ", active=" + skills.ActiveSkill?.name + ", timeScale=" + Time.timeScale, exception);
        }
        finally
        {
            Object.Destroy(root); Object.Destroy(player); Object.Destroy(floor); Object.Destroy(listener);
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(projectile.gameObject);
            foreach (var projectile in Object.FindObjectsByType<EnemyLobProjectile>(FindObjectsSortMode.None)) Object.Destroy(projectile.gameObject);
            Random.state = randomState;
            Application.runInBackground = background;
            foreach (var original in originals) if (original != null) original.SetActive(true);
        }
    }

    private static EnemyProjectile LiveRound() => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
        .Single(p => p.name.StartsWith("Boss23_Round") && p.GetComponent<Collider2D>().enabled);

    private static void Cooldown(AbstractCoolTimeSkill skill, float time) =>
        typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime").SetValue(skill, time);

    private static float ReadFloat(object target, string field) => (float)target.GetType().GetField(field, Private).GetValue(target);

    private static async Task Until(Func<bool> condition, float seconds, string message)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
