using System.Globalization;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Item.Effects
{
    [CreateAssetMenu(fileName = "Stat Item Effect", menuName = "KHO/Item Effect/Stat", order = 0)]
    public class StatItemEffect : ItemEffectSO
    {
        [SerializeField] private StatData targetStat;
        [SerializeField] private int priority;
        [SerializeField] private float value;
        [SerializeField] private ModifyMathType modifyType;

        public override void Apply(ModuleOwner owner, object key)
        {
            if (!owner.TryGetModule(out IStatModule statModule)) return;

            statModule.GetStat(targetStat).SetModify(ModifyKey(key), new StatModifyData(priority, value, modifyType));
        }

        public override void Remove(ModuleOwner owner, object key)
        {
            if (!owner.TryGetModule(out IStatModule statModule)) return;

            statModule.GetStat(targetStat).RemoveModify(ModifyKey(key));
        }

        // 예: "Physics Power +15", "Attack Speed +7%", "Critical Chance +4%p"
        public override string GetDescription()
        {
            if (targetStat == null) return string.Empty;

            bool multiply = modifyType == ModifyMathType.Multiply;
            // Add인데 1 미만의 소수면 시트의 "%p" 값(예: 치명타 확률)으로 본다.
            bool percentPoint = !multiply && value != 0f && Mathf.Abs(value) < 1f;

            float shown = multiply || percentPoint ? value * 100f : value;
            string sign = shown >= 0f ? "+" : "-";
            string number = Mathf.Abs(shown).ToString("0.##", CultureInfo.InvariantCulture);
            string unit = multiply ? "%" : percentPoint ? "%p" : string.Empty;

            return $"{targetStat.StatName} {sign}{number}{unit}";
        }

        private object ModifyKey(object key) => (key, this);
    }
}
