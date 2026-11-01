using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script.Agent.Players.Skills;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
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
