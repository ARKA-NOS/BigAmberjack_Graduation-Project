using _Scripts.Agent.Players.Modules;

namespace _Scripts.UI.PlayerUI.HUD_UI.Skill_UI
{
    // 스킬 슬롯 ViewModel이 읽는 Model. 플레이어 스킬 모듈과 그중 어느 슬롯인지.
    public readonly struct SkillSlotSource
    {
        public IPlayerSkillModule Module { get; }
        public PlayerSkillSlot Slot { get; }

        public SkillSlotSource(IPlayerSkillModule module, PlayerSkillSlot slot)
        {
            Module = module;
            Slot = slot;
        }
    }
}
