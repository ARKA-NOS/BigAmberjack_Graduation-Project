using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HDU_UI.HP_UI
{
    public class PlayerHealthUIBinder : AbstractUIBinder<Gauge>
    {
        [Header("Health Bar")]
        [SerializeField] private Image currentBar;
        [SerializeField] private Image lagBar;
        [SerializeField,Range(0,1f)] private float lagSpeed;

        private float _lagValue = 0f;
        
        [Header("Health Text")]
        [SerializeField] private TextMeshProUGUI healthText;

        private void Start()
        {
            float normal = CurrentValue.Normalized;
            _lagValue = normal;
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
                healthText.text = CurrentValue.ToString();
            }
        }
        
        
    }
}