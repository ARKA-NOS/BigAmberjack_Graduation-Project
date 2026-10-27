using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyTemporaryStatusUI
    {
        private static readonly string[] Paths = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged",
            "Prefabs/Enemy01_TwistedArm", "Prefabs/Enemy02_ProjectileRobot", "Bosses/RobotEyeBoss" };

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            var tasks = Paths.Select((path, i) => Scenario(path, 200f + i * 50f)).ToList();
            tasks.Add(Removal());
            tasks.Add(TimingOverrides());
            return await Task.WhenAll(tasks);
        }

        private static async Task<object> TimingOverrides()
        {
            GameObject arena = new GameObject("Different reaction timer lengths");
            try
            {
                EnemyBase enemy = Spawn(arena, Paths[0], 600f);
                var reaction = enemy.GetModule<EnemyHitReactionModule>();
                var settings = new SerializedObject(reaction);
                settings.FindProperty("hitStunDuration").floatValue = 0.05f;
                settings.FindProperty("knockbackDuration").floatValue = 0.45f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                await Frames(2); Hit(enemy, 0.01f);
                Check(Mathf.Abs(reaction.RemainingReactionTime - 0.45f) < 0.002f, "Must include longer knockback time");
                await Wait(0.2f); await Frames(1);
                Image fill = Fill(enemy.GetComponentInChildren<EnemyTemporaryStatusUI>(), "reactionFill");
                Check(!reaction.IsStunned && reaction.IsKnockedBack && fill.fillAmount > 0f, "Gauge stopped before movement lock");
                Set(reaction, "enableKnockback", false); await Frames(2);
                Check(!enemy.IsHitReacting && fill.fillAmount == 0f, "Disabling knockback did not clear actual gauge");
                settings.Update(); settings.FindProperty("hitStunDuration").floatValue = 0f; settings.ApplyModifiedPropertiesWithoutUndo();
                Hit(enemy, 0.01f); await Frames(2);
                Check(!enemy.IsHitReacting && fill.fillAmount == 0f, "No-reaction configuration should stay empty");
                return new { scenario = "longer knockback / short stun / option disabled / no fake reaction", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Scenario(string path, float x)
        {
            GameObject arena = new GameObject("Temporary UI test " + path);
            try
            {
                EnemyBase enemy = Spawn(arena, path, x);
                var hp = enemy.GetModule<IHealthModule>(); var reaction = enemy.GetModule<EnemyHitReactionModule>();
                var ui = enemy.GetComponentInChildren<EnemyTemporaryStatusUI>();
                Image health = Fill(ui, "healthFill"), stagger = Fill(ui, "reactionFill");
                Canvas canvas = ui.GetComponent<Canvas>();
                float max = hp.MaxHealth; bool boss = path.StartsWith("Bosses/");
                await Frames(2);
                Check(max > 0f && canvas.enabled && health.fillAmount == 1f && stagger.fillAmount == 0f, "Initial HP / empty reaction");
                Hit(enemy, max * 0.2f);
                Check(Mathf.Abs(health.fillAmount - 0.8f) < 0.001f, "HP event did not update UI");
                if (boss)
                {
                    await Frames(2);
                    Check(!enemy.IsHitReacting && reaction.ReactionRemainingNormalized == 0f && stagger.fillAmount == 0f,
                        "Immune boss displayed fake stun");
                }
                else
                {
                    Check(enemy.IsHitReacting && Mathf.Abs(reaction.RemainingReactionTime - 0.3f) < 0.002f &&
                        reaction.ReactionRemainingNormalized > 0.99f, "Actual reaction duration");
                    await Wait(0.22f); await Frames(1);
                    Check(enemy.IsHitReacting && !reaction.IsKnockedBack && stagger.fillAmount > 0f && stagger.fillAmount < 0.4f,
                        "Added stun did not outlast knockback / drain gauge");
                    Check(Mathf.Abs(stagger.fillAmount - reaction.ReactionRemainingNormalized) < Time.deltaTime / 0.3f + 0.02f,
                        "UI timer diverged from reaction timer");
                    Hit(enemy, max * 0.05f);
                    Check(reaction.ReactionRemainingNormalized > 0.99f, "Repeated hit did not refresh gauge interval");
                    await Frames(2);
                    Check(stagger.fillAmount > 0.7f, "Repeated hit did not refill gauge");
                    await Wait(reaction.RemainingReactionTime + 0.04f); await Frames(1);
                    Check(!enemy.IsHitReacting && reaction.RemainingReactionTime == 0f && stagger.fillAmount == 0f,
                        "Gauge and AI lock must end together");
                }
                hp.CurrentHealth = max * 0.9f;
                Check(Mathf.Abs(health.fillAmount - 0.9f) < 0.001f, "HealthModule healing event");
                Vector2 offset = new SerializedObject(ui).FindProperty("worldOffset").vector2Value;
                enemy.transform.position += new Vector3(1f, 0.2f, 0f);
                Transform visual = enemy.transform.Find("Visual");
                if (visual != null) { Vector3 scale = visual.localScale; scale.x *= -1f; visual.localScale = scale; }
                await Frames(2);
                Check(Vector3.Distance(ui.transform.position, enemy.transform.position + (Vector3)offset) < 0.02f,
                    "UI did not follow root");
                Check(ui.transform.lossyScale.x > 0f && Quaternion.Angle(ui.transform.rotation, Quaternion.identity) < 0.1f,
                    "UI flipped with sprite");

                ui.enabled = false; Check(!canvas.enabled, "Disabled UI stayed visible");
                float previousFill = health.fillAmount;
                Hit(enemy, max * 0.1f);
                Check(health.fillAmount == previousFill, "Disabled UI did not unsubscribe");
                if (!boss) Check(enemy.IsHitReacting, "UI disabled combat reaction");
                ui.enabled = true;
                Check(canvas.enabled && Mathf.Abs(health.fillAmount - hp.CurrentHealth / hp.MaxHealth) < 0.001f,
                    "Reenabled UI did not refresh current HP");
                reaction.enabled = false; await Frames(2);
                Check(!enemy.IsHitReacting && stagger.fillAmount == 0f && canvas.enabled, "Reaction disable did not empty gauge");
                reaction.enabled = true;
                Hit(enemy, max * 0.01f);
                enemy.ApplyDamage(new DamageData { DamageAmount = max * 2f }, enemy.transform.position, Vector2.right, Vector2.zero);
                Check(enemy.IsDead && !canvas.enabled && reaction.ReactionRemainingNormalized == 0f, "Death UI cleanup");
                return new { prefab = path, passed = true, scope = "HP events / actual timer / repeat hit / root follow / opt-outs / unsubscribe / death" };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Removal()
        {
            GameObject arena = new GameObject("UI removal test");
            try
            {
                EnemyBase enemy = Spawn(arena, Paths[0], 500f);
                var hp = enemy.GetModule<IHealthModule>(); await Frames(2);
                Object.Destroy(enemy.GetComponentInChildren<EnemyTemporaryStatusUI>().gameObject); await Frames(2);
                float before = hp.CurrentHealth; Hit(enemy, 1f);
                Check(hp.CurrentHealth == before - 1f && enemy.IsHitReacting, "UI removal changed damage or stun");
                await Wait(0.35f);
                Check(!enemy.IsHitReacting && enemy.GetComponentInChildren<EnemyTemporaryStatusUI>() == null,
                    "UI removal interrupted stun expiry");
                return new { scenario = "delete entire temporary UI child; HP and stun still work", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        public static async Task<object> RenderPreview()
        {
            RequirePlay();
            GameObject arena = new GameObject("Status UI render verification");
            RenderTexture target = new RenderTexture(640, 400, 24, RenderTextureFormat.ARGB32);
            Texture2D pixels = null; float timeScale = Time.timeScale;
            try
            {
                EnemyBase enemy = Spawn(arena, Paths[0], 0f);
                var hit = enemy.GetModule<EnemyHitReactionModule>();
                Set(hit, "enableKnockback", false); Set(hit, "enableBlink", false); Set(hit, "enableScalePulse", false);
                foreach (Transform t in enemy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                enemy.GetComponentInChildren<Animator>().enabled = false;
                GameObject cameraObject = new GameObject("UI proof camera"); cameraObject.transform.SetParent(arena.transform);
                cameraObject.transform.position = new Vector3(0f, 1.1f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.5f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
                camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false; camera.targetTexture = target;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                await Frames(3);
                var hp = enemy.GetModule<IHealthModule>(); hp.CurrentHealth = hp.MaxHealth * 0.75f;
                Hit(enemy, hp.MaxHealth * 0.1f); await Wait(0.15f);
                Time.timeScale = 0f; await Frames(3);
                var ui = enemy.GetComponentInChildren<EnemyTemporaryStatusUI>();
                Check(Mathf.Abs(Fill(ui, "healthFill").fillAmount - 0.65f) < 0.001f &&
                    Fill(ui, "reactionFill").fillAmount > 0.25f && Fill(ui, "reactionFill").fillAmount < 0.65f, "Preview gauge values");
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target; pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                }
                finally { RenderTexture.active = previous; }
                Color32[] colors = pixels.GetPixels32();
                string path = Application.dataPath + "/CTJ/Tools~/EnemyStatusBarsPreview.png";
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                int[] green = Enumerable.Range(0, colors.Length).Where(i => colors[i].g > 150 && colors[i].r < 150 && colors[i].b < 150).ToArray();
                int[] white = Enumerable.Range(0, colors.Length).Where(i => colors[i].r > 240 && colors[i].g > 240 && colors[i].b > 240).ToArray();
                Check(green.Length > 100 && white.Length > 100, $"Bars not rendered: green={green.Length}, white={white.Length}, UI position={ui.transform.position}, scale={ui.transform.lossyScale}");
                int hpMin = green.Min(i => i / target.width), hpMax = green.Max(i => i / target.width);
                int stunMin = white.Min(i => i / target.width), stunMax = white.Max(i => i / target.width);
                float heightRatio = (float)(stunMax - stunMin + 1) / (hpMax - hpMin + 1);
                Check(hpMin > stunMax && heightRatio > 0.4f && heightRatio < 0.6f, "White bar is not half-height below HP");
                return new { passed = true, path, greenPixels = green.Length, whitePixels = white.Length, heightRatio,
                    hp = Fill(ui, "healthFill").fillAmount, reaction = Fill(ui, "reactionFill").fillAmount };
            }
            finally
            {
                Time.timeScale = timeScale; arena.SetActive(false); Object.Destroy(arena);
                target.Release(); Object.Destroy(target); if (pixels != null) Object.Destroy(pixels);
            }
        }

        private static EnemyBase Spawn(GameObject arena, string path, float x)
        {
            GameObject staging = new GameObject("Inactive setup"); staging.SetActive(false);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + path + ".prefab");
                EnemyBase enemy = Object.Instantiate(prefab, new Vector3(x, 0f), Quaternion.identity, staging.transform);
                var patrol = enemy.GetComponentInChildren<EnemyPatrolModule>(); if (patrol != null) patrol.enabled = false;
                enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                enemy.transform.SetParent(arena.transform, true); return enemy;
            }
            finally { Object.Destroy(staging); }
        }

        private static Image Fill(EnemyTemporaryStatusUI ui, string field) =>
            new SerializedObject(ui).FindProperty(field).objectReferenceValue as Image;
        private static void Hit(EnemyBase enemy, float amount) => enemy.ApplyDamage(new DamageData { DamageAmount = amount },
            enemy.transform.position, Vector2.right, Vector2.zero);
        private static void Set(Object target, string field, bool value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Frames(int count)
        {
            int end = Time.frameCount + count; await Until(() => Time.frameCount >= end);
        }
        private static async Task Wait(float seconds)
        {
            float end = Time.time + seconds; await Until(() => Time.time >= end);
        }
        private static async Task Until(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Test timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
            SceneManager.GetActiveScene().path == "", "Isolated blank scene only");
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
