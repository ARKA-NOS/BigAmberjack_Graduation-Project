using DevLib.ModuleSystem;
using Lrw.Script.Currency;
using UnityEngine;

namespace SDW.Scripts.Interact.Costs
{
    // 재화를 소모하는 비용.
    [CreateAssetMenu(fileName = "Cost_Currency", menuName = "Interact/Cost/Currency Cost", order = 1)]
    public class CurrencyCostSO : ChoiceCostSO
    {
        [Tooltip("소모할 재화 종류와 수량")]
        [SerializeField] private Cost cost;

        public override bool CanPay(ModuleOwner owner)
        {
            if (cost.Type == null)
                return false;

            CurrencyData currency = CurrencySystem.GetCurrencyData(cost.Type);
            return currency != null && currency.Has(cost.Amount);
        }

        public override void Pay(ModuleOwner owner)
        {
            CurrencySystem.GetCurrencyData(cost.Type)?.Add(-cost.Amount);
            Debug.Log($"[Cost] 재화 소모: {cost.Type.CurrencyName} -{cost.Amount}");
        }
    }
}
