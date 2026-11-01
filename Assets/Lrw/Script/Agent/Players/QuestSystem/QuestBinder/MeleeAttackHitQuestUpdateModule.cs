using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script.Agent.Players.Skills;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // 기본 공격이 적에게 맞을 때마다 퀘스트 +1 (한 번 휘둘러 여러 적을 맞혀도 1)
    public class MeleeAttackHitQuestUpdateModule : AbstractQuestUpdateModule
    {
        protected override void OnInit(ModuleOwner owner)
        {
            EventBus<MeleeAttackHitEvent>.Subscribe(owner, OnMeleeAttackHit);
        }

        protected override void OnRelease()
        {
            EventBus<MeleeAttackHitEvent>.UnSubscribe(Owner, OnMeleeAttackHit);
        }

        private void OnMeleeAttackHit(MeleeAttackHitEvent evt) => AddQuestAmount(1f);
    }
}
