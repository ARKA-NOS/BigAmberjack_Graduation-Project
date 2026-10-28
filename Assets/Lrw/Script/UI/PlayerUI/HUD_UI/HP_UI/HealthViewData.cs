using Lrw.Script.Agent.HealthSystem.NewHealthSystem;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
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