using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // window초 안에 잃은 체력을 최대 체력 대비 %로 센다(퀘스트 목표 75 = 75%).
    public class HealthLossInWindowQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField, Min(0.1f)] private float window = 3f;

        private readonly Queue<(float time, float amount)> _losses = new();
        private INewHealthModule _health;
        private float _lossSum;

        protected override void OnInit(ModuleOwner owner)
        {
            _health = owner.GetModule<INewHealthModule>();
            _health.OnHealthChanged += OnHealthChanged;
        }

        protected override void OnRelease()
        {
            _health.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float newValue, float delta, float max)
        {
            if (delta >= 0f) return;

            _losses.Enqueue((Time.time, -delta));
            _lossSum -= delta;
            Refresh();
        }

        private void Update()
        {
            if (!IsInitialized || _losses.Count == 0) return;
            Refresh();
        }

        private void Refresh()
        {
            while (_losses.Count > 0 && Time.time - _losses.Peek().time > window)
                _lossSum -= _losses.Dequeue().amount;

            if (_losses.Count == 0) _lossSum = 0f;

            float maxHealth = _health.MaxHealth;
            SetQuestAmount(maxHealth > 0f ? _lossSum / maxHealth * 100f : 0f);
        }
    }
}
