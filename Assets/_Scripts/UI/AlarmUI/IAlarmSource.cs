using System;

namespace _Scripts.UI.AlarmUI
{
    // 알람 ViewModel이 읽는 Model. 게임 어디서든 보낸 알람을 받는다.
    public interface IAlarmSource
    {
        event Action<AlarmData> AlarmReceived;
    }
}
