using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace KHO.Scripts.ItemSystem.Effects
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

        // 한 아이템에 같은 스탯을 바꾸는 효과가 여러 개 있어도 덮어쓰지 않도록 효과 자신도 키에 포함한다.
        private object ModifyKey(object key) => (key, this);
    }
}
