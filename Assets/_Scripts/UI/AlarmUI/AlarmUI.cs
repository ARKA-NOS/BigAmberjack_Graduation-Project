using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.AlarmUI
{
    // 알람 한 개. AlarmListView가 만들고 지운다.
    // showTime 동안 보였다가 duration 동안 흐려지고, 다 흐려지면 알람의 Dismiss를 한 번 호출한다.
    [RequireComponent(typeof(CanvasGroup))]
    public class AlarmUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Image icon;
        private CanvasGroup canvasGroup;

        private AlarmItem _alarm;
        private float _fadeStartTime;
        private float _duration;
        private bool _dismissed;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Init(AlarmItem alarm, float showTime, float duration)
        {
            _alarm = alarm;
            title.text = alarm.Data.Title;
            message.text = alarm.Data.Message;
            icon.sprite = alarm.Data.Image;
            _fadeStartTime = Time.time + showTime;
            _duration = duration;
            _dismissed = false;
        }

        private void Update()
        {
            if (_alarm == null || _dismissed) return;

            float alpha = _duration > 0f
                ? Mathf.Clamp01(1 - ((Time.time - _fadeStartTime) / _duration))
                : (Time.time < _fadeStartTime ? 1f : 0f);
            canvasGroup.alpha = alpha;

            if (alpha <= 0f)
            {
                _dismissed = true;
                _alarm.Dismiss();
            }
        }
    }
}
