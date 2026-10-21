using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Currency;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // Tools~: Unity 게임 빌드에는 포함되지 않습니다. 공용 에셋/씬/저장 파일을 수정하지 않습니다.
    public static class VerifyEnemyCurrency
    {
        private const string Cat = "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab";
        private const string Ghost = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";
        private const string RewardStat = "Assets/CTJ/Prototypes/Stats/EnemyGoldReward.asset";
        private const string Gold = "Assets/Lrw/GameModule/Currency/Gold.asset";
        private const string Manager = "Assets/Lrw/GameModule/Currency/CurrencyManager.prefab";
        private static readonly MethodInfo Grant = typeof(EnemyCurrencyRewardModule).GetMethod(
            "GrantDeathReward", BindingFlags.Instance | BindingFlags.NonPublic);

        public static object RefreshAndValidate()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Refresh in Edit Mode only");
            foreach (string path in new[] { RewardStat, "Assets/CTJ/Prototypes/Stats/Catto_Stats.asset",
                "Assets/CTJ/Prototypes/Stats/MadGhost_Stats.asset", Cat, Ghost })
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return ValidatePrefabs();
        }

        public static object ValidatePrefabs()
        {
            return new[] { Cat, Ghost }.Select(path =>
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                Check(enemy != null, "Missing prefab");
                Check(enemy.GetComponents<Component>().Length == 4, "Root components: " + string.Join(", ", enemy.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "Missing")));
                string[] names = { "Visual", "EnemyMovement", "TerrainBody", "StatModule", "HealthModule",
                    "SkillModule", "HitReactionModule", "CurrencyRewardModule", "DeathModule" };
                Check(enemy.transform.Cast<Transform>().Select(t => t.name).SequenceEqual(names), "Hierarchy/order differs");
                IModule[] modules = enemy.GetComponentsInChildren<IModule>();
                Check(modules.Length == 7 && modules.Select(m => m.GetType()).Distinct().Count() == 7, "Missing/duplicate modules");
                Check(Array.FindIndex(modules, m => m is StatModule) < Array.FindIndex(modules, m => m is HealthModule), "Stat must precede Health");
                EnemySkillModule skills = enemy.GetComponentInChildren<EnemySkillModule>();
                EnemyAttackSkill attack = skills.GetComponentInChildren<EnemyAttackSkill>();
                Check(attack != null && attack.transform.parent == skills.transform && skills.Skills[0].Skill == attack, "Skill child reference");
                EnemyCurrencyRewardModule reward = enemy.GetComponentInChildren<EnemyCurrencyRewardModule>();
                SerializedObject so = new SerializedObject(reward);
                StatData stat = AssetDatabase.LoadAssetAtPath<StatData>(RewardStat);
                Check(so.FindProperty("rewardCurrency").objectReferenceValue == AssetDatabase.LoadAssetAtPath<CurrencySO>(Gold), "Gold reference");
                Check(so.FindProperty("rewardAmountStat").objectReferenceValue == stat, "Reward stat reference");
                StatGroup group = (StatGroup)new SerializedObject(enemy.GetComponentInChildren<StatModule>()).FindProperty("baseStats").objectReferenceValue;
                float amount = group.Stats.Single(s => s.StatData == stat).BaseValue;
                Check(amount == (path == Cat ? 10f : 15f), "Reward amount");
                foreach (Component component in enemy.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing component");
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                "Broken reference: " + component.name + "." + property.propertyPath);
                }
                return new { prefab = enemy.name, children = names, reward = amount, modules = modules.Length, referencesValid = true };
            }).ToArray();
        }

        public static object ValidateOpenScene()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Read scene in Edit Mode only");
            Scene scene = SceneManager.GetActiveScene();
            Check(scene.path == "Assets/CTJ/Scenes/CTJ_MapTestScene.unity", "Expected user's CTJ scene");
            EnemyBase[] enemies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyBase>(true)).ToArray();
            foreach (EnemyBase enemy in enemies)
            {
                Check(enemy.GetComponentsInChildren<EnemyCurrencyRewardModule>(true).Length == 1, "Scene reward missing/duplicated");
                Check(enemy.GetComponentsInChildren<EnemySkillModule>(true).Length == 1, "Scene skills duplicated");
                Check(enemy.GetComponentsInChildren<EnemyMovementModule>(true).Length == 1, "Scene movement duplicated");
            }
            CurrencyManager manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CurrencyManager>(true)).Single();
            Check(manager.GetComponentInParent<Lrw.Script._Core._Manager.GameManager>() != null, "Currency initialization parent missing");
            SerializedProperty currencies = new SerializedObject(manager).FindProperty("currencies");
            CurrencySO gold = AssetDatabase.LoadAssetAtPath<CurrencySO>(Gold);
            Check(Enumerable.Range(0, currencies.arraySize).Any(i => currencies.GetArrayElementAtIndex(i).objectReferenceValue == gold), "Scene Gold is unregistered");
            Check(!scene.isDirty, "Do not leave scene edits");
            return new { scene = scene.path, enemies = enemies.Select(e => e.name).ToArray(),
                gameManagerPresent = true, goldRegistered = true, isDirty = scene.isDirty };
        }

        public static async Task<object> RunScenarios()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path ==
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Use isolated CTJ Play scene only");
            // GameManager를 생성하지 않아 GameEnd/Save를 호출하지 않습니다. 회귀 테스트에도 이 런타임 관리자를 사용합니다.
            CurrencyManager manager = Object.FindFirstObjectByType<CurrencyManager>();
            if (manager == null)
            {
                manager = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CurrencyManager>(Manager));
                manager.name = "CTJ Currency Verification (runtime only)";
                manager.Initialize();
            }
            CurrencySO gold = AssetDatabase.LoadAssetAtPath<CurrencySO>(Gold);
            StatData amountStat = AssetDatabase.LoadAssetAtPath<StatData>(RewardStat);
            CurrencyData data = manager.GetCurrency(gold);
            Check(ReferenceEquals(data, CurrencySystem.GetCurrencyData(gold)), "Actual manager not registered");
            int initial = data.Count;
            int passed = 0;
            List<GameObject> instances = new List<GameObject>();
            Action<bool, string> test = (condition, message) => { Check(condition, message); passed++; };
            try
            {
                foreach (string path in new[] { Cat, Ghost })
                {
                    EnemyBase enemy = Spawn(path, instances);
                    IHealthModule hp = enemy.GetModule<IHealthModule>();
                    test(hp.MaxHealth == (path == Cat ? 40f : 30f), "Existing HP changed after hierarchy split");
                    int before = data.Count;
                    Damage(enemy, 2f);
                    test(data.Count == before && !enemy.IsDead, "Nonlethal hit paid reward");
                    EnemyCurrencyRewardModule reward = enemy.GetModule<EnemyCurrencyRewardModule>();
                    CurrencyData.CurrencyChanged callback = (current, prev) => Grant.Invoke(reward, null);
                    data.OnCurrencyChanged += callback;
                    try { Damage(enemy, 999f); }
                    finally { data.OnCurrencyChanged -= callback; }
                    int expected = before + (path == Cat ? 10 : 15);
                    test(enemy.IsDead && data.Count == expected, "Death/reentrant callback must grant once");
                    Damage(enemy, 999f);
                    hp.CurrentHealth = 0f;
                    Grant.Invoke(reward, null);
                    enemy.OnAttackHit();
                    test(data.Count == expected && !enemy.IsAttackInProgress, "Duplicate death/late attack");
                    test(!enemy.GetComponent<Rigidbody2D>().simulated && enemy.GetComponentsInChildren<Collider2D>().All(c => !c.enabled), "Death physics cleanup");
                }

                EnemyBase direct = Spawn(Cat, instances);
                direct.GetModule<IStatModule>().GetStat(amountStat).SetBaseValue(23.9f);
                int balance = data.Count;
                direct.GetModule<IHealthModule>().CurrentHealth = 0f;
                test(data.Count == balance + 23, "Read runtime stat and discard fraction");

                EnemyBase living = Spawn(Cat, instances);
                balance = data.Count;
                living.gameObject.SetActive(false);
                Object.Destroy(living.gameObject);
                await Task.Yield(); await Task.Yield();
                test(data.Count == balance, "Disable/despawn of living enemy paid reward");

                foreach (float value in new[] { 0f, -10f, 0.9f })
                {
                    EnemyBase zero = Spawn(Cat, instances);
                    zero.GetModule<IStatModule>().GetStat(amountStat).SetBaseValue(value);
                    balance = data.Count;
                    Damage(zero, 999f);
                    test(data.Count == balance, "Zero/negative/sub-unit reward changed currency");
                }
                EnemyBase disabled = Spawn(Cat, instances);
                disabled.GetModule<EnemyCurrencyRewardModule>().enabled = false;
                balance = data.Count;
                Damage(disabled, 999f);
                test(disabled.IsDead && data.Count == balance, "Reward opt-out failed");

                EnemyBase noManager = Spawn(Cat, instances);
                ServiceLocator.Register<ICurrencyManager>(new NullCurrencyManager());
                balance = data.Count;
                try { Damage(noManager, 999f); }
                finally { ServiceLocator.Register<ICurrencyManager>(manager); }
                test(noManager.IsDead && data.Count == balance, "Missing manager should skip reward safely");

                EnemyBase noCurrency = Spawn(Cat, instances);
                CurrencySO unregistered = ScriptableObject.CreateInstance<CurrencySO>();
                try
                {
                    SerializedObject so = new SerializedObject(noCurrency.GetModule<EnemyCurrencyRewardModule>());
                    so.FindProperty("rewardCurrency").objectReferenceValue = unregistered;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    balance = data.Count;
                    Damage(noCurrency, 999f);
                    test(noCurrency.IsDead && data.Count == balance, "Unregistered currency should skip safely");
                }
                finally { Object.Destroy(unregistered); }

                EnemyBase overflow = Spawn(Cat, instances);
                data.Add(int.MaxValue - data.Count);
                Damage(overflow, 999f);
                test(data.Count == int.MaxValue && overflow.IsDead, "Currency overflow wrapped balance");
                data.Add(initial - data.Count);
                await Task.Delay(1500);
                test(direct == null, "Death did not despawn after split");
                return new { passed, manager = "actual CurrencyManager prefab + Initialize", catGold = 10,
                    ghostGold = 15, fractionalGold = 23, expectedWarnings = 3, savedToDisk = false };
            }
            finally
            {
                ServiceLocator.Register<ICurrencyManager>(manager);
                data.Add(initial - data.Count);
                foreach (GameObject go in instances) if (go != null) Object.Destroy(go);
            }
        }

        private static EnemyBase Spawn(string path, List<GameObject> instances)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(path),
                new Vector3(7000f + instances.Count * 4f, 0f, 0f), Quaternion.identity);
            instances.Add(enemy.gameObject);
            enemy.GetComponent<Rigidbody2D>().gravityScale = 0f;
            return enemy;
        }

        private static void Damage(EnemyBase enemy, float amount) =>
            ((IDamageable)enemy).ApplyDamage(new DamageData { DamageAmount = amount }, Vector2.zero, Vector2.right, Vector2.zero);

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
