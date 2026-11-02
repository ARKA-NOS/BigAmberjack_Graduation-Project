using System;
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

        private float _initTime;
        private float _duration;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Init(AlarmData data, float duration)
        {
            title.text = data.Title;
            message.text = data.Message;
            icon.sprite = data.Image;
            _initTime = Time.time;
            _duration = duration;
        }

        private void Update()
        {
            float alpha = 1 - ((Time.time - _initTime) / _duration);
            canvasGroup.alpha = alpha;

            if (alpha <= 0f)
            {
                Destroy(gameObject);
            }
        }
        
    }
}