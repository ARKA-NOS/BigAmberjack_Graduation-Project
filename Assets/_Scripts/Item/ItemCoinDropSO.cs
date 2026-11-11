using _Scripts.Agent.Enemies;
using _Scripts.Currency;
using UnityEngine;

namespace _Scripts.Item
{
    // 아이템을 파괴했을 때 재화를 코인으로 흩뿌린다. 적 처치 보상과 같은 EnemyRewardCoin을 쓰며,
    // 코인이 튕기다 사라질 때 코인이 직접 재화를 지급하므로 여기서는 바로 Add하지 않는다.
    [CreateAssetMenu(fileName = "Item Coin Drop", menuName = "KHO/Item Coin Drop", order = 1)]
    public class ItemCoinDropSO : ScriptableObject
    {
        [SerializeField] private CurrencySO currency;
        [SerializeField] private EnemyRewardCoin coinPrefab;
        [SerializeField, Tooltip("코인 하나가 나타내는 기준 금액. 3이면 1~3골드: 1개, 4~6골드: 2개.")] [Min(1)]
        private int currencyPerCoin = 3;
        [SerializeField, Min(1)] private int maxCoinCount = 12;

        public void Drop(int amount, Vector3 origin)
        {
            if (amount <= 0) return;

            CurrencyData data = currency != null ? CurrencySystem.GetCurrencyData(currency) : null;
            if (data == null)
            {
                Debug.LogWarning($"[{name}] 재화를 찾지 못해 지급하지 않았습니다. (Currency 미지정 또는 CurrencyManager 없음)", this);
                return;
            }

            // 연출용 코인이 없으면 바로 지급한다.
            if (coinPrefab == null)
            {
                data.Add(amount);
                return;
            }

            int count = Mathf.Min(1 + (amount - 1) / currencyPerCoin, maxCoinCount);
            int baseAmount = amount / count;
            int remainder = amount % count;

            for (int i = 0; i < count; i++)
            {
                EnemyRewardCoin coin = Instantiate(coinPrefab, origin, Quaternion.identity);
                // 같은 방향으로 겹쳐 날아가지 않도록 구간을 나눠 흩뿌린다.
                float spread = (i + Random.Range(0.1f, 0.9f)) / count;
                coin.Launch(data, baseAmount + (i < remainder ? 1 : 0), spread);
            }
        }
    }
}
