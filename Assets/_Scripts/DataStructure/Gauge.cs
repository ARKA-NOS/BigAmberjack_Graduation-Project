using System;
using UnityEngine;

namespace _Scripts.DataStructure
{
    [Serializable]
    public struct Gauge
    {
        [field: SerializeField] public float MaxValue { get; private set; }
        [field: SerializeField] public float CurrentValue { get; private set; }

        public float Normalized => !Mathf.Approximately(MaxValue,0f) ? Mathf.Clamp01(CurrentValue / MaxValue) : 1f;
        public bool IsMax => Mathf.Approximately(CurrentValue, MaxValue);
        
        public Gauge(float currentValue, float maxValue)
        {
            CurrentValue = currentValue;
            MaxValue = maxValue;
        }
        
        public void SetValue(float currentValue, float maxValue)
        {
            CurrentValue = currentValue;
            MaxValue = maxValue;
        }
        
        public override string ToString()
            => $"{CurrentValue} / {MaxValue}";
        
        public string PercentText()
            => $"{Normalized}%";
        
    }
}