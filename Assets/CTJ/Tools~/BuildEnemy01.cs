using System;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using Lrw.Script.Agent.SkillSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // Pipeline run_script용. 기존 Catto/씬/공용 SO는 저장하지 않습니다.
    public static class BuildEnemy01
    {
        public const string PrefabPath = "Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab";
        private const string StatsPath = "Assets/CTJ/Prototypes/Stats/Enemy01_TwistedArm_Stats.asset";
        private const string SkillPath = "Assets/CTJ/Prototypes/Skills/Enemy01_TwistedArm_Melee.asset";

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only");
            foreach (string path in new[] { PrefabPath, StatsPath, SkillPath })
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Will not overwrite: " + path);

            StatGroup stats = Object.Instantiate(AssetDatabase.LoadAssetAtPath<StatGroup>(
                "Assets/CTJ/Prototypes/Stats/Catto_Stats.asset"));
            stats.name = "Enemy01_TwistedArm_Stats";
            SerializedObject statsSO = new SerializedObject(stats);
            SerializedProperty entries = statsSO.FindProperty("<Stats>k__BackingField");
            // Catto 순서: 최대 HP / 공격 준비 / Gold. 미정인 보상량은 기존 테스트값 10을 유지합니다.
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 50f;
            entries.GetArrayElementAtIndex(1).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 0.6f;
            statsSO.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(stats, StatsPath);

            SkillSO key = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SkillSO>(
                "Assets/CTJ/Prototypes/Skills/Catto_Melee.asset"));
            key.name = "Enemy01_TwistedArm_Melee";
            AssetDatabase.CreateAsset(key, SkillPath);

            GameObject root = PrefabUtility.LoadPrefabContents("Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab");
            try
            {
                root.name = "Enemy01_TwistedArm";
                EnemyBase enemy = root.GetComponent<EnemyBase>();
                Set(enemy, "moveSpeed", 6f);
                Set(root.GetComponentInChildren<EnemyMovementModule>(), "jumpHeight", 0f);
                Set(root.GetComponentInChildren<StatModule>(), "baseStats", stats);
                Set(root.GetComponentInChildren<EnemyHitReactionModule>(), "knockbackDuration", 0.2f);

                EnemyPatrolModule patrol = Child(root, "PatrolModule").AddComponent<EnemyPatrolModule>();
                Set(patrol, "halfWidth", 3f);
                Set(patrol, "patrolSpeed", 2f);
                Set(patrol, "pauseDuration", 0.6f);

                EnemyAttackWarningModule warning = Child(root, "AttackWarningModule").AddComponent<EnemyAttackWarningModule>();
                Set(warning, "warningMaterial", AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/CTJ/Prototypes/Sprites/EnemySolidHitFlash.mat"));
                SerializedObject warningSO = new SerializedObject(warning);
                SerializedProperty sprites = warningSO.FindProperty("sprites");
                sprites.arraySize = 1;
                sprites.GetArrayElementAtIndex(0).objectReferenceValue = root.GetComponentInChildren<SpriteRenderer>();
                warningSO.ApplyModifiedPropertiesWithoutUndo();

                EnemyMeleeSkill skill = root.GetComponentInChildren<EnemyMeleeSkill>();
                Set(skill, "<SkillSo>k__BackingField", key);
                Set(skill, "<BaseCoolTime>k__BackingField", 2.5f);
                Set(skill, "damage", 8f);
                Set(skill, "fallbackAttackDelay", 0.6f);
                Set(skill, "postBlinkDelay", 0.2f);
                Set(skill, "preparationWarning", warning);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssetIfDirty(stats);
            AssetDatabase.SaveAssetIfDirty(key);
            return PrefabPath;
        }

        private static GameObject Child(GameObject root, string name)
        {
            GameObject child = new GameObject(name);
            child.layer = root.layer;
            child.transform.SetParent(root.transform, false);
            return child;
        }

        private static void Set(Object obj, string name, object value)
        {
            SerializedObject serialized = new SerializedObject(obj);
            SerializedProperty p = serialized.FindProperty(name);
            if (p == null) throw new InvalidOperationException(obj.name + ": missing " + name);
            if (value is float f) p.floatValue = f;
            else if (value is Object reference) p.objectReferenceValue = reference;
            else throw new InvalidOperationException("Unsupported value: " + name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
