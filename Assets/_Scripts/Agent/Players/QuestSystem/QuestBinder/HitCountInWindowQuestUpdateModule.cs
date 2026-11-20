using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.QuestSystem.QuestBinder
{
    // window초 안에 맞은 횟수. 죽으면 0으로 돌아간다.
    public class HitCountInWindowQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField, Min(0.1f)] private float window = 5f;

        private readonly Queue<float> _hitTimes = new();
        private INewHealthModule _health;

        protected override void OnInit(ModuleOwner owner)
        {
            _health = owner.GetModule<INewHealthModule>();
            EventBus<AgentDamagedEvent>.Subscribe(owner, OnDamaged);
        }

        protected override void OnRelease()
        {
            EventBus<AgentDamagedEvent>.UnSubscribe(Owner, OnDamaged);
        }

        private void OnDamaged(AgentDamagedEvent evt)
        {
            _hitTimes.Enqueue(Time.time);
            Refresh();
        }

        private void Update()
        {
            if (!IsInitialized || _hitTimes.Count == 0) return;
            Refresh();
        }

        private void Refresh()
        {
            while (_hitTimes.Count > 0 && Time.time - _hitTimes.Peek() > window)
                _hitTimes.Dequeue();

            if (_health.IsDead) _hitTimes.Clear();

            SetQuestAmount(_hitTimes.Count);
        }
    }
}
