using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.DevLib.BattleSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyDeathBloodBurst
{
    private const string SourcePath = "Assets/_Prefabs/Enemies/Effects/HitBloodSpray.prefab";
    private const string DeathPath = "Assets/_Prefabs/Enemies/Effects/DeathBloodBurst.prefab";
    private const string MetalHitPath = "Assets/_Prefabs/Enemies/Effects/HitMetalFragments.prefab";
    private const string MetalBurstPath = "Assets/_Prefabs/Enemies/Effects/DeathMetalBurst.prefab";
    private const string EnemyPath = "Assets/_Prefabs/Enemies/Enemy/Enemy01/Enemy01_TwistedArm.prefab";
    private const string OriginalScene = "CTJ.DeathBloodBurst.OriginalScene";
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static EnemyBloodParticle[] Bloods() => Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None);

    public static object CheckAssets()
    {
        var normal = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath).GetComponent<EnemyBloodParticle>();
        var burstRoot = AssetDatabase.LoadAssetAtPath<GameObject>(DeathPath);
        Check(burstRoot != null && burstRoot.GetComponent<ParticleSystem>() != null &&
            burstRoot.GetComponent<EnemyHitParticle>() != null, "Death burst prefab incomplete");
        var burst = burstRoot.GetComponent<EnemyBloodParticle>();
        Check(burst != null, "Death burst behavior missing");
        var normalData = new SerializedObject(normal);
        var burstData = new SerializedObject(burst);
        Vector2 normalSpeed = normalData.FindProperty("speedRange").vector2Value;
        Vector2 burstSpeed = burstData.FindProperty("speedRange").vector2Value;
        Check(Mathf.Approximately(normalData.FindProperty("spreadAngle").floatValue, 105f) &&
            Mathf.Approximately(burstData.FindProperty("spreadAngle").floatValue, 360f), "Spray angle mismatch");
        Check(burstSpeed.x > normalSpeed.y && burstSpeed.y > burstSpeed.x, "Death burst must be faster than normal blood");
        Check(burstData.FindProperty("particleCount").vector2IntValue.x >
            normalData.FindProperty("particleCount").vector2IntValue.y, "Death burst needs more droplets");
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyBase>() != null).ToArray();
        var metalHit = AssetDatabase.LoadAssetAtPath<GameObject>(MetalHitPath).GetComponent<ParticleSystem>();
        var metalBurst = AssetDatabase.LoadAssetAtPath<GameObject>(MetalBurstPath).GetComponent<ParticleSystem>();
        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var modules = root.GetComponentsInChildren<EnemyParticleModule>(true);
            Check(modules.Length == 1, "Expected one particle module: " + path);
            var data = new SerializedObject(modules[0]);
            var hits = data.FindProperty("hitEffects");
            bool usesMetal = Enumerable.Range(0, hits.arraySize).Any(index =>
                hits.GetArrayElementAtIndex(index).FindPropertyRelative("prefab").objectReferenceValue == metalHit);
            var expected = usesMetal ? metalBurst : burstRoot.GetComponent<ParticleSystem>();
            Check(data.FindProperty("deathBurstPrefab").objectReferenceValue == expected,
                "Death burst reference missing: " + path);
        }
        return new { passed = true, enemies = paths.Length, normalSpeed, burstSpeed,
            spreadDegrees = burstData.FindProperty("spreadAngle").floatValue };
    }

    public static object BeginPlay()
    {
        Check(!EditorApplication.isPlaying && SceneManager.sceneCount == 1, "One saved scene in edit mode required");
        var scene = SceneManager.GetActiveScene();
        Check(!scene.isDirty && !string.IsNullOrEmpty(scene.path), "Save the current scene first");
        SessionState.SetString(OriginalScene, scene.path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return scene.path;
    }

    public static async Task<object> RunPlay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        float previousScale = Time.timeScale, previousDelta = Time.maximumDeltaTime;
        GameObject root = null;
        try
        {
            Time.timeScale = 1f;
            Time.maximumDeltaTime = 0.025f;
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath), new Vector3(0f, 2f), Quaternion.identity);
            var enemy = root.GetComponent<EnemyBase>();
            foreach (var module in root.GetComponentsInChildren<MonoBehaviour>())
                if (module is EnemyDeathModule || module is EnemyCurrencyRewardModule ||
                    module is EnemyDamageIndicatorModule || module is EnemyHitCameraShakeModule)
                    module.enabled = false;

            int started = 0;
            float startedAt = 0f;
            enemy.DeathStarted += () => { started++; startedAt = Time.time; };
            float hitAt = Time.time;
            enemy.ApplyDamage(new DamageData { DamageAmount = 99999f }, enemy.transform.position, Vector2.right, Vector2.left);
            Check(enemy.IsDead && enemy.IsPlayingDeathReaction, "Lethal hit did not start death reaction");
            Check(started == 1 && Mathf.Abs(startedAt - hitAt) < 0.001f, "Death did not start in the lethal hit frame");
            var animator = (Animator)typeof(EnemyBase).GetField("deathAnimator", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(enemy);
            Check(animator != null && animator.GetCurrentAnimatorStateInfo(0).fullPathHash ==
                Animator.StringToHash("Base Layer.Death"), "Death animation did not start immediately");
            var reaction = enemy.GetModule<EnemyHitReactionModule>();
            Check(reaction.IsBlinking && reaction.IsKnockedBack, "Blink/knockback did not overlap death animation");
            Check(Bloods().Count(b => b.name.StartsWith("HitBloodSpray")) == 1, "Lethal hit blood missing");
            Check(Bloods().Count(b => b.name.StartsWith("DeathBloodBurst")) == 1,
                "Death burst did not start with death animation");

            float deadline = Time.realtimeSinceStartup + 5f;
            while (enemy != null && enemy.IsPlayingDeathReaction && Time.realtimeSinceStartup < deadline)
                await Awaitable.NextFrameAsync();
            Check(enemy != null && !enemy.IsPlayingDeathReaction && started == 1,
                "Death reaction did not finish with one death event");
            float reactionOverlayTime = Time.time - hitAt;
            var burst = Bloods().SingleOrDefault(b => b.name.StartsWith("DeathBloodBurst"));
            Check(burst != null, "Death burst was not spawned");
            await Task.Delay(100);
            var particles = new ParticleSystem.Particle[64];
            int count = burst.GetComponent<ParticleSystem>().GetParticles(particles);
            Check(count >= 52 && count <= 64, "Wrong death droplet count: " + count);
            float xMin = float.PositiveInfinity, xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity, yMax = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = particles[i].position - burst.transform.position;
                xMin = Mathf.Min(xMin, offset.x); xMax = Mathf.Max(xMax, offset.x);
                yMin = Mathf.Min(yMin, offset.y); yMax = Mathf.Max(yMax, offset.y);
            }
            Check(xMin < -0.1f && xMax > 0.1f && yMin < -0.1f && yMax > 0.1f,
                "Death blood did not scatter in all directions");

            deadline = Time.realtimeSinceStartup + 4f;
            while (root != null && Time.realtimeSinceStartup < deadline)
                await Awaitable.NextFrameAsync();
            Check(root == null && burst != null, "Blood did not survive enemy despawn");
            return new { passed = true, deathEvents = started, deathAnimationImmediate = true,
                reactionOverlayTime,
                droplets = count, xMin, xMax, yMin, yMax, survivesEnemyDespawn = true };
        }
        finally
        {
            Time.timeScale = previousScale;
            Time.maximumDeltaTime = previousDelta;
            if (root != null) Object.Destroy(root);
            foreach (var blood in Bloods()) if (blood != null) Object.Destroy(blood.gameObject);
        }
    }

    public static void StopPlay() => EditorApplication.isPlaying = false;
    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop play first");
        string path = SessionState.GetString(OriginalScene, "");
        Check(!string.IsNullOrEmpty(path), "Original scene unknown");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(OriginalScene);
        return path;
    }
}
