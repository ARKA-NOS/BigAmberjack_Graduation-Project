using _Scripts._Core._ServiceLocator;

namespace _Scripts.Currency
{
    public static class CurrencySystem
    {
        public static CurrencyData GetCurrencyData(CurrencySO type)
            => ServiceLocator.Get<ICurrencyManager>()?.GetCurrency(type);
        
        
    }
}