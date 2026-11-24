using System;
using System.Collections.Generic;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Audio;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemySoundModule : Module
    {
        [Serializable]
        private struct AttackSound
        {
            public EnemyAttackSkill skill;
            public SoundCueSO cue;
        }

        [SerializeField] private SoundCueSO hitCue;
        [SerializeField] private SoundCueSO deathCue;
        [SerializeField] private AttackSound[] attackSounds = Array.Empty<AttackSound>();

        private readonly Dictionary<EnemyAttackSkill, SoundCueSO> _attackCues = new();
        private EnemyBase _enemy;
        private bool _subscribed;

        public override void Initialize(ModuleOwner owner)
        {
            Unsubscribe();
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _enemy == null || !isActiveAndEnabled) return;

            _enemy.DamageReceived += OnDamageReceived;
            _enemy.DeathStarted += OnDeathStarted;

            foreach (AttackSound attack in attackSounds)
            {
                if (attack.skill == null || attack.cue == null || !_attackCues.TryAdd(attack.skill, attack.cue))
                    continue;

                attack.skill.AttackPerformed += OnAttackPerformed;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            if (_enemy != null)
            {
                _enemy.DamageReceived -= OnDamageReceived;
                _enemy.DeathStarted -= OnDeathStarted;
            }

            foreach (EnemyAttackSkill skill in _attackCues.Keys)
                if (skill != null) skill.AttackPerformed -= OnAttackPerformed;

            _attackCues.Clear();
            _subscribed = false;
        }

        private void OnDamageReceived(DamageData damage, Vector2 point, Vector2 direction, Vector2 normal)
        {
            Play(hitCue, new Vector3(point.x, point.y, _enemy.transform.position.z));
        }

        private void OnDeathStarted() => Play(deathCue, _enemy.transform.position);

        private void OnAttackPerformed(EnemyAttackSkill skill, Vector3 position)
        {
            if (_attackCues.TryGetValue(skill, out SoundCueSO cue)) Play(cue, position);
        }

        private void Play(SoundCueSO cue, Vector3 position)
        {
            if (isActiveAndEnabled) PlayCue(cue, position);
        }

        internal static void PlayCue(SoundCueSO cue, Vector3 position)
        {
            if (cue != null)
                ServiceLocator.Get<ISoundManager>()?.PlaySfx(cue, position);
        }
    }
}
