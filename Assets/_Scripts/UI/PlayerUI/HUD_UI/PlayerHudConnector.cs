using _Scripts.Agent.EffectSystem;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players.Modules;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.MVVM.Connectors;
using _Scripts.UI.PlayerUI.HUD_UI.Effect_UI_System;
using _Scripts.UI.PlayerUI.HUD_UI.HP_UI;
using _Scripts.UI.PlayerUI.HUD_UI.Skill_UI;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI
{
    // 플레이어 프리팹에 붙어 플레이어 모듈(체력, 스킬, 효과)을 HUD ViewModel에 연결한다.
    // 모듈은 HUD를 모르고, 이 컴포넌트만 모듈과 ViewModel을 함께 안다. 비워 둔 ViewModel은 건너뛴다.
    public class PlayerHudConnector : ConnectorBase
    {
        [SerializeField] private HealthViewModel healthViewModel;

        [Header("Skill")]
        [SerializeField] private SkillSlotViewModel skill1ViewModel;
        [SerializeField] private SkillSlotViewModel skill2ViewModel;

        [Header("Effect")]
        [SerializeField] private EffectViewModel effectViewModel;

        protected override void Connect()
        {
            ModuleOwner owner = GetComponentInParent<ModuleOwner>();
            if (owner == null)
            {
                Debug.LogWarning($"[{name}] ModuleOwner를 찾지 못해 HUD를 연결하지 않습니다.", this);
                return;
            }

            if (healthViewModel != null && owner.TryGetModule(out IHealthLayers health))
                healthViewModel.Connect(health);

            if (owner.TryGetModule(out IPlayerSkillModule skills))
            {
                if (skill1ViewModel != null) skill1ViewModel.Connect(new SkillSlotSource(skills, PlayerSkillSlot.First));
                if (skill2ViewModel != null) skill2ViewModel.Connect(new SkillSlotSource(skills, PlayerSkillSlot.Second));
            }

            if (effectViewModel != null && owner.TryGetModule(out IEffectModule effects))
                effectViewModel.Connect(effects);
        }

        protected override void Disconnect()
        {
            if (healthViewModel != null) healthViewModel.Disconnect();
            if (skill1ViewModel != null) skill1ViewModel.Disconnect();
            if (skill2ViewModel != null) skill2ViewModel.Disconnect();
            if (effectViewModel != null) effectViewModel.Disconnect();
        }
    }
}
