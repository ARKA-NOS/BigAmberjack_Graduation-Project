using System.Collections.Generic;
using Lrw.Script.DataStructure;
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
            if (CurrentValue == null) return;

            List<HealthViewData> healthViewDataList = CurrentValue.Data;
            int showCount = Mathf.Min(_healthBarList.Count, healthViewDataList.Count);

            float sumCurrentHealth = 0f;
            float sumRangeMaxHealth = 0f;
            for (int i = 0; i < showCount; i++)
            {
                HPBarUI currentHpBarUI = _healthBarList[i];
                HealthViewData currentViewData = healthViewDataList[i];
                Gauge healthGauge = currentViewData.HealthGauge;
                float currentHealth = healthGauge.CurrentValue;

                // 체력이 남은 바만 켠다 (항목마다 따로 판정)
                bool isShow = currentHealth > 0f;
                currentHpBarUI.SetActive(isShow);
                if (isShow)
                {
                    currentHpBarUI.SetBar(currentViewData.HealthColor,
                        currentHealth,
                        healthGauge,
                        currentViewData.HasMaxHealth
                        );
                    sumCurrentHealth += currentHealth;
                }

                sumRangeMaxHealth += healthGauge.MaxValue;
            }

            for (int i = showCount; i < _healthBarList.Count; i++)
            {
                _healthBarList[i].SetActive(false);
            }
            
            spaceElement.flexibleWidth = sumRangeMaxHealth - sumCurrentHealth;
        }
    }
}