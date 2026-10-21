using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent.StatSystem;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // Task.Yield는 Update 중 재개될 수 있습니다. 실제 렌더링에 쓰이는 LateUpdate 이후 위치를 기록합니다.
    [DefaultExecutionOrder(10000)]
    public sealed class LaserCameraSample : MonoBehaviour
    {
        public Vector3 Position { get; private set; }
        private void LateUpdate() => Position = transform.position;
    }

    public static class VerifyBossLaserFeedback
    {
        private const string Root = "Assets/CTJ/Prototypes/Bosses/";
        private static int _checks;

        public static object Validate()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode only");
            foreach (string path in new[] { "BossLaserDelay.asset", "RobotEye_Stats.asset", "RobotEyeBoss.prefab" })
                AssetDatabase.ImportAsset(Root + path, ImportAssetOptions.ForceUpdate);
            RobotEyeBoss boss = AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Root + "RobotEyeBoss.prefab");
            BossLaserSkill laser = boss.GetComponentInChildren<BossLaserSkill>();
            SerializedObject so = new SerializedObject(laser);
            StatData key = AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset");
            Check(so.FindProperty("attackDelayStat").objectReferenceValue == key, "Separate laser delay stat");
            Check(AssetDatabase.LoadAssetAtPath<StatGroup>(Root + "RobotEye_Stats.asset").Stats.Single(s => s.StatData == key).BaseValue == 0.25f, "Laser delay 0.25");
            Check(so.FindProperty("cameraShake").objectReferenceValue == laser.GetComponent<BossLaserShake>(), "Shake connection");
            Check(laser.PostBlinkDelay == 0.15f, "Keep readable warning hold");
            return new { valid = true, preparation = 0.25f, hold = 0.15f, traversalSpeed = 20f, safeSide = "right / left / right" };
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject RootObject;
            public readonly RobotEyeBoss Boss;
            public readonly EnemyDamageProbe Probe;
            public BossArenaModule Arena => Boss.GetModule<BossArenaModule>();
            public BossFlightModule Flight => Boss.GetModule<BossFlightModule>();
            public BossLaserSkill Laser => Boss.GetComponentInChildren<BossLaserSkill>();
            public EnemySkillModule Skills => Boss.GetModule<EnemySkillModule>();
            public Fixture(float x)
            {
                RootObject = new GameObject("Laser feedback verification"); RootObject.SetActive(false);
                GameObject target = new GameObject("Probe"); target.transform.SetParent(RootObject.transform);
                target.transform.position = new Vector3(x + 30f, 1f, 0f);
                Probe = target.AddComponent<EnemyDamageProbe>();
                target.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
                target.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                GameObject extra = new GameObject("Second collider"); extra.transform.SetParent(target.transform, false);
                extra.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
                Boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Root + "RobotEyeBoss.prefab"), new Vector3(x, 4f, 0f), Quaternion.identity, RootObject.transform);
                Set(Boss, "target", target.transform); Set(Boss, "firstVolleyDelay", 1000f);
                Set(Boss.GetComponentInChildren<BossArenaModule>(), "introHeight", 0f);
                RootObject.SetActive(true);
                Set(Laser, "blinkBeforeAttack", false); Set(Laser, "postBlinkDelay", 0.02f);
                Boss.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset")).SetBaseValue(0.03f);
            }
            public async Task Ready() => await Until(() => Boss.CurrentState == RobotEyeBossState.Waiting);
            public async Task Fire(int pass, Vector2 start, Vector2 end)
            {
                Flight.Place(start); Laser.ConfigureTraversal(Arena.GetLaserDangerBounds(pass), start, end);
                Check(Skills.TryUseSpecifiedSkill(Laser), "Specified skill entry");
                Check(Probe.ReceivedHitCount == 0 && !Laser.GetComponent<BossLaserShake>().IsShaking, "No damage/shake before release");
                await Until(() => Laser.IsFiring);
            }
            public void Dispose() { RootObject.SetActive(false); Object.Destroy(RootObject); }
        }

        public static async Task<object> Run()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity", "Use isolated boss test scene");
            _checks = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                using (Fixture f = new Fixture(3000f + pass * 100f))
                {
                    await f.Ready();
                    Vector2 start = f.Arena.GetLaserStart(pass), end = f.Arena.GetLaserEnd(pass);
                    Bounds danger = f.Arena.GetLaserDangerBounds(pass);
                    float safeX = pass % 2 == 0 ? f.Arena.LaserRight.x - 1f : f.Arena.LaserLeft.x + 1f;
                    f.Probe.transform.position = new Vector3(safeX, 0.7f, 0f);
                    await f.Fire(pass, start, end);
                    Check(Mathf.Abs(danger.size.x - 12.8f) < 0.001f, "20% safe strip");
                    Check((pass % 2 == 0 && danger.max.x < safeX) || (pass % 2 == 1 && danger.min.x > safeX), "Alternating safe side");
                    AttackTelegraphUI warning = f.Boss.GetComponentsInChildren<AttackTelegraphUI>().Single(t => t.IsVisible);
                    RectTransform rect = warning.GetComponent<RectTransform>();
                    Check(Vector2.Distance(rect.position, danger.center) < 0.001f && Vector2.Distance(rect.sizeDelta, danger.size) < 0.001f,
                        "Canvas covers full danger region, excludes safe strip, stays visible during firing");
                    bool diagonal = false, contained = true;
                    for (int i = 0; i <= 80; i++)
                    {
                        f.Flight.Place(Vector2.Lerp(start, end, i / 80f)); f.Laser.TickBeam();
                        Bounds beam = f.Laser.BeamBounds;
                        contained &= beam.min.x >= danger.min.x - 0.001f && beam.max.x <= danger.max.x + 0.001f &&
                            beam.min.y >= danger.min.y - 0.001f && beam.max.y <= danger.max.y + 0.001f;
                        diagonal |= Mathf.Abs(f.Laser.BeamDirection.x) > 0.15f;
                    }
                    Check(contained, "Visual/damage beam including thickness stays inside warned area");
                    Check(diagonal, "Laser rotates into a diagonal");
                    Check(f.Probe.ReceivedHitCount == 0, "Safe strip takes zero damage for entire pass");
                    f.Laser.FinishTraversal();
                    Check(!warning.IsVisible && !f.Laser.GetComponent<BossLaserShake>().IsShaking, "Finish hides warning and stops shake");
                }
            }
            using (Fixture f = new Fixture(3400f))
            {
                await f.Ready();
                Vector2 start = f.Arena.GetLaserStart(0), end = f.Arena.GetLaserEnd(0);
                f.Probe.transform.position = new Vector3(3400f, 0.7f, 0f);
                await f.Fire(0, start, end);
                Check(f.Probe.ReceivedHitCount == 0, "Target not in initial beam");
                f.Flight.Place(end); f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1 && f.Probe.TotalDamage == 30f, "Whole traversal in one tick must not tunnel");
                for (int i = 0; i < 10; i++) f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1, "One hit per traversal despite multiple colliders/ticks");
                f.Boss.SetTarget(null);
                Check(!f.Laser.IsFiring && !f.Laser.GetComponent<BossLaserShake>().IsShaking, "Target loss cancels shake and beam");
            }
            using (Fixture f = new Fixture(3500f))
            {
                await f.Ready();
                Vector2 start = f.Arena.GetLaserStart(0), end = f.Arena.GetLaserEnd(0);
                f.Flight.Place(Vector2.Lerp(start, end, 0.5f)); f.Laser.ConfigureTraversal(f.Arena.GetLaserDangerBounds(0), start, end);
                Bounds beam = f.Laser.BeamBounds;
                // 빔 AABB의 위쪽 먼 모서리: 사각형 전체를 피격 범위로 사용하면 오판정하는 위치입니다.
                f.Probe.transform.position = new Vector3(beam.min.x + 0.15f, beam.max.y - 0.25f, 0f);
                Check(f.Skills.TryUseSpecifiedSkill(f.Laser), "Start diagonal beam");
                await Until(() => f.Laser.IsFiring);
                Check(f.Probe.ReceivedHitCount == 0, "AABB corner outside actual diagonal beam must not hit");
                f.Probe.transform.position = f.Laser.BeamCenter; f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1, "Actual rotated box hits");
                f.Boss.gameObject.SetActive(false);
                Check(!f.Laser.GetComponent<BossLaserShake>().IsShaking, "Disable cleanup");
            }
            await ProductionPhase();
            await Shake();
            return new { passed = true, checks = _checks, scope = "alternating safe zones / warning geometry / diagonal swept collision / shake lifetime and camera restoration" };
        }

        private static async Task ProductionPhase()
        {
            using (Fixture f = new Fixture(3600f))
            {
                await f.Ready();
                Set(f.Laser, "blinkBeforeAttack", true); Set(f.Laser, "postBlinkDelay", 0.15f);
                f.Boss.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset")).SetBaseValue(0.25f);
                f.Boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, f.Boss.transform.position, Vector2.right, Vector2.left);
                for (int pass = 0; pass < 3; pass++)
                {
                    await Until(() => f.Boss.CurrentState == RobotEyeBossState.LaserSweep && f.Boss.CompletedTraversals == pass);
                    float warningAt = Time.time;
                    Check(f.Laser.DangerBounds == f.Arena.GetLaserDangerBounds(pass), "FSM uses correct safe side on each pass");
                    await Until(() => f.Laser.IsFiring);
                    Check(Time.time - warningAt >= 0.38f && Time.time - warningAt < 0.65f, "Production warning about 0.4 seconds");
                    float releaseAt = Time.time;
                    Check(f.Laser.GetComponent<BossLaserShake>().IsShaking, "Production beam starts shake");
                    await Until(() => f.Boss.CompletedTraversals > pass);
                    Check(Time.time - releaseAt < 1f, "Production traversal completes in under one second");
                    Check(!f.Laser.GetComponent<BossLaserShake>().IsShaking, "No shake between passes");
                }
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                Check(f.Boss.CompletedTraversals == 3 && !f.Boss.IsInvulnerable, "Production phase completes exactly three passes");
            }
        }

        private static async Task Shake()
        {
            Camera camera = Camera.main;
            CinemachineExternalImpulseListener listener = camera.GetComponent<CinemachineExternalImpulseListener>();
            Check(listener != null && listener.enabled && listener.ChannelMask == BossLaserShake.ImpulseChannel, "CTJ camera listener wired");
            GameObject go = new GameObject("Shake verification");
            BossLaserShake shake = go.AddComponent<BossLaserShake>();
            LaserCameraSample sample = camera.gameObject.AddComponent<LaserCameraSample>();
            Vector3 before = camera.transform.position;
            try
            {
                shake.StartShake(new Bounds(new Vector3(before.x, before.y, 0f), Vector3.one * 10f));
                await Wait(0.1f);
                Check(shake.IsShaking && Vector3.Distance(before, sample.Position) > 0.001f, "Live camera actually moves");
                Vector3 impulse; Quaternion rotation;
                Check(CinemachineImpulseManager.Instance.GetImpulseAt(new Vector3(before.x, before.y, 0f), true,
                    BossLaserShake.ImpulseChannel, out impulse, out rotation), "Own channel signal is active");
                shake.StopShake(); await Wait(0.05f);
                Check(!shake.IsShaking && Vector3.Distance(before, sample.Position) < 0.0001f, "No camera drift after stop");
                Check(!CinemachineImpulseManager.Instance.GetImpulseAt(new Vector3(before.x, before.y, 0f), true,
                    BossLaserShake.ImpulseChannel, out impulse, out rotation), "Signal removed after stop");
                shake.StartShake(new Bounds(before, Vector3.one));
                var cancelled = (CinemachineImpulseManager.ImpulseEvent)typeof(BossLaserShake)
                    .GetField("_impulse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shake);
                shake.StopShake();
                Check(cancelled.SignalSource == null && cancelled.Envelope.Duration > 0f, "Same-frame cancel silences signal and permits event recycling");
                shake.enabled = false; shake.StartShake(new Bounds(before, Vector3.one));
                Check(!shake.IsShaking, "Inspector opt-out");
            }
            finally { shake.StopShake(); Object.Destroy(go); Object.Destroy(sample); }
        }

        public static async Task<object> Capture()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity", "Use isolated boss scene");
            RobotEyeBoss boss = Object.FindFirstObjectByType<RobotEyeBoss>();
            bool wasActive = boss.gameObject.activeSelf;
            boss.gameObject.SetActive(false);
            try
            {
                using (Fixture f = new Fixture(0f))
                {
                    await f.Ready();
                    foreach (int pass in new[] { 0, 1 })
                    {
                        Vector2 start = f.Arena.GetLaserStart(pass), end = f.Arena.GetLaserEnd(pass);
                        f.Flight.Place(Vector2.Lerp(start, end, 0.5f));
                        f.Laser.ConfigureTraversal(f.Arena.GetLaserDangerBounds(pass), start, end);
                        Check(f.Skills.TryUseSpecifiedSkill(f.Laser), "Preview skill start");
                        await Until(() => f.Laser.IsFiring);
                        f.Laser.TickBeam(); Canvas.ForceUpdateCanvases();
                        CaptureCamera("Assets/CTJ/Tools~/LaserSafeSide" + pass + ".png");
                        f.Skills.StopAttack();
                    }
                }
            }
            finally { boss.gameObject.SetActive(wasActive); }
            return new { paths = new[] { "Assets/CTJ/Tools~/LaserSafeSide0.png", "Assets/CTJ/Tools~/LaserSafeSide1.png" } };
        }

        private static void CaptureCamera(string path)
        {
            Camera camera = Camera.main;
            RenderTexture previous = camera.targetTexture, active = RenderTexture.active;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; Object.Destroy(rt); Object.Destroy(texture); }
        }
        private static Task Wait(float seconds) { float end = Time.time + seconds; return Until(() => Time.time >= end); }
        private static async Task Until(Func<bool> condition)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(12);
            while (!condition())
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > end) throw new InvalidOperationException("Laser test timeout");
                await Task.Yield();
            }
        }
        private static void Set(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field);
            if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else p.objectReferenceValue = (Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }
    }
}
