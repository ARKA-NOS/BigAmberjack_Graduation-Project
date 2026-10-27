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
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class BuildEnemy02
    {
        public const string PrefabPath = "Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab";
        private const string StatsPath = "Assets/CTJ/Prototypes/Stats/Enemy02_ProjectileRobot_Stats.asset";
        private const string SkillPath = "Assets/CTJ/Prototypes/Skills/Enemy02_ProjectileRobot_Shot.asset";
        private const string WarningPath = "Assets/CTJ/Prototypes/Prefabs/Enemy02_AimLine.prefab";

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit only");
            foreach (string path in new[] { PrefabPath, StatsPath, SkillPath, WarningPath })
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Will not overwrite: " + path);
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab");
            try
            {
                root.name = "Enemy02_ProjectileRobot";
                root.transform.localScale = Vector3.one;
                EnemyBase enemy = root.GetComponent<EnemyBase>();
                EnemyProjectileSkill attack = root.GetComponentInChildren<EnemyProjectileSkill>();
                StatModule statModule = root.GetComponentInChildren<StatModule>();
                StatGroup stats = Object.Instantiate((StatGroup)new SerializedObject(statModule).FindProperty("baseStats").objectReferenceValue);
                stats.name = "Enemy02_ProjectileRobot_Stats";
                Object hpStat = new SerializedObject(root.GetComponentInChildren<HealthModule>()).FindProperty("maxHpStatData").objectReferenceValue;
                Object delayStat = new SerializedObject(attack).FindProperty("attackDelayStat").objectReferenceValue;
                SerializedObject statSO = new SerializedObject(stats);
                SerializedProperty entries = statSO.FindProperty("<Stats>k__BackingField");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    Object key = entry.FindPropertyRelative("<StatData>k__BackingField").objectReferenceValue;
                    if (key == hpStat) entry.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 30f;
                    if (key == delayStat) entry.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 1.8f;
                }
                statSO.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(stats, StatsPath);
                Set(statModule, "baseStats", stats);
                SkillSO skillKey = Object.Instantiate(attack.SkillSo);
                skillKey.name = "Enemy02_ProjectileRobot_Shot";
                AssetDatabase.CreateAsset(skillKey, SkillPath);

                Set(enemy, "moveSpeed", 0f); Set(enemy, "detectionRange", 10f); Set(enemy, "chaseReleaseRange", 14f);
                Set(enemy, "facingRoot", null); Set(enemy, "deathAnimator", null);
                Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; rb.constraints = RigidbodyConstraints2D.FreezeAll;
                Set(root.GetComponentInChildren<EnemyMovementModule>(), "jumpHeight", 0f);

                // 원본 MadGhost가 아니라 로드한 복제본의 비주얼만 교체합니다.
                Object.DestroyImmediate(root.transform.Find("Visual").gameObject);
                GameObject visual = Child(root, "Visual");
                visual.AddComponent<SortingGroup>().sortingOrder = 10;
                Sprite square = AssetDatabase.LoadAllAssetsAtPath("Assets/CTJ/Prototypes/Sprites/TestWhite.png").OfType<Sprite>().First();
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
                GameObject maskObject = Child(visual, "AboveGroundMask");
                maskObject.transform.localPosition = new Vector3(0f, 5f);
                maskObject.transform.localScale = new Vector3(10f / square.bounds.size.x, 10f / square.bounds.size.y, 1f);
                SpriteMask mask = maskObject.AddComponent<SpriteMask>(); mask.sprite = square;

                GameObject rise = Child(visual, "RiseRoot");
                GameObject body = Child(rise, "Body");
                Rect(body, "Housing", square, material, new Vector2(0f, 0.6f), new Vector2(0.9f, 1.2f), new Color(0.16f, 0.21f, 0.27f), 0);
                Rect(body, "Panel", square, material, new Vector2(0f, 0.66f), new Vector2(0.72f, 0.94f), new Color(0.35f, 0.48f, 0.55f), 1);
                Rect(body, "Eye", square, material, new Vector2(0f, 1.02f), new Vector2(0.4f, 0.10f), new Color(1f, 0.55f, 0.15f), 2);
                GameObject pivot = Child(rise, "LauncherPivot"); pivot.transform.localPosition = new Vector3(0f, 0.9f);
                pivot.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                GameObject launcher = Child(pivot, "Launcher");
                Rect(launcher, "Barrel", square, material, new Vector2(0.32f, 0f), new Vector2(0.72f, 0.22f), new Color(0.1f, 0.14f, 0.18f), 3);
                Rect(launcher, "BarrelStripe", square, material, new Vector2(0.44f, 0f), new Vector2(0.13f, 0.24f), new Color(0.8f, 0.48f, 0.12f), 4);
                Rect(launcher, "Joint", square, material, Vector2.zero, new Vector2(0.28f, 0.3f), new Color(0.22f, 0.31f, 0.37f), 5);
                GameObject muzzle = Child(pivot, "FirePoint"); muzzle.transform.localPosition = new Vector3(0.78f, 0f);
                Rect(visual, "GroundRim", square, material, new Vector2(0f, 0.025f), new Vector2(1.08f, 0.05f), new Color(0.09f, 0.12f, 0.16f), 6);

                BoxCollider2D hurtbox = root.GetComponent<BoxCollider2D>();
                hurtbox.isTrigger = true; hurtbox.size = new Vector2(0.9f, 1.2f); hurtbox.offset = new Vector2(0f, 0.6f);
                EnemyTurretModule turret = Child(root, "TurretModule").AddComponent<EnemyTurretModule>();
                Set(turret, "riseRoot", rise.transform); Set(turret, "launcherPivot", pivot.transform);
                Set(turret, "launcherVisual", launcher); Set(turret, "hurtbox", hurtbox);
                // 편집 중에도 기본 대기 외형이 보이도록 저장합니다. 기준 위치는 컴포넌트의 (0,0,0)입니다.
                rise.transform.localPosition = Vector3.down * 0.6f;
                launcher.SetActive(true);

                EnemyHitReactionModule reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
                Set(reaction, "enableKnockback", false); Set(reaction, "hitStunDuration", 0.2f);
                SerializedObject reactionSO = new SerializedObject(reaction);
                SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
                SerializedProperty sprites = reactionSO.FindProperty("sprites"); sprites.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                reactionSO.ApplyModifiedPropertiesWithoutUndo();

                GameObject warning = PrefabUtility.LoadPrefabContents("Assets/CTJ/Prototypes/Prefabs/AttackTelegraphCanvas.prefab");
                try
                {
                    warning.name = "Enemy02_AimLine";
                    Set(warning.GetComponent<AttackTelegraphUI>(), "areaColor", new Color32(255, 60, 60, 120));
                    warning.GetComponent<Canvas>().sortingOrder = 9;
                    PrefabUtility.SaveAsPrefabAsset(warning, WarningPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(warning); }

                Set(attack, "<SkillSo>k__BackingField", skillKey); Set(attack, "<BaseCoolTime>k__BackingField", 6f);
                Set(attack, "damage", 15f); Set(attack, "fallbackAttackDelay", 1.8f); Set(attack, "postBlinkDelay", 0.2f);
                Set(attack, "blinkBeforeAttack", false); Set(attack, "telegraphOpacity", 1f);
                Set(attack, "attackAnimator", null); Set(attack, "firePoint", muzzle.transform); Set(attack, "turret", turret);
                Set(attack, "showAimLine", true); Set(attack, "trackTargetDuringPreparation", true); Set(attack, "aimAtTargetCenter", true);
                Set(attack, "aimLineWidth", 0.045f);
                Set(attack, "telegraphPrefab", AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(WarningPath));
                EnemySkillModule skills = root.GetComponentInChildren<EnemySkillModule>();
                SerializedObject selection = new SerializedObject(skills);
                SerializedProperty entry0 = selection.FindProperty("skills").GetArrayElementAtIndex(0);
                entry0.FindPropertyRelative("maxRange").floatValue = 10f;
                entry0.FindPropertyRelative("maxHeightDifference").floatValue = -1f;
                entry0.FindPropertyRelative("requireGrounded").boolValue = false;
                entry0.FindPropertyRelative("origin").objectReferenceValue = muzzle.transform;
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

        private static void Rect(GameObject parent, string name, Sprite square, Material material, Vector2 position, Vector2 size, Color color, int order)
        {
            GameObject child = Child(parent, name);
            child.transform.localPosition = position;
            child.transform.localScale = new Vector3(size.x / square.bounds.size.x, size.y / square.bounds.size.y, 1f);
            SpriteRenderer sprite = child.AddComponent<SpriteRenderer>();
            sprite.sprite = square; sprite.sharedMaterial = material; sprite.color = color; sprite.sortingOrder = order;
            sprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        private static void Set(Object obj, string field, object value)
        {
            SerializedObject so = new SerializedObject(obj); SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new InvalidOperationException("Missing field: " + field);
            if (value == null) p.objectReferenceValue = null;
            else if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else if (value is Color c) p.colorValue = c;
            else if (value is Color32 c32) p.colorValue = c32;
            else if (value is Object reference) p.objectReferenceValue = reference;
            else throw new InvalidOperationException("Unsupported field: " + field);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
