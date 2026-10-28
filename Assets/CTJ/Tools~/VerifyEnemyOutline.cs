using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyOutline
    {
        private static readonly string[] Paths = {
            "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
            "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss"
        };

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            return await Task.WhenAll(Paths.Select((path, index) => Lifecycle(path, 8000f + index * 100f)));
        }

        public static async Task<object> CombinedFeedback()
        {
            RequirePlay();
            GameObject root = new GameObject("Outline / existing feedback verification");
            try
            {
                EnemyBase enemy = Spawn(root, Paths[2], new Vector3(9000f, 0f));
                var outline = enemy.GetComponentInChildren<EnemyOutlineModule>();
                var reaction = enemy.GetComponentInChildren<EnemyHitReactionModule>();
                var warning = enemy.GetComponentInChildren<EnemyAttackWarningModule>();
                warning.enabled = true; // 경고 모듈 자체의 조합 검증에만 켭니다. 01 프리팹에서는 비활성입니다.
                Set(reaction, "enableBlink", true); Set(reaction, "enableScalePulse", true);
                SpriteRenderer source = Sources(outline)[0]; Material original = source.sharedMaterial; Color color = source.color;
                Vector3 scale = source.transform.localScale;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo show = typeof(EnemyAttackWarningModule).GetMethod("Show", flags);
                MethodInfo hide = typeof(EnemyAttackWarningModule).GetMethod("Hide", flags);
                await Frames(2); show.Invoke(warning, null); await Frames(2);
                Check(warning.IsShowing && source.color == Color.red && !outline.IsShowingHit, "Attack warning coexists with red outline");
                Hit(enemy); await Frames(2);
                Check(reaction.IsBlinking && !warning.IsShowing && outline.IsShowingHit &&
                    Outlines(enemy).All(s => s.color == Color.white), "Hit outline / existing flash / warning priority");
                Check(source.transform.localScale != scale && source.transform.Find("TemporaryOutline").localScale == Vector3.one,
                    "Outline must inherit hit scale exactly once");
                await Wait(0.4f);
                Check(!outline.IsShowingHit && !reaction.IsBlinking && warning.IsShowing && source.color == Color.red &&
                    source.transform.localScale == scale, "Hit restore / pending warning / scale restore");
                outline.enabled = false; Check(warning.IsShowing && source.color == Color.red, "Outline toggle disturbed warning");
                hide.Invoke(warning, null);
                Check(source.sharedMaterial == original && source.color == color, "Original material/color not restored");
                return new { passed = true, scope = "attack warning + blink + scale pulse + outline + independent removal" };
            }
            finally { root.SetActive(false); Object.Destroy(root); }
        }

        private static async Task<object> Lifecycle(string path, float x)
        {
            GameObject root = new GameObject("Outline lifecycle " + path);
            try
            {
                EnemyBase enemy = Spawn(root, path, new Vector3(x, 0f));
                EnemyOutlineModule module = enemy.GetComponentInChildren<EnemyOutlineModule>();
                SpriteRenderer[] sources = Sources(module);
                Material[] original = sources.Select(s => s.sharedMaterial).ToArray();
                foreach (SpriteRenderer source in sources)
                {
                    var block = new MaterialPropertyBlock(); block.SetFloat("_OutlineTestMarker", 17f); source.SetPropertyBlock(block);
                }
                await Frames(3);
                Check(Outlines(enemy).Length == sources.Length, "One overlay per source: " + path);
                Check(!module.IsShowingHit && module.CurrentColor.r > module.CurrentColor.g * 2f, "Default red");
                INewHealthModule hp = enemy.GetModule<INewHealthModule>();
                Hit(enemy); await Frames(2);
                Check(module.IsShowingHit && module.CurrentColor == Color.white, "Hit white with blink/knockback disabled");
                Check(Outlines(enemy).All(s => s.color == Color.white), "White overlay color");
                await Wait(0.15f); Hit(enemy); await Wait(0.15f);
                Check(module.IsShowingHit, "Repeated hit refresh"); hp.ApplyHeal(1f);
                await Wait(0.13f); Check(!module.IsShowingHit, "Heal must not refresh flash");

                sources[0].flipX = !sources[0].flipX; sources[0].flipY = !sources[0].flipY;
                sources[0].transform.localScale = new Vector3(-1.2f, 0.8f, 1f);
                sources[0].sortingOrder += 2;
                Sprite next = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(sources[0].sprite)).OfType<Sprite>()
                    .FirstOrDefault(s => s != sources[0].sprite);
                if (next != null) sources[0].sprite = next;
                await Frames(2);
                SpriteRenderer overlay = sources[0].transform.Find("TemporaryOutline").GetComponent<SpriteRenderer>();
                Check(overlay.sprite == sources[0].sprite && overlay.flipX == sources[0].flipX && overlay.flipY == sources[0].flipY &&
                    overlay.sortingOrder == (new SerializedObject(module).FindProperty("outerSilhouetteOnly").boolValue
                        ? sources.Min(s => s.sortingOrder) - 1 : sources[0].sortingOrder + 1) && overlay.maskInteraction == sources[0].maskInteraction &&
                    overlay.transform.localScale == Vector3.one, "Animation / flip / sorting / mask / scale sync");
                sources[0].enabled = false; await Frames(2); Check(!overlay.enabled, "Disabled source overlay remained");
                sources[0].enabled = true;
                module.gameObject.SetActive(false); Check(Outlines(enemy).All(s => !s.enabled), "Immediate cleanup on child disable");
                Hit(enemy); module.gameObject.SetActive(true); await Frames(2);
                Check(!module.IsShowingHit && Outlines(enemy).Length == sources.Length, "Reenable should not duplicate renderers or retain old hit");
                Hit(enemy); Check(module.IsShowingHit, "Reenable subscription");
                module.enabled = false;
                for (int i = 0; i < sources.Length; i++)
                {
                    Check(sources[i].sharedMaterial == original[i], "Source material changed");
                    var block = new MaterialPropertyBlock(); sources[i].GetPropertyBlock(block);
                    Check(block.GetFloat("_OutlineTestMarker") == 17f, "Source property block changed");
                }
                module.enabled = true; Object.Destroy(module.gameObject); await Frames(3);
                Check(Outlines(enemy).Length == 0, "Removing module left orphan overlays");
                float before = hp.CurrentHealth; Hit(enemy); Check(hp.CurrentHealth == before - 1f, "Damage broken after removal");
                return new { passed = true, path, scope = "HP subscription / timing / heal / boss immunity / render sync / disable / removal / originals preserved" };
            }
            finally { root.SetActive(false); Object.Destroy(root); }
        }

        public static async Task<object> RenderPreview()
        {
            RequirePlay();
            GameObject root = new GameObject("Outline render proof");
            RenderTexture rt = new RenderTexture(960, 400, 24, RenderTextureFormat.ARGB32);
            Texture2D before = null, red = null, white = null, restored = null;
            try
            {
                string[] paths = { Paths[0], Paths[1], Paths[3] };
                EnemyBase[] enemies = paths.Select((p, i) => Spawn(root, p, new Vector3((i - 1) * 2.5f, 0f))).ToArray();
                EnemyOutlineModule[] modules = enemies.Select(e => e.GetComponentInChildren<EnemyOutlineModule>()).ToArray();
                foreach (EnemyBase enemy in enemies)
                {
                    EnemyTurretModule turret = enemy.GetComponentInChildren<EnemyTurretModule>();
                    if (turret != null)
                    {
                        turret.enabled = false; enemy.transform.Find("Visual/RiseRoot").localPosition = new Vector3(0f, -0.2f);
                        enemy.transform.Find("Visual/RiseRoot/LauncherPivot").localRotation = Quaternion.identity;
                    }
                    foreach (Transform t in enemy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                }
                foreach (EnemyOutlineModule module in modules) { Set(module, "hitDuration", 10f); module.enabled = false; }
                GameObject cam = new GameObject("Proof camera"); cam.transform.SetParent(root.transform);
                cam.transform.position = new Vector3(0f, 1f, -10f);
                Camera camera = cam.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.8f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 31; camera.targetTexture = rt; camera.allowHDR = false; camera.allowMSAA = false;
                cam.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                await Frames(4); before = Read(rt);
                foreach (EnemyOutlineModule module in modules) module.enabled = true;
                await Frames(4); red = Read(rt);
                foreach (EnemyBase enemy in enemies) Hit(enemy);
                await Frames(4); white = Read(rt);
                Color32[] a = before.GetPixels32(), b = red.GetPixels32(), c = white.GetPixels32();
                int redPixels = 0, whitePixels = 0, interior = 0, outside = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i].a < 2 && (b[i].a > 2 || c[i].a > 2)) outside++;
                    if (a[i].a < 240) continue;
                    bool edge = b[i].r > a[i].r + 20 && b[i].r > b[i].g * 2f;
                    if (edge) { redPixels++; if (c[i].r > 240 && c[i].g > 240 && c[i].b > 240) whitePixels++; }
                    else if (Close(a[i], b[i]) && Close(a[i], c[i])) interior++;
                }
                Check(redPixels > 300 && whitePixels > redPixels * 0.95f, $"No red/white outlines: {redPixels}/{whitePixels}");
                Check(interior > redPixels && outside == 0, $"Silhouette/inner details changed: interior={interior}, edge={redPixels}, outside={outside}");
                string basePath = Application.dataPath + "/CTJ/Tools~/";
                File.WriteAllBytes(basePath + "EnemyOutline_Normal.png", red.EncodeToPNG());
                File.WriteAllBytes(basePath + "EnemyOutline_Hit.png", white.EncodeToPNG());

                // 모듈을 끄면 픽셀이 원본으로 돌아오는지 확인합니다.
                foreach (EnemyOutlineModule module in modules) module.enabled = false;
                await Frames(3); restored = Read(rt);
                Check(a.Zip(restored.GetPixels32(), (left, right) => Close(left, right)).All(v => v), "Disable did not restore original rendering");
                // 사망 시 남은 흰색 타이머가 있어도 렌더링을 종료해야 합니다.
                modules[0].enabled = true;
                var health = enemies[0].GetModule<INewHealthModule>(); health.ApplyDamage(health.CurrentHealth);
                await Frames(2); Check(Outlines(enemies[0]).All(s => !s.enabled), "Dead enemy outline remained");
                return new { passed = true, redPixels, whitePixels, interior, outside, scope = "actual URP / red-white / atlas frames / turret mask / silhouette / disable / death" };
            }
            finally
            {
                root.SetActive(false); Object.Destroy(root); rt.Release(); Object.Destroy(rt);
                foreach (Texture2D image in new[] { before, red, white, restored }) if (image != null) Object.Destroy(image);
            }
        }

        private static EnemyBase Spawn(GameObject root, string path, Vector3 position)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + path + ".prefab"),
                position, Quaternion.identity, root.transform);
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            foreach (Animator animator in enemy.GetComponentsInChildren<Animator>()) animator.enabled = false;
            EnemyPatrolModule patrol = enemy.GetComponentInChildren<EnemyPatrolModule>(); if (patrol != null) patrol.enabled = false;
            var feedback = enemy.GetComponentInChildren<EnemyHitReactionModule>();
            Set(feedback, "enableBlink", false); Set(feedback, "enableKnockback", false); Set(feedback, "enableScalePulse", false);
            Set(feedback, "hitStunDuration", 0f);
            enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            return enemy;
        }
        private static SpriteRenderer[] Sources(EnemyOutlineModule module)
        {
            SerializedProperty sprites = new SerializedObject(module).FindProperty("sprites");
            return Enumerable.Range(0, sprites.arraySize).Select(i => sprites.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer).ToArray();
        }
        private static SpriteRenderer[] Outlines(EnemyBase enemy) => enemy.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(s => s.name == "TemporaryOutline").ToArray();
        private static void Hit(EnemyBase enemy) => enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.right, Vector2.left);
        private static bool Close(Color32 a, Color32 b) => Math.Abs(a.r - b.r) <= 2 && Math.Abs(a.g - b.g) <= 2 && Math.Abs(a.b - b.b) <= 2 && Math.Abs(a.a - b.a) <= 2;
        private static Texture2D Read(RenderTexture rt)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt; Texture2D pixels = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); pixels.Apply(); return pixels;
            }
            finally { RenderTexture.active = previous; }
        }
        private static void Set(Object obj, string field, object value)
        {
            SerializedObject so = new SerializedObject(obj); SerializedProperty p = so.FindProperty(field);
            if (value is bool b) p.boolValue = b; else if (value is float f) p.floatValue = f;
            else throw new InvalidOperationException("Unsupported field: " + field);
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
            DateTime end = DateTime.UtcNow.AddSeconds(10);
            while (!predicate()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < end, "Verification timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
            SceneManager.GetActiveScene().path == "", "Use isolated blank test scene");
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
