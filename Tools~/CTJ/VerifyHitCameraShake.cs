using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.DevLib.BattleSystem;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyHitCameraShake
{
    private const string EnemyPath = "Assets/_Prefabs/Enemies/Enemy/Enemy01/Enemy01_TwistedArm.prefab";
    private const string CameraPath = "Assets/_Prefabs/Player/PlayerCam.prefab";
    private const string SceneKey = "CTJ.HitCameraShake.OriginalScene";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static CinemachineImpulseManager Manager => CinemachineImpulseManager.Instance;
    private static List<CinemachineImpulseManager.ImpulseEvent> Events =>
        (List<CinemachineImpulseManager.ImpulseEvent>)typeof(CinemachineImpulseManager).GetField("m_ActiveEvents", Private).GetValue(Manager);
    private static int Count => Events?.Count ?? 0;
    private static void Check(bool success, string message) { if (!success) throw new Exception(message); }
    private static void Hit(EnemyBase enemy, float damage, bool critical = false) => enemy.ApplyDamage(
        new DamageData { DamageAmount = damage, IsCritical = critical }, enemy.transform.position, Vector2.right, Vector2.left);
    private static Vector3 Sample(Vector3 position, int channel = 1)
    {
        Manager.GetImpulseAt(position, true, channel, out var offset, out var rotation);
        Check(Quaternion.Angle(rotation, Quaternion.identity) < 0.001f && Mathf.Abs(offset.z) < 0.00001f, "Unexpected Z/rotation shake");
        return offset;
    }

    public static object CheckAssets()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(path =>
                AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyBase>() != null).ToArray();
        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.GetComponentsInChildren<EnemyHitCameraShakeModule>(true).Length == 1, "One module required: " + path);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + path);
            var module = root.GetComponentInChildren<EnemyHitCameraShakeModule>(true);
            Check(module.enabled && module.gameObject.activeSelf, "Inactive module: " + path);
            var data = new SerializedObject(module);
            Check(data.FindProperty("_impulseChannel").intValue == 1, "Wrong channel: " + path);
            Check(data.FindProperty("_criticalStrength").floatValue > data.FindProperty("_normalStrength").floatValue, "Critical not stronger: " + path);
        }
        var listener = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPath).GetComponent<CinemachineImpulseListener>();
        Check(listener != null && listener.enabled && listener.Use2DDistance && (listener.ChannelMask & 1) != 0, "Player camera listener mismatch");
        return new { passed = true, enemyPrefabs = paths.Length, channel = 1, playerListenerGain = listener.Gain };
    }

    public static object BeginPlay()
    {
        Check(!EditorApplication.isPlaying && SceneManager.sceneCount == 1, "One saved scene in edit mode required");
        var scene = SceneManager.GetActiveScene();
        Check(!scene.isDirty && !string.IsNullOrEmpty(scene.path), "Save the current scene yourself first");
        string path = scene.path;
        SessionState.SetString(SceneKey, path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return path;
    }

    public static async Task<object> RunPlay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        float previousClock = CinemachineCore.CurrentTimeOverride;
        bool previousTimeMode = Manager.IgnoreTimeScale;
        GameObject root = null, cameraRoot = null, target = null;
        try
        {
            // 검증용 빈 씬에서만 시간을 고정합니다. 게임 코드/씬 에셋은 변경하지 않습니다.
            Manager.Clear();
            Manager.IgnoreTimeScale = false;
            CinemachineCore.CurrentTimeOverride = 100f;
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath));
            var enemy = root.GetComponent<EnemyBase>();
            root.GetComponent<Rigidbody2D>().simulated = false;
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>())
                if (component is EnemyParticleModule || component is EnemyHitReactionModule || component is EnemyDeathModule ||
                    component is EnemyCurrencyRewardModule || component is EnemyDamageIndicatorModule) component.enabled = false;
            var module = enemy.GetModule<EnemyHitCameraShakeModule>();
            var health = enemy.GetModule<INewHealthModule>();
            float initialHealth = health.CurrentHealth;

            target = new GameObject("Stationary camera target");
            cameraRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CameraPath));
            var camera = cameraRoot.GetComponent<CinemachineCamera>();
            camera.Follow = target.transform;
            camera.InternalUpdateCameraState(Vector3.up, -1f);

            Hit(enemy, 2f);
            Check(Count == 1, "Normal damage must emit exactly one impulse");
            float normalDuration = Events[0].SignalSource.SignalDuration;
            CinemachineCore.CurrentTimeOverride = 100f + normalDuration * 0.5f;
            var normal = Sample(Vector3.zero);
            Check(normal.magnitude > 0.02f && normal.magnitude < 0.1f, "Normal signal outside weak range");
            camera.InternalUpdateCameraState(Vector3.up, 1f / 60f);
            float actualCameraOffset = camera.State.PositionCorrection.magnitude;
            Check(actualCameraOffset > 0.02f, "Existing PlayerCam listener did not apply the hit impulse");
            Check(Sample(new Vector3(30, 0, -10)).sqrMagnitude < 0.000001f, "Distant hit shakes camera");
            Check(Sample(Vector3.zero, 64).sqrMagnitude < 0.000001f, "Hit leaked into boss laser channel");
            float partial = Sample(new Vector3(16, 0, -10)).magnitude;
            Check(partial > 0f && partial < normal.magnitude, "Distance falloff missing");
            CinemachineCore.CurrentTimeOverride = 101f;
            Check(Sample(Vector3.zero).sqrMagnitude == 0 && Count == 0, "Normal impulse did not expire");

            Hit(enemy, 2f, true);
            Check(Count == 1, "Critical damage must emit exactly one impulse");
            float criticalDuration = Events[0].SignalSource.SignalDuration;
            CinemachineCore.CurrentTimeOverride = 101f + criticalDuration * 0.5f;
            float critical = Sample(Vector3.zero).magnitude;
            Check(Mathf.Abs(critical / normal.magnitude - 2f) < 0.01f && criticalDuration > normalDuration, "Critical intensity/duration mismatch");
            Check(Mathf.Approximately(initialHealth - health.CurrentHealth, 4f), "Visual critical changed damage calculation");
            CinemachineCore.CurrentTimeOverride = 102f;
            Sample(Vector3.zero);

            Hit(enemy, 0f); Hit(enemy, -1f); Hit(enemy, float.NaN);
            health.ApplyHeal(1f);
            Check(Count == 0, "Invalid damage or healing caused a shake");
            module.enabled = false;
            Hit(enemy, 1f);
            Check(Count == 0, "Disabled module emitted an impulse");
            module.enabled = true;
            module.Initialize(enemy);
            Hit(enemy, 1f);
            Check(Count == 1, "Reinitialize / re-enable duplicated subscriptions");
            CinemachineCore.CurrentTimeOverride = 103f;
            Sample(Vector3.zero);

            typeof(EnemyHitCameraShakeModule).GetField("_normalStrength", Private).SetValue(module, 0f);
            Hit(enemy, 1f);
            Check(Count == 0, "Zero strength emitted an impulse");
            typeof(EnemyHitCameraShakeModule).GetField("_normalStrength", Private).SetValue(module, 0.06f);
            module.PlayShake(false, new Vector2(float.NaN, 0f));
            Check(Count == 1, "Invalid direction did not fall back safely");
            CinemachineCore.CurrentTimeOverride = 103.06f;
            Check(Sample(Vector3.zero).magnitude > 0.02f, "Fallback direction produced invalid signal");
            CinemachineCore.CurrentTimeOverride = 104f;
            Sample(Vector3.zero);

            Hit(enemy, 99999f, true);
            Check(enemy.IsDead && Count == 1, "Lethal hit missing or duplicated");
            Object.Destroy(root);
            await Awaitable.NextFrameAsync();
            CinemachineCore.CurrentTimeOverride = 104.09f;
            Check(root == null && Sample(Vector3.zero).magnitude > 0.04f, "Enemy destruction cut off lethal shake");
            CinemachineCore.CurrentTimeOverride = 105f;
            Check(Sample(Vector3.zero).sqrMagnitude == 0 && Count == 0, "Lethal impulse leaked");
            camera.InternalUpdateCameraState(Vector3.up, 1f / 60f);
            Check(camera.State.PositionCorrection.sqrMagnitude < 0.000001f, "Camera did not settle after shake");
            return new { passed = true, normalPeakSample = normal.magnitude, criticalPeakSample = critical,
                normalDuration, criticalDuration, actualCameraOffset, distanceFalloffSample = partial,
                lethalSurvivesEnemy = true, disabledAndZeroStrengthSuppressed = true, noDuplicateSubscriptions = true,
                invalidDamageAndHealingSuppressed = true, cameraSettled = true };
        }
        finally
        {
            Manager.Clear();
            Manager.IgnoreTimeScale = previousTimeMode;
            CinemachineCore.CurrentTimeOverride = previousClock;
            if (root != null) Object.Destroy(root);
            if (cameraRoot != null) Object.Destroy(cameraRoot);
            if (target != null) Object.Destroy(target);
        }
    }

    public static void StopPlay() => EditorApplication.isPlaying = false;
    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop play first");
        string path = SessionState.GetString(SceneKey, "");
        Check(!string.IsNullOrEmpty(path), "Original scene unknown");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(SceneKey);
        return path;
    }
}
