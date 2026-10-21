using Lrw.Script._Core._EventSystem;

namespace SDW.Scripts.RunEnd
{
    public enum RunEndReason
    {
        BossCleared,
        PlayerDead
    }

    // 탐험(런)이 끝났음을 알리는 이벤트. 보스 클리어 또는 플레이어 사망 시 발생한다.
    public class RunEndedEvent : IEvent
    {
        public RunEndReason Reason { get; private set; }

        public RunEndedEvent Init(RunEndReason reason)
        {
            Reason = reason;
            return this;
        }
    }

    public static class RunEndEvents
    {
        private static readonly RunEndedEvent RunEnded = new();

        public static void RaiseRunEnded(RunEndReason reason)
            => EventBus<RunEndedEvent>.Raise(RunEnded.Init(reason));
    }
}
