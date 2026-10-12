using Unity.Properties;
using UnityEngine;

namespace Lrw.Script.Currency
{
    [CreateAssetMenu(fileName = "Currency View Module", menuName = "Currency/Currency View Module", order = 0)]
    public class CurrencyViewModule : ScriptableObject
    {
        [field:SerializeField] public CurrencySO Currency { get; private set; }

        [CreateProperty]
        public string CurrencyName
            => Currency?.CurrencyName ?? "";
        
        [CreateProperty]
        public Sprite CurrencyIcon
            => Currency?.CurrencyIcon;
        
        [CreateProperty] public int GetCurrentCount
            => CurrencySystem.GetCurrencyData(Currency)?.Count ?? 0;
    }
}