using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script.Agent.Players.Skills;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem.Titles
{
    // MeleeHitStackTitleSo가 런타임에 붙이는 버프. 맞힐 때마다 중첩 +1, 지속시간 갱신.
    public class MeleeHitStackBuff : MonoBehaviour
    {
        private ModuleOwner _owner;
        private Stat _stat;
        private float _valuePerStack;
        private ModifyMathType _modifyType;
        private float _duration;
        private int _maxStack;

        private int _stack;
        private float _expireTime;
        private bool _isInitialized;

        public void Init(ModuleOwner owner, Stat stat, float valuePerStack, ModifyMathType modifyType, float duration, int maxStack)
        {
            _owner = owner;
            _stat = stat;
            _valuePerStack = valuePerStack;
            _modifyType = modifyType;
            _duration = duration;
            _maxStack = maxStack;
            _isInitialized = true;

            EventBus<MeleeAttackHitEvent>.Subscribe(_owner, OnMeleeAttackHit);
        }

        private void OnMeleeAttackHit(MeleeAttackHitEvent evt)
        {
            _stack = Mathf.Min(_stack + 1, _maxStack);
            _expireTime = Time.time + _duration;
            _stat.SetModify(this, new StatModifyData(0, _valuePerStack * _stack, _modifyType));
        }

        private void Update()
        {
            if (_stack <= 0 || Time.time < _expireTime) return;
            ClearStack();
        }

        private void ClearStack()
        {
            _stack = 0;
            _stat.RemoveModify(this);
        }

        private void OnDestroy()
        {
            if (!_isInitialized) return;
            _isInitialized = false;
            EventBus<MeleeAttackHitEvent>.UnSubscribe(_owner, OnMeleeAttackHit);
            ClearStack();
        }
    }
}
