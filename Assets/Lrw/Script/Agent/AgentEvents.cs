using DevLib.BattleSystem;
using Lrw.Script._Core._EventSystem;

namespace Lrw.Script.Agent
{
    // Agent가 피해를 받았을 때 발생. 맞은 Agent를 키로 발생시키므로 구독할 때도 같은 Agent(Owner)를 키로 넘긴다.
    public class AgentDamagedEvent : IEvent
    {
        public Agent Target { get; private set; }
        public DamageData DamageData { get; private set; }

        public AgentDamagedEvent Init(Agent target, DamageData damageData)
        {
            Target = target;
            DamageData = damageData;
            return this;
        }
    }

    public static class AgentEvents
    {
        private static readonly AgentDamagedEvent AgentDamaged = new();

        public static void RaiseDamaged(Agent target, DamageData damageData)
            => EventBus<AgentDamagedEvent>.Raise(target, AgentDamaged.Init(target, damageData));
    }
}
