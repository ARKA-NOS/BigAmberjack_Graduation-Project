using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Currency;
using _Scripts.DevLib.BattleSystem;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyBoss23DeathFeedback
{
    private const string BossPath = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    public static object CheckAssets()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies/Enemy", "Assets/_Prefabs/Enemies/Bosses" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(path => !path.Contains("/Legacy/")).ToArray();
        int enemies = 0;
        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var enemy = root.GetComponent<EnemyBase>();
            if (enemy == null) continue;
            enemies++;
            bool boss = path == BossPath;
            Check(new SerializedObject(enemy).FindProperty("completeDeathAnimationBeforeReward").boolValue == boss,
                "Death policy mismatch: " + path);
            var flash = root.GetComponentInChildren<EnemyDeathScreenFlashModule>(true);
            Check((flash != null) == boss && (root.GetComponentInChildren<EnemyDeathSlowMotionModule>(true) != null) == boss,
                "Death modules must be connected only to Boss23");
            if (flash != null) Check(new SerializedObject(flash).FindProperty("flashPrefab").objectReferenceValue != null, "Missing flash view");
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, "Missing script");
                foreach (var component in node.GetComponents<Component>())
                {
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, "Broken reference");
                }
            }
        }
        return new { enemies, modulesOnlyOnBoss23 = true, referencesValid = true };
    }

    public static object Enter()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !SceneManager.GetActiveScene().isDirty &&
            SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use saved EnemyTestScene in Edit Mode");
        EditorApplication.EnterPlaymode();
        return "Starting boss death feedback verification";
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(root => root.activeSelf && root.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        var oldCurrency = ServiceLocator.Get<ICurrencyManager>();
        float originalScale = Time.timeScale;
        foreach (var root in roots) root.SetActive(false);
        var counter = new GameObject("Boss death test counter").AddComponent<EnemyKillCountManager>();
        var target = new GameObject("Distant coin collector");
        target.transform.position = new Vector3(100f, 0f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPath);
        var rewardData = new SerializedObject(prefab.GetComponentInChildren<EnemyCurrencyRewardModule>());
        var currency = new TestCurrency { Data = new CurrencyData((CurrencySO)rewardData.FindProperty("rewardCurrency").objectReferenceValue, 0) };
        ServiceLocator.Register<ICurrencyManager>(currency);
        GameObject bossRoot = null, extra = null;
        try
        {
            Time.timeScale = 1f;
            bossRoot = Spawn(prefab, target.transform);
            var boss = bossRoot.GetComponent<EnemyBase>();
            var animator = bossRoot.GetComponentInChildren<Animator>();
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            int deaths = 0;
            boss.DeathStarted += () => deaths++;
            double started = Time.realtimeSinceStartupAsDouble;
            Kill(boss);
            Check(boss.IsDead && deaths == 1 && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "Death animation did not start immediately");
            Check(Coins().Length == 0, "Boss coins spawned before animation end");
            Check(Mathf.Abs(Time.timeScale - 0.2f) < 0.001f, "Death slow motion missing");
            var flash = Object.FindAnyObjectByType<EnemyDeathScreenFlash>();
            Check(flash != null, "Screen flash missing");
            var group = flash.GetComponent<CanvasGroup>();
            var image = flash.GetComponentInChildren<UnityEngine.UI.Image>();
            Canvas.ForceUpdateCanvases();
            Check(flash.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay &&
                image.rectTransform.anchorMin == Vector2.zero && image.rectTransform.anchorMax == Vector2.one &&
                image.color == new Color(1f, 1f, 1f, 0.35f) && !image.raycastTarget && !group.blocksRaycasts,
                "Flash is not a translucent full-screen non-blocking UI");
            var metal = Object.FindObjectsByType<EnemyHitParticle>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.GetComponent<EnemyMetalFragmentParticle>() != null);
            Check(metal != null, "No death metal burst");
            int metalCount = metal.GetComponent<ParticleSystem>().particleCount;
            Check(metalCount >= 16 && metalCount <= 32, "Temporary metal count multiplier not applied");
            await Until(() => Time.realtimeSinceStartupAsDouble - started > 0.12, 1f);
            Check(flash != null && group.alpha > 0f && group.alpha < 1f && Time.timeScale < 0.3f,
                $"Flash did not fade independently: view={flash != null}, alpha={(group != null ? group.alpha : -1f)}, scale={Time.timeScale}, elapsed={Time.realtimeSinceStartupAsDouble - started}");
            await Until(() => Object.FindAnyObjectByType<EnemyDeathScreenFlash>() == null, 1f);
            double flashEnded = Time.realtimeSinceStartupAsDouble - started;
            await Until(() => Mathf.Approximately(Time.timeScale, 1f), 1f);
            double slowEnded = Time.realtimeSinceStartupAsDouble - started;
            Check(Coins().Length == 0 && bossRoot != null, "Boss was removed or rewarded before final frame");
            await Until(() => Coins().Length > 0, 2f);
            double coinsAt = Time.realtimeSinceStartupAsDouble - started;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            Check(state.IsName("Base Layer.Death") && state.normalizedTime >= 1f && animator.speed == 0f,
                "Reward did not use frozen final death pose");
            int gold = Coins().Sum(coin => coin.Amount);
            Check(gold >= 45 && gold <= 65 && counter.GetKillCount() == 1 && deaths == 1, "Gold or kill count incorrect");
            Kill(boss);
            Check(Coins().Sum(coin => coin.Amount) == gold && deaths == 1, "Duplicate lethal hit repeated feedback or reward");
            await Until(() => bossRoot == null, 1f);
            Check(Mathf.Approximately(Time.timeScale, 1f), "Time did not restore");
            ClearCoins();
            await Awaitable.NextFrameAsync();

            // Each effect can be disabled independently; destruction restores global time.
            bossRoot = Spawn(prefab, target.transform);
            bossRoot.GetComponentInChildren<EnemyDeathScreenFlashModule>().enabled = false;
            Kill(bossRoot.GetComponent<EnemyBase>());
            Check(Object.FindAnyObjectByType<EnemyDeathScreenFlash>() == null && Mathf.Approximately(Time.timeScale, 0.2f),
                "Flash disable also disabled time effect");
            Object.Destroy(bossRoot); await Awaitable.NextFrameAsync();
            Check(Mathf.Approximately(Time.timeScale, 1f), "Destroy during slow motion leaked time scale");
            bossRoot = Spawn(prefab, target.transform);
            bossRoot.GetComponentInChildren<EnemyDeathSlowMotionModule>().enabled = false;
            Kill(bossRoot.GetComponent<EnemyBase>());
            Check(Object.FindAnyObjectByType<EnemyDeathScreenFlash>() != null && Mathf.Approximately(Time.timeScale, 1f),
                "Slow motion disable also disabled flash");
            Object.Destroy(bossRoot); await Awaitable.NextFrameAsync();
            Check(Object.FindAnyObjectByType<EnemyDeathScreenFlash>() == null, "Flash leaked after owner destruction");

            // Existing PausingWindow saves the current scale, writes zero, then restores the saved scale.
            Time.timeScale = 0.7f;
            bossRoot = Spawn(prefab, target.transform);
            Kill(bossRoot.GetComponent<EnemyBase>());
            float beforePause = Time.timeScale;
            Check(Mathf.Abs(beforePause - 0.14f) < 0.001f, "Original non-unit time scale not preserved");
            Time.timeScale = 0f;
            await Awaitable.NextFrameAsync();
            Check(Time.timeScale == 0f, "Slow motion unpaused game");
            bossRoot.GetComponentInChildren<EnemyDeathSlowMotionModule>().enabled = false;
            Check(Time.timeScale == 0f, "Disabling time module unpaused game");
            Time.timeScale = beforePause;
            await Until(() => Mathf.Approximately(Time.timeScale, 0.7f), 0.5f);
            Object.Destroy(bossRoot); await Awaitable.NextFrameAsync();

            Time.timeScale = 1f;
            bossRoot = Spawn(prefab, target.transform);
            extra = Spawn(prefab, target.transform);
            Kill(bossRoot.GetComponent<EnemyBase>());
            Kill(extra.GetComponent<EnemyBase>());
            Check(Mathf.Approximately(Time.timeScale, 0.2f), "Overlapping deaths compounded slow motion");
            Object.Destroy(bossRoot); Object.Destroy(extra); await Awaitable.NextFrameAsync();
            Check(Mathf.Approximately(Time.timeScale, 1f), "Overlapping effects did not restore time");

            // Normal enemies retain the existing immediate temporary death/reward path.
            bossRoot = Spawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemies/Enemy/Enemy17/Enemy17_IntakeBiter.prefab"), target.transform);
            bossRoot.GetComponent<Rigidbody2D>().simulated = false;
            Kill(bossRoot.GetComponent<EnemyBase>());
            Check(Coins().Length > 0 && Mathf.Approximately(Time.timeScale, 1f) &&
                Object.FindAnyObjectByType<EnemyDeathScreenFlash>() == null, "Boss policy changed normal enemy death");
            return new { metalCount, gold, deathEvents = deaths, flashEndedRealSeconds = flashEnded,
                slowEndedRealSeconds = slowEnded, coinsAtRealSeconds = coinsAt,
                independentToggles = true, pauseSafe = true, overlappingSafe = true, normalDeathUnchanged = true };
        }
        finally
        {
            if (bossRoot != null) Object.Destroy(bossRoot);
            if (extra != null) Object.Destroy(extra);
            Object.Destroy(target); Object.Destroy(counter.gameObject);
            ClearCoins();
            foreach (var effect in Object.FindObjectsByType<EnemyHitParticle>(FindObjectsSortMode.None))
                typeof(EnemyHitParticle).GetMethod("Release", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(effect, null);
            await Awaitable.NextFrameAsync();
            ServiceLocator.Register<ICurrencyManager>(oldCurrency);
            foreach (var root in roots) if (root != null) root.SetActive(true);
            Time.timeScale = originalScale;
        }
    }

    private static GameObject Spawn(GameObject prefab, Transform target)
    {
        var root = Object.Instantiate(prefab, Vector3.up * 1.05f, Quaternion.identity);
        root.GetComponent<EnemyBase>().SetTarget(target);
        return root;
    }
    private static void Kill(EnemyBase enemy) => enemy.ApplyDamage(new DamageData { DamageAmount = 999999f },
        enemy.transform.position, Vector2.right, Vector2.left);
    private static EnemyRewardCoin[] Coins() => Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None);
    private static void ClearCoins() { foreach (var coin in Coins()) Object.Destroy(coin.gameObject); }
    private sealed class TestCurrency : ICurrencyManager
    {
        public CurrencyData Data;
        public CurrencyData GetCurrency(CurrencySO type) => Data;
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), "Verification timed out");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
