using _Scripts._Core._EventSystem;
using _Scripts.DataStructure;

namespace _Scripts.Loading
{
    public static class LoadingEvents
    {
        private static readonly SetLoadingGaugeEvent SetLoadingGauge = new();
        
        public static void RaiseSetLoadingGauge(string name, Gauge gauge)
        {
            LoadingData loadingData = new(name, gauge);
            EventBus<SetLoadingGaugeEvent>.Raise(SetLoadingGauge.Init(loadingData));
        }
    }

    public class SetLoadingGaugeEvent : TemplateEvent<SetLoadingGaugeEvent,LoadingData>
    { }
}