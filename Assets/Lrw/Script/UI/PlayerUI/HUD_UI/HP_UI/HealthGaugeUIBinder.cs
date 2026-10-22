using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    /// <summary>
    /// 인스펙터에서 고른 체력 하나만 표시한다. 그 체력이 없으면 target을 숨긴다.
    /// </summary>
    public class HealthGaugeUIBinder : AbstractUIBinder<HealthList>
    {
        [SerializeField] private AbstractHealthSo targetHealth;
        [SerializeField] private HealthLayerUI healthUI;

        protected override void DataUpdate()
        {
            if (healthUI == null) return;

            HealthViewData view = default;
            bool found = CurrentValue != null && CurrentValue.TryGet(targetHealth, out view);
            if (healthUI.gameObject.activeSelf != found)
                healthUI.gameObject.SetActive(found);

            if (found) healthUI.SetViewData(view);
        }
    }
}
