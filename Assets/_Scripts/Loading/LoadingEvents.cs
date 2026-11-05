using _Scripts._Core._EventSystem;

namespace _Scripts.Loading
{
    public static class LoadingEvents
    {
        private static readonly SetLoadingGaugeEvent SetLoadingGauge = new();

        public static object RaiseSetLoadingGauge(string name, float current, float max)
        {
            EventBus<SetLoadingGaugeEvent>.Raise(SetLoadingGauge.Init(new(name, current, max)));
            return null;
        }
    }

    public class SetLoadingGaugeEvent : TemplateEvent<SetLoadingGaugeEvent,LoadingData>
    { }
}