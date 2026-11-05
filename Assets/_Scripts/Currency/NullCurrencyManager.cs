using _Scripts._Core._ServiceLocator;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Currency
{
    public struct NullCurrencyManager : ICurrencyManager
    {
        public CurrencyData GetCurrency(CurrencySO currencySo) => null;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<ICurrencyManager>(new NullCurrencyManager());
        }
    }
}