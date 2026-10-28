using System.Collections.Generic;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem;
using Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.HealthBarUI;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    public class NewHealthUIBinder : AbstractUIBinder<HealthData>
    {
        [Header("Prefabs")]
        [SerializeField] private HPBarUI healthImagePrefab;
        [SerializeField] private HPTextUI healthTextPrefab;

        [Header("UI")] 
        [SerializeField] private Transform barContainer;
        [SerializeField] private LayoutElement barSpaceElement;
        [SerializeField] private Transform textContainer;
        
        private List<HPBarUI> _healthBarList;
        private List<HPTextUI> _healthTextList;
        
        protected override void Awake()
        {
            base.Awake();

            _healthBarList = new();
            _healthTextList = new();
        }

        protected override void OnDataChanged()
        {
            if(CurrentValue == null) return;
            
            for (int i = _healthBarList.Count; i < CurrentValue.HealthCount; i++)
            {
                _healthBarList.Add(CreateHealthBarUI());
                _healthTextList.Add(CreateHealthTextUI());
            }
            
            barSpaceElement.transform.SetAsLastSibling();
        }
        
        private HPBarUI CreateHealthBarUI()
            => Instantiate(healthImagePrefab,barContainer);
        
        private HPTextUI CreateHealthTextUI()
            => Instantiate(healthTextPrefab,textContainer);
        
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
                
                HPTextUI currentHpTextUI = _healthTextList[i];
                
                IHealth health = currentViewData.Health;
                IRangeHealth rangeHealth = health as IRangeHealth;
                bool showMaxHealth = rangeHealth != null && rangeHealth.ShowMaxHealth;
                Gauge healthGauge = rangeHealth?.HpGauge ?? new Gauge(health.HP, health.HP);
                
                float currentHealth = healthGauge.CurrentValue;

                // 체력이 남은 바만 켠다 (항목마다 따로 판정)
                bool isShow = currentHealth > 0f;
                currentHpBarUI.SetActive(isShow);
                currentHpTextUI.SetActive(isShow);
                if (isShow)
                {
                    currentHpBarUI.SetBar(health.GetHealthColor(), currentHealth);
                    currentHpTextUI.SetText(currentViewData.HealthSo.HealthTextColor,
                        healthGauge, showMaxHealth);
                    sumCurrentHealth += currentHealth;
                }

                sumRangeMaxHealth += showMaxHealth ? healthGauge.MaxValue : healthGauge.CurrentValue;
            }

            for (int i = showCount; i < _healthBarList.Count; i++)
            {
                _healthBarList[i].SetActive(false);
                _healthTextList[i].SetActive(false);
            }
            
            float spaceFlexible = sumRangeMaxHealth - sumCurrentHealth;
            barSpaceElement.flexibleWidth = spaceFlexible;
        }

        
    }
}