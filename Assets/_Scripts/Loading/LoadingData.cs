using _Scripts.DataStructure;

namespace _Scripts.Loading
{
    public struct LoadingData
    {
        public string Text;
        public Gauge LoadingGauge;
        public bool IsComplete;

        public LoadingData(string loadingText, Gauge gauge)
        {
            Text = loadingText;
            LoadingGauge = gauge;
            IsComplete = gauge.IsMax;
        }

        public override string ToString()
            => $"{Text} Loading : " + (IsComplete ? "Complete" : $": {LoadingGauge}");
        
    }
}