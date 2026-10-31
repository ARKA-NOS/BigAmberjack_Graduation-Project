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
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyCoinReward
    {
        private const string CoinPath = "Assets/CTJ/Prototypes/Prefabs/EnemyRewardCoin.prefab";
        private const string GoldPath = "Assets/Lrw/GameModule/Currency/Gold.asset";
        private const string SpriteFolder = "Assets/CTJ/Assets/Animated Gold Coin - Free/Gold/";
        private static readonly string[] Enemies = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged",
            "Prefabs/Enemy01_TwistedArm", "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" };

        public static object ConfigurePrefabs()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            Sprite[] spin = SliceSheet("gold_spin", 7);
            Sprite[] collect = SliceSheet("gold_collect", 6);
            GameObject root = PrefabUtility.LoadPrefabContents(CoinPath);
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = spin[0];
                root.transform.localScale = Vector3.one * 1.2f; // Opaque coin diameter is about 0.22 world units.
                var settings = new SerializedObject(root.GetComponent<EnemyRewardCoin>());
                SetSprites(settings.FindProperty("spinFrames"), spin);
                SetSprites(settings.FindProperty("collectFrames"), collect);
                settings.FindProperty("collisionRadius").floatValue = 0.115f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, CoinPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return RunEdit();
        }

        private static Sprite[] SliceSheet(string name, int count)
        {
            string path = SpriteFolder + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name);
            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                string frameName = name + "_" + i;
                rects[i] = new SpriteRect { name = frameName, rect = new Rect(i * 25, 0, 25, 25),
                    alignment = SpriteAlignment.Center, pivot = Vector2.one * 0.5f,
                    spriteID = existing.TryGetValue(frameName, out SpriteRect old) ? old.spriteID : GUID.Generate() };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        private static void SetSprites(SerializedProperty property, Sprite[] sprites)
        {
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        public static object RunEdit()
        {
            var coin = AssetDatabase.LoadAssetAtPath<EnemyRewardCoin>(CoinPath);
            Check(coin != null && coin.GetComponent<SpriteRenderer>().sprite != null, "Coin visual missing");
            Check(coin.GetComponent<Collider2D>() == null && coin.GetComponent<Rigidbody2D>() == null, "Coins must not push characters");
            var coinSettings = new SerializedObject(coin);
            Check(coinSettings.FindProperty("spinFrames").arraySize == 7 && coinSettings.FindProperty("collectFrames").arraySize == 6,
                "Full spin and collect sheets required");
            foreach (string sheet in new[] { "gold_spin", "gold_collect" })
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(SpriteFolder + sheet + ".png").OfType<Sprite>())
                    Check(sprite.rect.size == new Vector2(25f, 25f) && sprite.pivot == new Vector2(12.5f, 12.5f), "Uniform frames and pivot");
            foreach (string relative in Enemies)
            {
                string path = "Assets/CTJ/Prototypes/" + relative + ".prefab";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + relative);
                var reward = new SerializedObject(prefab.GetComponentInChildren<EnemyCurrencyRewardModule>(true));
                Check(reward.FindProperty("coinPrefab").objectReferenceValue == coin, "Coin link: " + relative);
                Check(reward.FindProperty("currencyPerCoin").intValue == 3, "Three-gold coin threshold: " + relative);
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
                foreach (int reward in new[] { 0, 1, 3, 4, 6, 7, 10, 11, 36, 37, 1000000 })
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
            int expectedCount = amount == 0 ? 0 : Math.Min((amount + 2) / 3, 12);
            Check(coins.Length == expectedCount && coins.Sum(c => c.Amount) == amount, "Count / total: " + amount);
            if (coins.Length > 0) Check(coins.Max(c => c.Amount) - coins.Min(c => c.Amount) <= 1, "Even distribution");
            health.ApplyDamage(99999f);
            Check(Coins().Length == expectedCount, "No duplicate death drop");
            // The visual lifetime must not depend on the dead enemy object.
            Object.Destroy(enemy.gameObject);
            float maxSpread = 0f, maxHeight = 0f;
            var landed = new Dictionary<EnemyRewardCoin, Vector3>();
            var bounceHeights = new Dictionary<EnemyRewardCoin, float>();
            var descended = new HashSet<EnemyRewardCoin>();
            var collectSprites = new HashSet<Sprite>();
            var spinSprites = new HashSet<Sprite>();
            float end = Time.time + 5f;
            while (coins.Any(c => c != null) && Time.time < end)
            {
                foreach (EnemyRewardCoin coin in coins.Where(c => c != null))
                {
                    maxSpread = Mathf.Max(maxSpread, Mathf.Abs(coin.transform.position.x - deathPosition.x));
                    maxHeight = Mathf.Max(maxHeight, coin.transform.position.y);
                    SpriteRenderer visual = coin.GetComponent<SpriteRenderer>();
                    Check(Mathf.Approximately(visual.color.a, 1f), "Do not alpha-fade the sprite");
                    if (!coin.IsCollecting) spinSprites.Add(visual.sprite);
                    else collectSprites.Add(visual.sprite);
                    if (coin.BounceCount > 0 && !landed.ContainsKey(coin))
                    {
                        landed[coin] = coin.transform.position;
                        if (floor) Check(coin.transform.position.y >= 0.09f && coin.transform.position.y < 0.25f, "Landed above floor");
                        bounceHeights[coin] = coin.transform.position.y;
                    }
                    if (coin.BounceCount > 0)
                    {
                        if (coin.transform.position.y < bounceHeights[coin] - 0.03f) descended.Add(coin);
                        bounceHeights[coin] = Mathf.Max(bounceHeights[coin], coin.transform.position.y);
                    }
                    if (floor && coin.IsCollecting)
                        Check(coin.BounceCount == 2 && coin.transform.position.y < 0.25f, "Collect after two bounces, on the floor");
                }
                int finishedAmount = coins.Where(c => c != null && c.GetComponent<SpriteRenderer>().enabled).Sum(c => c.Amount);
                Check(gold.Count - before == amount - finishedAmount, "Only vanished coins may award");
                await Task.Yield();
            }
            Check(Coins().Length == 0 && gold.Count == before + amount, "Final exact payout: " + amount);
            if (amount > 0)
            {
                Check(spinSprites.Count > 1 && collectSprites.Count == 6 && maxHeight > 0.4f, "Spin and full six-frame collect animation");
                if (floor) Check(landed.Count == expectedCount && descended.Count == expectedCount &&
                    bounceHeights.All(pair => pair.Value - landed[pair.Key].y > 0.08f && pair.Value - landed[pair.Key].y < 0.4f),
                    "Every coin physically rises and falls after contact, with a small bounce");
                if (floor) Check(maxSpread < 4f && maxHeight < deathPosition.y + 1.5f,
                    "Modest scatter: " + relative + " spread=" + maxSpread + " height=" + maxHeight + " origin=" + deathPosition);
            }
            stage.SetActive(false); Object.Destroy(stage);
            await Task.Yield();
            return new { enemy = relative, amount, coins = expectedCount, floor, maxSpread, maxHeight, bounced = descended.Count,
                collectFramesSeen = collectSprites.Count };
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
                    if (coins[i] != null && coins[i].BounceCount > 0 && heights[i] == -100f) heights[i] = coins[i].transform.position.y;
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
                        .Invoke(coin, new object[] { currency, 3, (i + 0.5f) / 12f });
                    coins.Add(coin);
                }
                foreach (var shot in new[] { new { time = 0.25f, name = "Scatter" }, new { time = 0.5f, name = "Bounce" }, new { time = 0.55f, name = "Collect" } })
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
