using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies.Managers;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackTokenModule : Module
    {
        [Tooltip("대기 제한에 걸린 뒤 다시 큐의 끝에 등록하기까지의 게임 시간입니다.")]
        [SerializeField, Min(0f)] private float retryDelay = 0.5f;

        private EnemyBase _enemy;
        private EnemySkillModule _skills;
        private AttackTokenManager _manager;
        private AbstractSkill _requestedSkill;
        private float _retryAt;
        private bool _reportedMissingManager;

        public bool HasToken { get; private set; }
        public bool IsWaiting { get; private set; }
        internal AbstractSkill RequestedSkill => _requestedSkill;
        internal bool IsEligible => Ready && !_enemy.IsHitReacting && _requestedSkill != null &&
            _skills.CanRequestTokenFor(_requestedSkill);
        internal bool CanRetainToken => Ready && (_skills.IsBusy || IsEligible);
        private bool Ready => isActiveAndEnabled && _enemy != null && _enemy.isActiveAndEnabled &&
            !_enemy.IsDead && _skills != null;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _skills = owner.GetModule<EnemySkillModule>();
        }

        internal bool TryAcquire(AbstractSkill skill)
        {
            if (!Ready || Time.time < _retryAt) return false;

            AttackTokenManager manager = ServiceLocator.Get<AttackTokenManager>();
            if (manager == null || !manager.isActiveAndEnabled)
            {
                Release();
                if (!_reportedMissingManager)
                {
                    Debug.LogWarning("공격 토큰 매니저가 없습니다. GameManager의 AttackTokenManager를 활성화하세요.", this);
                    _reportedMissingManager = true;
                }

                return false;
            }

            if (_manager != manager || _requestedSkill != skill) Release();
            _manager = manager;
            _requestedSkill = skill;
            if (HasToken) return true;

            IsWaiting = true;
            bool acquired = manager.TryAcquire(this);
            if (!acquired && !IsEligible) Release();

            return acquired;
        }

        internal void OnTokenGranted(AttackTokenManager manager)
        {
            _manager = manager;
            HasToken = true;
            IsWaiting = false;
        }

        internal void OnQueueRemoved(bool expired)
        {
            IsWaiting = false;
            _requestedSkill = null;
            if (expired) _retryAt = Time.time + Mathf.Max(0f, retryDelay);
        }

        internal void OnTokenReleased()
        {
            HasToken = false;
            _requestedSkill = null;
        }

        internal void OnManagerStopped()
        {
            Release();
            _skills?.StopAttack();
        }

        internal void Release()
        {
            AttackTokenManager manager = _manager;
            _manager = null;
            _requestedSkill = null;
            HasToken = IsWaiting = false;
            if (manager != null) manager.Release(this);
        }

        private void Update()
        {
            if ((IsWaiting || HasToken) && !CanRetainToken) Release();
        }

        private void OnDisable() => Release();

        private void OnDestroy() => Release();
    }
}
