using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Currency
{
    public struct NullCurrencyManager : ICurrencyManager
    {
        public CurrencyData GetCurrency(CurrencySO currencySo) => null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<ICurrencyManager>(new NullCurrencyManager());
        }
    }
}