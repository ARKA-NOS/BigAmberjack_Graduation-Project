using System;
using System.Collections.Generic;
using System.Linq;
using CTJ.Enemies;
using UnityEditor;
using UnityEngine;

namespace CTJ.Tools
{
    // 기존 프리팹을 재생성하지 않고 시각 전용 Transform과 피격 설정만 갱신합니다.
    public static class ConfigureEnemyHitScale
    {
        private static readonly string[] Names =
        { "Catto_Melee", "MadGhost_Ranged", "Enemy01_TwistedArm", "Enemy02_ProjectileRobot" };

        public static object Apply()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode only");
            var reports = new List<object>();
            foreach (string name in Names)
            {
                string path = "Assets/CTJ/Prototypes/Prefabs/" + name + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    EnemyBase enemy = root.GetComponent<EnemyBase>();
                    EnemyHitReactionModule reaction = root.GetComponentInChildren<EnemyHitReactionModule>(true);
                    // 특히 사용자 수정이 있는 터렛의 다른 모듈/스킬 수치를 보존합니다.
                    var unchanged = root.GetComponentsInChildren<Component>(true)
                        .Where(c => c != null && !(c is Transform) && c != enemy && c != reaction)
                        .ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
                    Transform[] targets;
                    if (name == "Enemy02_ProjectileRobot")
                    {
                        targets = new[] { root.transform.Find("Visual/RiseRoot/Body"),
                            root.transform.Find("Visual/RiseRoot/LauncherPivot/Launcher") };
                    }
                    else
                    {
                        targets = new[] { IsolateVisual(root, enemy) };
                    }
                    Check(targets.All(t => t != null), "Missing graphics: " + name);
                    SerializedObject so = new SerializedObject(reaction);
                    if (so.FindProperty("enableKnockback").boolValue)
                        so.FindProperty("knockbackSpeed").floatValue = 6.4f;
                    so.FindProperty("enableScalePulse").boolValue = true;
                    so.FindProperty("scalePulseAmount").floatValue = 0.08f;
                    so.FindProperty("scalePulseDuration").floatValue = 0.16f;
                    SerializedProperty list = so.FindProperty("scaleTargets"); list.arraySize = targets.Length;
                    for (int i = 0; i < targets.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    foreach (var pair in unchanged)
                        Check(EditorJsonUtility.ToJson(pair.Key) == pair.Value, "Unexpected component change: " + pair.Key);
                    ValidateRoot(root, name);
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    Check(success, "Save failed: " + path);
                    reports.Add(new { prefab = name, targets = targets.Select(t => t.name).ToArray(), otherComponentsPreserved = true });
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return reports;
        }

        private static Transform IsolateVisual(GameObject root, EnemyBase enemy)
        {
            Transform old = root.transform.Find("Visual"); Check(old != null, "Visual missing");
            if (old.Find("HitVisual") != null) return old.Find("HitVisual");
            Animator animator = old.GetComponent<Animator>();
            Check(old.GetComponent<SpriteRenderer>() != null && animator != null, "Unexpected visual structure");
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                // 기존 클립은 자신의 SpriteRenderer만 제어합니다. 계층 의존 클립은 자동 변경하지 않습니다.
                Check(AnimationUtility.GetCurveBindings(clip).All(b => b.path == "" && b.type != typeof(Transform)) &&
                    AnimationUtility.GetObjectReferenceCurveBindings(clip).All(b => b.path == "" && b.type != typeof(Transform)),
                    "Animation depends on hierarchy/scale: " + clip.name);
            }
            Transform visual = new GameObject("Visual").transform;
            visual.gameObject.layer = old.gameObject.layer;
            visual.SetParent(root.transform, false); visual.SetSiblingIndex(old.GetSiblingIndex());
            visual.localPosition = old.localPosition; visual.localRotation = old.localRotation; visual.localScale = old.localScale;
            // Hitbox/FirePoint는 새 방향 전환 루트로 옮겨 피격 스케일의 영향을 받지 않게 합니다.
            while (old.childCount > 0) old.GetChild(0).SetParent(visual, false);
            old.name = "HitVisual"; old.SetParent(visual, false); old.SetSiblingIndex(0);
            old.localPosition = Vector3.zero; old.localRotation = Quaternion.identity; old.localScale = Vector3.one;
            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("facingRoot").objectReferenceValue = visual; so.ApplyModifiedPropertiesWithoutUndo();
            return old;
        }

        public static object Validate()
        {
            foreach (string name in Names)
                ValidateRoot(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/Prefabs/" + name + ".prefab"), name);
            var boss = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab");
            var bossSettings = new SerializedObject(boss.GetComponentInChildren<EnemyHitReactionModule>(true));
            Check(!bossSettings.FindProperty("enableScalePulse").boolValue && !bossSettings.FindProperty("enableKnockback").boolValue,
                "Boss feedback opt-outs changed");
            return new { passed = true, checkedPrefabs = 5, bossOptOutPreserved = true };
        }

        private static void ValidateRoot(GameObject root, string name)
        {
            Check(root != null, "Missing prefab: " + name);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script");
                foreach (Component component in child.GetComponents<Component>())
                {
                    SerializedObject so = new SerializedObject(component); SerializedProperty p = so.GetIterator();
                    while (p.NextVisible(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0,
                                "Broken reference: " + component + " / " + p.propertyPath);
                }
            }
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>(true);
            var settings = new SerializedObject(reaction); var targets = settings.FindProperty("scaleTargets");
            Check(settings.FindProperty("enableScalePulse").boolValue && targets.arraySize > 0, "Scale pulse not wired");
            for (int i = 0; i < targets.arraySize; i++)
            {
                Transform target = targets.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                Check(target != null && target != root.transform && target.IsChildOf(root.transform), "Invalid target");
                Check(target.GetComponentInChildren<Collider2D>(true) == null && target.GetComponentInChildren<Rigidbody2D>(true) == null,
                    "Physics in scale target");
                Check(!target.GetComponentsInChildren<Transform>(true).Any(t => t.name == "FirePoint"), "Muzzle in scale target");
            }
            Check(settings.FindProperty("enableKnockback").boolValue == (name != "Enemy02_ProjectileRobot"), "Knockback option changed");
            if (name != "Enemy02_ProjectileRobot")
            {
                Check(Mathf.Approximately(settings.FindProperty("knockbackSpeed").floatValue, 6.4f), "Speed");
                Check(new SerializedObject(root.GetComponent<EnemyBase>()).FindProperty("facingRoot").objectReferenceValue == root.transform.Find("Visual"), "Facing root");
            }
        }

        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
