using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyRobotEyeBoss
    {
        private const string Root = "Assets/CTJ/Prototypes/Bosses";
        private const string SessionKey = "CTJ.BossTest.PreviousScene";
        private const string ActiveKey = "CTJ.BossTest.Active";
        private static int _checks;

        public static object Snapshot() => Object.FindObjectsByType<RobotEyeBoss>(FindObjectsSortMode.None).Select(b => new
        {
            name = b.name, state = b.CurrentState.ToString(), position = b.transform.position.ToString(),
            target = b.Target != null ? b.Target.position.ToString() : "null", reachable = b.HasCombatTarget,
            health = b.GetModule<IHealthModule>()?.CurrentHealth, flight = b.GetModule<BossFlightModule>()?.IsMoving,
            arrived = b.GetModule<BossFlightModule>()?.HasArrived, time = Time.time
        }).ToArray();

        public static async Task<object> CaptureLaser()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "/BossPatternTest.unity", "Use boss scene");
            RobotEyeBoss boss = Object.FindFirstObjectByType<RobotEyeBoss>();
            boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, boss.transform.position, Vector2.right, Vector2.left);
            BossLaserSkill beam = boss.GetComponentInChildren<BossLaserSkill>();
            await Until(() => beam.IsFiring);
            return Capture("Assets/CTJ/Tools~/RobotEyeLaserPreview.png");
        }

        private static object Capture(string path)
        {
            Camera camera = Camera.main;
            RenderTexture previous = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = new RenderTexture(1280, 720, 24);
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                return new { path };
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = previousActive;
                Object.Destroy(render);
                Object.Destroy(texture);
            }
        }

        public static object ValidatePrefab()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                foreach (string path in new[] { Root + "/RobotEyeBoss.prefab", "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab", "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab" })
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            RobotEyeBoss boss = AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Root + "/RobotEyeBoss.prefab");
            Check(boss != null && typeof(BossBase).IsAbstract, "BossBase/prefab");
            Check(boss.GetComponentInChildren<EnemyMovementModule>() == null, "Ground movement must not exist");
            Check(boss.GetComponentsInChildren<Collider2D>().All(c => c.isTrigger), "No body collision");
            Check(boss.gameObject.layer == LayerMask.NameToLayer("Enemy"), "Player attack target layer");
            IModule[] modules = boss.GetComponentsInChildren<IModule>();
            Check(modules.Length == 7 && modules.Select(m => m.GetType()).Distinct().Count() == 7, "Module uniqueness");
            Check(boss.GetComponentInChildren<BossArenaModule>() != null, "Separate arena module");
            EnemySkillModule skills = boss.GetComponentInChildren<EnemySkillModule>();
            Check(skills.Skills.Count == 2 && skills.Skills.All(e => e.Skill != null && e.Skill.enabled && e.Skill.SkillSo != null && !e.UseForAutomaticSelection),
                "Pattern skills must be listed and enabled");
            SerializedProperty configured = new SerializedObject(skills).FindProperty("skills");
            Check(configured.GetArrayElementAtIndex(0).FindPropertyRelative("useSkill").boolValue &&
                configured.GetArrayElementAtIndex(1).FindPropertyRelative("useSkill").boolValue, "Use Skill settings");
            Check(skills.Skills[0].Skill == boss.GetComponentInChildren<BossRadialVolleySkill>() &&
                skills.Skills[1].Skill == boss.GetComponentInChildren<BossLaserSkill>(), "Pattern skill references");
            Check(new SerializedObject(boss).FindProperty("arenaHalfWidth") == null, "Arena settings must not remain on boss");
            foreach (string path in new[] { Root + "/RobotEyeBoss.prefab", "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab", "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab" })
            {
                SerializedObject so = new SerializedObject(AssetDatabase.LoadAssetAtPath<EnemyBase>(path));
                Check(so.FindProperty("attackInterval") == null && so.FindProperty("attackRange") == null, "No legacy attack fields");
            }
            Check(boss.GetComponentInChildren<BossRadialVolleySkill>().SkillSo != boss.GetComponentInChildren<BossLaserSkill>().SkillSo,
                "Distinct skill keys");
            StatGroup stats = AssetDatabase.LoadAssetAtPath<StatGroup>(Root + "/RobotEye_Stats.asset");
            Check(stats.Stats.Any(s => s.BaseValue == 2500f) && stats.Stats.Any(s => s.BaseValue == 30f), "HP/attack stats");
            foreach (Component component in boss.GetComponentsInChildren<Component>(true))
            {
                Check(component != null, "Missing script");
                SerializedProperty p = new SerializedObject(component).GetIterator();
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference)
                        Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0, "Broken reference " + p.propertyPath);
            }
            return new { prefab = boss.name, modules = modules.Length, referencesValid = true,
                firstWarningAt = new SerializedObject(boss).FindProperty("firstVolleyDelay").floatValue };
        }

        public static string Begin()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(ActiveKey, false), "Existing Play session");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Check(!SceneManager.GetSceneAt(i).isDirty, "Save your scene before starting the isolated test");
            SessionState.SetString(SessionKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Root + "/BossPatternTest.unity");
            EditorApplication.isPlaying = true;
            return "Starting isolated boss test; original scene setup preserved.";
        }

        public static string Stop()
        {
            Check(SessionState.GetBool(ActiveKey, false), "Not our test session");
            EditorApplication.isPlaying = false;
            return "Stopping boss test.";
        }

        public static string Restore()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Wait for Edit Mode");
            if (!SessionState.GetBool(ActiveKey, false)) return "Nothing to restore.";
            string path = SessionState.GetString(SessionKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.EraseString(SessionKey);
            SessionState.EraseBool(ActiveKey);
            return "Original start scene restored.";
        }

        public static async Task<object> Run()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "/BossPatternTest.unity", "Use boss test scene");
            _checks = 0;
            List<string> scenarios = new List<string>();
            await Normal(); scenarios.Add("intro / delay / warning snapshot / 0.5s recovery / movement");
            await Laser(); scenarios.Add("1001/1000 threshold / invulnerability / 3 one-way passes / one hit per pass / center return / one-shot latch");
            await Interruptions(); scenarios.Add("real target loss / disabled / recovery / distant target stays valid");
            await CleanupAndDeath(); scenarios.Add("phase cancellation / owned projectile cleanup / lethal damage priority / death cleanup");
            await SkillEntry(); scenarios.Add("designated skill registration / cooldown / overlap / ordinary AI entry unchanged");
            await SweptBeamAndDelayedTarget(); scenarios.Add("fast swept beam / separate child damage receivers / delayed target threshold");
            await PlayerCaster(); scenarios.Add("existing PlayerTestPack attack caster damages boss trigger");
            await DistanceDoesNotCancel(); scenarios.Add("distant warning completes / launched bullets persist / lifetime / distant laser completes");
            await SkillConfiguration(); scenarios.Add("enabled list required / unlisted skill rejected / disabled pattern does not stall");
            return new { passed = true, checks = _checks, scenarios };
        }

        private sealed class Fixture : IDisposable
        {
            public GameObject Root;
            public RobotEyeBoss Boss;
            public EnemyDamageProbe Target;
            public BossRadialVolleySkill Radial => Boss.GetComponentInChildren<BossRadialVolleySkill>();
            public BossLaserSkill Beam => Boss.GetComponentInChildren<BossLaserSkill>();
            public IHealthModule Health => Boss.GetModule<IHealthModule>();
            public EnemySkillModule Skills => Boss.GetModule<EnemySkillModule>();
            public Fixture(float x, float firstDelay = 100f)
            {
                Root = new GameObject("CTJ boss verification");
                Root.SetActive(false);
                GameObject target = new GameObject("Boss probe");
                target.transform.SetParent(Root.transform);
                target.transform.position = new Vector3(x, 0.6f, 0f);
                target.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 1f);
                target.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                Target = target.AddComponent<EnemyDamageProbe>();
                GameObject extra = new GameObject("Extra hurtbox");
                extra.transform.SetParent(target.transform, false);
                extra.AddComponent<BoxCollider2D>().isTrigger = true;
                Boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(RootPath), new Vector3(x, 4f, 0f), Quaternion.identity, Root.transform);
                Set(Boss, "target", target.transform);
                BossArenaModule arena = Boss.GetComponentInChildren<BossArenaModule>();
                Set(arena, "introHeight", 0.4f);
                Set(Boss, "flightSpeed", 30f);
                Set(arena, "halfWidth", 2f);
                Set(Boss, "laserSweepSpeed", 8f);
                Set(Boss, "firstVolleyDelay", firstDelay);
                Set(Boss, "normalRestTime", 100f);
                foreach (Object skill in new Object[] { Radial, Beam })
                {
                    Set(skill, "blinkBeforeAttack", false);
                    Set(skill, "postBlinkDelay", 0.04f);
                }
                Root.SetActive(true);
                Boss.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>("Assets/CTJ/Prototypes/Stats/EnemyAttackDelay.asset")).SetBaseValue(0.05f);
                Physics2D.SyncTransforms();
            }
            private const string RootPath = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
            public void Hit(float amount) => Boss.ApplyDamage(new DamageData { DamageAmount = amount }, Boss.transform.position, Vector2.right, Vector2.left);
            public void Dispose() { if (Root != null) { Root.SetActive(false); Object.Destroy(Root); } }
        }

        private static async Task Normal()
        {
            using (Fixture f = new Fixture(1000f, 1f))
            {
                Check(f.Health.CurrentHealth == 2500f && f.Boss.GetComponentInChildren<EnemyMovementModule>() == null, "HP / no injected ground module");
                await Wait(0.15f);
                Check(!f.Radial.IsUsing, "Attack must not bypass first delay");
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                Check(Vector2.Distance(f.Boss.transform.position, f.Boss.ArenaCenter) < 0.02f, "Intro must descend to center");
                await Until(() => f.Radial.IsPreparing);
                Vector2 origin = f.Radial.LockedOrigin;
                Vector2 destination = f.Boss.LockedMoveDestination;
                f.Target.transform.position += Vector3.right;
                await Until(() => f.Radial.DidFire);
                float fired = Time.time;
                Check(Vector2.Distance(origin, f.Boss.transform.position) < 0.02f && f.Radial.LockedOrigin == origin, "Warning origin must remain fixed");
                await Wait(0.25f);
                Check(f.Boss.CurrentState == RobotEyeBossState.RadialVolley && Vector2.Distance(origin, f.Boss.transform.position) < 0.02f, "Do not move during 0.5s recovery");
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.MoveToTarget);
                Check(Time.time - fired >= 0.48f, "0.5s recovery duration");
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                Check(Vector2.Distance(destination, f.Boss.transform.position) < 0.02f, "Move toward snapshot, not current target position");
            }
        }

        private static async Task Laser()
        {
            using (Fixture f = new Fixture(1100f))
            {
                f.Hit(1499f);
                Check(f.Health.CurrentHealth == 1001f && !f.Boss.HasUsedLaserPhase, "1001 must not trigger");
                f.Hit(1f);
                Check(f.Health.CurrentHealth == 1000f && f.Boss.HasUsedLaserPhase && f.Boss.IsInvulnerable, "1000 must immediately trigger once");
                f.Hit(9999f);
                Check(f.Health.CurrentHealth == 1000f && !f.Boss.IsDead, "Damage blocked during phase");
                await Until(() => f.Beam.IsPreparing);
                Vector2 preparationPosition = f.Boss.transform.position;
                Check(f.Target.ReceivedHitCount == 0, "No warning damage");
                await Until(() => f.Beam.IsFiring);
                Check(Vector2.Distance(preparationPosition, f.Boss.transform.position) < 0.3f, "No movement during warning");
                f.Target.transform.position = new Vector3(f.Boss.transform.position.x, 0.6f, 0f);
                for (int i = 0; i < 20; i++) f.Beam.TickBeam();
                Check(f.Target.ReceivedHitCount == 1 && f.Target.TotalDamage == 30f, "Multi-collider / sustained detection must hit once");
                f.Target.transform.position += Vector3.right * 3f;
                f.Beam.TickBeam();
                f.Target.transform.position = new Vector3(f.Boss.transform.position.x, 0.6f, 0f);
                f.Beam.TickBeam();
                Check(f.Target.ReceivedHitCount == 1, "Re-entry in same traversal must not hit again");
                f.Target.transform.position = new Vector3(1100f, 0.6f, 0f);
                List<int> completions = new List<int>();
                for (int pass = 1; pass <= 3; pass++)
                {
                    int wanted = pass;
                    await Until(() => f.Boss.CompletedTraversals >= wanted);
                    completions.Add(f.Boss.CompletedTraversals);
                    Check(Mathf.Abs(f.Boss.transform.position.x - (1100f + (pass % 2 == 1 ? 2f : -2f))) < 0.1f, "One-way endpoint " + pass);
                }
                Check(f.Target.ReceivedHitCount == 3 && f.Target.TotalDamage == 90f, "Exactly one hit per traversal / 3 traversals");
                await Until(() => !f.Boss.IsInvulnerable);
                Check(f.Boss.CompletedTraversals == 3 && Vector2.Distance(f.Boss.transform.position, f.Boss.ArenaCenter) < 0.02f, "Return center and clear invulnerability");
                f.Hit(1f);
                await Wait(0.15f);
                Check(f.Health.CurrentHealth == 999f && f.Boss.CompletedTraversals == 3 && !f.Boss.IsInvulnerable, "Never trigger a second threshold phase");
            }
        }

        private static async Task Interruptions()
        {
            using (Fixture f = new Fixture(1200f))
            {
                f.Hit(1500f);
                await Until(() => f.Beam.IsFiring);
                f.Boss.SetTarget(null);
                Check(!f.Boss.IsInvulnerable && !f.Skills.IsBusy && !f.Boss.GetModule<BossFlightModule>().IsMoving, "Target loss cleanup");
                Vector2 stopped = f.Boss.transform.position;
                await Wait(0.15f);
                Check(Vector2.Distance(stopped, f.Boss.transform.position) < 0.01f, "Target loss must stop");
                f.Boss.SetTarget(f.Target.transform);
                await Wait(0.1f);
                Check(f.Boss.HasUsedLaserPhase && !f.Boss.IsInvulnerable, "Target reacquire must not rearm phase");
                f.Boss.enabled = false;
                Check(!f.Skills.IsBusy && !f.Boss.IsInvulnerable, "Disable cleanup");
                f.Boss.enabled = true;
                f.Target.transform.position += Vector3.right * 50f;
                await Wait(0.1f);
                Check(f.Boss.HasCombatTarget && f.Boss.CurrentState != RobotEyeBossState.Dormant, "Distance must not invalidate target");
            }
        }

        private static async Task CleanupAndDeath()
        {
            EnemyProjectile foreign = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyProjectile>(Root + "/RobotEyeProjectile.prefab"), new Vector3(900f, 100f, 0f), Quaternion.identity);
            try
            {
                using (Fixture f = new Fixture(1300f, 0.01f))
                {
                    await Until(() => f.Radial.IsPreparing);
                    EnemyProjectile owned = Object.Instantiate(foreign);
                    f.Boss.TrackProjectile(owned);
                    f.Hit(1501f);
                    Check(!f.Radial.IsUsing && f.Boss.IsInvulnerable && !owned.gameObject.activeInHierarchy && foreign.gameObject.activeInHierarchy,
                        "Threshold interrupts normal attack and clears only own bullets");
                    f.Boss.gameObject.SetActive(false);
                    Check(!f.Boss.IsInvulnerable && !f.Beam.IsUsing, "Inactive cleanup");
                }
                using (Fixture f = new Fixture(1400f))
                {
                    EnemyProjectile owned = Object.Instantiate(foreign);
                    f.Boss.TrackProjectile(owned);
                    f.Hit(3000f);
                    Check(f.Boss.IsDead && !f.Boss.HasUsedLaserPhase && !f.Boss.IsInvulnerable, "Lethal hit has priority over phase");
                    Check(!owned.gameObject.activeInHierarchy && foreign.gameObject.activeInHierarchy && !f.Skills.IsBusy, "Death cleanup / foreign bullet preserved");
                    Check(f.Boss.GetComponentsInChildren<Collider2D>().All(c => !c.enabled), "Death disables hurtboxes");
                    await Until(() => f.Boss == null);
                }
            }
            finally { if (foreign != null) Object.Destroy(foreign.gameObject); }
        }

        private static async Task SkillEntry()
        {
            using (Fixture f = new Fixture(1500f))
            using (Fixture other = new Fixture(1520f))
            {
                await Wait(0.1f);
                Check(!f.Skills.CanAttackTarget(), "Boss does not use ordinary ground skill selector");
                Check(!f.Skills.TryUseSpecifiedSkill(other.Radial), "Foreign skill must be rejected");
                Set(f.Radial, "<BaseCoolTime>k__BackingField", 3f);
                Check(f.Skills.TryUseSpecifiedSkill(f.Radial), "Registered designated skill accepted");
                Check(!f.Skills.TryUseSpecifiedSkill(f.Beam), "Busy skill must not be overwritten");
                f.Skills.StopAttack();
                Check(!f.Skills.TryUseSpecifiedSkill(f.Radial), "Cancel must not reset cooldown");
                Check(other.Skills.TryUseSpecifiedSkill(other.Radial), "Shared SO cooldown stays per instance");
            }
        }

        private static async Task SweptBeamAndDelayedTarget()
        {
            using (Fixture f = new Fixture(1600f))
            {
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                EnemyDamageProbe childReceiver = f.Target.transform.GetChild(0).gameObject.AddComponent<EnemyDamageProbe>();
                f.Boss.GetModule<BossFlightModule>().Place(new Vector2(1598f, 6.5f));
                f.Beam.ConfigureTraversal(0f);
                Check(f.Skills.TryUseSpecifiedSkill(f.Beam), "Start independent sustained beam");
                await Until(() => f.Beam.IsFiring);
                Check(f.Target.ReceivedHitCount + childReceiver.ReceivedHitCount == 0, "No hit outside beam");
                f.Boss.GetModule<BossFlightModule>().Place(new Vector2(1602f, 6.5f));
                f.Beam.TickBeam();
                Check(f.Target.ReceivedHitCount + childReceiver.ReceivedHitCount == 1, "Swept gap: hits=" + f.Target.ReceivedHitCount + "/" + childReceiver.ReceivedHitCount +
                    " beam=" + f.Beam.BeamBounds + " body=" + f.Boss.GetComponent<Rigidbody2D>().position + " transform=" + f.Boss.transform.position + " firing=" + f.Beam.IsFiring);
                for (int i = 0; i < 10; i++) f.Beam.TickBeam();
                Check(f.Target.ReceivedHitCount + childReceiver.ReceivedHitCount == 1, "No duplicate target-level hit");
            }
            using (Fixture f = new Fixture(1700f))
            {
                f.Boss.SetTarget(null);
                f.Hit(1500f);
                Check(!f.Boss.HasUsedLaserPhase && !f.Boss.IsInvulnerable, "No target: defer phase rather than leave invulnerability on");
                await Wait(0.1f);
                f.Boss.SetTarget(f.Target.transform);
                await Until(() => f.Boss.HasUsedLaserPhase);
                Check(f.Boss.IsInvulnerable, "Delayed target resumes threshold processing");
            }
        }

        private static async Task PlayerCaster()
        {
            using (Fixture f = new Fixture(1800f))
            {
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Lrw/GameModule/Player/Character/PlayerTestPack Variant.prefab");
                OverlapDamageCaster caster = Object.Instantiate(source.GetComponentInChildren<OverlapDamageCaster>(true),
                    f.Boss.transform.position, Quaternion.identity, f.Root.transform);
                caster.InitCaster(null);
                Physics2D.SyncTransforms();
                Check(caster.CastDamage(10f, Vector2.right, 0f), "Player caster must detect boss trigger");
                Check(f.Health.CurrentHealth == 2490f, "Player caster must deliver damage exactly once");
            }
        }

        private static EnemyProjectile[] OwnedShots(RobotEyeBoss boss) =>
            ((List<EnemyProjectile>)typeof(BossBase).GetField("_ownedProjectiles",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(boss)).ToArray();

        private static async Task DistanceDoesNotCancel()
        {
            using (Fixture f = new Fixture(1900f, 0.3f))
            {
                await Until(() => f.Radial.IsPreparing);
                Vector2 origin = f.Radial.LockedOrigin;
                f.Target.transform.position += new Vector3(50f, 30f, 0f);
                await Until(() => f.Radial.DidFire);
                Check(f.Boss.HasCombatTarget && f.Radial.LockedOrigin == origin, "Distant target must not cancel or re-aim warning");
                EnemyProjectile[] shots = OwnedShots(f.Boss);
                Check(shots.Length == 12 && shots.All(p => p != null && p.isActiveAndEnabled), "Full radial volley must fire outside arena");
                Vector3[] before = shots.Select(p => p.transform.position).ToArray();
                await Wait(0.2f);
                Check(shots.All(p => p != null && p.isActiveAndEnabled), "Distant target must not delete launched bullets");
                Check(shots.Select((p, i) => Vector3.Distance(p.transform.position, before[i]) > 0.1f).All(moved => moved), "Projectiles must continue flight");
                f.Boss.SetTarget(null);
                Check(shots.All(p => p != null && p.gameObject.activeInHierarchy), "Even actual target loss must not erase launched shots");
                await Until(() => shots.All(p => p == null));
                Check(OwnedShots(f.Boss).All(p => p == null), "Projectiles must still expire naturally");
            }
            using (Fixture f = new Fixture(2000f))
            {
                f.Hit(1500f);
                await Until(() => f.Beam.IsPreparing);
                f.Target.transform.position += new Vector3(50f, 30f, 0f);
                await Until(() => f.Boss.CompletedTraversals == 3);
                Check(f.Boss.HasCombatTarget && f.Boss.HasUsedLaserPhase, "Distant target must not cancel laser traversals");
                await Until(() => !f.Boss.IsInvulnerable);
                Check(Vector2.Distance(f.Boss.transform.position, f.Boss.ArenaCenter) < 0.02f, "Distant laser must finish and return center");
            }
        }

        private static async Task SkillConfiguration()
        {
            using (Fixture f = new Fixture(2100f))
            {
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                SetEntry(f.Skills, 0, "useSkill", false);
                Check(!f.Skills.IsSkillEnabled(f.Radial) && !f.Skills.TryUseSpecifiedSkill(f.Radial), "Disabled entry must block specified execution");
                SetEntry(f.Skills, 0, "useSkill", true);
                Check(f.Skills.TryUseSpecifiedSkill(f.Radial), "Enabled entry must allow specified execution");
                SetEntry(f.Skills, 0, "useSkill", false);
                await Wait(0.05f);
                Check(!f.Skills.IsBusy, "Disabling entry must cancel running skill");
                // 자식 스킬은 공용 모듈에 초기화되어 있지만 목록에서 제거하면 지정 실행도 금지합니다.
                SerializedObject so = new SerializedObject(f.Skills);
                so.FindProperty("skills").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                Check(!f.Skills.TryUseSpecifiedSkill(f.Beam), "Registered child not in configured list must be rejected");
                f.Hit(1500f);
                Check(!f.Boss.IsInvulnerable && !f.Boss.HasUsedLaserPhase, "Unavailable laser must not leave boss invulnerable");
            }
            using (Fixture f = new Fixture(2200f, 0.1f))
            {
                SetEntry(f.Skills, 0, "useSkill", false);
                await Wait(0.5f);
                Check(!f.Radial.IsUsing && f.Boss.CurrentState == RobotEyeBossState.Waiting, "Disabled radial must not stall FSM");
                f.Hit(1500f);
                SetEntry(f.Skills, 1, "useSkill", false);
                await Until(() => !f.Boss.IsInvulnerable);
                Check(f.Boss.CurrentState == RobotEyeBossState.Waiting, "Disabled laser mid-pattern must return and clear invulnerability");
            }
        }

        private static void SetEntry(EnemySkillModule module, int index, string property, bool value)
        {
            SerializedObject so = new SerializedObject(module);
            so.FindProperty("skills").GetArrayElementAtIndex(index).FindPropertyRelative(property).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static async Task Until(Func<bool> condition)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(12);
            while (!condition())
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > end) throw new InvalidOperationException("Boss test timeout");
                await Task.Yield();
            }
        }
        private static Task Wait(float seconds) { float until = Time.time + seconds; return Until(() => Time.time >= until); }
        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(message);
            _checks++;
        }
        private static void Set(Object obj, string name, object value)
        {
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty p = so.FindProperty(name);
            if (p == null) throw new InvalidOperationException("Missing field: " + name);
            if (value is Object reference) p.objectReferenceValue = reference;
            else if (value is float number) p.floatValue = number;
            else if (value is bool flag) p.boolValue = flag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
