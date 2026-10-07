using System;
using UnityEngine;

namespace Lrw.Script.Currency
{
    [Serializable]
    public struct Cost
    {
        [field:SerializeField] public CurrencySO Type { get; private set; }
        [field:SerializeField] public int Amount { get; private set; }
    }
}