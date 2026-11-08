using _Scripts._Core._EventSystem;

namespace _Scripts.Loading
{
    public static class LoadingEvents
    {
        private static readonly SetLoadingGaugeEvent SetLoadingGauge = new();

        public static object RaiseSetLoadingGauge(string name, float current, float max)
        {
            LoadingData loadingData = new(name, current, max);
            EventBus<SetLoadingGaugeEvent>.Raise(SetLoadingGauge.Init(loadingData));
            return null;
        }
    }

    public class SetLoadingGaugeEvent : TemplateEvent<SetLoadingGaugeEvent,LoadingData>
    { }
}