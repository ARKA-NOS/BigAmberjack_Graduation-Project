using _Scripts.DataStructure;

namespace _Scripts.Loading
{
    public struct LoadingData
    {
        public string Name;
        public Gauge LoadingGauge;

        public LoadingData(string name, float current, float max)
        {
            Name = name;
            LoadingGauge = new Gauge(current, max);
        }
        
    }
}