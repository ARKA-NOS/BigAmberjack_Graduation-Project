using System;

namespace _Scripts.UI.AlarmUI
{
    // 화면에 떠 있는 알람 하나. AlarmViewModel이 알람마다 만든다.
    // 내용이 같은 알람이 연달아 와도 따로 표시되도록 클래스(참조 비교)로 둔다.
    public class AlarmItem
    {
        public AlarmData Data { get; }

        // 알람이 다 사라졌을 때 View가 호출한다. ViewModel이 목록에서 뺀다.
        public Action Dismiss { get; }

        public AlarmItem(AlarmData data, Action<AlarmItem> dismiss)
        {
            Data = data;
            Dismiss = () => dismiss?.Invoke(this);
        }
    }
}
