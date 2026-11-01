using CTJ.Enemies;
using DevLib.ModuleSystem;
using Lrw.Script.Currency;
using UnityEngine;

namespace SDW.Scripts.Interact.Chest.Effects
{
    // 재화를 코인으로 흩뿌려 지급하는 효과. 적 처치 보상과 같은 EnemyRewardCoin을 쓰며,
    // 코인이 바운스 후 소멸할 때 직접 재화를 지급하므로 여기서는 즉시 Add하지 않는다.
    [CreateAssetMenu(fileName = "Effect_CoinDrop", menuName = "Chest/Effect/Coin Drop", order = 2)]
    public class CurrencyCoinDropEffectSO : ChoiceEffectSO, IOriginEffect
    {
        [Tooltip("지급할 재화 종류")]
        [SerializeField] private CurrencySO currency;
        [SerializeField, Min(1)] private int minAmount = 10;
        [SerializeField] private int maxAmount = 10;

        [Header("Coin Drop")]
        [SerializeField] private EnemyRewardCoin coinPrefab;
        [Tooltip("코인 하나가 나타내는 기준 금액. 3이면 1~3골드: 1개, 4~6골드: 2개입니다.")]
        [SerializeField, Min(1)] private int currencyPerCoin = 3;
        [SerializeField, Min(1)] private int maxCoinCount = 12;

        public override bool CanApply(ModuleOwner owner)
        {
            return currency != null && coinPrefab != null && CurrencySystem.GetCurrencyData(currency) != null;
        }

        // 위치 정보가 없으면 연출 없이 즉시 지급한다.
        public override void Apply(ModuleOwner owner)
        {
            CurrencySystem.GetCurrencyData(currency)?.Add(RollAmount());
        }

        public void Apply(ModuleOwner owner, Vector3 origin)
        {
            CurrencyData data = CurrencySystem.GetCurrencyData(currency);
            if (data == null || coinPrefab == null)
                return;

            int amount = RollAmount();
            int count = Mathf.Min(1 + (amount - 1) / Mathf.Max(1, currencyPerCoin), Mathf.Max(1, maxCoinCount));
            int baseAmount = amount / count;
            int remainder = amount % count;

            for (int i = 0; i < count; i++)
            {
                EnemyRewardCoin coin = Instantiate(coinPrefab, origin, Quaternion.identity);
                // 같은 방향으로 겹쳐 날아가지 않도록 구간을 나눠 분산한다.
                float spread = (i + Random.Range(0.1f, 0.9f)) / count;
                coin.Launch(data, baseAmount + (i < remainder ? 1 : 0), spread);
            }

            Debug.Log($"[Chest] 코인 드랍: {currency.CurrencyName} +{amount} ({count}개)");
        }

        private int RollAmount()
        {
            return Mathf.Max(1, Random.Range(minAmount, Mathf.Max(minAmount, maxAmount) + 1));
        }
    }
}
