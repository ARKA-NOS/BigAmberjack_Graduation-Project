using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Currency;
using UnityEngine;

namespace CTJ.Enemies
{
    // 보상 총액/분배는 이 모듈, 낙하/소멸/개별 지급은 EnemyRewardCoin이 담당합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyCurrencyRewardModule : Module, IAfterInitModule
    {
        [Tooltip("CurrencyManager에 등록된 재화 에셋. 현재 프로토타입은 공용 Gold를 사용합니다.")]
        [SerializeField] private CurrencySO rewardCurrency;
        [Tooltip("소유 적의 StatModule에서 읽을 보상 수량 스탯. 소수 부분은 버립니다.")]
        [SerializeField] private StatData rewardAmountStat;

        [Header("Coin Drop")]
        [SerializeField] private EnemyRewardCoin coinPrefab;
        [Tooltip("코인 하나가 나타내는 기준 금액. 3이면 1~3골드: 1개, 4~6골드: 2개입니다.")]
        [SerializeField, Min(1)] private int currencyPerCoin = 3;
        [SerializeField, Min(1)] private int maxCoinCount = 12;
        [Tooltip("적 루트의 월드 위치에 더할 생성 오프셋입니다. 상하 반전 모듈이 켜지면 Y 방향도 반전합니다.")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.35f);

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

            // 사망 이벤트가 여러 번 호출되어도 코인은 한 묶음만 생성합니다.
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

            if (coinPrefab == null)
            {
                Debug.LogWarning($"{_enemy.name}: 사망 보상의 Coin Prefab을 연결하세요.", this);
                return;
            }

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

            int count = Mathf.Min(1 + (amount - 1) / Mathf.Max(1, currencyPerCoin), Mathf.Max(1, maxCoinCount));
            int baseAmount = amount / count;
            int remainder = amount % count;
            Vector2 offset = spawnOffset;
            EnemyVerticalFlipModule verticalFlip = _enemy.GetModule<EnemyVerticalFlipModule>();
            if (verticalFlip != null && verticalFlip.IsFlipped) offset.y = -offset.y;
            Vector3 position = _enemy.transform.position + (Vector3)offset;
            for (int i = 0; i < count; i++)
            {
                // 부모를 지정하지 않아 적의 사망 연출/제거와 코인의 수명이 독립적입니다.
                EnemyRewardCoin coin = Instantiate(coinPrefab, position, Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(coin.gameObject, _enemy.gameObject.scene);
                // 구간을 나누어 같은 방향에 모두 겹쳐 날아가는 것을 줄입니다.
                float spread = (i + Random.Range(0.1f, 0.9f)) / count;
                coin.Launch(currency, baseAmount + (i < remainder ? 1 : 0), spread);
            }
        }
    }
}
