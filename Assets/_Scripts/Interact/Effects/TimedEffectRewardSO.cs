using DevLib.ModuleSystem;
using Lrw.Script.Agent.EffectSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Effects
{
    // 일정 시간 동안 유지되는 버프/디버프를 거는 효과. 플레이어의 EffectModule에 효과를 넘기므로 지속시간 처리와 HUD 표시는 EffectModule이 맡는다.
    // 시간제 스탯 버프는 Create > Effect > Stat Effect로 StatEffectDataSo를 만들어 연결한다.
    [CreateAssetMenu(fileName = "Effect_Timed", menuName = "Interact/Effect/Timed Effect Reward", order = 4)]
    public class TimedEffectRewardSO : ChoiceEffectSO
    {
        [Tooltip("걸어줄 효과 (지속시간, 스탯, 변화량은 이 에셋에서 설정)")]
        [SerializeField] private AbstractEffectDataSo effect;

        public override bool CanApply(ModuleOwner owner)
        {
            return effect != null && owner.TryGetModule(out EffectModule _);
        }

        public override void Apply(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out EffectModule effectModule))
                return;

            effectModule.TakeEffect(effect);
            Debug.Log($"[Reward] 효과 적용: {effect.EffectName}");
        }
    }
}
