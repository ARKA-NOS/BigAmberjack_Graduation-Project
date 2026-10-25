using DevLib.ModuleSystem;
using Lrw.Script.Currency;
using UnityEngine;

namespace SDW.Scripts.Interact.Effects
{
    // 재화를 지급하는 효과. 상자의 Rewards나 NPC 선택지 Effects에 넣어서 사용한다.
    [CreateAssetMenu(fileName = "Effect_Currency", menuName = "Interact/Effect/Currency Reward", order = 1)]
    public class CurrencyRewardEffectSO : ChoiceEffectSO
    {
        [Tooltip("지급할 재화 종류")]
        [SerializeField] private CurrencySO currency;
        [Tooltip("지급할 수량")]
        [SerializeField, Min(1)] private int minAmount = 10;
        [SerializeField] private int maxAmount = 10;

        public override bool CanApply(ModuleOwner owner)
        {
            return currency != null && CurrencySystem.GetCurrencyData(currency) != null;
        }

        public override void Apply(ModuleOwner owner)
        {
            int amount = Random.Range(minAmount, maxAmount + 1);
            CurrencySystem.GetCurrencyData(currency)?.Add(amount);
            Debug.Log($"[Reward] 재화 획득: {currency.CurrencyName} +{amount}");
        }
    }
}
