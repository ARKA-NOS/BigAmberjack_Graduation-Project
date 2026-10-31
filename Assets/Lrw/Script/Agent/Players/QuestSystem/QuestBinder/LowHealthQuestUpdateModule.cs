using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // 살아 있는 채로 체력이 기준 이하로 떨어지면 +1
    public class LowHealthQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField, Min(0f)] private float healthThreshold = 1f;

        private INewHealthModule _health;

        protected override void OnInit(ModuleOwner owner)
        {
            _health = owner.GetModule<INewHealthModule>();
            _health.OnHealthChanged += OnHealthChanged;
        }

        protected override void OnRelease()
        {
            _health.OnHealthChanged -= OnHealthChanged;
        }

        // OnHealthChanged는 IsDead가 바뀌기 전에 호출되므로 newValue로 생존을 판단한다.
        private void OnHealthChanged(float newValue, float delta, float max)
        {
            if (delta >= 0f || newValue <= 0f || newValue > healthThreshold) return;
            AddQuestAmount(1f);
        }
    }
}
