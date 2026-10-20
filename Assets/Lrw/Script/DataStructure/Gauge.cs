using UnityEngine;

namespace Lrw.Script.DataStructure
{
    public struct Gauge
    {
        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        public float Normalized => !Mathf.Approximately(MaxValue,0f) ? Mathf.Clamp01(CurrentValue / MaxValue) : 1f;
        
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

    }
}