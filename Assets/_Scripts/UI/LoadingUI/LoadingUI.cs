using _Scripts.DataStructure;
using _Scripts.Loading;
using _Scripts.UI.BindSystem;
using TMPro;
using UnityEngine;

namespace _Scripts.UI.LoadingUI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingUI : AbstractUIBinder<LoadingData>
    {
        [SerializeField] private RectTransform barFill;

        [Header("Loading Title")] 
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI loadingTitle;
        [SerializeField] private float delay = 1f;

        [Header("Fade Setting")] 
        [SerializeField] private float fadeSpeed = 1f;
        private CanvasGroup _canvasGroup;
        
        private const string LoadingTitleFormat = "Loading{0}";
        private const char PointChar = '.';
        private int pointCount = 0;

        private float _lastTime;

        protected override void Awake()
        {
            base.Awake();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        protected override void DataUpdate()
        {
            UpdateCanvasGroup();
            UpdateFill();
            UpdateText();
            UpdateLoadingTitle();
        }

        private void UpdateCanvasGroup()
        {
            float target = CurrentValue.IsComplete ? 0f : 1f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
            _canvasGroup.interactable = !CurrentValue.IsComplete;
        }

        private void UpdateFill()
        {
            if(barFill == null) return;
            barFill.localScale = new Vector3(CurrentValue.LoadingGauge.Normalized, 1, 1);
        }
        
        private void UpdateText()
        {
            if(loadingText == null) return;
            loadingText.text = CurrentValue.Text;
        }
        
        private void UpdateLoadingTitle()
        {
            if(loadingTitle == null) return;

            if (Time.time - _lastTime < delay) return;
            _lastTime += delay;
            
            if (++pointCount > 3) pointCount = 0;
            loadingTitle.text = string.Format(LoadingTitleFormat, new string(PointChar,pointCount));
        }
        
    }
}