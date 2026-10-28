using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Currency;
using Lrw.Script._Core._ServiceLocator;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyCoinReward
    {
        private const string CoinPath = "Assets/CTJ/Prototypes/Prefabs/EnemyRewardCoin.prefab";
        private const string GoldPath = "Assets/Lrw/GameModule/Currency/Gold.asset";
        private static readonly string[] Enemies = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged",
            "Prefabs/Enemy01_TwistedArm", "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" };

        public static object ConfigurePrefabs()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            EnemyRewardCoin coin = AssetDatabase.LoadAssetAtPath<EnemyRewardCoin>(CoinPath);
            if (coin == null)
            {
                var root = new GameObject("EnemyRewardCoin");
                try
                {
                    SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<CurrencySO>(GoldPath).CurrencyIcon;
                    Check(renderer.sprite != null, "Existing Gold icon required");
                    root.transform.localScale = Vector3.one * (0.24f / renderer.sprite.bounds.size.y);
                    renderer.sortingOrder = 15;
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
                    root.AddComponent<EnemyRewardCoin>();
                    coin = PrefabUtility.SaveAsPrefabAsset(root, CoinPath).GetComponent<EnemyRewardCoin>();
                }
                finally { Object.DestroyImmediate(root); }
            }
            foreach (string relative in Enemies)
            {
                string path = "Assets/CTJ/Prototypes/" + relative + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var reward = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>(true));
                    reward.FindProperty("coinPrefab").objectReferenceValue = coin;
                    reward.FindProperty("currencyPerCoin").intValue = 5;
                    reward.FindProperty("maxCoinCount").intValue = 12;
                    reward.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return RunEdit();
        }

        public static object RunEdit()
        {
            var coin = AssetDatabase.LoadAssetAtPath<EnemyRewardCoin>(CoinPath);
            Check(coin != null && coin.GetComponent<SpriteRenderer>().sprite != null, "Coin visual missing");
            Check(coin.GetComponent<Collider2D>() == null && coin.GetComponent<Rigidbody2D>() == null, "Coins must not push characters");
            foreach (string relative in Enemies)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + relative + ".prefab");
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + relative);
                var reward = new SerializedObject(prefab.GetComponentInChildren<EnemyCurrencyRewardModule>(true));
                Check(reward.FindProperty("coinPrefab").objectReferenceValue == coin, "Coin link: " + relative);
                Check(reward.FindProperty("rewardCurrency").objectReferenceValue != null &&
                    reward.FindProperty("rewardAmountStat").objectReferenceValue != null, "Reward data: " + relative);
            }
            return new { passed = true, wiredEnemies = Enemies.Length, icon = coin.GetComponent<SpriteRenderer>().sprite.name };
        }

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Use isolated verification scene");
            var errors = new List<string>();
            Application.LogCallback capture = (message, trace, type) =>
            { if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) errors.Add(message); };
            Application.logMessageReceived += capture;
            var arena = new GameObject("Coin verification");
            var previous = ServiceLocator.Get<ICurrencyManager>();
            var manager = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CurrencyManager>("Assets/Lrw/GameModule/Currency/CurrencyManager.prefab"));
            manager.Initialize(); // No GameEnd/save: only runtime data is changed.
            CurrencyData gold = manager.GetCurrency(AssetDatabase.LoadAssetAtPath<CurrencySO>(GoldPath));
            var results = new List<object>();
            try
            {
                foreach (string relative in Enemies)
                    results.Add(await Case(arena, relative, gold, null, true));
                results.Add(await Case(arena, Enemies[5], gold, 61, true));
                // Thresholds, remainder distribution, cap, no-floor fallback, and enemy removal.
                foreach (int reward in new[] { 0, 1, 5, 6, 11, 60, 61, 1000000 })
                    results.Add(await Case(arena, Enemies[0], gold, reward, reward != 11));
                results.Add(await LandingCases(arena, gold));
                results.Add(await SceneCleanup(gold));
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, cases = results, errors };
            }
            finally
            {
                foreach (EnemyRewardCoin coin in Coins()) Object.Destroy(coin.gameObject);
                arena.SetActive(false); Object.Destroy(arena);
                Object.Destroy(manager.gameObject);
                ServiceLocator.Register<ICurrencyManager>(previous);
                Application.logMessageReceived -= capture;
            }
        }

        private static async Task<object> Case(GameObject arena, string relative, CurrencyData gold, int? overrideReward, bool floor)
        {
            var stage = new GameObject("Case " + relative); stage.SetActive(false); stage.transform.SetParent(arena.transform);
            if (floor) Terrain(stage, new Vector2(10000f, -0.2f), new Vector2(12f, 0.4f));
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + relative + ".prefab"),
                new Vector3(10000f, 0f), Quaternion.identity, stage.transform);
            enemy.GetComponent<Rigidbody2D>().gravityScale = 0f;
            enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            stage.SetActive(true); enemy.SetTarget(null);
            var rewardModule = enemy.GetModule<EnemyCurrencyRewardModule>();
            StatData key = (StatData)new SerializedObject(rewardModule).FindProperty("rewardAmountStat").objectReferenceValue;
            Stat stat = enemy.GetModule<IStatModule>().GetStat(key);
            if (overrideReward.HasValue) stat.SetBaseValue(overrideReward.Value);
            int amount = Mathf.FloorToInt(stat.Value);
            int before = gold.Count;
            Vector3 deathPosition = enemy.transform.position;
            INewHealthModule health = enemy.GetModule<INewHealthModule>();
            health.ApplyDamage(health.CurrentHealth + 1f);
            EnemyRewardCoin[] coins = Coins();
            Check(enemy.IsDead && gold.Count == before, "Death must not award immediately");
            int expectedCount = amount == 0 ? 0 : Math.Min((amount + 4) / 5, 12);
            Check(coins.Length == expectedCount && coins.Sum(c => c.Amount) == amount, "Count / total: " + amount);
            if (coins.Length > 0) Check(coins.Max(c => c.Amount) - coins.Min(c => c.Amount) <= 1, "Even distribution");
            health.ApplyDamage(99999f);
            Check(Coins().Length == expectedCount, "No duplicate death drop");
            // The visual lifetime must not depend on the dead enemy object.
            Object.Destroy(enemy.gameObject);
            float maxSpread = 0f, maxHeight = 0f;
            var landed = new Dictionary<EnemyRewardCoin, Vector3>();
            bool fadeSeen = false;
            float end = Time.time + 5f;
            while (coins.Any(c => c != null) && Time.time < end)
            {
                foreach (EnemyRewardCoin coin in coins.Where(c => c != null))
                {
                    maxSpread = Mathf.Max(maxSpread, Mathf.Abs(coin.transform.position.x - deathPosition.x));
                    maxHeight = Mathf.Max(maxHeight, coin.transform.position.y);
                    if (!coin.IsRising) continue;
                    if (!landed.ContainsKey(coin))
                    {
                        landed[coin] = coin.transform.position;
                        if (floor) Check(coin.transform.position.y >= 0.09f && coin.transform.position.y < 0.25f, "Landed above floor");
                    }
                    float alpha = coin.GetComponent<SpriteRenderer>().color.a;
                    if (alpha > 0.1f && alpha < 0.8f && coin.transform.position.y > landed[coin].y) fadeSeen = true;
                }
                int finishedAmount = coins.Where(c => c != null && c.GetComponent<SpriteRenderer>().enabled).Sum(c => c.Amount);
                Check(gold.Count - before == amount - finishedAmount, "Only vanished coins may award");
                await Task.Yield();
            }
            Check(Coins().Length == 0 && gold.Count == before + amount, "Final exact payout: " + amount);
            if (amount > 0)
            {
                Check(fadeSeen && landed.Count == expectedCount && maxHeight > 0.4f, "Arc / rise / fade");
                if (floor) Check(maxSpread < 4f && maxHeight < deathPosition.y + 1.5f,
                    "Modest scatter: " + relative + " spread=" + maxSpread + " height=" + maxHeight + " origin=" + deathPosition);
            }
            stage.SetActive(false); Object.Destroy(stage);
            await Task.Yield();
            return new { enemy = relative, amount, coins = expectedCount, floor, maxSpread, maxHeight, fadeSeen };
        }

        private static async Task<object> LandingCases(GameObject arena, CurrencyData gold)
        {
            var stage = new GameObject("Terrain cases"); stage.transform.SetParent(arena.transform);
            // Trigger over solid floor: trigger must not count as landing.
            var trigger = Terrain(stage, new Vector2(11000f, 0.6f), new Vector2(8f, 0.2f)); trigger.isTrigger = true;
            Terrain(stage, new Vector2(11000f, -0.2f), new Vector2(8f, 0.4f));
            // One-way platform is below the apex: rise through it, land on its top.
            var platform = Terrain(stage, new Vector2(11020f, 0.45f), new Vector2(8f, 0.1f));
            platform.usedByEffector = true; platform.gameObject.AddComponent<PlatformEffector2D>().useOneWay = true;
            // Thin floor and side wall test swept movement.
            Terrain(stage, new Vector2(11040f, -0.01f), new Vector2(8f, 0.02f));
            Terrain(stage, new Vector2(11040.2f, 1f), new Vector2(0.05f, 4f));
            int before = gold.Count;
            EnemyRewardCoin source = AssetDatabase.LoadAssetAtPath<EnemyRewardCoin>(CoinPath);
            var coins = new List<EnemyRewardCoin>();
            foreach (float x in new[] { 11000f, 11020f, 11040f })
            {
                var coin = Object.Instantiate(source, new Vector3(x, 0.2f), Quaternion.identity);
                var settings = new SerializedObject(coin);
                settings.FindProperty("upwardSpeedRange").vector2Value = new Vector2(4f, 4f);
                settings.ApplyModifiedPropertiesWithoutUndo(); // Must clear the one-way platform at a deterministic height.
                typeof(EnemyRewardCoin).GetMethod("Launch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(coin, new object[] { gold, 1, 0.8f });
                coins.Add(coin);
            }
            var heights = new float[] { -100f, -100f, -100f };
            float end = Time.time + 2.5f;
            while (coins.Any(c => c != null) && Time.time < end)
            {
                for (int i = 0; i < coins.Count; i++)
                    if (coins[i] != null && coins[i].IsRising && heights[i] == -100f) heights[i] = coins[i].transform.position.y;
                await Task.Yield();
            }
            Check(heights[0] >= 0.09f && heights[0] < 0.2f, "Ignore terrain trigger");
            Check(heights[1] >= 0.59f && heights[1] < 0.7f, "One-way top landing: " + heights[1]);
            Check(heights[2] >= 0.09f && heights[2] < 0.2f, "Wall slide / thin floor: " + heights[2] +
                "; current=" + (coins[2] == null ? "gone" : coins[2].transform.position.ToString("F5")));
            Check(gold.Count == before + 3 && Coins().Length == 0, "Terrain payouts");
            Object.Destroy(stage); await Task.Yield();
            return new { terrain = "trigger / one-way / wall / thin floor", heights };
        }

        public static async Task<object> RunTerrainPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Use isolated verification scene");
            var root = new GameObject("Focused terrain verification");
            var gold = new CurrencyData(AssetDatabase.LoadAssetAtPath<CurrencySO>(GoldPath), 0);
            try { return new { terrain = await LandingCases(root, gold), unload = await SceneCleanup(gold) }; }
            finally
            {
                foreach (EnemyRewardCoin coin in Coins()) Object.Destroy(coin.gameObject);
                root.SetActive(false); Object.Destroy(root);
            }
        }

        private static async Task<object> SceneCleanup(CurrencyData gold)
        {
            int before = gold.Count;
            Scene temporary = SceneManager.CreateScene("Coin unload verification");
            var coin = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyRewardCoin>(CoinPath), new Vector3(12000f, 0.2f), Quaternion.identity);
            SceneManager.MoveGameObjectToScene(coin.gameObject, temporary);
            typeof(EnemyRewardCoin).GetMethod("Launch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(coin, new object[] { gold, 7, 0.5f });
            Check(gold.Count == before, "No payout on launch");
            AsyncOperation unload = SceneManager.UnloadSceneAsync(temporary);
            while (!unload.isDone) await Task.Yield();
            Check(coin == null && gold.Count == before + 7, "Scene cleanup settles vanished coin exactly once");
            await Task.Yield();
            Check(gold.Count == before + 7, "No duplicate cleanup payout");
            return new { sceneUnload = "settled once", amount = 7 };
        }

        public static async Task<object> RenderProof()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Use isolated verification scene");
            var root = new GameObject("Coin render proof");
            var cameraObject = new GameObject("Coin camera"); cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.5f;
            camera.transform.position = new Vector3(0f, 0.7f, -10f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.1f);
            var rt = new RenderTexture(840, 420, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
            var coins = new List<EnemyRewardCoin>();
            try
            {
                var floor = Terrain(root, new Vector2(0f, -0.1f), new Vector2(8f, 0.2f));
                SpriteRenderer floorVisual = floor.gameObject.AddComponent<SpriteRenderer>();
                var texture = Texture2D.whiteTexture;
                floorVisual.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, texture.width);
                floorVisual.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
                floorVisual.color = new Color(0.25f, 0.29f, 0.33f); floorVisual.drawMode = SpriteDrawMode.Sliced; floorVisual.size = floor.size;
                var currency = new CurrencyData(AssetDatabase.LoadAssetAtPath<CurrencySO>(GoldPath), 0);
                var snapshots = new List<string>();
                for (int i = 0; i < 12; i++)
                {
                    var coin = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyRewardCoin>(CoinPath), new Vector3(0f, 0.35f), Quaternion.identity);
                    typeof(EnemyRewardCoin).GetMethod("Launch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(coin, new object[] { currency, 5, (i + 0.5f) / 12f });
                    coins.Add(coin);
                }
                foreach (var shot in new[] { new { time = 0.25f, name = "Scatter" }, new { time = 0.45f, name = "Rise" }, new { time = 0.3f, name = "Fade" } })
                {
                    float until = Time.time + shot.time;
                    while (Time.time < until) await Task.Yield();
                    camera.Render(); RenderTexture previous = RenderTexture.active;
                    var pixels = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    try
                    {
                        RenderTexture.active = rt; pixels.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0); pixels.Apply();
                        string path = "Assets/CTJ/Tools~/Coin" + shot.name + ".png";
                        System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG()); snapshots.Add(path);
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(pixels); }
                }
                return new { snapshots, coinsRemaining = coins.Count(c => c != null), paid = currency.Count };
            }
            finally
            {
                foreach (var coin in coins) if (coin != null) Object.Destroy(coin.gameObject);
                camera.targetTexture = null; Object.Destroy(rt); Object.Destroy(root);
            }
        }

        private static BoxCollider2D Terrain(GameObject parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Terrain"); go.layer = 6; go.transform.SetParent(parent.transform); go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>(); box.size = size; return box;
        }
        private static EnemyRewardCoin[] Coins() => Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None);
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
