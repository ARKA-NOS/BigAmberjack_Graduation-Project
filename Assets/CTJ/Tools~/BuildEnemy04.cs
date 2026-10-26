using System;
using System.Linq;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.UI;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.SkillSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // 최초 생성용. 다른 적/씬/공용 SO를 저장하지 않습니다.
    public static class BuildEnemy04
    {
        private const string PrefabPath = "Assets/CTJ/Prototypes/Prefabs/Enemy04_MechanicArcher.prefab";
        private const string StatsPath = "Assets/CTJ/Prototypes/Stats/Enemy04_MechanicArcher_Stats.asset";
        private const string SkillPath = "Assets/CTJ/Prototypes/Skills/Enemy04_MechanicArcher_Shot.asset";
        private const string ArrowPath = "Assets/CTJ/Prototypes/Prefabs/Enemy04_Arrow.prefab";
        private const string AimPath = "Assets/CTJ/Prototypes/Prefabs/Enemy04_AimLine.prefab";

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only");
            foreach (string path in new[] { PrefabPath, StatsPath, SkillPath, ArrowPath, AimPath })
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Will not overwrite: " + path);

            GameObject arrow = PrefabUtility.LoadPrefabContents("Assets/CTJ/Prototypes/Prefabs/MadGhostProjectile.prefab");
            try
            {
                arrow.name = "Enemy04_Arrow";
                arrow.transform.localScale = Vector3.one;
                arrow.GetComponent<CircleCollider2D>().radius = 0.125f;
                arrow.GetComponent<EnemyProjectile>().Speed = 15f;
                arrow.GetComponent<EnemyProjectile>().Lifetime = 0.8f;
                Set(arrow.GetComponent<EnemyProjectile>(), "blockingLayers", 1 << 6);
                Object.DestroyImmediate(arrow.GetComponent<SpriteRenderer>());
                Sprite square = AssetDatabase.LoadAllAssetsAtPath("Assets/CTJ/Prototypes/Sprites/TestWhite.png").OfType<Sprite>().First();
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
                // 콜라이더는 화살촉 위치. 그래픽만 뒤로 길게 뺍니다.
                Rect(arrow, "Shaft", square, material, new Vector2(-0.25f, 0f), new Vector2(0.65f, 0.075f), new Color(1f, 0.8f, 0.3f));
                GameObject tip = Rect(arrow, "Tip", square, material, Vector2.zero, new Vector2(0.16f, 0.16f), Color.white);
                tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                PrefabUtility.SaveAsPrefabAsset(arrow, ArrowPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(arrow); }

            GameObject aim = PrefabUtility.LoadPrefabContents("Assets/CTJ/Prototypes/Prefabs/Enemy02_AimLine.prefab");
            try
            {
                aim.name = "Enemy04_AimLine";
                Set(aim.GetComponent<AttackTelegraphUI>(), "areaColor", new Color32(255, 60, 60, 100));
                PrefabUtility.SaveAsPrefabAsset(aim, AimPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(aim); }

            GameObject root = PrefabUtility.LoadPrefabContents("Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab");
            try
            {
                root.name = "Enemy04_MechanicArcher";
                EnemyBase enemy = root.GetComponent<EnemyBase>();
                EnemyProjectileSkill attack = root.GetComponentInChildren<EnemyProjectileSkill>();
                StatModule statModule = root.GetComponentInChildren<StatModule>();
                StatGroup stats = Object.Instantiate((StatGroup)new SerializedObject(statModule).FindProperty("baseStats").objectReferenceValue);
                stats.name = "Enemy04_MechanicArcher_Stats";
                Object hp = new SerializedObject(root.GetComponentInChildren<HealthModule>()).FindProperty("maxHpStatData").objectReferenceValue;
                Object delay = new SerializedObject(attack).FindProperty("attackDelayStat").objectReferenceValue;
                Object gold = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>()).FindProperty("rewardAmountStat").objectReferenceValue;
                SerializedObject statSO = new SerializedObject(stats);
                SerializedProperty entries = statSO.FindProperty("<Stats>k__BackingField");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    Object key = entry.FindPropertyRelative("<StatData>k__BackingField").objectReferenceValue;
                    if (key == hp) entry.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 55f;
                    if (key == delay) entry.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 0.65f;
                    if (key == gold) entry.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 10f;
                }
                statSO.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(stats, StatsPath);
                Set(statModule, "baseStats", stats);
                SkillSO skillKey = Object.Instantiate(attack.SkillSo);
                skillKey.name = "Enemy04_MechanicArcher_Shot";
                AssetDatabase.CreateAsset(skillKey, SkillPath);

                // 원본 스프라이트의 임시 외형은 재사용하고 몸체 높이만 1.7m로 맞춥니다.
                root.transform.localScale = Vector3.one * (1.7f / root.GetComponent<BoxCollider2D>().size.y);
                Set(enemy, "moveSpeed", 3.2f); Set(enemy, "detectionRange", 12f); Set(enemy, "chaseReleaseRange", 14f);
                Set(root.GetComponentInChildren<EnemyMovementModule>(), "jumpHeight", 0f);
                Child(root, "KeepDistanceModule").AddComponent<EnemyKeepDistanceModule>();
                foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>()) sprite.color = new Color(0.65f, 0.85f, 1f);

                EnemyHitReactionModule reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
                Set(reaction, "hitStunDuration", 0.22f); // 기획 0.2초 × 경직 배율 1.1
                Set(reaction, "knockbackSpeed", 6.4f * 1.1f);
                Set(reaction, "knockbackDuration", 0.18f);
                Set(reaction, "cancelAttackOnHit", true);

                Set(attack, "<SkillSo>k__BackingField", skillKey); Set(attack, "<BaseCoolTime>k__BackingField", 2.8f);
                Set(attack, "damage", 14f); Set(attack, "fallbackAttackDelay", 0.65f); Set(attack, "postBlinkDelay", 0.15f);
                Set(attack, "recoveryTime", 0.6f); Set(attack, "blinkBeforeAttack", false);
                Set(attack, "attackAnimator", null); // 전용 공격 클립 전까지 Animation Event로 후딜을 조기 종료하지 않습니다.
                Set(attack, "projectilePrefab", AssetDatabase.LoadAssetAtPath<EnemyProjectile>(ArrowPath));
                Set(attack, "projectileSpeed", 15f); Set(attack, "projectileLifetime", 0.8f);
                Set(attack, "showAimLine", true); Set(attack, "trackTargetDuringPreparation", true);
                Set(attack, "faceTargetDuringPreparation", true); Set(attack, "aimAtTargetCenter", true);
                Set(attack, "aimLineWidth", 0.045f); Set(attack, "telegraphOpacity", 1f);
                Set(attack, "telegraphPrefab", AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(AimPath));
                SerializedObject selection = new SerializedObject(root.GetComponentInChildren<EnemySkillModule>());
                SerializedProperty entry0 = selection.FindProperty("skills").GetArrayElementAtIndex(0);
                entry0.FindPropertyRelative("minRange").floatValue = 5f;
                entry0.FindPropertyRelative("maxRange").floatValue = 11f;
                entry0.FindPropertyRelative("maxHeightDifference").floatValue = -1f;
                entry0.FindPropertyRelative("requireGrounded").boolValue = true;
                entry0.FindPropertyRelative("requireClearPath").boolValue = true;
                entry0.FindPropertyRelative("origin").objectReferenceValue = new SerializedObject(attack).FindProperty("firePoint").objectReferenceValue;
                selection.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssetIfDirty(stats); AssetDatabase.SaveAssetIfDirty(skillKey);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return PrefabPath;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new GameObject(name); child.layer = parent.layer;
            child.transform.SetParent(parent.transform, false); return child;
        }

        private static GameObject Rect(GameObject parent, string name, Sprite square, Material material, Vector2 position, Vector2 size, Color color)
        {
            GameObject child = Child(parent, name); child.transform.localPosition = position;
            child.transform.localScale = new Vector3(size.x / square.bounds.size.x, size.y / square.bounds.size.y, 1f);
            SpriteRenderer sprite = child.AddComponent<SpriteRenderer>();
            sprite.sprite = square; sprite.sharedMaterial = material; sprite.color = color; sprite.sortingOrder = 12;
            return child;
        }

        private static void Set(Object obj, string field, object value)
        {
            SerializedObject so = new SerializedObject(obj); SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new InvalidOperationException("Missing field: " + field);
            if (value == null) p.objectReferenceValue = null;
            else if (value is float f) p.floatValue = f;
            else if (value is int n) p.intValue = n;
            else if (value is bool b) p.boolValue = b;
            else if (value is Color32 color) p.colorValue = color;
            else if (value is Object reference) p.objectReferenceValue = reference;
            else throw new InvalidOperationException("Unsupported field: " + field);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
