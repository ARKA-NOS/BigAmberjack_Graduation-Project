// 기존 CTJ 프로토타입에 피격/사망 에셋만 연결하는 Unity Pipeline 구성 도구입니다.
using System;
using System.Linq;
using Agents.Players;
using CTJ.Enemies;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CTJ.Tools
{
    public static class ConfigureEnemyDeath
    {
        private const string Root = "Assets/CTJ/Prototypes";

        public static object InspectPlayerSetup()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameModules/Player/Prefabs/Player.prefab");
            PlayerMeleeSkillModule attack = player.GetComponentInChildren<PlayerMeleeSkillModule>(true);
            return new
            {
                healthModules = player.GetComponentsInChildren<HealthModule>(true).Length,
                statModules = player.GetComponentsInChildren<StatModule>(true).Length,
                attackDamage = new SerializedObject(attack).FindProperty("attackDmg").floatValue,
                caster = attack.GetComponentInChildren<OverlapDamageCaster>(true).GetType().FullName
            };
        }

        public static string Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before configuring CTJ prefabs.");
            if (!AssetDatabase.IsValidFolder(Root + "/Stats"))
                AssetDatabase.CreateFolder(Root, "Stats");
            ConfigureEnemy("Catto", "Catto_Melee", 26, 6f);
            ConfigureEnemy("MadGhost", "MadGhost_Ranged", 22, 8f);
            return Validate();
        }

        private static void ConfigureEnemy(string art, string prefabName, int startFrame, float health)
        {
            AnimationClip clip = CreateDeathClip(art, startFrame);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/Animations/" + art + ".controller");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState death = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Death");
            if (death == null) death = machine.AddState("Death", new Vector3(500, 150));
            death.motion = clip;
            death.writeDefaultValues = false;
            // Death는 코드로 직접 진입하며 다른 상태로 돌아가지 않습니다.
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);

            StatGroup stats = CreateStats(art, health);
            string path = Root + "/Prefabs/" + prefabName + ".prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SerializedObject enemy = new SerializedObject(prefab.GetComponent<EnemyBase>());
                enemy.FindProperty("deathAnimator").objectReferenceValue = prefab.GetComponentInChildren<Animator>();
                enemy.FindProperty("deathDespawnDelay").floatValue = 1.1f;
                enemy.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject module = new SerializedObject(prefab.GetComponent<StatModule>());
                module.FindProperty("baseStats").objectReferenceValue = stats;
                module.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        private static AnimationClip CreateDeathClip(string art, int startFrame)
        {
            string path = Root + "/Animations/" + art + "_Death.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) return existing;
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(Root + "/Sprites/" + art + ".png")
                .OfType<Sprite>().OrderBy(s => s.name).ToArray();
            AnimationClip clip = new AnimationClip { name = art + "_Death", frameRate = 10f };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[10];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / 10f, value = sprites[startFrame + Math.Min(i, 8)] };
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.stopTime = 0.9f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static StatGroup CreateStats(string art, float health)
        {
            string path = Root + "/Stats/" + art + "_Stats.asset";
            StatGroup existing = AssetDatabase.LoadAssetAtPath<StatGroup>(path);
            if (existing != null) return existing;
            StatData maxHealth = AssetDatabase.LoadAssetAtPath<StatData>("Assets/Lrw/GameModule/Stats/MaxHealth.asset");
            if (maxHealth == null) throw new InvalidOperationException("Missing shared MaxHealth stat.");
            StatGroup group = ScriptableObject.CreateInstance<StatGroup>();
            group.name = art + "_Stats";
            SerializedObject data = new SerializedObject(group);
            SerializedProperty list = data.FindProperty("<Stats>k__BackingField");
            list.arraySize = 1;
            SerializedProperty item = list.GetArrayElementAtIndex(0);
            item.FindPropertyRelative("<StatData>k__BackingField").objectReferenceValue = maxHealth;
            item.FindPropertyRelative("<BaseValue>k__BackingField").floatValue = health;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(group, path);
            return group;
        }

        public static string Validate()
        {
            foreach (string name in new[] { "Catto_Melee", "MadGhost_Ranged" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab");
                if (prefab == null || prefab.GetComponent<EnemyBase>() == null || prefab.GetComponent<Collider2D>() == null)
                    throw new InvalidOperationException("Damage receiver/body collider missing: " + name);
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new InvalidOperationException("Missing script: " + child.name);
                StatModule module = prefab.GetComponent<StatModule>();
                StatGroup stats = (StatGroup)new SerializedObject(module).FindProperty("baseStats").objectReferenceValue;
                if (stats == null || stats.Stats.Length != 1 || stats.Stats[0].BaseValue <= 0f)
                    throw new InvalidOperationException("Missing maximum health configuration: " + name);
                SerializedObject enemy = new SerializedObject(prefab.GetComponent<EnemyBase>());
                Animator animator = (Animator)enemy.FindProperty("deathAnimator").objectReferenceValue;
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                AnimatorState state = controller.layers[0].stateMachine.states.Select(s => s.state).Single(s => s.name == "Death");
                AnimationClip clip = state.motion as AnimationClip;
                if (state.transitions.Length != 0 || clip == null || AnimationUtility.GetAnimationClipSettings(clip).loopTime ||
                    enemy.FindProperty("deathDespawnDelay").floatValue < clip.length)
                    throw new InvalidOperationException("Invalid death animation/cleanup time: " + name);
            }
            return "Validated: Catto HP 6, Mad Ghost HP 8; same-root damage receivers; 0.9s non-looping Death clips; cleanup at 1.1s.";
        }

        public static string RefreshAndValidate()
        {
            AssetDatabase.ImportAsset(Root + "/EnemyDamageDeathGuide.md");
            AssetDatabase.ImportAsset(Root + "/Animations/Catto.controller");
            AssetDatabase.ImportAsset(Root + "/Animations/MadGhost.controller");
            return Validate();
        }
    }
}
