using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    public class PlayerHealthUIBinder : AbstractUIBinder<Gauge>
    {
        [Header("Health Bar")]
        [SerializeField] private Image currentBar;
        [SerializeField] private Image lagBar;
        [SerializeField,Range(0,1f)] private float lagSpeed;
        
        private float _lagValue = 0f;
        private bool _hasValue;

        [Header("Health Text")]
        [SerializeField] private TextMeshProUGUI healthText;

        protected override void OnDataChanged()
        {
            float normal = CurrentValue.Normalized;

            // 처음 값이 들어오거나 회복이면 바로 맞추고, 피해일 때만 지연 바가 천천히 따라간다
            if (!_hasValue || normal > _lagValue)
            {
                _lagValue = normal;
                _hasValue = true;
            }

            SetBarSize(normal, _lagValue);
        }
        
        protected override void DataUpdate()
        {
            HealthBarUpdate();
            HealthTextUpdate();
        }
        
        private void HealthBarUpdate()
        {
            float normal = CurrentValue.Normalized;
            
            _lagValue = Mathf.MoveTowards(_lagValue,normal , Time.deltaTime * lagSpeed);
            SetBarSize(normal,_lagValue);
        }
        
        
        private void SetBarSize(float currentBarSize, float lagBarSize)
        {
            currentBar.rectTransform.localScale = new Vector3(currentBarSize, 1, 1);
            lagBar.rectTransform.localScale = new Vector3(lagBarSize, 1, 1);
        }
        
        private void HealthTextUpdate()
        {
            if (healthText != null)
            {
                healthText.text = $"{CurrentValue.CurrentValue} / {CurrentValue.MaxValue}";
            }
        }
        
    }
}