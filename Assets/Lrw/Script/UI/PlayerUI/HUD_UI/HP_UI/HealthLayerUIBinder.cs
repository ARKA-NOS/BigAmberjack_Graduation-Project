using System.Collections.Generic;
using Lrw.Script._Core._Debug;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    /// <summary>
    /// 모든 체력을 하나씩 HealthLayerUI로 표시한다 (기본 체력 → 추가 체력 순서)
    /// </summary>
    public class HealthLayerUIBinder : AbstractUIBinder<HealthList>
    {
        [SerializeField] private HealthLayerUI healthLayerUIPrefab;

        private readonly List<HealthLayerUI> _healthLayerUIs = new();

        protected override void Awake()
        {
            base.Awake();

            if (healthLayerUIPrefab == null)
                FDebug.LogError("[HealthLayerUIBinder] healthLayerUIPrefab is null");
        }

        protected override void DataUpdate()
        {
            if (CurrentValue == null || healthLayerUIPrefab == null) return;

            IReadOnlyList<HealthViewData> views = CurrentValue.Views;

            for (int i = _healthLayerUIs.Count; i < views.Count; i++)
                _healthLayerUIs.Add(Instantiate(healthLayerUIPrefab, transform));

            for (int i = 0; i < _healthLayerUIs.Count; i++)
            {
                bool active = i < views.Count;
                _healthLayerUIs[i].gameObject.SetActive(active);
                if (active) _healthLayerUIs[i].SetViewData(views[i]);
            }
        }
    }
}
