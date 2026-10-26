using System.Collections.Generic;
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
                float currentHealth = currentViewData.HealthGauge.CurrentValue;

                // 체력이 남은 바만 켠다 (항목마다 따로 판정)
                bool isShow = currentHealth > 0f;
                currentHpBarUI.SetActive(isShow);
                if (isShow)
                {
                    currentHpBarUI.SetBar(currentViewData.HealthColor, currentHealth, currentViewData.HealthGauge);
                    sumCurrentHealth += currentHealth;
                }

                if (currentViewData.HasMaxHealth)
                    sumRangeMaxHealth += currentViewData.HealthGauge.MaxValue;
            }

            for (int i = showCount; i < _healthBarList.Count; i++)
            {
                _healthBarList[i].SetActive(false);
            }

            // 레이아웃 그룹이 flexibleWidth 비율대로 폭을 나누므로 HP 값을 그대로 쓴다
            // 기준 길이는 상한 있는 체력의 최대치 합이고, 현재 합이 넘치면 바 전체가 압축된다
            float barLength = Mathf.Max(sumRangeMaxHealth, sumCurrentHealth);
            spaceElement.flexibleWidth = barLength - sumCurrentHealth;
        }
    }
}