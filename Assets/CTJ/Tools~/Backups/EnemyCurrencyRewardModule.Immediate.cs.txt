using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Currency;
using UnityEngine;

namespace CTJ.Enemies
{
    // 잔액/저장은 공용 CurrencyManager가 담당하고, 이 모듈은 사망 보상만 전달합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyCurrencyRewardModule : Module, IAfterInitModule
    {
        [Tooltip("CurrencyManager에 등록된 재화 에셋. 현재 프로토타입은 공용 Gold를 사용합니다.")]
        [SerializeField] private CurrencySO rewardCurrency;
        [Tooltip("소유 적의 StatModule에서 읽을 보상 수량 스탯. 소수 부분은 버립니다.")]
        [SerializeField] private StatData rewardAmountStat;

        private EnemyBase _enemy;
        private Stat _rewardAmount;
        private bool _rewardHandled;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
        }

        public void AfterInit()
        {
            // 모든 모듈의 Initialize 이후 읽어 StatGroup의 개체별 기본값을 사용합니다.
            if (rewardAmountStat != null && _owner.TryGetModule(out IStatModule stats))
                _rewardAmount = stats.GetStat(rewardAmountStat);
        }

        internal void GrantDeathReward()
        {
            if (_rewardHandled || !isActiveAndEnabled || _enemy == null || !_enemy.IsDead)
                return;

            // Add가 변경 이벤트를 호출하므로, 외부 콜백에 재진입하기 전에 중복 지급을 차단합니다.
            _rewardHandled = true;
            if (rewardCurrency == null || _rewardAmount == null)
            {
                Debug.LogWarning($"{_enemy.name}: 사망 보상의 Currency / StatModule / 수량 Stat을 확인하세요.", this);
                return;
            }

            float value = _rewardAmount.Value;
            if (float.IsNaN(value) || float.IsInfinity(value) || (double)value > int.MaxValue)
            {
                Debug.LogWarning($"{_enemy.name}: 유효하지 않은 사망 보상 수량입니다: {value}", this);
                return;
            }
            if (value < 1f) return;
            int amount = Mathf.FloorToInt(value);

            // 생성 시점이 아닌 사망 시점에 조회해 매니저 초기화 순서에 대한 의존을 줄입니다.
            CurrencyData currency = CurrencySystem.GetCurrencyData(rewardCurrency);
            if (currency == null)
            {
                Debug.LogWarning($"{_enemy.name}: CurrencyManager 초기화와 {rewardCurrency.name} 등록을 확인하세요. 사망 보상은 지급되지 않았습니다.", this);
                return;
            }
            if ((long)currency.Count + amount > int.MaxValue)
            {
                Debug.LogWarning($"{_enemy.name}: 재화 보유량 한도를 초과하여 사망 보상을 지급하지 않았습니다.", this);
                return;
            }

            currency.Add(amount);
        }
    }
}
