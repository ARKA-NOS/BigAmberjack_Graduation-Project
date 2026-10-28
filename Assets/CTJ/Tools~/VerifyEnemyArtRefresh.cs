using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyArtRefresh
    {
        private const string Prefabs = "Assets/CTJ/Prototypes/Prefabs/";
        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            string[] names = { "Enemy01_TwistedArm", "Enemy02_ProjectileRobot", "Enemy04_MechanicArcher" };
            foreach (string name in names)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                Check(prefab != null, "Missing prefab: " + name);
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + name + "/" + child.name);
                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                "Missing reference: " + name + "/" + component.GetType().Name + "/" + property.propertyPath);
                }
                Animator animator = prefab.GetComponentInChildren<Animator>();
                if (name == "Enemy02_ProjectileRobot")
                {
                    Transform rim = prefab.transform.Find("Visual/GroundRim");
                    Check(rim != null && rim.GetComponent<SpriteRenderer>().maskInteraction == SpriteMaskInteraction.None,
                        "Ground rim must not clip its bottom outline");
                    continue;
                }
                bool zombie = name == "Enemy01_TwistedArm";
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    Check(bindings.Length == 1, "Sprite animation missing: " + clip.name);
                    foreach (ObjectReferenceKeyframe frame in AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]))
                    {
                        Check(frame.value is Sprite, "Null sprite key: " + clip.name);
                        string path = AssetDatabase.GetAssetPath(frame.value);
                        Check(zombie ? path == "Assets/CTJ/Assets/Zombie.png" : path.StartsWith("Assets/CTJ/Assets/ArcherHero/Final/"),
                            "Unexpected artwork source: " + clip.name);
                    }
                    if (!zombie) Check(AnimationUtility.GetAnimationEvents(clip).Length == 0, "Archer recovery must be controlled by skill");
                }
            }
            var sceneInstances = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => names.Any(n => e.name.StartsWith(n)))
                .Select(e => new { e.name, controller = e.GetComponentInChildren<Animator>()?.runtimeAnimatorController?.name,
                    warningEnabled = e.GetComponentInChildren<EnemyAttackWarningModule>()?.enabled,
                    outerOnly = new SerializedObject(e.GetComponentInChildren<EnemyOutlineModule>()).FindProperty("outerSilhouetteOnly").boolValue }).ToArray();
            Check(!ShaderUtil.GetShaderMessages(Shader.Find("CTJ/Enemies/TemporaryOutline")).Any(m => m.severity.ToString() == "Error"), "Shader compile error");
            return new { passed = true, scope = "missing scripts / references / sprite keys / animation events / shader", sceneInstances };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            return await Task.WhenAll(Combat(true), Combat(false));
        }

        private static async Task<object> Combat(bool zombie)
        {
            float x = zombie ? 12000f : 12100f;
            GameObject root = new GameObject("Art combat verification");
            try
            {
                GameObject floor = new GameObject("Ground"); floor.layer = 6; floor.transform.SetParent(root.transform);
                floor.transform.position = new Vector3(x, -0.5f); floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
                EnemyBase enemy = Spawn(root, zombie ? "Enemy01_TwistedArm" : "Enemy04_MechanicArcher", new Vector3(x, 0f));
                EnemyDamageProbe target = Target(root, new Vector3(x + (zombie ? 1f : 8f), 0f)); enemy.SetTarget(target.transform);
                EnemyAttackSkill skill = enemy.GetComponentInChildren<EnemyAttackSkill>();
                Animator animator = enemy.GetComponentInChildren<Animator>(); SpriteRenderer sprite = animator.GetComponent<SpriteRenderer>();
                Check(animator.runtimeAnimatorController.name == (zombie ? "Enemy01_Zombie" : "Enemy04_ArcherHero"), "Dedicated controller");
                await Until(() => skill.IsPreparing, "Prepare"); await Frames(1);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.AttackPreparation"), "Preparation animation");
                float until = Time.time + 0.4f;
                while (Time.time < until)
                {
                    Check(sprite.color == Color.white, "Preparation must preserve original artwork color");
                    if (zombie) Check(!enemy.GetModule<EnemyAttackWarningModule>().IsShowing, "No red warning");
                    await Task.Yield();
                }
                await Until(() => skill.IsRecovering, "Hit / release");
                if (zombie)
                {
                    Check(target.ReceivedHitCount == 1 && target.TotalDamage == 8f, "Zombie animation event hits exactly once");
                    enemy.OnAttackHit(); Check(target.ReceivedHitCount == 1, "No duplicate event damage");
                }
                else
                {
                    await Frames(1);
                    Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack"), "Release animation");
                    float release = Time.time; await Wait(0.3f);
                    Check(skill.IsUsing, "Visual must not end 0.6s recovery early");
                    await Until(() => !skill.IsUsing, "Recovery ends");
                    Check(Time.time - release >= 0.55f, "Recovery timing changed");
                    await Until(() => target.ReceivedHitCount == 1, "Archer arrow damage");
                    Check(target.TotalDamage == 14f, "Archer damage");
                }
                await Until(() => skill.IsPreparing, "Next preparation");
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.right, Vector2.left);
                await Frames(1);
                Check(!skill.IsUsing && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"), "Cancel returns to idle");
                Check(enemy.GetModule<EnemyHitReactionModule>().IsBlinking, "Hit feedback remains enabled");
                enemy.ApplyDamage(new DamageData { DamageAmount = 9999f }, enemy.transform.position, Vector2.right, Vector2.left);
                await Frames(1);
                Check(enemy.IsDead && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "Death animation");
                return new { passed = true, enemy = zombie ? "Zombie" : "ArcherHero", scope = "prepare / damage / recovery / cancel / hit feedback / death" };
            }
            finally { Cleanup(root); }
        }

        public static async Task<object> RenderProof()
        {
            RequirePlay();
            GameObject root = new GameObject("Art render verification");
            Camera camera = CameraFor(root, new Vector3(0f, 0.7f, -10f), 1.1f);
            RenderTexture rt = new RenderTexture(720, 480, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
            var results = new List<object>();
            try
            {
                EnemyBase turret = Spawn(root, "Enemy02_ProjectileRobot", Vector3.zero);
                turret.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                turret.GetComponentInChildren<EnemyTurretModule>().enabled = false;
                EnemyHitReactionModule reaction = turret.GetComponentInChildren<EnemyHitReactionModule>();
                Set(reaction, "enableBlink", false); Set(reaction, "enableScalePulse", false); Set(reaction, "enableKnockback", false);
                EnemyOutlineModule outline = turret.GetComponentInChildren<EnemyOutlineModule>();
                Transform rise = turret.transform.Find("Visual/RiseRoot");
                Transform pivot = rise.Find("LauncherPivot");
                foreach (float angle in new[] { 0f, 45f, -90f })
                {
                    rise.localPosition = Vector3.zero; pivot.rotation = Quaternion.Euler(0, 0, angle);
                    outline.enabled = false; await Frames(2); Texture2D baseline = Capture(camera, rt);
                    outline.enabled = true; await Frames(2); Texture2D shown = Capture(camera, rt);
                    try
                    {
                        Color32[] before = baseline.GetPixels32(), after = shown.GetPixels32(); Color32 background = before[0];
                        int interiorChanges = 0, outsideChanges = 0;
                        for (int i = 0; i < before.Length; i++)
                        {
                            if (Close(before[i], after[i])) continue;
                            if (Close(before[i], background)) outsideChanges++; else interiorChanges++;
                        }
                        Check(outsideChanges > 100, "Outer outline missing: " + angle);
                        Check(interiorChanges == 0, "Internal part borders changed: " + angle + " / " + interiorChanges);
                        results.Add(new { angle, interiorChanges, outsideChanges });
                        if (angle == 0f)
                        {
                            File.WriteAllBytes("Assets/CTJ/Tools~/Enemy02_OuterOutline.png", shown.EncodeToPNG());
                            turret.ApplyDamage(new DamageData { DamageAmount = 1f }, turret.transform.position, Vector2.right, Vector2.left);
                            await Frames(2); Texture2D white = Capture(camera, rt);
                            try { File.WriteAllBytes("Assets/CTJ/Tools~/Enemy02_OuterOutline_Hit.png", white.EncodeToPNG()); }
                            finally { Object.Destroy(white); }
                            await Wait(0.3f);
                        }
                    }
                    finally { Object.Destroy(baseline); Object.Destroy(shown); }
                }
                rise.localPosition = new Vector3(0, -0.6f, 0); pivot.rotation = Quaternion.Euler(0, 0, -90);
                await Frames(2); Texture2D buried = Capture(camera, rt);
                try { File.WriteAllBytes("Assets/CTJ/Tools~/Enemy02_OuterOutline_Buried.png", buried.EncodeToPNG()); }
                finally { Object.Destroy(buried); }

                turret.gameObject.SetActive(false);
                EnemyBase zombie = Spawn(root, "Enemy01_TwistedArm", new Vector3(-2.5f, 0f));
                EnemyBase archer = Spawn(root, "Enemy04_MechanicArcher", new Vector3(2.5f, 0f));
                foreach (EnemyBase enemy in new[] { zombie, archer })
                {
                    enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    if (enemy.GetComponentInChildren<EnemyPatrolModule>() is EnemyPatrolModule patrol) patrol.enabled = false;
                    enemy.GetComponentInChildren<Animator>().speed = 0f;
                }
                turret.gameObject.SetActive(true); rise.localPosition = Vector3.zero; pivot.rotation = Quaternion.identity;
                camera.transform.position = new Vector3(0, 0.9f, -10); camera.orthographicSize = 2.2f;
                rt.Release(); rt.width = 1200; rt.height = 500; rt.Create();
                await Frames(2); Texture2D preview = Capture(camera, rt);
                try { File.WriteAllBytes("Assets/CTJ/Tools~/EnemyArtRefresh_Preview.png", preview.EncodeToPNG()); }
                finally { Object.Destroy(preview); }
                foreach (EnemyBase enemy in new[] { zombie, archer })
                {
                    Animator animator = enemy.GetComponentInChildren<Animator>();
                    animator.Play("Base Layer.AttackPreparation", 0, 0.75f); animator.Update(0f);
                }
                await Frames(2); preview = Capture(camera, rt);
                try { File.WriteAllBytes("Assets/CTJ/Tools~/EnemyArtRefresh_Preparation.png", preview.EncodeToPNG()); }
                finally { Object.Destroy(preview); }
                Shader shader = Shader.Find("CTJ/Enemies/TemporaryOutline");
                Check(!ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"), "Shader compile error");
                return new { passed = true, pixelComparisons = results, preview = "Assets/CTJ/Tools~/EnemyArtRefresh_Preview.png" };
            }
            finally { Cleanup(root); rt.Release(); Object.Destroy(rt); }
        }

        private static EnemyBase Spawn(GameObject root, string name, Vector3 position)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Prefabs + name + ".prefab"), position, Quaternion.identity, root.transform);
            enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            return enemy;
        }
        private static EnemyDamageProbe Target(GameObject root, Vector3 position)
        {
            GameObject target = new GameObject("Target"); target.transform.SetParent(root.transform); target.transform.position = position;
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
            collider.size = new Vector2(0.5f, 0.8f); collider.offset = new Vector2(0, 0.4f);
            return target.AddComponent<EnemyDamageProbe>();
        }
        private static Camera CameraFor(GameObject root, Vector3 position, float size)
        {
            GameObject go = new GameObject("Verification camera"); go.transform.SetParent(root.transform); go.transform.position = position;
            Camera camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.04f, 0.055f, 0.07f, 0f);
            camera.allowHDR = false; camera.allowMSAA = false;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            return camera;
        }
        private static Texture2D Capture(Camera camera, RenderTexture rt)
        {
            camera.Render(); RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                Texture2D pixels = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); pixels.Apply(); return pixels;
            }
            finally { RenderTexture.active = previous; }
        }
        private static bool Close(Color32 a, Color32 b) => Math.Abs(a.r - b.r) <= 2 && Math.Abs(a.g - b.g) <= 2 && Math.Abs(a.b - b.b) <= 2;
        private static void Set(Object target, string field, bool value)
        {
            SerializedObject so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Cleanup(GameObject root)
        {
            root.SetActive(false); Object.Destroy(root);
            foreach (EnemyProjectile p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
        }
        private static async Task Frames(int count) { int end = Time.frameCount + count; await Until(() => Time.frameCount >= end, "Frames"); }
        private static async Task Wait(float seconds) { float end = Time.time + seconds; await Until(() => Time.time >= end, "Wait"); }
        private static async Task Until(Func<bool> predicate, string label)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(12);
            while (!predicate()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Timeout: " + label); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play only");
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
