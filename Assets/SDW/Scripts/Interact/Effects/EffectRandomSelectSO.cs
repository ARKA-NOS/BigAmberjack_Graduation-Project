using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Effects
{
    // 등록된 효과 중 하나만 무작위로 뽑아서 적용하는 효과.
    // 적용할 수 없는 효과(CanApply == false)는 후보에서 제외한다.
    [CreateAssetMenu(fileName = "EffectRandomSelectSO", menuName = "Random/EffectRandomSelectSO")]
    public class EffectRandomSelectSO : ChoiceEffectSO
    {
        [Tooltip("후보 효과들. 이 중 하나만 무작위로 적용된다.")]
        [SerializeField] private List<ChoiceEffectSO> effects = new();

        // 후보 버퍼. Apply가 호출될 때마다 재사용해서 GC 할당을 피한다.
        private readonly List<ChoiceEffectSO> _candidates = new();

        public override bool CanApply(ModuleOwner owner)
        {
            foreach (var effect in effects)
            {
                if (IsValid(effect) && effect.CanApply(owner))
                    return true;
            }

            return false;
        }

        public override void Apply(ModuleOwner owner)
        {
            _candidates.Clear();
            foreach (var effect in effects)
            {
                if (IsValid(effect) && effect.CanApply(owner))
                    _candidates.Add(effect);
            }

            if (_candidates.Count == 0)
                return;

            var picked = _candidates[Random.Range(0, _candidates.Count)];
            Debug.Log($"[RandomEffect] 선택된 효과: {picked.name}");
            picked.Apply(owner);
        }

        // null이거나 자기 자신을 넣어서 무한 재귀가 되는 경우를 막는다.
        private bool IsValid(ChoiceEffectSO effect)
        {
            return effect != null && effect != this;
        }
    }
}
