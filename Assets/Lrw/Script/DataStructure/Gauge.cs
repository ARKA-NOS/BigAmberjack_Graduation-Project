namespace Lrw.Script.DataStructure
{
    public class Gauge
    {
        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        public float Normalized => CurrentValue / MaxValue;
        
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