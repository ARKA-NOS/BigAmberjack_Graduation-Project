using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players.Modules;

namespace _Scripts.UI.PlayerUI.HUD_UI.StatusRing_UI
{
    // 원형 HP/MP 게이지가 읽는 Model 묶음. Connector가 플레이어 모듈에서 찾아 만든다.
    // 마나 모듈이 없는 캐릭터도 있을 수 있어서 Mana는 null일 수 있다.
    public readonly struct StatusRingSource
    {
        public INewHealthModule Health { get; }

        // 체력 겹별 값. 매 프레임 합계를 읽을 때 할당 없이 돌리려고 따로 받는다.
        public IHealthLayers Layers { get; }

        public IManaModule Mana { get; }

        public StatusRingSource(INewHealthModule health, IHealthLayers layers, IManaModule mana)
        {
            Health = health;
            Layers = layers;
            Mana = mana;
        }
    }
}
