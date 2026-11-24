using System.Collections.Generic;
using _Scripts.Agent.Enemies.Skills;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Bosses
{
    [DisallowMultipleComponent]
    public sealed class ProductionControlDrone : BossBase
    {
        private enum State { Dormant, Engage, GroundAttack, Ascend, AirAttack, Descend, Recover, Return }

        private enum Pattern { Salvo, Laser, Bomb }

        [SerializeField] private DroneSalvoSkill regularSalvo;
        [SerializeField] private DroneSalvoSkill rapidSalvo;
        [SerializeField] private DroneDownwardLaserSkill laser;
        [SerializeField] private DroneBombSkill bomb;
        [SerializeField] private DroneVisualModule visual;
        [SerializeField, Range(0f, 1f)] private float airPhaseHealthRatio = 0.5f;
        [SerializeField, Min(0.1f)] private float lowAltitude = 1.05f;
        [SerializeField, Min(0.1f)] private float highAltitude = 4.55f;
        [Tooltip("부채꼴 레이저를 준비할 때의 바닥 기준 높이입니다. 폭탄의 High Altitude와 별도입니다.")]
        [SerializeField, Min(0.1f)] private float laserAltitude = 6.5f;
        [Tooltip("레이저와 폭탄을 준비할 때의 상승 이동 속도입니다. 지상 이동·하강 속도와 별도입니다.")]
        [SerializeField, Min(0.1f)] private float ascentSpeed = 6f;
        [SerializeField, Min(0.1f)] private float arenaEdgeMargin = 1.05f;
        [SerializeField, Min(0f)] private float airRecoveryTime = 1.2f;
        [SerializeField] private LayerMask terrainLayers = 1 << 6;

        private BossArenaModule _arena;
        private State _state;
        private EnemyAttackSkill _airSkill;

        private bool _nextRapid;
        private bool _phaseLaserPending;
        private float _nextSalvoAt;
        private float _recoverUntil;
        private float _nextNavigationAt;

        private readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[1];
        private readonly List<Pattern> _availablePatterns = new List<Pattern>(3);

        protected override bool UpdatesWithoutCombatTarget => true;
        public bool IsAirPhase { get; private set; }
        internal float FloorY => _arena.FloorY;
        internal LayerMask TerrainLayers => terrainLayers;
        internal BossArenaModule Arena => _arena;
        private Vector2 Home => new Vector2(_arena.Center.x, FloorY + lowAltitude);

        protected override void InitializeBoss()
        {
            _arena = GetModule<BossArenaModule>();

            if (_arena == null || Flight == null || Skills == null || visual == null)
            {
                Debug.LogError("ProductionControlDrone requires Arena, Flight, Skill and DroneVisual modules.", this);
                enabled = false;

                return;
            }

            Flight.Place(Home);
        }

        protected override void UpdateBoss()
        {
            if (_arena == null || Flight == null || Skills == null) return;

            if (!IsAirPhase && NewHealthModule != null && NewHealthModule.MaxHealth > 0f &&
                NewHealthModule.CurrentHealth <= NewHealthModule.MaxHealth * airPhaseHealthRatio)
            {
                IsAirPhase = true;
                _phaseLaserPending = true;
            }

            if (_phaseLaserPending && _state == State.AirAttack && _airSkill == laser && laser.DidFire)
                _phaseLaserPending = false;

            bool canPursue = HasCombatTarget && IsTargetInRange(ChaseReleaseRange);

            switch (_state)
            {
                case State.Dormant:
                    if (HasCombatTarget && IsTargetInRange(DetectionRange)) Change(State.Engage);

                    break;

                case State.Engage:
                    if (!canPursue) Change(State.Return);
                    else SelectAttack();

                    break;

                case State.GroundAttack:
                    if (!Skills.IsBusy)
                    {
                        var salvo = _nextRapid ? rapidSalvo : regularSalvo;

                        if (salvo != null && salvo.DidFire)
                        {
                            _nextSalvoAt = salvo.CoolTimeStartTime + salvo.CoolTime;
                            _nextRapid = !_nextRapid;
                        }

                        Change(canPursue ? State.Engage : State.Return);
                    }

                    break;

                case State.Ascend:
                    if (!canPursue) Change(State.Return);
                    else if (Flight.HasArrived)
                    {
                        if (Skills.TryUseSpecifiedSkill(_airSkill) && Skills.IsBusy)
                            Change(State.AirAttack);
                        else
                            Change(State.Descend);
                    }

                    break;

                case State.AirAttack:
                    if (!Skills.IsBusy)
                    {
                        Change(State.Descend);
                    }

                    break;

                case State.Descend:
                    if (Flight.HasArrived && visual.HasFinished("Descend")) Change(State.Recover);

                    break;

                case State.Recover:
                    if (Time.time >= _recoverUntil) Change(canPursue ? State.Engage : State.Return);

                    break;

                case State.Return:
                    if (Flight.HasArrived) Change(State.Dormant);

                    break;
            }
        }

        private void SelectAttack()
        {
            float horizontalDistance = Mathf.Abs(Target.position.x - transform.position.x);
            var salvo = _nextRapid ? rapidSalvo : regularSalvo;

            // 전환 레이저를 실제로 발사하기 전에는 무작위 패턴을 선택하지 않습니다.
            if (_phaseLaserPending)
            {
                if (laser == null || !Skills.IsSkillEnabled(laser)) _phaseLaserPending = false;
                else
                {
                    if (!TryAscend(laser)) Reposition(salvo != null ? salvo.Range : 8f, horizontalDistance);

                    return;
                }
            }

            if (!IsAirPhase)
            {
                if (!TrySalvo(salvo, horizontalDistance)) Reposition(salvo != null ? salvo.Range : 8f, horizontalDistance);

                return;
            }

            _availablePatterns.Clear();

            if (CanUseSalvo(salvo, horizontalDistance)) _availablePatterns.Add(Pattern.Salvo);
            if (TryPlanAscent(laser, out Vector2 laserPoint)) _availablePatterns.Add(Pattern.Laser);
            if (TryPlanAscent(bomb, out Vector2 bombPoint)) _availablePatterns.Add(Pattern.Bomb);

            if (_availablePatterns.Count == 0)
            {
                Reposition(salvo != null && Skills.IsSkillEnabled(salvo) ? salvo.Range : bomb != null ? bomb.Range : 6f,
                    horizontalDistance);

                return;
            }

            switch (_availablePatterns[Random.Range(0, _availablePatterns.Count)])
            {
                case Pattern.Salvo:
                    TrySalvo(salvo, horizontalDistance);
                    break;

                case Pattern.Laser:
                    StartAscent(laser, laserPoint);
                    break;

                case Pattern.Bomb:
                    StartAscent(bomb, bombPoint);
                    break;
            }
        }

        private bool CanUseSalvo(DroneSalvoSkill salvo, float horizontalDistance) =>
            salvo != null && horizontalDistance <= salvo.Range && Time.time >= _nextSalvoAt &&
            Skills.IsSkillEnabled(salvo) && Skills.CanUseSkill(salvo.SkillSo);

        private bool TrySalvo(DroneSalvoSkill salvo, float horizontalDistance)
        {
            if (!CanUseSalvo(salvo, horizontalDistance) || !Skills.TryUseSpecifiedSkill(salvo) || !Skills.IsBusy) return false;

            Change(State.GroundAttack);

            return true;
        }

        private bool TryAscend(EnemyAttackSkill skill)
        {
            if (!TryPlanAscent(skill, out Vector2 highPoint)) return false;

            StartAscent(skill, highPoint);

            return true;
        }

        private bool TryPlanAscent(EnemyAttackSkill skill, out Vector2 highPoint)
        {
            highPoint = default;

            if (skill == null || !Skills.IsSkillEnabled(skill) || skill.RemainingCoolTime > 0f) return false;

            float x = skill == laser ? _arena.Center.x : transform.position.x;
            if (skill == bomb && Mathf.Abs(Target.position.x - x) > bomb.Range) return false;

            highPoint = new Vector2(x, FloorY + (skill == laser ? laserAltitude : highAltitude));

            if (skill == laser && !laser.CanPlanFrom(highPoint)) return false;
            if (skill == bomb && !bomb.CanPlanFrom(highPoint, Target)) return false;

            Vector2 from = transform.position;
            Vector2 offset = highPoint - from;
            var filter = new ContactFilter2D { useTriggers = false };

            filter.SetLayerMask(terrainLayers);

            if (Physics2D.CircleCast(from, 0.4f, offset.normalized, filter, _terrainHits, offset.magnitude) > 0)
                return false;

            return true;
        }

        private void StartAscent(EnemyAttackSkill skill, Vector2 highPoint)
        {
            Flight.MoveTo(highPoint, ascentSpeed);
            _airSkill = skill;
            Change(State.Ascend);
        }

        private void Reposition(float range, float horizontalDistance)
        {
            if (horizontalDistance <= range)
            {
                Flight.Stop();
                FaceTarget();

                return;
            }

            if (Flight.IsMoving || Time.time < _nextNavigationAt) return;

            float direction = Mathf.Sign(Target.position.x - transform.position.x);
            Vector2 point = new Vector2(ClampX(Target.position.x - direction * range * 0.72f), Home.y);

            _nextNavigationAt = Time.time + 0.15f;
            OnMoveDirectionChanged(point.x - transform.position.x);
            Flight.MoveTo(point, MoveSpeed);
        }

        internal float ClampX(float x)
        {
            float margin = Mathf.Min(arenaEdgeMargin, (_arena.LaserRight.x - _arena.LaserLeft.x) * 0.45f);

            return Mathf.Clamp(x, _arena.LaserLeft.x + margin, _arena.LaserRight.x - margin);
        }

        private void Change(State state)
        {
            _state = state;

            switch (state)
            {
                case State.GroundAttack:
                    Flight.Stop();
                    break;

                case State.Ascend:
                    FaceTarget();
                    visual.Play("Rise");
                    break;

                case State.Descend:
                    Flight.MoveTo(new Vector2(transform.position.x, Home.y), MoveSpeed);
                    visual.Play("Descend");
                    break;

                case State.Recover:
                    visual.Play("Idle");
                    _recoverUntil = Time.time + airRecoveryTime;
                    break;

                case State.Return:
                    Flight.MoveTo(Home, MoveSpeed);
                    visual.Play("Descend");
                    break;

                case State.Dormant:
                    Flight.Stop();
                    visual.Play("Idle");
                    break;
            }
        }

        protected override void FixedUpdateBoss() => Flight?.Tick();

        protected override void OnCombatInterrupted()
        {
            if (_phaseLaserPending && _airSkill == laser && laser != null && laser.DidFire) _phaseLaserPending = false;

            if (isActiveAndEnabled && !IsDead && _arena != null && Flight != null && visual != null) Change(State.Return);
        }
    }
}
