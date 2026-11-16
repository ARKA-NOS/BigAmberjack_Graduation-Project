using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.DevLib.BattleSystem;
using Lean.Pool;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemyParticlePooling
{
    private const string ScenePath = "Assets/_Scenes/Test/EnemyTestScene.unity";
    private const string PreviousScene = "CTJ.EnemyParticlePooling.PreviousScene";
    private const string Effects = "Assets/_Prefabs/Enemies/Effects/";
    private const string Enemies = "Assets/_Prefabs/Enemies/Enemy/";

    public static object CheckAssets()
    {
        CheckReferences("Enemy17/Enemy17_IntakeBiter.prefab", "HitBloodSpray", "DeathBloodBurst");
        CheckReferences("Enemy21/Enemy21_CutterAutomaton.prefab", "HitMetalFragments", "DeathMetalBurst");
        return new { particlePrefabs = 4 };
    }

    private static void CheckReferences(string enemy, string hit, string death)
    {
        var module = AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + enemy).GetComponentInChildren<EnemyParticleModule>();
        var data = new SerializedObject(module);
        Check(data.FindProperty("hitEffects").GetArrayElementAtIndex(0)
            .FindPropertyRelative("prefab").objectReferenceValue == LoadParticle(hit), "Broken hit prefab reference");
        Check(data.FindProperty("deathBurstPrefab").objectReferenceValue == LoadParticle(death), "Broken death prefab reference");
    }

    public static object BeginPlay()
    {
        var scene = SceneManager.GetActiveScene();
        Check(!EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.sceneCount == 1 &&
            !scene.isDirty && !string.IsNullOrEmpty(scene.path), "One saved scene in Edit Mode required");
        string previousScene = scene.path;
        SessionState.SetString(PreviousScene, previousScene);
        if (previousScene != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
        return new { scene = ScenePath, previousScene };
    }

    public static async Task<object> Run()
    {
        var scene = SceneManager.GetActiveScene();
        Check(Application.isPlaying && scene.path == ScenePath, "Open EnemyTestScene in Play Mode");
        bool background = Application.runInBackground;
        float previousScale = Time.timeScale;
        var sceneEnemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        var fixtures = new List<EnemyBase>();
        try
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            foreach (var enemy in sceneEnemies) enemy.gameObject.SetActive(false);

            var blood = await VerifyFamily("Enemy17/Enemy17_IntakeBiter.prefab", "HitBloodSpray", "DeathBloodBurst", -36f);
            var metal = await VerifyFamily("Enemy21/Enemy21_CutterAutomaton.prefab", "HitMetalFragments", "DeathMetalBurst", 36f);
            await VerifyImpact();
            await VerifySceneUnload();
            return new { scene = scene.path, blood, metal, impactReused = true, additiveSceneCleanup = true };
        }
        finally
        {
            foreach (var enemy in fixtures)
                if (enemy != null) Object.Destroy(enemy.gameObject);
            foreach (var enemy in sceneEnemies)
                if (enemy != null) enemy.gameObject.SetActive(true);
            Application.runInBackground = background;
            Time.timeScale = previousScale;
        }

        async Task<object> VerifyFamily(string enemyPath, string hitName, string deathName, float x)
        {
            var hitPrefab = LoadParticle(hitName);
            var deathPrefab = LoadParticle(deathName);
            var first = CreateEnemy(enemyPath, x, 0.8f);
            var second = CreateEnemy(enemyPath, x + 7f, 1.4f);
            Hit(first, 1f, Vector2.right);
            var hitPool = FindPool(hitPrefab);
            var original = Active(hitPool).Single();
            var originalEffect = original.GetComponent<ParticleSystem>();
            var trail = original.transform.Find("Blood Trails");
            if (trail != null)
                await Until(() => trail.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0, 2f, "No blood trail before return");
            await Until(() => hitPool.Spawned == 0, 8f, "Hit effect did not return at the end of its lifetime");
            Check(original != null && !original.activeInHierarchy && originalEffect.particleCount == 0 &&
                (trail == null || trail.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0), "Return did not clear particles or trail");

            Hit(second, 1f, Vector2.left);
            Check(Active(hitPool).Single() == original && original.scene == scene &&
                original.transform.right.x < -0.99f && Vector3.Distance(original.transform.position, second.transform.position) < 2f &&
                Vector3.Distance(original.transform.localScale, hitPrefab.transform.localScale * 1.4f) < 0.001f &&
                originalEffect.particleCount > 0, "Reused hit effect retained position, rotation, scale or old simulation state");

            Hit(first, 1f, Vector2.right);
            var firstCurrent = Active(hitPool).Single(clone => clone != original);
            Hit(first, 1f, Vector2.right);
            var current = Active(hitPool);
            Check(hitPool.Spawned == 2 && hitPool.Total == 2 && current.Contains(original) && current.Contains(firstCurrent),
                "The per-enemy limit did not return its own oldest instance");

            Hit(first, 999f, Vector2.right);
            var deathPool = FindPool(deathPrefab);
            var burst = Active(deathPool).Single();
            Check(first.IsDead && burst.GetComponent<ParticleSystem>().particleCount > 0, "No particles on death");
            await Until(() => first == null, 4f, "Dead enemy was not removed");
            Check(burst.activeInHierarchy, "Death burst was removed with its enemy");
            Hit(second, 999f, Vector2.left);
            Check(deathPool.Spawned == 2, "Overlapping death bursts did not keep their own instances");
            await Until(() => deathPool.Spawned == 0 && hitPool.Spawned == 0, 8f, "Effects did not return");
            Check(deathPool.Despawned == 2 && hitPool.Despawned == 2, "Pool lost inactive instances");

            var third = CreateEnemy(enemyPath, x, 1f);
            Hit(third, 999f, Vector2.right);
            Check(deathPool.Total == 2 && deathPool.Spawned == 1 && hitPool.Total == 2, "Next kill allocated a new instance");
            await Until(() => deathPool.Spawned == 0 && hitPool.Spawned == 0, 8f, "Reused effects did not return");
            return new { hitReused = true, deathReused = true, ownerLimit = true, deathOutlivedEnemy = true };
        }

        async Task VerifySceneUnload()
        {
            var additive = SceneManager.CreateScene("CTJ Particle Pool Probe");
            try
            {
                var enemy = CreateEnemy("Enemy17/Enemy17_IntakeBiter.prefab", -36f, 1f);
                SceneManager.MoveGameObjectToScene(enemy.gameObject, additive);
                Hit(enemy, 1f, Vector2.right);
                var pool = FindPool(LoadParticle("HitBloodSpray"));
                var active = Active(pool).Single();
                Check(active.scene == additive && pool.gameObject.scene == scene, "Wrong VFX scene ownership");
                await SceneManager.UnloadSceneAsync(additive);
                Check(active == null && pool.Spawned == 0 && !LeanPool.Links.ContainsKey(active),
                    "Unloading the effect's scene left a stale pool entry");
            }
            finally
            {
                if (additive.isLoaded) await SceneManager.UnloadSceneAsync(additive);
            }
        }

        async Task VerifyImpact()
        {
            const string path = Enemies + "Enemy22/Enemy22_Scrap.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var terrain = new GameObject("CTJ impact test ground") { layer = LayerMask.NameToLayer("Ground") };
            var floor = terrain.AddComponent<BoxCollider2D>();
            floor.size = new Vector2(8f, 1f);
            terrain.transform.position = new Vector3(51f, -0.5f);
            var projectile = Object.Instantiate(prefab, new Vector3(49f, 1f), Quaternion.identity);
            try
            {
                Physics2D.SyncTransforms();
                var hitPrefab = LoadParticle("HitMetalFragments");
                var pool = FindPool(hitPrefab);
                int pooledCount = pool.Total;
                var launch = typeof(EnemyLobProjectile).GetMethod("Launch", BindingFlags.Instance | BindingFlags.NonPublic);
                launch.Invoke(projectile.GetComponent<EnemyLobProjectile>(), new object[]
                {
                    null, null, new Vector2(52f, 0f), 0f, 1.2f, (LayerMask)LayerMask.GetMask("Ground"), null
                });
                await Until(() => pool.Spawned == 1, 2f, "Projectile impact did not use the metal pool");
                var impact = Active(pool).Single();
                Check(pool.Total == pooledCount && Vector3.Distance(impact.transform.position, new Vector3(52f, 0f)) < 0.03f &&
                    Vector3.Distance(impact.transform.localScale, hitPrefab.transform.localScale * 0.75f) < 0.001f,
                    "Projectile impact did not reuse the pool at the expected position and scale");
                await Until(() => pool.Spawned == 0, 4f, "Projectile impact did not return");
            }
            finally
            {
                if (projectile != null) Object.Destroy(projectile);
                Object.Destroy(terrain);
            }
        }

        EnemyBase CreateEnemy(string path, float x, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + path);
            var enemy = Object.Instantiate(prefab, new Vector3(x, 2f), Quaternion.identity).GetComponent<EnemyBase>();
            fixtures.Add(enemy);
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            foreach (var module in enemy.GetComponentsInChildren<MonoBehaviour>())
                if (module is EnemyDeathModule || module is EnemyCurrencyRewardModule ||
                    module is EnemyDamageIndicatorModule || module is EnemyHitCameraShakeModule)
                    module.enabled = false;
            var data = new SerializedObject(enemy.GetComponentInChildren<EnemyParticleModule>());
            data.FindProperty("effectScale").floatValue = scale;
            data.FindProperty("maxConcurrentEffects").intValue = 1;
            data.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }
    }

    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode first");
        string path = SessionState.GetString(PreviousScene, "");
        Check(!string.IsNullOrEmpty(path), "Previous scene unknown");
        if (SceneManager.GetActiveScene().path != path) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(PreviousScene);
        return new { scene = path };
    }

    private static ParticleSystem LoadParticle(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Effects + name + ".prefab");
        Check(prefab != null && prefab.GetComponent<EnemyHitParticle>() != null &&
            prefab.GetComponent<ParticleSystem>() != null, "Incomplete particle prefab " + name);
        return prefab.GetComponent<ParticleSystem>();
    }

    private static LeanGameObjectPool FindPool(ParticleSystem prefab)
    {
        LeanGameObjectPool pool = null;
        Check(LeanGameObjectPool.TryFindPoolByPrefab(prefab.gameObject, ref pool), "No pool for " + prefab.name);
        return pool;
    }

    private static List<GameObject> Active(LeanGameObjectPool pool)
    {
        var clones = new List<GameObject>();
        pool.GetClones(clones, addDespawnedClones: false);
        return clones;
    }

    private static void Hit(EnemyBase enemy, float damage, Vector2 direction) =>
        enemy.ApplyDamage(new DamageData { DamageAmount = damage }, enemy.transform.position, direction, -direction);

    private static async Task Until(Func<bool> condition, float timeout, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
