using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Currency;
using Lrw.Script._Core._ServiceLocator;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyVerticalFlip
    {
        private const string Prefab = "Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab";
        public static object ConfigurePrefab()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            GameObject root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var module = root.GetComponentInChildren<EnemyVerticalFlipModule>(true);
                if (module == null)
                {
                    var go = new GameObject("VerticalFlipModule");
                    go.transform.SetParent(root.transform, false);
                    module = go.AddComponent<EnemyVerticalFlipModule>();
                }
                module.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return RunEdit();
        }

        public static object RunEdit()
        {
            var source = AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefab);
            var module = source.GetComponentInChildren<EnemyVerticalFlipModule>(true);
            Check(module != null && module.transform.parent == source.transform && module.gameObject.activeSelf && !module.enabled,
                "Active child, component disabled by default");
            Check(source.transform.localScale == Vector3.one, "Unflipped source transform");
            foreach (Transform child in source.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + child.name);
            foreach (string path in new[] { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
                "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" })
                Check(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + path + ".prefab")
                    .GetComponentInChildren<EnemyVerticalFlipModule>(true) == null, "Optional module added to unsupported prefab: " + path);
            return new { passed = true, scope = "Enemy02 only / disabled child / intact references" };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            var errors = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            try
            {
                object toggles = await ToggleAndCancel();
                object right = await CeilingCombat(4100f, 1f);
                object left = await CeilingCombat(4200f, -1f);
                object reward = await CeilingReward();
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, toggles, right, left, reward, errors };
            }
            finally { Application.logMessageReceived -= capture; }
        }

        private static async Task<object> ToggleAndCancel()
        {
            var arena = Arena(4000f);
            try
            {
                var enemy = Spawn(arena, 4000f, false);
                var flip = enemy.GetModule<EnemyVerticalFlipModule>();
                var turret = enemy.GetModule<EnemyTurretModule>();
                var skill = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                Vector3 rootPosition = enemy.transform.position;
                Check(flip != null && !flip.IsFlipped && !flip.enabled, "Disabled module must be discoverable");
                Check(Vector2.Angle(turret.AimDirection, Vector2.down) < 0.1f, "Normal stowed direction");
                var hurtbox = enemy.GetComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
                float normalY = hurtbox.bounds.center.y;
                flip.SetFlipped(true); Physics2D.SyncTransforms();
                Check(flip.IsFlipped && enemy.transform.localScale == new Vector3(1f, -1f, 1f), "Flip only Y");
                Check(Mathf.Abs(hurtbox.bounds.center.y - (8f - normalY)) < 0.01f, "Mirrored hurtbox");
                Check(Vector2.Angle(turret.AimDirection, Vector2.up) < 0.1f, "Ceiling stowed direction");
                flip.SetFlipped(true);
                Check(enemy.transform.localScale.y == -1f, "Repeated enable must not double flip");
                flip.gameObject.SetActive(false);
                Check(enemy.transform.localScale.y == 1f && !flip.IsFlipped, "Inactive child restores pose");
                flip.gameObject.SetActive(true);
                Check(enemy.transform.localScale.y == -1f && flip.IsFlipped, "Child reactivation reapplies pose");
                flip.enabled = false;
                Check(enemy.transform.localScale.y == 1f, "Inspector checkbox off restores pose");
                flip.enabled = true;
                var target = Target(arena, new Vector2(4004f, 0f)); enemy.SetTarget(target.transform);
                await Until(() => skill.IsPreparing);
                flip.SetFlipped(false);
                Check(!skill.IsUsing && !enemy.IsAttackInProgress, "Runtime flip cancels stale aim");
                Check(!Line(skill).IsVisible && enemy.transform.position == rootPosition, "Line cleared / anchor unchanged");
                flip.SetFlipped(true); enemy.SetTarget(null);
                await Until(() => turret.Deployment == 0f);
                Check(Vector2.Angle(turret.AimDirection, Vector2.up) < 0.15f, "Ceiling launcher folds into ceiling");
                return new { defaultOff = true, toggles = "API / enabled / GameObject", staleAimCancelled = true };
            }
            finally { Cleanup(arena); }
        }

        private static async Task<object> CeilingCombat(float x, float side)
        {
            var arena = Arena(x);
            try
            {
                var enemy = Spawn(arena, x, true);
                var flip = enemy.GetModule<EnemyVerticalFlipModule>();
                var turret = enemy.GetModule<EnemyTurretModule>();
                var skill = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                var target = Target(arena, new Vector2(x + side * 4f, 0f));
                Transform rise = enemy.transform.Find("Visual/RiseRoot");
                Transform muzzle = (Transform)new SerializedObject(skill).FindProperty("firePoint").objectReferenceValue;
                float idleY = rise.position.y;
                Check(flip.IsFlipped && enemy.transform.localScale.y == -1f, "Pre-enabled module initializes correctly");
                enemy.SetTarget(target.transform);
                await Until(() => turret.IsDeployed);
                Check(rise.position.y < idleY - 0.55f, "Ceiling deployment must move downward");
                await Until(() => skill.IsHoldingWarning);
                Check(skill.CastDirection.y < 0f && Mathf.Sign(skill.CastDirection.x) == side, "Aim toward player below ceiling");
                Check(Vector2.Angle((Vector2)muzzle.position - turret.AimOrigin, skill.CastDirection) < 0.1f, "Muzzle geometry / cast direction agree");
                var line = Line(skill);
                Check(line.IsVisible && Vector2.Angle(line.transform.TransformVector(Vector3.right), skill.CastDirection) < 0.15f,
                    "Reflected-parent aim line follows actual shot");
                var ui = enemy.GetComponentInChildren<EnemyTemporaryStatusUI>();
                if (ui == null) ui = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyTemporaryStatusUI>(
                    "Assets/CTJ/Prototypes/Prefabs/EnemyTemporaryStatusUI.prefab"), enemy.transform);
                await Frames(2);
                Check(ui.transform.position.y < enemy.transform.position.y &&
                    Vector3.Dot(ui.transform.TransformVector(Vector3.up).normalized, Vector3.up) > 0.99f &&
                    Vector3.Dot(ui.transform.TransformVector(Vector3.right).normalized, Vector3.right) > 0.99f,
                    "Health bar stays upright below ceiling");
                await Until(() => target.ReceivedHitCount > 0);
                Check(Mathf.Approximately(target.TotalDamage, 15f), "Ceiling shot delivers existing damage");
                enemy.SetTarget(null);
                await Until(() => turret.Deployment == 0f);
                Check(Mathf.Abs(rise.position.y - idleY) < 0.01f && Vector2.Angle(turret.AimDirection, Vector2.up) < 0.15f,
                    "Retract upward and fold into ceiling");
                float hp = enemy.GetModule<INewHealthModule>().CurrentHealth;
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.up, Vector2.down);
                Check(enemy.GetModule<INewHealthModule>().CurrentHealth == hp - 1f && flip.IsFlipped, "Hit feedback does not undo orientation");
                return new { side, downwardDeployment = true, targetHit = true, aimLineAndMuzzleAligned = true, uprightUI = true };
            }
            finally { Cleanup(arena); }
        }

        private static async Task<object> CeilingReward()
        {
            var arena = Arena(4300f);
            ICurrencyManager previous = ServiceLocator.Get<ICurrencyManager>();
            var manager = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CurrencyManager>("Assets/Lrw/GameModule/Currency/CurrencyManager.prefab"));
            manager.Initialize();
            try
            {
                var enemy = Spawn(arena, 4300f, true);
                enemy.GetModule<EnemyCurrencyRewardModule>().enabled = true;
                var gold = manager.GetCurrency(AssetDatabase.LoadAssetAtPath<CurrencySO>("Assets/Lrw/GameModule/Currency/Gold.asset"));
                int before = gold.Count;
                enemy.GetModule<INewHealthModule>().ApplyDamage(9999f);
                var coins = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None);
                Check(coins.Length == 5 && coins.All(c => c.transform.position.y < 4f) && gold.Count == before,
                    "Coins spawn below ceiling, payout delayed");
                await Until(() => coins.All(c => c == null));
                Check(gold.Count == before + 15, "Ceiling death reward conserved");
                return new { coinCount = 5, gold = 15, belowCeiling = true };
            }
            finally
            {
                Cleanup(arena); Object.Destroy(manager.gameObject);
                ServiceLocator.Register<ICurrencyManager>(previous);
            }
        }

        public static async Task<object> RenderPreview()
        {
            RequirePlay();
            var arena = Arena(4400f);
            var rt = new RenderTexture(480, 400, 24, RenderTextureFormat.ARGB32);
            try
            {
                var enemy = Spawn(arena, 4400f, true);
                foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                var cameraObject = new GameObject("Ceiling turret preview camera"); cameraObject.transform.SetParent(arena.transform);
                cameraObject.transform.position = new Vector3(4400f, 3.45f, -10f);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.1f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
                camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false; camera.targetTexture = rt;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                await Frames(4);
                string idle = SavePreview(rt, "CeilingTurret_Idle.png");
                enemy.SetTarget(Target(arena, new Vector2(4404f, 0f)).transform);
                var skill = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                await Until(() => skill.IsHoldingWarning); await Frames(2);
                string deployed = SavePreview(rt, "CeilingTurret_Deployed.png");
                return new { idle, deployed };
            }
            finally { Cleanup(arena); rt.Release(); Object.Destroy(rt); }
        }

        private static string SavePreview(RenderTexture rt, string filename)
        {
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                string path = Application.dataPath + "/CTJ/Tools~/" + filename;
                File.WriteAllBytes(path, texture.EncodeToPNG()); return path;
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }

        private static EnemyBase Spawn(GameObject arena, float x, bool flipped)
        {
            var staging = new GameObject("Spawn staging"); staging.transform.SetParent(arena.transform); staging.SetActive(false);
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefab), new Vector3(x, 4f), Quaternion.identity, staging.transform);
            enemy.GetComponentInChildren<EnemyVerticalFlipModule>(true).enabled = flipped;
            enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            staging.SetActive(true); enemy.SetTarget(null); return enemy;
        }
        private static GameObject Arena(float x)
        {
            var arena = new GameObject("Vertical flip verification");
            foreach (float y in new[] { -0.5f, 4.5f })
            {
                var surface = new GameObject("Surface"); surface.transform.SetParent(arena.transform); surface.layer = 6;
                surface.transform.position = new Vector3(x, y); surface.AddComponent<BoxCollider2D>().size = new Vector2(24f, 1f);
            }
            return arena;
        }
        private static EnemyDamageProbe Target(GameObject parent, Vector2 position)
        {
            var target = new GameObject("Target"); target.transform.SetParent(parent.transform); target.transform.position = position;
            target.layer = 7; var box = target.AddComponent<BoxCollider2D>(); box.isTrigger = true;
            box.size = new Vector2(0.6f, 0.8f); box.offset = Vector2.up * 0.4f;
            return target.AddComponent<EnemyDamageProbe>();
        }
        private static AttackTelegraphUI Line(EnemyProjectileSkill skill) => (AttackTelegraphUI)typeof(EnemyAttackSkill)
            .GetProperty("Telegraph", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skill);
        private static void Cleanup(GameObject arena)
        {
            arena.SetActive(false); Object.Destroy(arena);
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(projectile.gameObject);
        }
        private static async Task Frames(int count)
        {
            int end = Time.frameCount + count; await Until(() => Time.frameCount >= end);
        }
        private static async Task Until(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(12);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Verification timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
            SceneManager.GetActiveScene().path == "", "Isolated verification scene required");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
