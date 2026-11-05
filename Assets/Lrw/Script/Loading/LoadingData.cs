using Lrw.Script.DataStructure;

namespace Lrw.Script.Loading
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