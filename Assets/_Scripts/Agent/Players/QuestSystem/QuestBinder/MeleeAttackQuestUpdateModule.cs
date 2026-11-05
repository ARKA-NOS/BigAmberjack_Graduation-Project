using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.Skills;
using _Scripts.DevLib.ModuleSystem;

namespace _Scripts.Agent.Players.QuestSystem.QuestBinder
{
    // 기본 공격을 사용할 때마다 퀘스트 +1
    public class MeleeAttackQuestUpdateModule : AbstractQuestUpdateModule
    {
        protected override void OnInit(ModuleOwner owner)
        {
            EventBus<MeleeAttackUsedEvent>.Subscribe(owner, OnMeleeAttackUsed);
        }

        protected override void OnRelease()
        {
            EventBus<MeleeAttackUsedEvent>.UnSubscribe(Owner, OnMeleeAttackUsed);
        }

        private void OnMeleeAttackUsed(MeleeAttackUsedEvent evt) => AddQuestAmount(1f);
    }
}
