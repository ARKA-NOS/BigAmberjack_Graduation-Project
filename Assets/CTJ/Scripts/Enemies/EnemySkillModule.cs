using System;
using System.Collections.Generic;
using CTJ.Enemies.Skills;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem;
using Lrw.Script.Agent.SkillSystem.Interface;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using UnityEngine;

namespace CTJ.Enemies
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
            [SerializeField, Min(0f)] private float minRange;
            [SerializeField, Min(0f)] private float maxRange = 1.5f;
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
            public float MaxRange => Mathf.Max(0f, maxRange);
            public float ChargeDuration => Mathf.Max(0f, chargeDuration);

            internal bool Matches(EnemyBase enemy, EnemyMovementModule movement)
            {
                if (!Enabled || movement == null || enemy.Target == null ||
                    !enemy.Target.gameObject.activeInHierarchy || (requireGrounded && !movement.IsGrounded))
                    return false;
                float distance = Vector2.Distance(enemy.transform.position, enemy.Target.position);
                if (distance < Mathf.Max(0f, minRange) || distance > MaxRange ||
                    (maxHeightDifference >= 0f && Mathf.Abs(movement.GetTargetHeightDifference(enemy.Target)) > maxHeightDifference))
                    return false;
                Collider2D body = enemy.Target.GetComponent<Collider2D>();
                Vector2 aim = body != null ? (Vector2)body.bounds.center : (Vector2)enemy.Target.position;
                return !requireClearPath || movement.HasClearPath(origin != null ? (Vector2)origin.position : movement.BodyCenter, aim);
            }
        }

        [Tooltip("위에서부터 우선순위. 조건과 쿨타임을 만족하는 첫 스킬을 사용합니다. 실행 도중 AI가 다른 스킬로 취소하지는 않습니다.")]
        [SerializeField] private List<SkillEntry> skills = new List<SkillEntry>();
        private readonly HashSet<AbstractSkill> _registeredSkills = new HashSet<AbstractSkill>();
        private EnemyBase _enemy;
        private EnemyMovementModule _movement;
        private AbstractSkill _activeSkill;
        private Transform _castTarget;
        private float _chargeReleaseTime;

        public IReadOnlyList<SkillEntry> Skills => skills;
        public AbstractSkill ActiveSkill => IsBusy ? _activeSkill : null;
        public bool IsBusy => _activeSkill != null &&
            (_activeSkill.IsUsing || _activeSkill is IChargeSkill { IsCharging: true });
        private bool Ready => isActiveAndEnabled && _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead;
        public float MaximumRange
        {
            get
            {
                float range = 0f;
                foreach (SkillEntry entry in skills)
                    if (entry != null && entry.Enabled) range = Mathf.Max(range, entry.MaxRange);
                return range;
            }
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _movement = owner.GetModule<EnemyMovementModule>();
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
                if (IsValid(entry) && entry.Matches(_enemy, _movement)) return true;
            return false;
        }

        public bool TryUseAttack()
        {
            if (!Ready || IsBusy || _enemy.IsHitReacting) return false;
            foreach (SkillEntry entry in skills)
            {
                if (!IsValid(entry) || !entry.Matches(_enemy, _movement) || !CanUseSkill(entry.Skill.SkillSo)) continue;
                _activeSkill = entry.Skill;
                _castTarget = _enemy.Target;
                _chargeReleaseTime = Time.time + entry.ChargeDuration;
                UseSkill(_activeSkill.SkillSo, true);
                return true;
            }
            return false;
        }

        private bool IsValid(SkillEntry entry) => entry != null && entry.Enabled && _registeredSkills.Contains(entry.Skill);

        private void Update()
        {
            if (!IsBusy) return;
            if (!Ready || !_activeSkill.isActiveAndEnabled || _castTarget == null ||
                !_castTarget.gameObject.activeInHierarchy || _enemy.Target != _castTarget)
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
