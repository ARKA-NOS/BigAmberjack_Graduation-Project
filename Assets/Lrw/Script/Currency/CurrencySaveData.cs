using System;

namespace Lrw.Script.Currency
{
    [Serializable]
    public struct CurrencySaveData
    {
        public CurrencySO currencyType;
        public int currencyCount;
        
        public CurrencySaveData(CurrencyData currencyData)
        {
            currencyType = currencyData.CurrencyType;
            currencyCount = currencyData.Count;
        }
        
        public CurrencySaveData(CurrencySO type, int count)
        {
            currencyType =  type;
            currencyCount = count;
        }
        
        public CurrencyData ChangeCurrency() 
            => new(currencyType, currencyCount);
    }
}