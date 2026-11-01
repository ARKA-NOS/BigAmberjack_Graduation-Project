using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;

namespace Lrw.Script.Agent.Players.Skills
{
    // 플레이어 스킬이 내는 이벤트. 스킬을 쓴 ModuleOwner를 키로 발생시키므로 구독할 때도 같은 Owner를 키로 넘긴다.
    public class MeleeAttackUsedEvent : IEvent
    {
        public ModuleOwner Owner { get; private set; }

        public MeleeAttackUsedEvent Init(ModuleOwner owner)
        {
            Owner = owner;
            return this;
        }
    }

    // 기본 공격이 적에게 맞았을 때(한 번 휘둘러 여러 적을 맞혀도 1번).
    public class MeleeAttackHitEvent : IEvent
    {
        public ModuleOwner Owner { get; private set; }

        public MeleeAttackHitEvent Init(ModuleOwner owner)
        {
            Owner = owner;
            return this;
        }
    }

    public static class PlayerSkillEvents
    {
        private static readonly MeleeAttackUsedEvent MeleeAttackUsed = new();
        private static readonly MeleeAttackHitEvent MeleeAttackHit = new();

        public static void RaiseMeleeAttackUsed(ModuleOwner owner)
            => EventBus<MeleeAttackUsedEvent>.Raise(owner, MeleeAttackUsed.Init(owner));

        public static void RaiseMeleeAttackHit(ModuleOwner owner)
            => EventBus<MeleeAttackHitEvent>.Raise(owner, MeleeAttackHit.Init(owner));
    }
}
