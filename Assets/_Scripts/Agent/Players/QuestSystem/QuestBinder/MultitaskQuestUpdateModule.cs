using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts.Agent.Players.Skills;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.QuestSystem.QuestBinder
{
    public class MultitaskQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField, Min(0.1f)] private float window = 5f;
        [SerializeField, Min(1)] private int requiredDash = 2;
        [SerializeField, Min(1)] private int requiredAttack = 2;

        private readonly Queue<float> _dashTimes = new();
        private readonly Queue<float> _attackTimes = new();

        protected override void OnInit(ModuleOwner owner)
        {
            EventBus<DashUsedEvent>.Subscribe(owner, OnDash);
            EventBus<MeleeAttackUsedEvent>.Subscribe(owner, OnAttack);
        }

        protected override void OnRelease()
        {
            EventBus<DashUsedEvent>.UnSubscribe(Owner, OnDash);
            EventBus<MeleeAttackUsedEvent>.UnSubscribe(Owner, OnAttack);
        }

        private void OnDash(DashUsedEvent evt)
        {
            _dashTimes.Enqueue(Time.time);
            Check();
        }

        private void OnAttack(MeleeAttackUsedEvent evt)
        {
            _attackTimes.Enqueue(Time.time);
            Check();
        }

        private void Check()
        {
            Trim(_dashTimes);
            Trim(_attackTimes);

            if (_dashTimes.Count < requiredDash || _attackTimes.Count < requiredAttack) return;

            _dashTimes.Clear();
            _attackTimes.Clear();
            AddQuestAmount(1);
        }

        private void Trim(Queue<float> times)
        {
            while (times.Count > 0 && Time.time - times.Peek() > window)
                times.Dequeue();
        }
    }
}
