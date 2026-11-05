using _Scripts.Agent.HealthSystem.NewHealthSystem;

namespace _Scripts.UI.PlayerUI.HUD_UI.HP_UI
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