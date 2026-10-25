using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // 01 검증 도구의 BeginPlay/StopPlay/RestoreScene으로 만든 빈 임시 씬을 재사용합니다.
    public static class VerifyEnemy02
    {
        private const string Prefab = "Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab";
        private static AttackTelegraphUI Line(EnemyProjectileSkill skill) => (AttackTelegraphUI)typeof(EnemyAttackSkill)
            .GetProperty("Telegraph", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(skill);

        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefab);
            Check(enemy != null, "Missing prefab");
            foreach (Transform t in enemy.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script: " + t.name);
            EnemyProjectileSkill skill = enemy.GetComponentInChildren<EnemyProjectileSkill>();
            EnemyTurretModule turret = enemy.GetComponentInChildren<EnemyTurretModule>();
            Check(enemy.GetComponentInChildren<EnemySkillModule>().Skills[0].Skill == skill, "Skill registration");
            SerializedObject attack = new SerializedObject(skill);
            foreach (string reference in new[] { "projectilePrefab", "firePoint", "telegraphPrefab", "turret" })
                Check(attack.FindProperty(reference).objectReferenceValue != null, "Missing attack reference: " + reference);
            Check(attack.FindProperty("turret").objectReferenceValue == turret && skill.CoolTime == 6f, "Turret / 6 sec cooldown");
            Check(attack.FindProperty("damage").floatValue == 15f && attack.FindProperty("showAimLine").boolValue &&
                attack.FindProperty("trackTargetDuringPreparation").boolValue, "Damage / aiming defaults");
            Check(enemy.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Kinematic &&
                enemy.GetComponent<Rigidbody2D>().constraints == RigidbodyConstraints2D.FreezeAll, "Anchored physics");
            Check(enemy.transform.Find("Visual/RiseRoot/Body") != null && enemy.transform.Find("Visual/RiseRoot/LauncherPivot/Launcher") != null,
                "Separated body / launcher");
            Check(enemy.GetComponentInChildren<SortingGroup>() != null && enemy.GetComponentInChildren<SpriteMask>() != null, "Local mask group");
            EnemyBase ghost = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab");
            SerializedObject oldShot = new SerializedObject(ghost.GetComponentInChildren<EnemyProjectileSkill>());
            Check(!oldShot.FindProperty("trackTargetDuringPreparation").boolValue && oldShot.FindProperty("showAimLine").boolValue &&
                oldShot.FindProperty("turret").objectReferenceValue == null, "MadGhost defaults changed");
            Check(new SerializedObject(ghost.GetComponentInChildren<EnemyHitReactionModule>()).FindProperty("hitStunDuration").floatValue == 0f,
                "Existing hit timing changed");
            return new { passed = true, scope = "scripts / prefab references / stationary physics / mask isolation / original defaults" };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            return await Task.WhenAll(DeploymentAimAndHit(), HiddenFixedAim(), ProjectileSurvivesDeath(), BlockedShot());
        }

        public static async Task<object> CheckProjectileSettings()
        {
            RequirePlay();
            return await Task.WhenAll(ProjectileSettings(2400f, 0f, 0f), ProjectileSettings(2500f, 4f, 0.5f),
                ProjectileSettings(2600f, 12f, 0f), ProjectileSettings(2700f, 0f, 0.6f));
        }

        public static async Task<object> CheckTurretTiming()
        {
            RequirePlay();
            return await Task.WhenAll(TurretTiming(2800f, 0.3f), TurretTiming(2900f, 0.7f));
        }

        private static async Task<object> TurretTiming(float x, float recovery)
        {
            GameObject arena = Arena(x);
            EnemyProjectile bullet = null;
            try
            {
                EnemyBase enemy = Spawn(arena, x);
                EnemyTurretModule turret = enemy.GetModule<EnemyTurretModule>();
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                SerializedObject timing = new SerializedObject(turret);
                float emergeDuration = timing.FindProperty("emergeDuration").floatValue;
                float retractDuration = timing.FindProperty("retractDuration").floatValue;
                // 기존 설정은 저장하지 않고 실행 중 복제본에서만 비교합니다.
                Set(shot, "recoveryTime", recovery);
                EnemyDamageProbe target = Target(arena, new Vector2(x + 8f, 0f));
                float began = Time.time; enemy.SetTarget(target.transform);
                await Until(() => turret.IsDeployed);
                float emergeElapsed = Time.time - began;
                Check(Mathf.Abs(emergeElapsed - emergeDuration) < 0.08f, $"Emergence duration: {emergeElapsed}");
                await Until(() => shot.IsPreparing); await Until(() => !shot.IsPreparing);
                bullet = FindShot(x); Check(bullet != null, "Timing test did not fire");
                float releasedAt = Time.time;
                Transform pivot = enemy.transform.Find("Visual/RiseRoot/LauncherPivot");
                Vector3 firedDirection = pivot.right;
                // 공격 거리 밖 / 추적 해제 거리 안: 회수 중에는 바로 들어가거나 재조준하면 안 됩니다.
                target.transform.position = new Vector3(x + 11f, 2f);
                await Wait(recovery * 0.5f);
                Check(shot.IsUsing && turret.IsDeployed && Vector2.Angle(firedDirection, pivot.right) < 0.1f,
                    "Recovery must keep firing direction and deployment");
                await Until(() => !shot.IsUsing);
                float recoveryElapsed = Time.time - releasedAt;
                Check(Mathf.Abs(recoveryElapsed - recovery) < 0.08f, $"Recovery duration: {recoveryElapsed}, configured: {recovery}");
                await Until(() => turret.Deployment < 1f);
                Check(Vector2.Angle(turret.AimDirection, Vector2.down) < 0.15f, "Must fold down before retracting");
                float retractBegan = Time.time;
                await Until(() => turret.Deployment == 0f);
                float retractElapsed = Time.time - retractBegan;
                Check(Mathf.Abs(retractElapsed - retractDuration) < 0.08f, $"Retraction duration: {retractElapsed}");
                return new { passed = true, recovery, emergeElapsed, recoveryElapsed, retractElapsed,
                    scope = "slower emerge/retract / configurable post-shot direction and deployment hold" };
            }
            finally
            {
                if (bullet != null) Object.Destroy(bullet.gameObject);
                Object.Destroy(arena);
            }
        }

        public static async Task<object> CheckLauncherMotion()
        {
            RequirePlay();
            GameObject arena = Arena(3000f);
            try
            {
                EnemyBase enemy = Spawn(arena, 3000f);
                EnemyTurretModule turret = enemy.GetModule<EnemyTurretModule>();
                enemy.GetModule<EnemySkillModule>().enabled = false;
                Set(turret, "maxRotationSpeed", 90f);
                Transform launcher = enemy.transform.Find("Visual/RiseRoot/LauncherPivot/Launcher");
                Check(launcher.gameObject.activeSelf && Vector2.Angle(turret.AimDirection, Vector2.down) < 0.1f, "Visible downward launcher at rest");
                EnemyDamageProbe target = Target(arena, new Vector2(3005f, 0f)); enemy.SetTarget(target.transform);
                await Until(() => turret.Deployment > 0f);
                while (!turret.IsDeployed)
                {
                    Check(launcher.gameObject.activeSelf && Vector2.Angle(turret.AimDirection, Vector2.down) < 0.15f,
                        "Launcher must emerge pointing down, without visibility pop");
                    await Frames(1);
                }
                float peak = 0f;
                float end = Time.time + 0.25f;
                while (Time.time < end) peak = Mathf.Max(peak, await RotationFrame(turret, 90f));
                Check(Vector2.Angle(turret.AimDirection, Vector2.down) > 1f &&
                    Vector2.Angle(turret.AimDirection, Vector2.down) < 30f, "Aim must turn gradually instead of snapping");
                target.transform.position = new Vector3(2995f, 2f);
                end = Time.time + 2.5f;
                while (Time.time < end) peak = Mathf.Max(peak, await RotationFrame(turret, 90f));
                Vector2 desired = (Vector2)target.GetComponent<Collider2D>().bounds.center - turret.AimOrigin;
                Check(Vector2.Angle(turret.AimDirection, desired) < 1f, "Smooth aim did not reach target across angle wrap");
                target.transform.position = new Vector3(3020f, 2f);
                end = Time.time + 5f;
                while (turret.Deployment > 0f)
                {
                    Check(Time.time < end, "Fold/retract timeout");
                    if (Vector2.Angle(turret.AimDirection, Vector2.down) > 0.15f)
                        Check(turret.Deployment == 1f, "Body retracted before launcher folded");
                    peak = Mathf.Max(peak, await RotationFrame(turret, 90f));
                    if (turret.Deployment < 1f)
                        Check(Vector2.Angle(turret.AimDirection, Vector2.down) < 0.15f && launcher.gameObject.activeSelf,
                            "Downward launcher must travel with retracting body");
                }
                // 복귀 도중 재진입해도 아래 방향을 유지하며 현재 높이에서 다시 올라옵니다.
                target.transform.position = new Vector3(3005f, 0f);
                await Until(() => turret.Deployment > 0.25f);
                target.transform.position = new Vector3(3020f, 0f);
                await Wait(0.1f);
                float partial = turret.Deployment;
                target.transform.position = new Vector3(3005f, 0f);
                await Wait(0.1f);
                Check(turret.Deployment > partial && Vector2.Angle(turret.AimDirection, Vector2.down) < 0.15f,
                    "Partial re-entry must resume upward without rotation snap");
                return new { passed = true, peakDegreesPerSecond = peak,
                    scope = "downward emergence / smooth tracking / maximum speed / wraparound / fold before retract / re-entry" };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<float> RotationFrame(EnemyTurretModule turret, float maximum)
        {
            float start = Time.time; Vector2 before = turret.AimDirection;
            await Frames(1);
            float elapsed = Time.time - start;
            float degrees = Vector2.Angle(before, turret.AimDirection);
            Check(degrees <= maximum * elapsed + 0.08f, $"Rotation limit exceeded: {degrees} degrees in {elapsed}s");
            return elapsed > 0f ? degrees / elapsed : 0f;
        }

        public static async Task<object> CheckTrackedShotAlignment()
        {
            RequirePlay();
            GameObject arena = Arena(3100f);
            EnemyProjectile bullet = null;
            try
            {
                EnemyBase enemy = Spawn(arena, 3100f);
                EnemyTurretModule turret = enemy.GetModule<EnemyTurretModule>();
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                Set(turret, "maxRotationSpeed", 90f);
                EnemyDamageProbe target = Target(arena, new Vector2(3106f, 2f)); enemy.SetTarget(target.transform);
                await Until(() => shot.IsPreparing);
                float started = Time.time, peak = 0f;
                while (!shot.IsHoldingWarning)
                {
                    Check(Time.time - started < 5f && shot.IsPreparing, "Tracking test interrupted");
                    if (Time.time - started > 1f) target.transform.position = new Vector3(3094f, 4f);
                    peak = Mathf.Max(peak, await RotationFrame(turret, 90f));
                    Check(Vector2.Angle(shot.CastDirection, turret.AimDirection) < 0.1f &&
                        Vector2.Angle(Line(shot).transform.right, turret.AimDirection) < 0.1f,
                        "Aim line / shot direction diverged from actual launcher during smooth tracking");
                }
                Vector2 locked = turret.AimDirection;
                target.transform.position = new Vector3(3103f, 4f);
                await Wait(0.08f);
                Check(Vector2.Angle(locked, turret.AimDirection) < 0.1f && shot.IsAimLocked, "Final hold still rotating");
                await Until(() => !shot.IsPreparing);
                bullet = FindShot(3100f);
                Check(bullet != null && Vector2.Angle(bullet.transform.right, locked) < 0.1f,
                    "Projectile did not use locked physical barrel direction");
                return new { passed = true, peakDegreesPerSecond = peak,
                    scope = "tracking speed cap / physical barrel equals warning and projectile / final hold" };
            }
            finally
            {
                if (bullet != null) Object.Destroy(bullet.gameObject);
                Object.Destroy(arena);
            }
        }

        private static async Task<object> ProjectileSettings(float x, float speed, float lifetime)
        {
            GameObject arena = Arena(x);
            EnemyProjectile bullet = null;
            try
            {
                EnemyBase enemy = Spawn(arena, x);
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                EnemyProjectile template = (EnemyProjectile)new SerializedObject(shot).FindProperty("projectilePrefab").objectReferenceValue;
                float originalSpeed = template.Speed, originalLifetime = template.Lifetime;
                SerializedObject templateData = new SerializedObject(template);
                Check(Mathf.Approximately(originalSpeed, templateData.FindProperty("speed").floatValue) &&
                    Mathf.Approximately(originalLifetime, templateData.FindProperty("lifetime").floatValue), "Properties must read serialized fields");
                Set(shot, "projectileSpeed", speed); Set(shot, "projectileLifetime", lifetime);
                float expectedSpeed = speed > 0f ? speed : originalSpeed;
                float expectedLifetime = lifetime > 0f ? lifetime : originalLifetime;
                EnemyDamageProbe target = Target(arena, new Vector2(x + 8f, 0f)); enemy.SetTarget(target.transform);
                await Until(() => shot.IsPreparing); await Frames(2);
                float laneLength = Line(shot).GetComponent<RectTransform>().sizeDelta.x - 0.045f;
                Check(Mathf.Abs(laneLength - expectedSpeed * expectedLifetime) < 0.001f, "Aim line must use skill speed * lifetime");
                await Until(() => !shot.IsPreparing);
                bullet = FindShot(x); Check(bullet != null, "Missing configured shot");
                float releasedAt = Time.time;
                Check(bullet.Speed == expectedSpeed && bullet.Lifetime == expectedLifetime &&
                    Mathf.Approximately(bullet.TravelDistance, expectedSpeed * expectedLifetime), "Instance settings not applied");
                target.transform.position += Vector3.right * 50f;
                Rigidbody2D body = bullet.GetComponent<Rigidbody2D>();
                Vector2 start = body.position; float startTime = Time.fixedTime;
                await Until(() => Time.fixedTime >= startTime + 0.1f);
                float measuredSpeed = Vector2.Distance(start, body.position) / (Time.fixedTime - startTime);
                Check(Mathf.Abs(measuredSpeed - expectedSpeed) < 0.1f, $"Actual motion speed: {measuredSpeed}, expected: {expectedSpeed}");
                if (lifetime > 0f)
                {
                    await Until(() => bullet == null);
                    Check(Mathf.Abs(Time.time - releasedAt - expectedLifetime) < 0.08f, "Configured lifetime did not control expiry");
                }
                else
                {
                    // 클램프가 실제 필드에도 적용되는지 실행 중 복제본으로만 확인합니다.
                    bullet.Speed = -1f; bullet.Lifetime = 0f;
                    SerializedObject instanceData = new SerializedObject(bullet);
                    Check(bullet.Speed == 0.01f && bullet.Lifetime == 0.01f &&
                        instanceData.FindProperty("speed").floatValue == 0.01f &&
                        instanceData.FindProperty("lifetime").floatValue == 0.01f, "Property setter backing fields / minimum");
                }
                Check(template.Speed == originalSpeed && template.Lifetime == originalLifetime, "Shared projectile prefab was modified");
                return new { passed = true, speed, lifetime, expectedSpeed, expectedLifetime, measuredSpeed,
                    scope = "prefab fallback / independent overrides / line length / actual speed / expiry / original unchanged" };
            }
            finally
            {
                if (bullet != null) Object.Destroy(bullet.gameObject);
                Object.Destroy(arena);
            }
        }

        private static async Task<object> DeploymentAimAndHit()
        {
            GameObject arena = Arena(2000f);
            try
            {
                EnemyBase enemy = Spawn(arena, 2000f);
                EnemyTurretModule turret = enemy.GetModule<EnemyTurretModule>();
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                EnemySkillModule skills = enemy.GetModule<EnemySkillModule>();
                IHealthModule hp = enemy.GetModule<IHealthModule>();
                await Wait(0.2f);
                Check(turret.Deployment == 0f && hp.CurrentHealth == 30f && !skills.IsBusy, "Buried idle / HP / delayed target");
                float depth = new SerializedObject(turret).FindProperty("buriedDepth").floatValue;
                Check(Mathf.Abs(enemy.GetComponent<BoxCollider2D>().size.y - Mathf.Max(0.05f, 1.2f - depth)) < 0.001f, "Buried hurtbox");
                Vector3 origin = enemy.transform.position;
                EnemyDamageProbe target = Target(arena, new Vector2(2005f, 0f)); enemy.SetTarget(target.transform);
                await Wait(0.1f);
                Check(turret.Deployment > 0f && turret.Deployment < 1f && !skills.IsBusy, "No fire while emerging");
                await Until(() => shot.IsPreparing);
                Check(turret.IsDeployed && Mathf.Abs(enemy.GetComponent<BoxCollider2D>().size.y - 1.2f) < 0.001f, "Full emergence before preparation");
                float began = Time.time;
                await Wait(0.15f); Vector2 before = shot.CastDirection;
                target.transform.position = new Vector3(2004f, 2f);
                await Wait(0.3f);
                Check(Vector2.Angle(before, shot.CastDirection) > 10f && Line(shot).IsVisible, "Tracking aim / line");
                Set(shot, "showAimLine", false); target.transform.position = new Vector3(2004f, 1f);
                before = shot.CastDirection; await Wait(0.3f);
                Check(!Line(shot).IsVisible && Vector2.Angle(before, shot.CastDirection) > 5f, "Hidden line must still track");
                Set(shot, "showAimLine", true); await Wait(0.1f);
                Check(Line(shot).IsVisible, "Line re-enable during preparation");
                await Until(() => shot.IsHoldingWarning);
                float lockedAt = Time.time;
                Vector2 locked = shot.CastDirection;
                Vector3 aimPosition = target.transform.position;
                target.transform.position = new Vector3(2003f, 4f);
                await Wait(0.08f);
                Transform pivot = enemy.transform.Find("Visual/RiseRoot/LauncherPivot");
                Check(shot.IsAimLocked && Vector2.Angle(locked, shot.CastDirection) < 0.01f &&
                    Vector2.Angle(pivot.right, locked) < 0.05f, "Final lock / turret must not overwrite skill aim");
                await Until(() => !shot.IsPreparing);
                Check(Time.time - lockedAt >= 0.18f && Time.time - began >= 1.9f, "Preparation and lock duration");
                EnemyProjectile projectile = FindShot(2000f);
                Check(projectile != null && !Line(shot).IsVisible, "Released projectile / hidden line");
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, origin, Vector2.right, Vector2.left);
                Check(enemy.IsHitReacting && !enemy.GetModule<EnemyHitReactionModule>().IsKnockedBack && shot.IsUsing,
                    "Stationary stagger without cancelling already released shot");
                target.transform.position = aimPosition;
                await Until(() => target.ReceivedHitCount == 1);
                Check(target.TotalDamage == 15f && Vector3.Distance(enemy.transform.position, origin) < 0.001f,
                    $"15 damage / stationary root: damage={target.TotalDamage:R}, displacement={Vector3.Distance(enemy.transform.position, origin):R}");
                target.transform.position = new Vector3(2050f, 0f);
                await Until(() => turret.Deployment == 0f);
                Check(enemy.transform.Find("Visual/RiseRoot/LauncherPivot/Launcher").gameObject.activeSelf &&
                    Vector2.Angle(turret.AimDirection, Vector2.down) < 0.15f, "Retract folded launcher without hiding it");

                target.transform.position = new Vector3(2005f, 0f);
                await Until(() => shot.IsPreparing);
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, origin, Vector2.right, Vector2.left);
                Check(!skills.IsBusy && !Line(shot).IsVisible && enemy.IsHitReacting, "Preparation cancellation / line cleanup");
                await Wait(0.1f); Check(enemy.IsHitReacting, "Configured stagger too short");
                await Wait(0.15f); Check(!enemy.IsHitReacting && Vector3.Distance(enemy.transform.position, origin) < 0.001f && hp.CurrentHealth == 28f, "Stagger ends without displacement");
                return new { scenario = "idle / emergence / tracking / line toggle / final lock / 15 damage / retract / stationary stagger and cancel", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> HiddenFixedAim()
        {
            GameObject arena = Arena(2100f);
            try
            {
                EnemyBase enemy = Spawn(arena, 2100f);
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                Set(shot, "showAimLine", false); Set(shot, "trackTargetDuringPreparation", false);
                EnemyDamageProbe target = Target(arena, new Vector2(2105f, 0f)); enemy.SetTarget(target.transform);
                await Until(() => shot.IsPreparing);
                EnemyTurretModule turret = enemy.GetModule<EnemyTurretModule>();
                Vector2 initial = ((Vector2)target.GetComponent<Collider2D>().bounds.center - turret.AimOrigin).normalized;
                target.transform.position = new Vector3(2104f, 2f);
                await Until(() => shot.IsHoldingWarning);
                Check(!Line(shot).IsVisible && Vector2.Angle(initial, shot.CastDirection) < 0.15f && shot.IsAimLocked,
                    "Smooth aim must retain initial target direction when tracking is off");
                await Until(() => !shot.IsPreparing);
                EnemyProjectile bullet = FindShot(2100f);
                Check(bullet != null && !Line(shot).IsVisible && Vector2.Angle(bullet.transform.right, initial) < 0.1f, "Hidden line must not suppress firing");
                Object.Destroy(bullet.gameObject);
                return new { scenario = "line off for entire cast / tracking off / actual fixed-direction projectile", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> ProjectileSurvivesDeath()
        {
            GameObject arena = Arena(2200f);
            try
            {
                EnemyBase enemy = Spawn(arena, 2200f);
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                EnemyDamageProbe target = Target(arena, new Vector2(2208f, 0f)); enemy.SetTarget(target.transform);
                await Until(() => shot.IsPreparing); await Until(() => !shot.IsPreparing);
                EnemyProjectile bullet = FindShot(2200f); Check(bullet != null, "No projectile before death");
                enemy.GetModule<EnemyCurrencyRewardModule>().enabled = false; enemy.GetModule<EnemyDeathModule>().enabled = false;
                Set(enemy, "deathDespawnDelay", 0f);
                enemy.ApplyDamage(new DamageData { DamageAmount = 100f }, enemy.transform.position, Vector2.zero, Vector2.zero);
                await Wait(0.1f);
                Check(enemy == null && bullet != null, "Projectile was removed with shooter");
                await Until(() => target.ReceivedHitCount == 1);
                Check(target.TotalDamage == 15f, "Projectile after death failed damage");
                return new { scenario = "released projectile persists after shooter destruction", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> BlockedShot()
        {
            GameObject arena = Arena(2300f);
            try
            {
                EnemyBase enemy = Spawn(arena, 2300f);
                EnemyDamageProbe target = Target(arena, new Vector2(2306f, 0f)); enemy.SetTarget(target.transform);
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                Box(arena, new Vector2(2303f, 1f), new Vector2(0.1f, 2f));
                await Until(() => enemy.GetModule<EnemyTurretModule>().IsDeployed); await Frames(2);
                Check(enemy.GetModule<EnemyTurretModule>().IsDeployed && !shot.IsUsing, "Wall must block firing but not emergence");
                Object.Destroy(arena.transform.Find("Wall").gameObject);
                await Until(() => shot.IsPreparing); await Until(() => !shot.IsPreparing);
                EnemyProjectile bullet = FindShot(2300f); Check(bullet != null, "No shot after wall removed");
                Box(arena, new Vector2(2303f, 1f), new Vector2(0.1f, 2f));
                await Until(() => bullet == null);
                Check(target.ReceivedHitCount == 0, "Shot crossed blocking wall");
                return new { scenario = "stationary line of sight / projectile terrain collision", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        public static async Task<object> RenderPreview()
        {
            RequirePlay();
            GameObject arena = new GameObject("Enemy02 Render Verification");
            RenderTexture rt = new RenderTexture(480, 400, 24, RenderTextureFormat.ARGB32);
            Texture2D idle = null, raised = null, hit = null;
            try
            {
                EnemyBase enemy = Spawn(arena, 4000f);
                foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>(); Set(shot, "showAimLine", false);
                GameObject cameraObject = new GameObject("Verification Camera"); cameraObject.transform.SetParent(arena.transform);
                cameraObject.transform.position = new Vector3(4000f, 0.45f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.1f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 31;
                camera.allowHDR = false; camera.allowMSAA = false; camera.targetTexture = rt;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                await Frames(4); idle = Read(rt);
                CheckMask(idle, "idle");
                EnemyHitReactionModule reaction = enemy.GetModule<EnemyHitReactionModule>();
                Set(reaction, "blinkDuration", 0.4f); Set(reaction, "blinkInterval", 1f);
                Set(reaction, "enableScalePulse", false); // 이 검사는 원본과 마스크 윤곽을 비교합니다.
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.right, Vector2.left);
                await Frames(4); hit = Read(rt); CheckMask(hit, "hit while buried");
                Color32[] p0 = idle.GetPixels32(), p1 = hit.GetPixels32();
                Check(p0.Zip(p1, (a, b) => Math.Abs(a.a - b.a) <= 2).All(v => v), "Hit flash ignores burial mask");
                Check(p1.Where(c => c.a > 100).Select(c => (c.r << 16) | (c.g << 8) | c.b).Distinct().Count() >= 3,
                    "White hit blend erased body / launcher details");
                await Wait(0.45f);
                EnemyDamageProbe target = Target(arena, new Vector2(4003f, 1.7f)); enemy.SetTarget(target.transform);
                await Until(() => enemy.GetModule<EnemyTurretModule>().IsDeployed); await Wait(0.9f); await Frames(4);
                raised = Read(rt); CheckMask(raised, "deployed");
                Check(raised.GetPixels32().Count(c => c.a > 100) > p0.Count(c => c.a > 100) * 1.5f, "Body did not visibly emerge");
                string idlePath = Application.dataPath + "/CTJ/Tools~/Enemy02_Buried.png";
                string raisedPath = Application.dataPath + "/CTJ/Tools~/Enemy02_Deployed.png";
                File.WriteAllBytes(idlePath, idle.EncodeToPNG()); File.WriteAllBytes(raisedPath, raised.EncodeToPNG());
                return new { passed = true, idlePath, raisedPath, scope = "actual URP pixels / ground clipping / solid hit flash / body and launcher emergence" };
            }
            finally
            {
                arena.SetActive(false); Object.Destroy(arena);
                rt.Release(); Object.Destroy(rt);
                if (idle != null) Object.Destroy(idle); if (raised != null) Object.Destroy(raised); if (hit != null) Object.Destroy(hit);
            }
        }

        public static async Task<object> CheckExistingPatrolBoundary()
        {
            RequirePlay();
            GameObject arena = Arena(5000f);
            try
            {
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(
                    "Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab"), new Vector3(5000f, 0.05f), Quaternion.identity, arena.transform);
                EnemyPatrolModule patrol = enemy.GetModule<EnemyPatrolModule>();
                Set(patrol, "halfWidth", 1f);
                // 프리팹의 현재 Pause Duration(3초)은 유지하고, 경계 회귀 검사에서만 대기를 짧게 합니다.
                Set(patrol, "pauseDuration", 0.1f);
                enemy.GetComponent<Rigidbody2D>().position = new Vector2(5001.02f, 0.05f);
                float before = enemy.GetComponent<Rigidbody2D>().position.x;
                await Wait(2f);
                Check(enemy.transform.position.x <= patrol.MaxX && enemy.transform.position.x >= patrol.MinX,
                    $"01 right boundary: before={before:R}, x={enemy.transform.position.x:R}, bounds={patrol.MinX}..{patrol.MaxX}");
                float afterRight = enemy.transform.position.x;
                patrol.enabled = false;
                enemy.GetComponent<Rigidbody2D>().position = new Vector2(4998.98f, 0.05f);
                patrol.enabled = true;
                await Wait(2f);
                Check(enemy.transform.position.x >= patrol.MinX && enemy.transform.position.x <= patrol.MaxX,
                    $"01 left boundary: x={enemy.transform.position.x:R}, bounds={patrol.MinX}..{patrol.MaxX}");
                return new { passed = true, before, afterRight, afterLeft = enemy.transform.position.x, minX = patrol.MinX, maxX = patrol.MaxX,
                    velocity = enemy.GetComponent<Rigidbody2D>().linearVelocityX,
                    scope = "Approved 01 patrol fix / both boundaries / re-enable" };
            }
            finally { Object.Destroy(arena); }
        }

        private static void CheckMask(Texture2D texture, string state)
        {
            Color32[] pixels = texture.GetPixels32();
            Check(pixels.Count(c => c.a > 100) > 1000, "Nothing rendered: " + state);
            int groundRow = Mathf.FloorToInt((1.1f - 0.45f) / 2.2f * texture.height) - 2;
            Check(!pixels.Take(groundRow * texture.width).Any(c => c.a > 5), "Pixels below ground: " + state);
        }

        private static Texture2D Read(RenderTexture rt)
        {
            RenderTexture old = RenderTexture.active;
            Texture2D result = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = rt; result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); result.Apply(); return result; }
            catch { Object.Destroy(result); throw; }
            finally { RenderTexture.active = old; }
        }

        private static EnemyBase Spawn(GameObject arena, float x) => Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefab), new Vector3(x, 0f), Quaternion.identity, arena.transform);
        private static EnemyProjectile FindShot(float x) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.transform.position.x > x - 1f && p.transform.position.x < x + 10f);
        private static GameObject Arena(float x)
        {
            GameObject arena = new GameObject("Enemy02 verification " + x);
            Box(arena, new Vector2(x, -0.5f), new Vector2(30f, 1f)); return arena;
        }
        private static void Box(GameObject parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(size.x < 1f ? "Wall" : "Ground"); obj.transform.SetParent(parent.transform);
            obj.transform.position = position; obj.layer = 6; obj.AddComponent<BoxCollider2D>().size = size;
        }
        private static EnemyDamageProbe Target(GameObject parent, Vector2 position)
        {
            GameObject obj = new GameObject("Target"); obj.transform.SetParent(parent.transform); obj.transform.position = position;
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
            collider.size = new Vector2(0.5f, 0.8f); collider.offset = new Vector2(0f, 0.4f);
            return obj.AddComponent<EnemyDamageProbe>();
        }
        private static void Set(Object obj, string field, object value)
        {
            SerializedObject so = new SerializedObject(obj); SerializedProperty p = so.FindProperty(field);
            Check(p != null, "Missing field: " + field);
            if (value is bool b) p.boolValue = b; else if (value is float f) p.floatValue = f;
            else throw new InvalidOperationException("Unsupported value");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Frames(int frames)
        {
            int until = Time.frameCount + frames; await Until(() => Time.frameCount >= until);
        }
        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds; await Until(() => Time.time >= until);
        }
        private static async Task Until(Func<bool> predicate)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(15);
            while (!predicate()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < end, "Verification timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
            SceneManager.GetActiveScene().path == "", "Use the isolated empty verification scene");
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
