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
        public static async Task<object> RunScenarios()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path ==
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Use CTJ test Play session");
            return new[] { await Render(1f, true), await Render(0.6f, false) };
        }

        private static async Task<object> Render(float alpha, bool savePreview)
        {
            GameObject root = new GameObject("CTJ Solid Hit Render Verification");
            RenderTexture target = new RenderTexture(640, 320, 24, RenderTextureFormat.ARGB32);
            Texture2D before = null, after = null, restored = null;
            try
            {
                string[] names = { "Catto_Melee", "MadGhost_Ranged" };
                EnemyBase[] enemies = new EnemyBase[2];
                for (int i = 0; i < enemies.Length; i++)
                {
                    EnemyBase prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/" + names[i] + ".prefab");
                    enemies[i] = Object.Instantiate(prefab, new Vector3(12000f + (i == 0 ? -1.6f : 1.6f), 0f), Quaternion.identity, root.transform);
                    enemies[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    enemies[i].GetComponentInChildren<Animator>().enabled = false;
                    foreach (SpriteRenderer sprite in enemies[i].GetComponentsInChildren<SpriteRenderer>())
                    {
                        sprite.gameObject.layer = 31;
                        sprite.color = new Color(1f, 1f, 1f, alpha);
                        sprite.flipX = i == 1; // 반전된 원거리 스프라이트도 같은 실루엣이어야 합니다.
                    }
                    SerializedObject so = new SerializedObject(enemies[i].GetModule<EnemyHitReactionModule>());
                    so.FindProperty("enableKnockback").boolValue = false;
                    so.FindProperty("blinkDuration").floatValue = 10f;
                    so.FindProperty("blinkInterval").floatValue = 10f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                GameObject cameraObject = new GameObject("CTJ Offscreen Proof Camera");
                cameraObject.transform.SetParent(root.transform);
                cameraObject.transform.position = new Vector3(12000f, 1f, -10f);
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
                    beforePath = Path.Combine(Application.dataPath, "CTJ/Tools~/EnemyHitOriginal.png");
                    flashPath = Path.Combine(Application.dataPath, "CTJ/Tools~/EnemyHitSolidRed.png");
                    File.WriteAllBytes(beforePath, before.EncodeToPNG());
                    File.WriteAllBytes(flashPath, after.EncodeToPNG());
                }
                Color32[] original = before.GetPixels32(), flash = after.GetPixels32();
                int filled = 0, wrongColor = 0, changedAlpha = 0;
                for (int i = 0; i < original.Length; i++)
                {
                    if (Math.Abs(original[i].a - flash[i].a) > 2) changedAlpha++;
                    if (original[i].a < 100) continue;
                    filled++;
                    // 검은 윤곽선을 포함한 모든 가시 픽셀이 초록/파랑 없는 빨강이어야 합니다.
                    if (flash[i].r < 100 || flash[i].g > 2 || flash[i].b > 2) wrongColor++;
                }
                Check(filled > 1000, "No sprites rendered: " + filled);
                Check(changedAlpha == 0, "Flash changed silhouette/alpha: " + changedAlpha);
                Check(wrongColor == 0, "Texture RGB/black outline remains or shader failed: " + wrongColor);
                int[] originalColors = original.Where(p => p.a >= 100).Select(p => (p.r << 16) | (p.g << 8) | p.b).Distinct().ToArray();
                // 현재 두 원본 스프라이트의 팔레트는 어두운 몸체/분홍 눈의 2색입니다.
                Check(originalColors.Length >= 2 && original.Any(p => p.a >= 100 && p.r < 64),
                    "Baseline must contain original dark body and details: " + string.Join(",", originalColors));

                foreach (EnemyBase enemy in enemies) enemy.GetModule<EnemyHitReactionModule>().enabled = false;
                await Frames(4);
                restored = Read(target);
                Color32[] restoredPixels = restored.GetPixels32();
                Check(original.SequenceEqual(restoredPixels), "Original rendered pixels not restored");
                return new { scenario = "actual-URP-solid-fill-alpha-and-material-restore", alpha, filled, wrongColor, changedAlpha,
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
