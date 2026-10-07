using Lrw.Script._Core._ServiceLocator;

namespace Lrw.Script.Currency
{
    public static class CurrencySystem
    {
        public static CurrencyData GetCurrencyData(CurrencySO type)
            => ServiceLocator.Get<ICurrencyManager>().GetCurrency(type);
    }
}