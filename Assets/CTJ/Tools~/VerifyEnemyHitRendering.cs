using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // 실제 URP 카메라 픽셀을 확인합니다. 런타임 복제본과 CTJ/Tools~의 미리보기 PNG만 생성합니다.
    public static class VerifyEnemyHitRendering
    {
        public static object RunEdit()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CTJ/Shaders/EnemySolidHitFlash.shader");
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Hit shader compile error");
            string[] paths = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
                "Prefabs/Enemy02_ProjectileRobot", "Bosses/RobotEyeBoss" };
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + path + ".prefab");
                var so = new SerializedObject(prefab.GetComponentInChildren<EnemyHitReactionModule>(true));
                Check(so.FindProperty("hitColor").colorValue == Color.white &&
                    Mathf.Approximately(so.FindProperty("hitColorBlend").floatValue, 0.35f), "Default hit color/blend: " + path);
                Material material = so.FindProperty("hitMaterial").objectReferenceValue as Material;
                Check(material != null && material.shader == shader && material.GetFloat("_HitBlend") == 1f,
                    "Shared material defaults changed: " + path);
            }
            return new { passed = true, scope = "shader compilation / 5 prefab defaults / shared warning material unchanged" };
        }

        public static async Task<object> RunScenarios()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Use isolated test Play session");
            return new[] { await Render(1f, 0.35f, true), await Render(0.6f, 0.35f, false),
                await Render(1f, 0f, false), await Render(1f, 1f, false) };
        }

        private static async Task<object> Render(float alpha, float blend, bool savePreview)
        {
            GameObject root = new GameObject("CTJ Blended Hit Render Verification");
            RenderTexture target = new RenderTexture(640, 320, 24, RenderTextureFormat.ARGB32);
            Texture2D before = null, after = null, restored = null;
            try
            {
                string[] names = { "Catto_Melee", "MadGhost_Ranged", "Enemy02_ProjectileRobot" };
                EnemyBase[] enemies = new EnemyBase[names.Length];
                for (int i = 0; i < enemies.Length; i++)
                {
                    EnemyBase prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/" + names[i] + ".prefab");
                    // 멀리 떨어진 좌표의 GPU 정밀도 차이를 제외하고 색/알파만 비교합니다.
                    enemies[i] = Object.Instantiate(prefab, new Vector3((i - 1) * 2.3f, 0f), Quaternion.identity, root.transform);
                    enemies[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    Animator animator = enemies[i].GetComponentInChildren<Animator>(); if (animator != null) animator.enabled = false;
                    EnemyTurretModule turret = enemies[i].GetComponentInChildren<EnemyTurretModule>();
                    if (turret != null)
                    {
                        // 조준/전개는 별도 동작 검사에서 다루고, 여기서는 몸통과 런처의 색을 모두 비교합니다.
                        turret.enabled = false;
                        enemies[i].transform.Find("Visual/RiseRoot").localPosition = Vector3.zero;
                        enemies[i].transform.Find("Visual/RiseRoot/LauncherPivot").localRotation = Quaternion.identity;
                    }
                    foreach (Transform child in enemies[i].GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                    foreach (SpriteRenderer sprite in enemies[i].GetComponentsInChildren<SpriteRenderer>())
                    {
                        sprite.gameObject.layer = 31;
                        Color color = sprite.color; color.a = alpha; sprite.color = color;
                        sprite.flipX = i == 1; // 반전된 원거리 스프라이트도 같은 실루엣이어야 합니다.
                        var properties = new MaterialPropertyBlock();
                        properties.SetFloat("_CTJVerificationMarker", 7f); sprite.SetPropertyBlock(properties);
                    }
                    SerializedObject so = new SerializedObject(enemies[i].GetModule<EnemyHitReactionModule>());
                    so.FindProperty("enableKnockback").boolValue = false;
                    so.FindProperty("enableScalePulse").boolValue = false; // 알파/무늬 비교에서 크기 변화는 분리합니다.
                    so.FindProperty("hitColorBlend").floatValue = blend;
                    so.FindProperty("blinkDuration").floatValue = 10f;
                    so.FindProperty("blinkInterval").floatValue = 10f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                GameObject cameraObject = new GameObject("CTJ Offscreen Proof Camera");
                cameraObject.transform.SetParent(root.transform);
                cameraObject.transform.position = new Vector3(0f, 1f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1.8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 31;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                camera.targetTexture = target;
                await Frames(4);
                before = Read(target);

                foreach (EnemyBase enemy in enemies)
                    enemy.ApplyDamage(new DamageData { DamageAmount = 2f }, enemy.transform.position, Vector2.right, Vector2.left);
                await Frames(4);
                after = Read(target);
                string beforePath = null, flashPath = null;
                if (savePreview)
                {
                    beforePath = Path.Combine(Application.dataPath, "CTJ/Tools~/EnemyHitBlendOriginal.png");
                    flashPath = Path.Combine(Application.dataPath, "CTJ/Tools~/EnemyHitBlendWhite.png");
                    File.WriteAllBytes(beforePath, before.EncodeToPNG());
                    File.WriteAllBytes(flashPath, after.EncodeToPNG());
                }
                Color32[] original = before.GetPixels32(), flash = after.GetPixels32();
                int filled = 0, wrongColor = 0, changedAlpha = 0, brightened = 0;
                for (int i = 0; i < original.Length; i++)
                {
                    if (Math.Abs(original[i].a - flash[i].a) > 2) changedAlpha++;
                    if (original[i].a < 100) continue;
                    filled++;
                    if (blend == 0f)
                    {
                        if (Math.Abs(flash[i].r - original[i].r) > 2 || Math.Abs(flash[i].g - original[i].g) > 2 ||
                            Math.Abs(flash[i].b - original[i].b) > 2) wrongColor++;
                    }
                    else if (blend == 1f)
                    {
                        if (flash[i].r < 250 || flash[i].g < 250 || flash[i].b < 250) wrongColor++;
                    }
                    else
                    {
                        if (flash[i].r + 2 < original[i].r || flash[i].g + 2 < original[i].g || flash[i].b + 2 < original[i].b) wrongColor++;
                        if (flash[i].r > original[i].r + 5 || flash[i].g > original[i].g + 5 || flash[i].b > original[i].b + 5) brightened++;
                    }
                }
                Check(filled > 1000, "No sprites rendered: " + filled);
                Check(changedAlpha == 0, $"Flash changed silhouette/alpha: {changedAlpha}, alpha={alpha}, blend={blend}, " +
                    string.Join("; ", Enumerable.Range(0, original.Length).Where(i => Math.Abs(original[i].a - flash[i].a) > 2)
                        .Take(8).Select(i => $"({i % target.width},{i / target.width}) {original[i]} -> {flash[i]}")));
                Check(wrongColor == 0, $"Incorrect white blend / original tint: {wrongColor}, alpha={alpha}, blend={blend}, sample=" +
                    string.Join("; ", original.Zip(flash, (a, b) => new { a, b }).Where(p => p.a.a > 100 &&
                        (Math.Abs(p.a.r - p.b.r) > 2 || Math.Abs(p.a.g - p.b.g) > 2 || Math.Abs(p.a.b - p.b.b) > 2))
                        .Take(3).Select(p => p.a + " -> " + p.b)));
                int detailColors = flash.Where(p => p.a >= 100).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().Count();
                if (blend > 0f && blend < 1f)
                    Check(brightened > filled / 2 && detailColors >= 3, "Blend erased details or did not brighten");
                int[] originalColors = original.Where(p => p.a >= 100).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().ToArray();
                // 현재 두 원본 스프라이트의 팔레트는 어두운 몸체/분홍 눈의 2색입니다.
                Check(originalColors.Length >= 2 && original.Any(p => p.a >= 100 && p.r < 64),
                    "Baseline must contain original dark body and details: " + string.Join(",", originalColors));

                foreach (EnemyBase enemy in enemies) enemy.GetModule<EnemyHitReactionModule>().enabled = false;
                foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>())
                {
                    var properties = new MaterialPropertyBlock(); sprite.GetPropertyBlock(properties);
                    Check(properties.GetFloat("_CTJVerificationMarker") == 7f && properties.GetFloat("_HitBlend") == 0f,
                        "Original property block was not restored");
                }
                await Frames(4);
                restored = Read(target);
                Color32[] restoredPixels = restored.GetPixels32();
                Check(original.SequenceEqual(restoredPixels), "Original rendered pixels not restored");
                return new { scenario = "actual-URP-white-blend-details-alpha-tint-and-property-restore", alpha, blend, filled, brightened, detailColors, wrongColor, changedAlpha,
                    beforePath, flashPath, passed = true };
            }
            finally
            {
                root.SetActive(false);
                Object.Destroy(root);
                target.Release(); Object.Destroy(target);
                if (before != null) Object.Destroy(before);
                if (after != null) Object.Destroy(after);
                if (restored != null) Object.Destroy(restored);
            }
        }

        private static Texture2D Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                return image;
            }
            catch { Object.Destroy(image); throw; }
            finally { RenderTexture.active = previous; }
        }
        private static async Task Frames(int count)
        {
            int frame = Time.frameCount + count;
            DateTime timeout = DateTime.UtcNow.AddSeconds(10);
            while (Time.frameCount < frame)
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < timeout, "Render wait timeout");
                await Task.Yield();
            }
        }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
