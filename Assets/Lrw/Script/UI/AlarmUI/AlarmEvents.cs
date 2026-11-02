using Lrw.Script._Core._EventSystem;
using UnityEngine;

namespace Lrw.Script.UI.AlarmUI
{
    public static class AlarmEvents
    {
        private static readonly AlarmSendEvent AlarmSend = new();
        
        public static void SendAlarm(string title, string message, Sprite image)
            => EventBus<AlarmSendEvent>.Raise(AlarmSend.Init(new AlarmData(title, message, image)));
        
    }
    
    public class AlarmSendEvent : TemplateEvent<AlarmSendEvent, AlarmData>
    {}
}