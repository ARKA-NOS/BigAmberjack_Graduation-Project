using System;
using System.Collections.Generic;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.SkillSystem;
using _Scripts.Agent.SkillSystem.Interface;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 공용 SkillModule은 등록/쿨타임 검사/실행을, 이 모듈은 적의 선택 규칙만 담당합니다.
    [DisallowMultipleComponent]
    public sealed class EnemySkillModule : SkillModule
    {
        [Serializable]
        public sealed class SkillEntry
        {
            [SerializeField] private AbstractSkill skill;
            [SerializeField] private bool useSkill = true;
            [Tooltip("끄면 거리 기반 자동 선택에서는 제외하고, 보스 패턴의 지정 실행으로만 사용합니다.")]
            [SerializeField] private bool useForAutomaticSelection = true;
            [Tooltip("도약처럼 사용할 수 없을 때 추격을 계속해야 하는 스킬에 사용합니다. 끄면 기존처럼 사거리 안에서 쿨타임을 기다립니다.")]
            [SerializeField] private bool requireUsableToEnterAttack;
            [SerializeField, Min(0f)] private float minRange;
            [Tooltip("거리 확보 모듈이 벽/절벽 때문에 후퇴할 수 없을 때만 최소 사거리 제한을 해제합니다. 시야/높이/최대 사거리 검사는 유지합니다.")]
            [SerializeField] private bool allowCloseAttackWhenRetreatBlocked;
            [SerializeField, Min(0f)] private float maxRange = 1.5f;
            [Tooltip("거리는 수평으로 재고, 높이 차이는 아래 설정으로 따로 확인합니다.")]
            [SerializeField] private bool useHorizontalRange;
            [Tooltip("발 높이 차이. 음수면 높이 제한 없음. 근접은 0.5 권장.")]
            [SerializeField] private float maxHeightDifference = 0.5f;
            [SerializeField] private bool requireGrounded = true;
            [SerializeField] private bool requireClearPath = true;
            [Tooltip("시야 검사 시작점. 원거리는 FirePoint, 비워두면 몸체 중심.")]
            [SerializeField] private Transform origin;
            [Tooltip("AbstractChargeSkill일 때만 사용. 이 시간 후 입력을 떼어 발동합니다.")]
            [SerializeField, Min(0f)] private float chargeDuration;

            public AbstractSkill Skill => skill;
            public bool Enabled => useSkill && skill != null && skill.isActiveAndEnabled && skill.SkillSo != null;
            public bool UseForAutomaticSelection => useForAutomaticSelection;
            public bool RequireUsableToEnterAttack => requireUsableToEnterAttack;
            public float MaxRange => Mathf.Max(0f, maxRange);
            public float ChargeDuration => Mathf.Max(0f, chargeDuration);

            internal bool IsInRange(EnemyBase enemy, Vector2 targetPosition, EnemyKeepDistanceModule keepDistance)
            {
                Vector2 offset = targetPosition - (Vector2)enemy.transform.position;
                float distance = useHorizontalRange ? Mathf.Abs(offset.x) : offset.magnitude;
                return distance <= MaxRange && (distance >= Mathf.Max(0f, minRange) ||
                    (allowCloseAttackWhenRetreatBlocked && keepDistance != null && keepDistance.IsRetreatBlocked(enemy.Target)));
            }

            internal bool Matches(EnemyBase enemy, EnemyMovementModule movement, EnemyFlightModule flight,
                EnemyKeepDistanceModule keepDistance)
            {
                if (!Enabled || enemy.Target == null || !enemy.Target.gameObject.activeInHierarchy ||
                    (requireGrounded && (movement == null || !movement.IsGrounded)))
                    return false;
                bool flying = flight != null && flight.isActiveAndEnabled;
                if (!flying && movement == null) return false;
                Vector2 targetPosition = flying ? flight.GetTargetCenter(enemy.Target) : (Vector2)enemy.Target.position;
                if (!IsInRange(enemy, targetPosition, keepDistance)) return false;
                if (maxHeightDifference >= 0f)
                {
                    float heightDifference = flying
                        ? targetPosition.y - flight.BodyCenter.y
                        : movement.GetTargetHeightDifference(enemy.Target);
                    if (Mathf.Abs(heightDifference) > maxHeightDifference) return false;
                }
                if (!requireClearPath) return true;
                if (flying)
                    return flight.HasClearPath(origin != null ? (Vector2)origin.position : flight.BodyCenter, targetPosition);
                Collider2D body = enemy.Target.GetComponent<Collider2D>();
                Vector2 aim = body != null ? (Vector2)body.bounds.center : (Vector2)enemy.Target.position;
                return movement.HasClearPath(origin != null ? (Vector2)origin.position : movement.BodyCenter, aim);
            }
        }

        [Tooltip("위에서부터 우선순위. 조건과 쿨타임을 만족하는 첫 스킬을 사용합니다. 실행 도중 AI가 다른 스킬로 취소하지는 않습니다.")]
        [SerializeField] private List<SkillEntry> skills = new List<SkillEntry>();
        private readonly HashSet<AbstractSkill> _registeredSkills = new HashSet<AbstractSkill>();
        private EnemyBase _enemy;
        private EnemyMovementModule _movement;
        private EnemyFlightModule _flight;
        private EnemyKeepDistanceModule _keepDistance;
        private AbstractSkill _activeSkill;
        private Transform _castTarget;
        private float _chargeReleaseTime;

        public IReadOnlyList<SkillEntry> Skills => skills;
        public AbstractSkill ActiveSkill => IsBusy ? _activeSkill : null;
        internal bool HasCommittedAttack => ActiveSkill is EnemyAttackSkill { CompletesAfterTargetLost: true };
        public bool IsBusy => _activeSkill != null &&
            (_activeSkill.IsUsing || _activeSkill is IChargeSkill { IsCharging: true });
        private bool Ready => isActiveAndEnabled && _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead;
        public float MaximumRange
        {
            get
            {
                float range = 0f;
                foreach (SkillEntry entry in skills)
                    if (entry != null && entry.Enabled && entry.UseForAutomaticSelection) range = Mathf.Max(range, entry.MaxRange);
                return range;
            }
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _movement = owner.GetModule<EnemyMovementModule>();
            _flight = owner.GetModule<EnemyFlightModule>();
            _keepDistance = owner.GetModule<EnemyKeepDistanceModule>();
            _registeredSkills.Clear();
            // 공용 AfterInit과 같은 탐색 범위/첫 SO만 등록 규칙을 사용합니다.
            HashSet<SkillSO> keys = new HashSet<SkillSO>();
            foreach (ISkill candidate in GetComponentsInChildren<ISkill>())
                if (candidate.SkillSo != null && keys.Add(candidate.SkillSo) && candidate is AbstractSkill skill)
                    _registeredSkills.Add(skill);
            foreach (SkillEntry entry in skills)
                if (entry == null || entry.Skill == null || !_registeredSkills.Contains(entry.Skill))
                    Debug.LogWarning($"{name}: 스킬 목록에는 이 모듈 아래의 활성 스킬과 고유 Skill SO를 연결하세요.", this);
        }

        // 쿨타임은 제외합니다. 사거리 안에서 쿨타임을 기다릴 때 타깃을 밀고 지나가지 않습니다.
        public bool CanAttackTarget()
        {
            if (!Ready) return false;
            foreach (SkillEntry entry in skills)
                if (IsValid(entry) && entry.UseForAutomaticSelection && entry.Matches(_enemy, _movement, _flight, _keepDistance) &&
                    (!entry.RequireUsableToEnterAttack || CanUseSkill(entry.Skill.SkillSo))) return true;
            return false;
        }

        public bool TryUseAttack()
        {
            if (!Ready || IsBusy || _enemy.IsHitReacting) return false;
            foreach (SkillEntry entry in skills)
            {
                if (!IsValid(entry) || !entry.UseForAutomaticSelection || !entry.Matches(_enemy, _movement, _flight, _keepDistance) || !CanUseSkill(entry.Skill.SkillSo)) continue;
                _activeSkill = entry.Skill;
                _castTarget = _enemy.Target;
                _chargeReleaseTime = Time.time + entry.ChargeDuration;
                UseSkill(_activeSkill.SkillSo, true);
                return true;
            }
            return false;
        }

        // 보스 FSM 등 실행할 스킬이 이미 정해진 호출자용. 잡몹의 거리/지상 선택 규칙은 건너뛰되
        // Skills 목록의 사용 설정/차징 시간과 공용 등록·쿨타임 검사를 그대로 적용합니다.
        public bool TryUseSpecifiedSkill(AbstractSkill skill)
        {
            if (!Ready || IsBusy || _enemy.IsHitReacting ||
                !TryGetEnabledEntry(skill, out SkillEntry entry) ||
                _enemy.Target == null || !_enemy.Target.gameObject.activeInHierarchy || !CanUseSkill(skill.SkillSo))
                return false;
            _activeSkill = skill;
            _castTarget = _enemy.Target;
            _chargeReleaseTime = Time.time + entry.ChargeDuration;
            UseSkill(skill.SkillSo, true);
            return true;
        }

        // 쿨타임과 분리된 구성 검사. 패턴 FSM이 사용 해제된 스킬을 무한히 기다리지 않도록 합니다.
        public bool IsSkillEnabled(AbstractSkill skill) => isActiveAndEnabled && TryGetEnabledEntry(skill, out _);

        internal bool IsTargetInSkillRange(AbstractSkill skill, Vector2 targetPosition) =>
            Ready && TryGetEnabledEntry(skill, out SkillEntry entry) &&
            entry.IsInRange(_enemy, targetPosition, _keepDistance);

        private bool TryGetEnabledEntry(AbstractSkill skill, out SkillEntry entry)
        {
            foreach (SkillEntry candidate in skills)
            {
                if (IsValid(candidate) && candidate.Skill == skill)
                {
                    entry = candidate;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        private bool IsValid(SkillEntry entry) => entry != null && entry.Enabled && _registeredSkills.Contains(entry.Skill);

        private void Update()
        {
            if (!IsBusy) return;
            if (!Ready || !IsSkillEnabled(_activeSkill) ||
                (!HasCommittedAttack && (_castTarget == null ||
                !_castTarget.gameObject.activeInHierarchy || _enemy.Target != _castTarget)))
            {
                StopAttack();
                return;
            }
            if (_activeSkill is IChargeSkill { IsCharging: true } && Time.time >= _chargeReleaseTime)
                UseSkill(_activeSkill.SkillSo, false);
        }

        public void StopAttack()
        {
            ForceStopCurrentSkill();
            _activeSkill = null;
            _castTarget = null;
        }

        public void OnAttackWarning()
        {
            if (Ready && ActiveSkill is EnemyAttackSkill skill) skill.OnAttackWarning();
        }

        public void OnAttackHit()
        {
            if (Ready && ActiveSkill is EnemyAttackSkill skill) skill.OnAttackHit();
        }

        public void OnAttackFinished()
        {
            if (Ready && ActiveSkill is EnemyAttackSkill skill) skill.OnAttackFinished();
        }

        private void OnDisable() => StopAttack();
    }
}
