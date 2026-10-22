using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Effects
{
    // 플레이어 스탯을 영구적으로(플레이어 오브젝트가 살아있는 동안) 바꾸는 효과.
    // 일정 시간만 유지되는 버프는 TimedEffectRewardSO를 쓴다.
    [CreateAssetMenu(fileName = "Effect_Stat", menuName = "Interact/Effect/Stat Reward", order = 3)]
    public class StatRewardEffectSO : ChoiceEffectSO
    {
        [Tooltip("바꿀 스탯")]
        [SerializeField] private StatData stat;
        [Tooltip("적용할 변화량. 음수 값을 넣으면 스탯이 감소한다.")]
        [SerializeField] private StatModifyData modify;

        public override bool CanApply(ModuleOwner owner)
        {
            return stat != null && owner.TryGetModule(out IStatModule _);
        }

        public override void Apply(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IStatModule statModule))
                return;

            // 적용할 때마다 새 키를 써서 같은 효과를 여러 번 받으면 중첩되게 한다. (SO를 키로 쓰면 덮어써진다)
            statModule.GetStat(stat).SetModify(new object(), modify);
            Debug.Log($"[Reward] 스탯 변화: {stat.StatName} {modify.ModifyType} {modify.Value}");
        }
    }
}
