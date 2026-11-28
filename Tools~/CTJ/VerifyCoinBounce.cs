using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Currency;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class VerifyCoinBounce
{
    private const string CoinPath = "Assets/_Prefabs/Enemies/Effects/EnemyRewardCoin.prefab";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static object CheckAssets()
    {
        var paths = new[] { CoinPath, "Assets/_Prefabs/Chest/ChestRewardCoin.prefab" };
        var result = new List<object>();

        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root != null && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) == 0, "Coin prefab script missing");
            var coin = root.GetComponent<EnemyRewardCoin>();
            var data = new SerializedObject(coin);
            Vector2 angle = data.FindProperty("bounceAngleRange").vector2Value;
            Vector2 height = data.FindProperty("bounceHeightMultiplierRange").vector2Value;
            Check(angle == new Vector2(-30f, 30f) && height == new Vector2(1f, 1.35f), "Bounce ranges not inherited");
            Check(data.FindProperty("spinFrames").arraySize > 0 && data.FindProperty("collectFrames").arraySize > 0,
                "Coin animation reference missing");
            Check(root.GetComponent<EnemyCoinTrail>() != null, "Trail missing");
            result.Add(new { path, angle = angle.ToString(), heightMultiplier = height.ToString() });
        }

        return new { prefabs = result };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene Play Mode");
        var originals = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(r => r.activeSelf && r.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        foreach (var original in originals) original.SetActive(false);
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var randomState = Random.state;
        Random.InitState(241124);

        var floor = new GameObject("Coin bounce test floor") { layer = LayerMask.NameToLayer("Ground") };
        floor.transform.position = Vector3.down * 0.1f;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 0.2f);
        var collector = new GameObject("Coin bounce test collector");
        collector.transform.position = new Vector3(5.5f, 1f, 0f);
        collector.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 2f);
        var listener = new GameObject("Coin bounce test listener");
        listener.AddComponent<AudioListener>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        var coins = new List<EnemyRewardCoin>();
        var currency = new CurrencyData(null, 0);
        int payments = 0;
        currency.OnCurrencyChanged += (_, __) => payments++;

        try
        {
            var angles = new List<float>();
            var heights = new List<float>();
            for (int i = 0; i < 128; i++)
            {
                var sample = Spawn(Vector3.up, 0);
                Velocity(sample, new Vector2(3f, -2f));
                Call(sample, "HandleGroundContact");
                Vector2 velocity = Velocity(sample);
                float angle = Mathf.Atan2(velocity.x, velocity.y) * Mathf.Rad2Deg;
                float height = velocity.y * velocity.y / (-2f * Physics2D.gravity.y * Read(sample, "gravityScale"));
                Check(angle >= -30.001f && angle <= 30.001f && height >= 0.999f && height <= 1.351f,
                    "Bounce outside configured range or minimum height");
                angles.Add(angle); heights.Add(height);
                Object.DestroyImmediate(sample.gameObject);
            }
            Check(angles.Count(a => a < 0f) > 30 && angles.Count(a => a > 0f) > 30 &&
                angles.Max() - angles.Min() > 50f && heights.Max() - heights.Min() > 0.25f,
                "Identical incoming velocities produced regular bounces");

            var fixedSample = Spawn(Vector3.up, 0);
            SetRange(fixedSample, "bounceAngleRange", new Vector2(12f, 12f));
            SetRange(fixedSample, "bounceHeightMultiplierRange", new Vector2(1.2f, 1.2f));
            Call(fixedSample, "HandleGroundContact");
            Vector2 fixedVelocity = Velocity(fixedSample);
            Check(Mathf.Abs(Mathf.Atan2(fixedVelocity.x, fixedVelocity.y) * Mathf.Rad2Deg - 12f) < 0.001f,
                "Equal angle endpoints should produce a fixed direction");
            Object.DestroyImmediate(fixedSample.gameObject);

            var reversedSample = Spawn(Vector3.up, 0);
            SetRange(reversedSample, "bounceAngleRange", new Vector2(90f, -90f));
            SetRange(reversedSample, "bounceHeightMultiplierRange", new Vector2(0.5f, -1f));
            Call(reversedSample, "HandleGroundContact");
            Vector2 corrected = Velocity(reversedSample);
            Check(Mathf.Abs(Mathf.Atan2(corrected.x, corrected.y) * Mathf.Rad2Deg) <= 60.001f &&
                Mathf.Abs(corrected.y * corrected.y / (-2f * Physics2D.gravity.y * Read(reversedSample, "gravityScale")) - 1f) < 0.001f,
                "Reversed ranges or minimum height clamp failed");
            Object.DestroyImmediate(reversedSample.gameObject);
            Check(currency.Count == 0 && payments == 0, "Sampling paid coins unexpectedly");
            coins.Clear();

            const int count = 16;
            var bounced = new bool[count];
            var homed = new bool[count];
            var startY = new float[count];
            var peakY = new float[count];
            bool bounceTrail = false, homingTrail = false;
            for (int i = 0; i < count; i++) Spawn(new Vector3(i * 0.025f, 1.2f, 0f), 3, (i + 0.5f) / count);
            Check(currency.Count == 0, "Money paid at spawn");
            float deadline = Time.realtimeSinceStartup + 9f;

            while (coins.Any(c => c != null) && Time.realtimeSinceStartup < deadline)
            {
                for (int i = 0; i < count; i++)
                {
                    var coin = coins[i];
                    if (coin == null) continue;
                    var trail = coin.GetComponentInChildren<TrailRenderer>();
                    if (coin.BounceCount == 0 && !coin.IsHoming) Check(trail == null, "Trail started before floor contact");
                    if (coin.BounceCount > 0 && !bounced[i])
                    {
                        bounced[i] = true; startY[i] = coin.transform.position.y; peakY[i] = startY[i];
                    }
                    if (bounced[i] && !homed[i]) peakY[i] = Mathf.Max(peakY[i], coin.transform.position.y);
                    if (coin.IsHoming) homed[i] = true;
                    if (trail != null && trail.emitting)
                    {
                        if (coin.IsHoming) homingTrail = true;
                        else if (coin.BounceCount > 0) bounceTrail = true;
                    }
                }
                await Awaitable.NextFrameAsync();
            }

            Check(coins.All(c => c == null) && bounced.All(x => x) && homed.All(x => x), "Scatter/bounce/homing flow did not complete");
            float minActualHeight = peakY.Zip(startY, (peak, start) => peak - start).Min();
            float maxActualHeight = peakY.Zip(startY, (peak, start) => peak - start).Max();
            Check(minActualHeight > 0.9f && maxActualHeight < 1.45f && maxActualHeight - minActualHeight > 0.15f,
                "Physical bounce height variation or minimum height failed");
            Check(currency.Count == 48 && payments == 16 && bounceTrail && homingTrail,
                "Reward total, duplicate payment or trail flow changed");

            var expiry = Spawn(Vector3.up * 10f, 7);
            var expiryData = new SerializedObject(expiry);
            expiryData.FindProperty("coinDeathTime").floatValue = 0.15f;
            expiryData.ApplyModifiedPropertiesWithoutUndo();
            float expiryDeadline = Time.realtimeSinceStartup + 1f;
            while (expiry != null && Time.realtimeSinceStartup < expiryDeadline) await Awaitable.NextFrameAsync();
            Check(expiry == null && currency.Count == 55 && payments == 17, "Death-time fallback failed or paid twice");

            return new { sampled = 128, minAngle = angles.Min(), maxAngle = angles.Max(),
                minHeight = heights.Min(), maxHeight = heights.Max(), left = angles.Count(a => a < 0f),
                right = angles.Count(a => a > 0f), physicalCoins = count, minActualHeight, maxActualHeight,
                gold = currency.Count, payments, bounceTrail, homingTrail, expiryPaidOnce = true };

            EnemyRewardCoin Spawn(Vector3 position, int amount, float spread = 0.5f)
            {
                var coin = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<EnemyRewardCoin>();
                Call(coin, "SetCollector", collector.transform);
                Call(coin, "Launch", currency, amount, spread, 1f);
                coins.Add(coin);
                return coin;
            }
        }
        finally
        {
            foreach (var coin in coins) if (coin != null) Object.Destroy(coin.gameObject);
            Object.Destroy(floor); Object.Destroy(collector); Object.Destroy(listener);
            Random.state = randomState;
            Application.runInBackground = background;
            foreach (var original in originals) if (original != null) original.SetActive(true);
        }
    }

    private static object Call(object target, string name, params object[] args) =>
        typeof(EnemyRewardCoin).GetMethod(name, Private).Invoke(target, args);

    private static Vector2 Velocity(EnemyRewardCoin coin) => (Vector2)typeof(EnemyRewardCoin).GetField("_velocity", Private).GetValue(coin);

    private static void Velocity(EnemyRewardCoin coin, Vector2 value) => typeof(EnemyRewardCoin).GetField("_velocity", Private).SetValue(coin, value);

    private static float Read(EnemyRewardCoin coin, string name) => (float)typeof(EnemyRewardCoin).GetField(name, Private).GetValue(coin);

    private static void SetRange(EnemyRewardCoin coin, string name, Vector2 range)
    {
        var settings = new SerializedObject(coin);
        settings.FindProperty(name).vector2Value = range;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
