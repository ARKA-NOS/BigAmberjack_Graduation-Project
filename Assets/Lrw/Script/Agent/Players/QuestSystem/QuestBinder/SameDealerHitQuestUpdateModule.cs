using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // 같은 적(같은 개체)에게 맞은 횟수 중 가장 큰 값
    public class SameDealerHitQuestUpdateModule : AbstractQuestUpdateModule
    {
        private readonly Dictionary<ModuleOwner, int> _hitCounts = new();
        private int _maxCount;

        protected override void OnInit(ModuleOwner owner)
        {
            EventBus<AgentDamagedEvent>.Subscribe(owner, OnDamaged);
        }

        protected override void OnRelease()
        {
            EventBus<AgentDamagedEvent>.UnSubscribe(Owner, OnDamaged);
        }

        private void OnDamaged(AgentDamagedEvent evt)
        {
            ModuleOwner dealer = evt.DamageData.Dealer;
            if (dealer == null) return;

            _hitCounts.TryGetValue(dealer, out int count);
            _hitCounts[dealer] = ++count;
            _maxCount = Mathf.Max(_maxCount, count);
            SetQuestAmount(_maxCount);
        }
    }
}
