using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HDU_UI.HP_UI
{
    public class PlayerHealthUIBinder : AbstractUIBinder<Gauge>
    {
        [Header("Health Bar")]
        [SerializeField] private Image healthBar;
        [SerializeField] private float lagSpeed;
        
        [SerializeField] private Text healthText;
        
        protected override void DataUpdate()
        {
            HealthTextUpdate();
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