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
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Currency;
using Lrw.Script._Core._ServiceLocator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemy04
    {
        private const string Prefab = "Assets/CTJ/Prototypes/Prefabs/Enemy04_MechanicArcher.prefab";
        private const string Arrow = "Assets/CTJ/Prototypes/Prefabs/Enemy04_Arrow.prefab";
        private static AttackTelegraphUI Line(EnemyProjectileSkill skill) => (AttackTelegraphUI)typeof(EnemyAttackSkill)
            .GetProperty("Telegraph", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(skill);

        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefab);
            Check(enemy != null, "Prefab missing");
            foreach (Component component in enemy.GetComponentsInChildren<Component>(true))
            {
                Check(component != null, "Missing component");
                SerializedProperty property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                            "Broken reference: " + component.name + "." + property.propertyPath);
            }
            EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
            EnemySkillModule skills = enemy.GetComponentInChildren<EnemySkillModule>();
            Check(skills.Skills.Count == 1 && skills.Skills[0].Skill == shot && shot.SkillSo != null, "Skill registration");
            Check(shot.CoolTime == 2.8f && Float(shot, "damage") == 14f && Float(shot, "recoveryTime") == 0.6f, "Attack stats");
            foreach (string field in new[] { "projectilePrefab", "firePoint", "attackDelayStat", "telegraphPrefab" })
                Check(new SerializedObject(shot).FindProperty(field).objectReferenceValue != null, "Missing " + field);
            StatGroup stats = (StatGroup)new SerializedObject(enemy.GetComponentInChildren<StatModule>()).FindProperty("baseStats").objectReferenceValue;
            Check(stats.Stats[0].BaseValue == 55f && Mathf.Approximately(stats.Stats[1].BaseValue + shot.PostBlinkDelay, 0.8f) &&
                stats.Stats[2].BaseValue == 10f, "HP / 0.8 preparation / gold");
            SerializedProperty entry = new SerializedObject(skills).FindProperty("skills").GetArrayElementAtIndex(0);
            Check(entry.FindPropertyRelative("minRange").floatValue == 5f && entry.FindPropertyRelative("maxRange").floatValue == 11f,
                "5..11 attack selection");
            Check(enemy.DetectionRange == 12f && enemy.ChaseReleaseRange == 14f && Float(enemy, "moveSpeed") == 3.2f, "AI ranges/speed");
            Check(enemy.GetComponentInChildren<EnemyKeepDistanceModule>().MinimumDistance == 5f &&
                Float(enemy.GetComponentInChildren<EnemyHitReactionModule>(), "hitStunDuration") == 0.22f, "Spacing / stagger");
            EnemyProjectile arrow = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(Arrow);
            Check(Mathf.Approximately(arrow.Diameter, 0.25f) && arrow.TravelDistance == 12f, "Arrow width/range");
            Check(enemy.GetComponent<BoxCollider2D>().isTrigger && enemy.GetComponentInChildren<EnemyDeathModule>() != null &&
                enemy.GetComponentInChildren<EnemyCurrencyRewardModule>() != null, "Nonblocking hurtbox / rewards");
            foreach (string old in new[] { "MadGhost_Ranged", "Enemy02_ProjectileRobot" })
                Check(!new SerializedObject(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/" + old + ".prefab")
                    .GetComponentInChildren<EnemyProjectileSkill>()).FindProperty("faceTargetDuringPreparation").boolValue,
                    "Old enemy opt-in default changed");
            return new { passed = true, scope = "references / skill registration / stats / physics / original opt-in defaults" };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            object[] result = await Task.WhenAll(Movement(), AimAndHit(), Obstacle(false), Obstacle(true), ProjectileRules());
            object death = await DeathReward();
            return new { result, death };
        }

        public static async Task<object> RenderPreview()
        {
            RequirePlay();
            GameObject arena = Arena(0f, 40f);
            RenderTexture targetTexture = new RenderTexture(960, 400, 24, RenderTextureFormat.ARGB32);
            Texture2D pixels = null; float previousTimeScale = Time.timeScale;
            try
            {
                EnemyBase enemy = Spawn(arena, 0f);
                EnemyDamageProbe target = Target(arena, new Vector2(8f, 0f));
                Sprite square = AssetDatabase.LoadAllAssetsAtPath("Assets/CTJ/Prototypes/Sprites/TestWhite.png").OfType<Sprite>().First();
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
                GameObject targetVisual = new GameObject("Probe visual"); targetVisual.transform.SetParent(target.transform, false);
                targetVisual.transform.localPosition = new Vector3(0f, 0.6f);
                targetVisual.transform.localScale = new Vector3(0.5f / square.bounds.size.x, 1.2f / square.bounds.size.y, 1f);
                SpriteRenderer sprite = targetVisual.AddComponent<SpriteRenderer>(); sprite.sprite = square;
                sprite.sharedMaterial = material; sprite.color = new Color(0.3f, 0.8f, 0.9f);
                GameObject cameraObject = new GameObject("Enemy04 preview camera"); cameraObject.transform.SetParent(arena.transform);
                cameraObject.transform.position = new Vector3(5f, 1f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 3.2f;
                camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.targetTexture = targetTexture; camera.allowHDR = false; camera.allowMSAA = false;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                enemy.SetTarget(target.transform);
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                await Until(() => shot.IsPreparing, "Preview preparation"); await Wait(0.2f);
                Time.timeScale = 0f;
                int frame = Time.frameCount; await Until(() => Time.frameCount >= frame + 3, "Render frames");
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = targetTexture; pixels = new Texture2D(960, 400, TextureFormat.RGBA32, false);
                    pixels.ReadPixels(new Rect(0, 0, 960, 400), 0, 0); pixels.Apply();
                }
                finally { RenderTexture.active = previous; }
                int redPixels = pixels.GetPixels32().Count(c => c.r > 65 && c.r > c.g * 1.5f && c.r > c.b * 1.5f);
                Check(Line(shot).IsVisible && redPixels > 400, "Aim line not visibly rendered: " + redPixels);
                string path = Application.dataPath + "/CTJ/Tools~/Enemy04_AimingPreview.png";
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                return new { passed = true, path, redPixels };
            }
            finally
            {
                Time.timeScale = previousTimeScale; arena.SetActive(false); Cleanup(arena, 0f);
                targetTexture.Release(); Object.Destroy(targetTexture); if (pixels != null) Object.Destroy(pixels);
            }
        }

        private static async Task<object> Movement()
        {
            GameObject arena = Arena(6000f, 70f);
            try
            {
                EnemyBase enemy = Spawn(arena, 6000f); Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                await Wait(0.25f);
                Check(enemy.GetModule<INewHealthModule>().CurrentHealth == 55f && Mathf.Abs(body.linearVelocityX) < 0.01f, "Delayed target / idle HP");
                EnemyDamageProbe target = Target(arena, new Vector2(6013f, 0f)); enemy.SetTarget(target.transform);
                await Wait(0.2f); Check(Mathf.Abs(body.linearVelocityX) < 0.01f, "13m must not initially detect");
                target.transform.position = new Vector3(6011.7f, 0f);
                await Until(() => body.linearVelocityX > 3.1f, "3.2 m/s approach");
                await Until(() => enemy.IsAttackInProgress, "Stop at 11m and prepare");
                Check(Vector2.Distance(enemy.transform.position, target.transform.position) <= 11f && Mathf.Abs(body.linearVelocityX) < 0.01f,
                    "Attack should stop movement");
                target.transform.position = enemy.transform.position + Vector3.right * 15f;
                await Wait(0.1f); Check(!enemy.IsAttackInProgress && Mathf.Abs(body.linearVelocityX) < 0.01f, "Release above 14m");
                float stoppedX = body.position.x;
                target.transform.position = enemy.transform.position + Vector3.right * 13f;
                await Wait(0.2f); Check(Mathf.Abs(body.position.x - stoppedX) < 0.01f, "No return to spawn / release hysteresis");
                target.transform.position = enemy.transform.position + Vector3.right * 3f;
                await Until(() => body.linearVelocityX < -3.1f, "Retreat right-side player");
                Check(enemy.transform.Find("Visual").localScale.x > 0f, "Backpedal must face player");
                await Until(() => Vector2.Distance(enemy.transform.position, target.transform.position) >= 5f, "Reach minimum range");
                await Wait(0.06f); Check(Mathf.Abs(body.linearVelocityX) < 0.01f, "Stop after spacing regained");
                enemy.SetTarget(null); enemy.SetTarget(target.transform);
                target.transform.position = enemy.transform.position + Vector3.left * 3f;
                await Until(() => body.linearVelocityX > 3.1f, "Retreat left-side player");
                Check(enemy.transform.Find("Visual").localScale.x < 0f, "Left-facing backpedal");
                enemy.SetTarget(null); await Wait(0.1f); Check(Mathf.Abs(body.linearVelocityX) < 0.01f, "Null target cleanup");
                return new { passed = true, scenario = "delayed target / 12-14 hysteresis / approach / retreat both ways / facing / stop" };
            }
            finally { Cleanup(arena, 6000f); }
        }

        private static async Task<object> AimAndHit()
        {
            GameObject arena = Arena(6100f, 60f);
            try
            {
                EnemyBase enemy = Spawn(arena, 6100f); EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                EnemyDamageProbe target = Target(arena, new Vector2(6108f, 0f)); enemy.SetTarget(target.transform);
                await Until(() => shot.IsPreparing, "Start aiming"); float started = shot.CoolTimeStartTime;
                await Wait(0.1f); target.transform.position = new Vector3(6092f, 2f);
                await Wait(0.08f);
                Check(shot.CastDirection.x < 0f && shot.CastDirection.y > 0f && enemy.transform.Find("Visual").localScale.x < 0f,
                    "Preparation tracks and turns body");
                Set(shot, "showAimLine", false); await Wait(0.05f); Check(!Line(shot).IsVisible, "Hide optional aim line");
                Set(shot, "showAimLine", true);
                await Until(() => shot.IsHoldingWarning, "Final lock");
                float lockedAt = Time.time; Vector2 locked = shot.CastDirection; Vector3 aimPoint = target.transform.position;
                Check(Line(shot).IsVisible, "Line restored");
                target.transform.position = new Vector3(6108f, 0f);
                await Wait(0.07f);
                Check(shot.IsAimLocked && Vector2.Angle(locked, shot.CastDirection) < 0.01f && enemy.transform.Find("Visual").localScale.x < 0f,
                    "Direction must remain locked");
                await Until(() => !shot.IsPreparing, "Release");
                float released = Time.time;
                Check(released - started >= 0.78f && released - lockedAt >= 0.13f, "0.8 total / 0.15 locked warning");
                EnemyProjectile bullet = FindArrow(6100f);
                Check(bullet != null && Vector2.Angle(bullet.transform.right, locked) < 0.1f && !Line(shot).IsVisible, "Launch matches warning");
                target.transform.position = aimPoint;
                await Wait(0.3f); Check(shot.IsUsing, "Recovery must last 0.6 seconds");
                await Until(() => target.ReceivedHitCount == 1, "Arrow hits target");
                Check(target.TotalDamage == 14f, "14 damage exactly once");
                await Until(() => !shot.IsUsing, "Recovery ends");
                Check(Time.time - released >= 0.58f, "Recovery too short");
                await Until(() => shot.IsPreparing, "Next cooldown");
                Check(shot.CoolTimeStartTime - started >= 2.78f, "2.8 cooldown from preparation start");
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.right, Vector2.left);
                Check(!shot.IsUsing && !Line(shot).IsVisible && enemy.IsHitReacting && enemy.GetModule<INewHealthModule>().CurrentHealth == 54f,
                    "Hit cancels preparation, clears line, reduces HP");
                float hitAt = Time.time; await Wait(0.2f); Check(enemy.IsHitReacting, "0.22 stagger persists");
                await Until(() => !enemy.IsHitReacting, "Stagger ends");
                Check(Time.time - hitAt >= 0.21f && FindArrow(6100f) == null, "Cancelled attack must not emit delayed shot");
                return new { passed = true, scenario = "aim toggle / tracking / facing / lock / damage / recovery / cooldown / hit cancel" };
            }
            finally { Cleanup(arena, 6100f); }
        }

        private static async Task<object> Obstacle(bool cliff)
        {
            float x = cliff ? 6300f : 6200f; GameObject arena = Arena(x, cliff ? 4f : 40f);
            try
            {
                if (!cliff) Box(arena, new Vector2(x - 1.8f, 1.5f), new Vector2(0.1f, 3f));
                EnemyBase enemy = Spawn(arena, x);
                EnemyDamageProbe target = Target(arena, new Vector2(x + 1f, 0f)); enemy.SetTarget(target.transform);
                await Wait(1f);
                Check(enemy.transform.position.x > x - 1.8f && enemy.transform.position.y > -0.1f &&
                    Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocityX) < 0.01f && !enemy.IsAttackInProgress,
                    "Retreat must stop at " + (cliff ? "cliff" : "wall"));
                return new { passed = true, scenario = cliff ? "retreat cliff protection" : "retreat wall protection" };
            }
            finally { Cleanup(arena, x); }
        }

        private static async Task<object> ProjectileRules()
        {
            GameObject arena = Arena(6400f, 50f);
            EnemyProjectile bullet = null;
            try
            {
                EnemyBase enemy = Spawn(arena, 6400f);
                EnemyDamageProbe target = Target(arena, new Vector2(6408f, 0f));
                Box(arena, new Vector2(6404f, 2f), new Vector2(0.02f, 4f));
                enemy.SetTarget(target.transform); await Wait(0.25f);
                Check(!enemy.IsAttackInProgress, "No aiming through wall"); enemy.SetTarget(null);
                bullet = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyProjectile>(Arrow), new Vector3(6400f, 1f), Quaternion.identity);
                bullet.Launch(enemy, target.transform, 14f, Vector2.right);
                await Until(() => bullet == null, "Thin wall stops arrow"); Check(target.ReceivedHitCount == 0, "Wall blocks damage");
                bullet = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyProjectile>(Arrow), new Vector3(6400f, 5f), Quaternion.identity);
                bullet.Launch(enemy, target.transform, 14f, Vector2.left);
                float start = Time.time, furthest = 0f;
                while (bullet != null) { furthest = Mathf.Max(furthest, 6400f - bullet.transform.position.x); await Task.Yield(); }
                Check(furthest >= 11.4f && furthest <= 12.35f && Time.time - start >= 0.78f, "12m lifetime limit: " + furthest);
                return new { passed = true, scenario = "line of sight / thin terrain collision / 12m arrow lifetime", furthest };
            }
            finally { if (bullet != null) Object.Destroy(bullet.gameObject); Cleanup(arena, 6400f); }
        }

        private static async Task<object> DeathReward()
        {
            GameObject arena = Arena(6500f, 40f);
            CurrencyManager manager = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CurrencyManager>("Assets/Lrw/GameModule/Currency/CurrencyManager.prefab"));
            manager.Initialize(); // 읽기/런타임 등록만 수행. GameEnd(저장)는 호출하지 않습니다.
            EnemyKillCountManager counter = new GameObject("Runtime kill counter").AddComponent<EnemyKillCountManager>();
            try
            {
                CurrencyData gold = manager.GetCurrency(AssetDatabase.LoadAssetAtPath<CurrencySO>("Assets/Lrw/GameModule/Currency/Gold.asset"));
                int before = gold.Count;
                EnemyBase enemy = Spawn(arena, 6500f); EnemyDamageProbe target = Target(arena, new Vector2(6508f, 0f));
                enemy.SetTarget(target.transform);
                await Until(() => FindArrow(6500f) != null, "Death test arrow release");
                EnemyProjectile bullet = FindArrow(6500f);
                enemy.SetTarget(null);
                Check(bullet != null, "Target loss removed emitted arrow");
                enemy.ApplyDamage(new DamageData { DamageAmount = 55f }, enemy.transform.position, Vector2.right, Vector2.left);
                enemy.ApplyDamage(new DamageData { DamageAmount = 55f }, enemy.transform.position, Vector2.right, Vector2.left);
                Check(enemy.IsDead && gold.Count == before + 10 && counter.GetKillCount() == 1, "Gold/kill count must increment once");
                Check(bullet != null, "Death removed emitted arrow");
                await Until(() => target.ReceivedHitCount == 1, "Arrow survives owner death");
                return new { passed = true, scenario = "emitted arrow survives target loss/death / 10 gold / one kill" };
            }
            finally
            {
                ServiceLocator.Register<ICurrencyManager>(new NullCurrencyManager());
                Object.Destroy(manager.gameObject); Object.Destroy(counter.gameObject); Cleanup(arena, 6500f);
            }
        }

        private static EnemyBase Spawn(GameObject arena, float x) => Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefab),
            new Vector3(x, 0f), Quaternion.identity, arena.transform);
        private static GameObject Arena(float x, float width)
        {
            GameObject root = new GameObject("Enemy04 verification " + x);
            Box(root, new Vector2(x, -0.5f), new Vector2(width, 1f)); return root;
        }
        private static void Box(GameObject parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject("Terrain"); obj.transform.SetParent(parent.transform);
            obj.transform.position = position; obj.layer = 6; obj.AddComponent<BoxCollider2D>().size = size;
        }
        private static EnemyDamageProbe Target(GameObject parent, Vector2 position)
        {
            GameObject obj = new GameObject("Target"); obj.transform.SetParent(parent.transform); obj.transform.position = position;
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
            collider.size = new Vector2(0.5f, 0.8f); collider.offset = new Vector2(0f, 0.4f);
            return obj.AddComponent<EnemyDamageProbe>();
        }
        private static EnemyProjectile FindArrow(float x) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
            .FirstOrDefault(p => Mathf.Abs(p.transform.position.x - x) < 25f);
        private static void Cleanup(GameObject arena, float x)
        {
            foreach (EnemyProjectile p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                if (Mathf.Abs(p.transform.position.x - x) < 30f) Object.Destroy(p.gameObject);
            Object.Destroy(arena);
        }
        private static float Float(Object obj, string field) => new SerializedObject(obj).FindProperty(field).floatValue;
        private static void Set(Object obj, string field, bool value)
        {
            SerializedObject so = new SerializedObject(obj); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds; await Until(() => Time.time >= until, "Wait " + seconds);
        }
        private static async Task Until(Func<bool> predicate, string label)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(10);
            while (!predicate()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < end, "Timeout: " + label); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
            SceneManager.GetActiveScene().path == "", "Use isolated empty verification scene");
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
