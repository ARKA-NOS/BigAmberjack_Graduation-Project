using System.Collections.Generic;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.DataStructure;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI
{
    public struct HealthViewData
    {
        public AbstractHealthSo HealthSo;
        public IHealth Health;

        public HealthViewData(AbstractHealthSo healthSo, IHealth health)
        {
            HealthSo = healthSo;
            Health = health;
        }
    }
}