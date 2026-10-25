using System.Collections.Generic;
using Lrw.Script.DataStructure;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI
{
    public struct HealthViewData
    {
        public Color HealthColor;
        public Gauge HealthGauge;

        public HealthViewData(Color healthColor, Gauge healthGauge)
        {
            HealthColor = healthColor;
            HealthGauge = healthGauge;
        }
    }
}