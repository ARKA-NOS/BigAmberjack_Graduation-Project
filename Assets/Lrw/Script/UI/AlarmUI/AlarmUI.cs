using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.AlarmUI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AlarmUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Image icon;
        private CanvasGroup canvasGroup;

        private float _fadeStartTime;
        private float _duration;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Init(AlarmData data, float showTime, float duration)
        {
            title.text = data.Title;
            message.text = data.Message;
            icon.sprite = data.Image;
            _fadeStartTime = Time.time + showTime;
            _duration = duration;
        }

        private void Update()
        {
            float alpha = Mathf.Clamp01(1 - ((Time.time - _fadeStartTime) / _duration));
            canvasGroup.alpha = alpha;
            
            if (alpha <= 0f)
            {
                Destroy(gameObject);
            }
        }
        
    }
}