using System.Collections.Generic;
using Lrw.Script.DataStructure;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI
{
    public struct HealthViewData
    {
        public Color HealthColor;
        public bool HasMaxHealth;
        public Gauge HealthGauge;

        public HealthViewData(Color healthColor, bool hasMaxHealth, Gauge healthGauge)
        {
            HealthColor = healthColor;
            HasMaxHealth = hasMaxHealth;
            HealthGauge = healthGauge;
        }
    }
}