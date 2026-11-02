using Lrw.Script.UI.Template;
using UnityEngine;

namespace Lrw.Script.UI.AlarmUI
{
    public class AlarmUIManager : EventListener<AlarmSendEvent>
    {
        [SerializeField] private AlarmUI alarmUI;

        [Header("Fade Setting")]
        [SerializeField] private float showTime = 3f;
        [SerializeField] private float fadeDuration = 2f;
        
        
        protected override void HandleEvent(AlarmSendEvent evt)
        {
            AlarmUI currentAlarm = Instantiate(alarmUI, transform);
            currentAlarm.Init(evt.Value, showTime, fadeDuration);
        }
        
    }
}