using _Scripts._Core._EventSystem;

namespace _Scripts.Agent.Players.TitleSystem
{
    // 칭호를 달성했음을 알리는 이벤트. 칭호 달성 이력(도감)처럼 런을 넘어 기록하는 시스템이 구독한다.
    public class TitleCompletedEvent : IEvent
    {
        public AbstractTitleSo Title { get; private set; }

        public TitleCompletedEvent Init(AbstractTitleSo title)
        {
            Title = title;
            return this;
        }
    }

    public static class TitleEvents
    {
        private static readonly TitleCompletedEvent TitleCompleted = new();

        public static void RaiseTitleCompleted(AbstractTitleSo title)
            => EventBus<TitleCompletedEvent>.Raise(TitleCompleted.Init(title));
    }
}
