using Lrw.Script.UI.Template;
using UnityEngine;

namespace Lrw.Script.UI.AlarmUI
{
    public class AlarmUIManager : EventListener<AlarmSendEvent>
    {
        [SerializeField] private AlarmUI alarmUI;
        [SerializeField] private float duration = 5f;
        
        
        protected override void HandleEvent(AlarmSendEvent evt)
        {
            AlarmUI currentAlarm = Instantiate(alarmUI, transform);
            currentAlarm.Init(evt.Value, duration);
        }
        
    }
}