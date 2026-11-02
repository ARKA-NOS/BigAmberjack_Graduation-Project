using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyOuterContour
    {
        public static async Task<object> Render()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play only");
            AssetDatabase.ImportAsset("Assets/CTJ/Shaders/EnemyOutline.shader", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab", ImportAssetOptions.ForceUpdate);
            GameObject root = new GameObject("Contour verification");
            RenderTexture rt = new RenderTexture(144, 96, 24, RenderTextureFormat.ARGB32);
            try
            {
                EnemyBase turret = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab"), root.transform);
                turret.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                turret.GetComponentInChildren<EnemyTurretModule>().enabled = false;
                EnemyOutlineModule outline = turret.GetComponentInChildren<EnemyOutlineModule>();
                Transform rise = turret.transform.Find("Visual/RiseRoot");
                Transform pivot = rise.Find("LauncherPivot");
                Camera camera = new GameObject("Contour camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
                camera.orthographic = true; camera.orthographicSize = 1.1f; camera.backgroundColor = new Color(0.04f, 0.055f, 0.07f, 0);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.allowHDR = false; camera.allowMSAA = false; camera.targetTexture = rt;
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                var results = new List<object>();
                for (int i = 0; i < 8; i++)
                {
                    Vector3 position = i == 0 ? Vector3.zero : i == 1 ? new Vector3(12, 7) : new Vector3(-12, -7);
                    float angle = i < 2 ? 0f : i == 2 ? 180f : i == 3 ? 135f : -90f;
                    turret.transform.position = position;
                    turret.transform.localScale = i >= 6 ? new Vector3(-1.3f, 0.8f, 1f) : Vector3.one;
                    if (i >= 6) angle = 45f;
                    SerializedObject settings = new SerializedObject(outline);
                    settings.FindProperty("width").floatValue = i == 7 ? 6f : 2f; settings.ApplyModifiedPropertiesWithoutUndo();
                    rise.localPosition = i == 5 ? new Vector3(0, -0.6f) : Vector3.zero;
                    pivot.rotation = Quaternion.Euler(0, 0, angle);
                    camera.transform.position = position + new Vector3(0, 0.7f, -10);
                    outline.enabled = false; await Frames(); Texture2D before = Capture(camera, rt);
                    outline.enabled = true; await Frames(); Texture2D after = Capture(camera, rt);
                    try
                    {
                        Color32[] a = before.GetPixels32(), b = after.GetPixels32(); Color32 bg = a[0];
                        int expected = 0, missing = 0, interior = 0;
                        for (int y = 1; y < rt.height - 1; y++)
                            for (int x = 1; x < rt.width - 1; x++)
                            {
                                int p = y * rt.width + x;
                                if (!Close(a[p], bg)) { if (!Close(a[p], b[p])) interior++; continue; }
                                if (Close(a[p - 1], bg) && Close(a[p + 1], bg) && Close(a[p - rt.width], bg) && Close(a[p + rt.width], bg)) continue;
                                expected++;
                                if (Close(a[p], b[p])) missing++;
                            }
                        File.WriteAllBytes("Assets/CTJ/Tools~/Enemy02_Contour_" + i + ".png", after.EncodeToPNG());
                        results.Add(new { i, position = position.ToString(), angle, expected, missing, interior });
                        Check(expected > 100 && missing == 0 && interior == 0, "Incomplete contour: " + i + ", missing=" + missing + ", interior=" + interior);
                    }
                    finally { Object.Destroy(before); Object.Destroy(after); }
                }
                string[] shaderErrors = ShaderUtil.GetShaderMessages(Shader.Find("CTJ/Enemies/TemporaryOutline")).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
                Check(shaderErrors.Length == 0, "Shader compile error");
                return new { passed = true, results, shaderErrors };
            }
            finally { root.SetActive(false); Object.Destroy(root); rt.Release(); Object.Destroy(rt); }
        }
        private static Texture2D Capture(Camera camera, RenderTexture rt)
        {
            camera.Render(); RenderTexture previous = RenderTexture.active;
            try { RenderTexture.active = rt; var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply(); return t; }
            finally { RenderTexture.active = previous; }
        }
        private static bool Close(Color32 a, Color32 b) => Math.Abs(a.r - b.r) <= 2 && Math.Abs(a.g - b.g) <= 2 && Math.Abs(a.b - b.b) <= 2;
        private static async Task Frames() { int end = Time.frameCount + 2; while (Time.frameCount < end) await Task.Yield(); }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
