using System;
using _Scripts._Core._EventSystem;

namespace _Scripts.UI.AlarmUI
{
    // AlarmEvents.SendAlarm으로 보낸 알람(EventBus<AlarmSendEvent>)을 IAlarmSource로 감싼다. AlarmConnector가 만든다.
    public class EventBusAlarmSource : IAlarmSource
    {
        private Action<AlarmData> _alarmReceived;

        // 구독자가 생기면 EventBus를 구독하고, 모두 빠지면 해제한다. EventBus는 정적이라 씬이 바뀌어도 남으므로 해제가 중요하다.
        public event Action<AlarmData> AlarmReceived
        {
            add
            {
                if (_alarmReceived == null) EventBus<AlarmSendEvent>.Subscribe(HandleAlarmSend);
                _alarmReceived += value;
            }
            remove
            {
                _alarmReceived -= value;
                if (_alarmReceived == null) EventBus<AlarmSendEvent>.UnSubscribe(HandleAlarmSend);
            }
        }

        private void HandleAlarmSend(AlarmSendEvent evt) => _alarmReceived?.Invoke(evt.Value);
    }
}
