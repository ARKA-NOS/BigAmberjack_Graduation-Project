using System.Collections;
using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.Skills;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem.Titles
{
    public class MultitaskBuff : MonoBehaviour
    {
        private ModuleOwner _owner;
        private IDamageable _damageable;
        private Stat _damageStat;
        private Stat _dashRangeStat;
        private float _attackBonus;
        private float _rangeBonus;
        private float _invincibleTime;
        private int _requiredHits;

        private bool _attackBonusActive;
        private int _attacksSinceDash;
        private int _hitCount;
        private bool _dashBoostReady;
        private bool _isInitialized;

        public void Init(ModuleOwner owner, Stat damageStat, Stat dashRangeStat,
            float attackBonus, float rangeBonus, float invincibleTime, int requiredHits)
        {
            _owner = owner;
            _damageable = owner as IDamageable;
            _damageStat = damageStat;
            _dashRangeStat = dashRangeStat;
            _attackBonus = attackBonus;
            _rangeBonus = rangeBonus;
            _invincibleTime = invincibleTime;
            _requiredHits = requiredHits;
            _isInitialized = true;

            EventBus<DashUsedEvent>.Subscribe(_owner, OnDash);
            EventBus<MeleeAttackUsedEvent>.Subscribe(_owner, OnAttackUsed);
            EventBus<MeleeAttackHitEvent>.Subscribe(_owner, OnAttackHit);
        }

        private void OnDash(DashUsedEvent evt)
        {
            if (_dashBoostReady)
            {
                _dashBoostReady = false;
                _dashRangeStat.RemoveModify(this);
                StartCoroutine(ExtendInvincibility());
            }

            _hitCount = 0;
            _attacksSinceDash = 0;
            _attackBonusActive = true;
            _damageStat.SetModify(this, new StatModifyData(0, _attackBonus, ModifyMathType.Multiply));
        }

        private void OnAttackUsed(MeleeAttackUsedEvent evt)
        {
            if (!_attackBonusActive) return;
            if (++_attacksSinceDash >= 2) ClearAttackBonus();
        }

        private void OnAttackHit(MeleeAttackHitEvent evt)
        {
            ClearAttackBonus();

            if (_dashBoostReady || ++_hitCount < _requiredHits) return;

            _dashBoostReady = true;
            _dashRangeStat.SetModify(this, new StatModifyData(0, _rangeBonus, ModifyMathType.Multiply));
        }

        private void ClearAttackBonus()
        {
            if (!_attackBonusActive) return;
            _attackBonusActive = false;
            _damageStat.RemoveModify(this);
        }

        private IEnumerator ExtendInvincibility()
        {
            _damageable?.SetInvincibility(this, true);
            yield return new WaitForSeconds(_invincibleTime);
            _damageable?.SetInvincibility(this, false);
        }

        private void OnDestroy()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            EventBus<DashUsedEvent>.UnSubscribe(_owner, OnDash);
            EventBus<MeleeAttackUsedEvent>.UnSubscribe(_owner, OnAttackUsed);
            EventBus<MeleeAttackHitEvent>.UnSubscribe(_owner, OnAttackHit);
            _damageStat.RemoveModify(this);
            _dashRangeStat.RemoveModify(this);
            _damageable?.SetInvincibility(this, false);
        }
    }
}