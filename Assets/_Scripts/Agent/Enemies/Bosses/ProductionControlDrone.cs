using _Scripts.Agent.Enemies.Skills;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Bosses
{
    [DisallowMultipleComponent]
    public sealed class ProductionControlDrone : BossBase
    {
        private enum State { Dormant, Engage, GroundAttack, Ascend, AirAttack, Descend, Recover, Return }

        [SerializeField] private DroneSalvoSkill regularSalvo;
        [SerializeField] private DroneSalvoSkill rapidSalvo;
        [SerializeField] private DroneDownwardLaserSkill laser;
        [SerializeField] private DroneBombSkill bomb;
        [SerializeField] private DroneVisualModule visual;
        [SerializeField, Range(0f, 1f)] private float airPhaseHealthRatio = 0.5f;
        [SerializeField, Min(0.1f)] private float lowAltitude = 1.05f;
        [SerializeField, Min(0.1f)] private float highAltitude = 4.55f;
        [SerializeField, Min(0.1f)] private float arenaEdgeMargin = 1.05f;
        [SerializeField, Min(0f)] private float airRecoveryTime = 1.2f;
        [SerializeField] private LayerMask terrainLayers = 1 << 6;

        private BossArenaModule _arena;
        private State _state;
        private EnemyAttackSkill _airSkill;
        private bool _nextRapid;
        private bool _nextBomb;
        private float _nextSalvoAt;
        private float _recoverUntil;
        private float _nextNavigationAt;
        private readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[1];

        protected override bool UpdatesWithoutCombatTarget => true;
        public bool IsAirPhase { get; private set; }
        internal float FloorY => _arena.FloorY;
        internal LayerMask TerrainLayers => terrainLayers;
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
                        _nextBomb = _airSkill == laser;
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
            if (!IsAirPhase && NewHealthModule != null && NewHealthModule.MaxHealth > 0f)
                IsAirPhase = NewHealthModule.CurrentHealth <= NewHealthModule.MaxHealth * airPhaseHealthRatio;

            float horizontalDistance = Mathf.Abs(Target.position.x - transform.position.x);
            if (!IsAirPhase)
            {
                var salvo = _nextRapid ? rapidSalvo : regularSalvo;
                if (salvo != null && horizontalDistance <= salvo.Range && Time.time >= _nextSalvoAt &&
                    Skills.TryUseSpecifiedSkill(salvo) && Skills.IsBusy)
                {
                    Change(State.GroundAttack);
                    return;
                }
                Reposition(salvo != null ? salvo.Range : 8f, horizontalDistance);
                return;
            }

            EnemyAttackSkill first = _nextBomb ? bomb : laser;
            EnemyAttackSkill second = _nextBomb ? laser : bomb;
            if (TryAscend(first) || TryAscend(second)) return;
            Reposition(bomb != null ? bomb.Range : 6f, horizontalDistance);
        }

        private bool TryAscend(EnemyAttackSkill skill)
        {
            if (skill == null || !Skills.IsSkillEnabled(skill) || skill.RemainingCoolTime > 0f) return false;
            float x = skill == laser ? ClampX(Target.position.x) : transform.position.x;
            if (skill == bomb && Mathf.Abs(Target.position.x - x) > bomb.Range) return false;
            Vector2 highPoint = new Vector2(x, FloorY + highAltitude);
            if (skill == bomb && !bomb.CanPlanFrom(highPoint, Target)) return false;
            Vector2 from = transform.position;
            Vector2 offset = highPoint - from;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(terrainLayers);
            if (Physics2D.CircleCast(from, 0.4f, offset.normalized, filter, _terrainHits, offset.magnitude) > 0)
                return false;
            Flight.MoveTo(highPoint, MoveSpeed);
            _airSkill = skill;
            Change(State.Ascend);
            return true;
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
            if (isActiveAndEnabled && !IsDead && _arena != null && Flight != null && visual != null) Change(State.Return);
        }
    }
}
