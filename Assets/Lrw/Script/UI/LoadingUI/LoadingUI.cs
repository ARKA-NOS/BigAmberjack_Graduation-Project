using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem;
using TMPro;
using UnityEngine;

namespace Lrw.Script.UI.LoadingUI
{
    public class LoadingUI : AbstractUIBinder<Gauge>
    {
        [SerializeField] private RectTransform barFill;
        
        [Header("Loading Title")]
        [SerializeField] private TextMeshProUGUI loadingTitle;
        [SerializeField] private float delay = 1f;
        
        private const string LoadingTitleFormat = "Loading{0}";
        private const char PointChar = '.';
        private int pointCount = 0;

        private float _lastTime;
        
        protected override void DataUpdate()
        {
            UpdateFill();
            UpdateLoadingTitle();
        }
        
        private void UpdateFill()
        {
            if(barFill == null) return;
            barFill.localScale = new Vector3(CurrentValue.Normalized, 1, 1);
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