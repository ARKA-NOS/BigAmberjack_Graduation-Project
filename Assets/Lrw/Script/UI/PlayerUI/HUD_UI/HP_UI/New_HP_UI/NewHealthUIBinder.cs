using System.Collections.Generic;
using System.Linq;
using Lrw.Script.UI.BindSystem;
using Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI.HealthBarUI;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI
{
    public class NewHealthUIBinder : AbstractUIBinder<HealthData>
    {
        [Header("Prefabs")]
        [SerializeField] private HPBarUI healthImagePrefab;

        [Header("UI")] 
        [SerializeField] private LayoutElement spaceElement;
        
        private List<HPBarUI> _healthBarList;
        
        protected override void Awake()
        {
            base.Awake();
            _healthBarList = new();
        }

        protected override void OnDataChanged()
        {
            if(CurrentValue == null) return;
            
            for (int i = _healthBarList.Count; i < CurrentValue.HealthCount; i++)
            {
                _healthBarList.Add(CreateHealthBarUI());
            }
            
            spaceElement.transform.SetAsLastSibling();
        }
        
        private HPBarUI CreateHealthBarUI()
            => Instantiate(healthImagePrefab,transform);
        
        protected override void DataUpdate()
        {
            int showHealthCount = CurrentValue.Data.Sum(x => x.HealthGauge.CurrentValue > 0 ? 1 : 0);
            List<HealthViewData> healthViewDataList = CurrentValue.Data;
            float sumMaxHealth = healthViewDataList.Sum(x => x.HealthGauge.MaxValue);
            
            for (int i = 0; i < _healthBarList.Count; i++)
            {
                HPBarUI currentHpBarUI = _healthBarList[i];
                if (showHealthCount <= i)
                {
                    currentHpBarUI.SetActive(false);
                    break;
                }
                
                currentHpBarUI.SetActive(true); 
                
                HealthViewData currentViewData = healthViewDataList[i]; 
                
                float flex = Mathf.Approximately(sumMaxHealth,0f) ? 
                    0f : 
                    currentViewData.HealthGauge.CurrentValue / sumMaxHealth;
                currentHpBarUI.SetBar(currentViewData.HealthColor,flex);
            }
            
            if (Mathf.Approximately(sumMaxHealth, healthViewDataList[0].HealthGauge.CurrentValue))
            {
                _healthBarList[0].SetScale(healthViewDataList[0].HealthGauge.Normalized);
            }
            
        }
    }
}