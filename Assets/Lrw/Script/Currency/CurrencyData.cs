using System;
using UnityEngine;

namespace Lrw.Script.Currency
{
    [Serializable]
    public class CurrencyData
    {
        public readonly CurrencySO CurrencyType;
        public int Count { get; private set; }
        
        public delegate void CurrencyChanged(int current, int prev);

        public event CurrencyChanged OnCurrencyChanged;
        
        public CurrencyData(CurrencySO type,int count)
        {
            CurrencyType = type;
            Count = count;
        }

        public bool Has(int count)
            => Count >= count;

        public void Add(int count)
        {
            int prev = Count;
            Count += count;
            OnCurrencyChanged?.Invoke(Count, prev);
        }
        
    }
}