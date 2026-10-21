using Lrw.Script._Core._FSM;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    public enum RobotEyeBossState { Dormant, Intro, Waiting, RadialVolley, MoveToTarget, LaserPositioning, LaserSweep, ReturnCenter }

    public sealed class RobotEyeBoss : BossBase
    {
        [Header("Pattern")]
        [SerializeField] private BossRadialVolleySkill radialVolley;
        [SerializeField] private BossLaserSkill laser;
        [SerializeField, Min(0f)] private float firstVolleyDelay = 10f;
        [SerializeField, Min(0f)] private float normalRestTime = 3f;
        [SerializeField, Min(0.01f)] private float flightSpeed = 4f;
        [Tooltip("원형 탄막 후 플레이어 위치를 향해 이동할 때의 속도입니다. 등장·복귀·레이저 이동에는 적용하지 않습니다.")]
        [SerializeField, Min(0.01f)] private float targetMoveSpeed = 8f;
        [SerializeField, Min(0.01f)] private float laserPositionSpeed = 12f;
        [SerializeField, Min(0.01f)] private float laserSweepSpeed = 20f;
        [SerializeField, Min(0f)] private float laserHealthThreshold = 1000f;
        private StateMachine<RobotEyeBossState> _fsm;
        private BossArenaModule _arena;
        private Vector2 _moveDestination;
        private Vector2 _sweepDestination;
        private float _nextVolleyAt;
        private bool _introFinished;
        private bool _skillStarted;
        private bool _sweepMoving;
        public RobotEyeBossState CurrentState { get; private set; }
        public bool HasUsedLaserPhase { get; private set; }
        public int CompletedTraversals { get; private set; }
        public Vector2 ArenaCenter => _arena.Center;
        public Vector2 LockedMoveDestination => _moveDestination;
        protected override void InitializeBoss()
        {
            _arena = GetModule<BossArenaModule>();
            _nextVolleyAt = Time.time + Mathf.Max(0f, firstVolleyDelay);
            _fsm = new StateMachine<RobotEyeBossState>();
            foreach (RobotEyeBossState state in System.Enum.GetValues(typeof(RobotEyeBossState)))
                _fsm.AddState(state, new PatternState(this, state));
            if (Flight == null || _arena == null || Skills == null || radialVolley == null || laser == null)
            {
                Debug.LogError($"{name}: Flight / Arena / SkillModule / 두 보스 스킬 참조를 확인하세요.", this);
                enabled = false;
                return;
            }
            Flight.Place(_arena.IntroPosition);
            Change(RobotEyeBossState.Dormant);
        }

        protected override void UpdateBoss()
        {
            if (TryBeginLaserPhase()) return;
            _fsm.Update();
        }

        protected override void FixedUpdateBoss() => _fsm.FixedUpdate();

        protected override void OnHealthChanged(float current, float delta, float max)
        {
            // EnemyBase가 먼저 사망을 확정합니다. 치명타로 0이 됐다면 특수 패턴으로 살려내지 않습니다.
            if (!IsDead && isActiveAndEnabled && HasCombatTarget) TryBeginLaserPhase();
        }

        private bool TryBeginLaserPhase()
        {
            if (IsDead || HasUsedLaserPhase || HealthModule == null || HealthModule.CurrentHealth > laserHealthThreshold ||
                !Skills.IsSkillEnabled(laser))
                return false;
            HasUsedLaserPhase = true; // 취소/재진입/추가 피격보다 먼저 1회 사용을 확정합니다.
            _introFinished = true;
            Skills.StopAttack();
            ClearProjectiles();
            Flight.Stop();
            IsInvulnerable = true;
            CompletedTraversals = 0;
            Change(RobotEyeBossState.LaserPositioning);
            return true;
        }

        protected override void OnCombatInterrupted()
        {
            if (_fsm != null) Change(RobotEyeBossState.Dormant);
        }

        private void Change(RobotEyeBossState state)
        {
            CurrentState = state;
            _fsm.ChangeState(state);
        }

        private void Enter(RobotEyeBossState state)
        {
            switch (state)
            {
                case RobotEyeBossState.Dormant:
                    Flight?.Stop();
                    break;
                case RobotEyeBossState.Intro:
                    Flight.MoveTo(_arena.Center, flightSpeed);
                    break;
                case RobotEyeBossState.Waiting:
                    Flight.Stop();
                    break;
                case RobotEyeBossState.RadialVolley:
                    Flight.Stop();
                    _moveDestination = _arena.ClampFlightDestination(Target.position);
                    _skillStarted = Skills.TryUseSpecifiedSkill(radialVolley);
                    break;
                case RobotEyeBossState.MoveToTarget:
                    Flight.MoveTo(_moveDestination, targetMoveSpeed);
                    break;
                case RobotEyeBossState.LaserPositioning:
                    Flight.MoveTo(_arena.GetLaserStart(0), laserPositionSpeed);
                    break;
                case RobotEyeBossState.LaserSweep:
                    Flight.Stop();
                    _sweepMoving = false;
                    _sweepDestination = _arena.GetLaserEnd(CompletedTraversals);
                    laser.ConfigureTraversal(_arena.GetLaserDangerBounds(CompletedTraversals), transform.position, _sweepDestination);
                    _skillStarted = Skills.TryUseSpecifiedSkill(laser);
                    break;
                case RobotEyeBossState.ReturnCenter:
                    Skills.StopAttack();
                    Flight.MoveTo(_arena.Center, flightSpeed);
                    break;
            }
        }

        private void Tick(RobotEyeBossState state)
        {
            switch (state)
            {
                case RobotEyeBossState.Dormant:
                    Change(_introFinished ? RobotEyeBossState.Waiting : RobotEyeBossState.Intro);
                    break;
                case RobotEyeBossState.Intro:
                    if (Flight.HasArrived) { _introFinished = true; Change(RobotEyeBossState.Waiting); }
                    break;
                case RobotEyeBossState.Waiting:
                    if (Time.time >= _nextVolleyAt) Change(RobotEyeBossState.RadialVolley);
                    break;
                case RobotEyeBossState.RadialVolley:
                    if (!_skillStarted && !Skills.IsSkillEnabled(radialVolley)) Rest();
                    else if (!_skillStarted) _skillStarted = Skills.TryUseSpecifiedSkill(radialVolley);
                    else if (!Skills.IsBusy)
                    {
                        if (radialVolley.DidFire) Change(RobotEyeBossState.MoveToTarget);
                        else Rest();
                    }
                    break;
                case RobotEyeBossState.MoveToTarget:
                    if (Flight.HasArrived) Rest();
                    break;
                case RobotEyeBossState.LaserPositioning:
                    if (Flight.HasArrived) Change(RobotEyeBossState.LaserSweep);
                    break;
                case RobotEyeBossState.LaserSweep:
                    if (!_skillStarted && !Skills.IsSkillEnabled(laser)) Change(RobotEyeBossState.ReturnCenter);
                    else if (!_skillStarted) _skillStarted = Skills.TryUseSpecifiedSkill(laser);
                    else if (!Skills.IsBusy) Change(RobotEyeBossState.ReturnCenter);
                    else if (laser.IsFiring && !_sweepMoving)
                    {
                        _sweepMoving = true;
                        Flight.MoveTo(_sweepDestination, laserSweepSpeed);
                    }
                    break;
                case RobotEyeBossState.ReturnCenter:
                    if (Flight.HasArrived) { IsInvulnerable = false; Rest(); }
                    break;
            }
        }

        private void FixedTick(RobotEyeBossState state)
        {
            Flight.Tick();
            if (state != RobotEyeBossState.LaserSweep || !laser.IsFiring) return;
            laser.TickBeam();
            if (!_sweepMoving || !Flight.HasArrived) return;
            laser.FinishTraversal();
            CompletedTraversals++;
            Change(CompletedTraversals < 3 ? RobotEyeBossState.LaserSweep : RobotEyeBossState.ReturnCenter);
        }

        private void Rest()
        {
            _nextVolleyAt = Time.time + Mathf.Max(0f, normalRestTime);
            Change(RobotEyeBossState.Waiting);
        }

        // 공용 StateMachine을 그대로 사용하되, 이 보스의 상태만 전용 열거형으로 등록합니다.
        private sealed class PatternState : AbstractState
        {
            private readonly RobotEyeBoss _boss;
            private readonly RobotEyeBossState _state;
            public PatternState(RobotEyeBoss boss, RobotEyeBossState state) { _boss = boss; _state = state; }
            public override void Enter() => _boss.Enter(_state);
            public override void StateUpdate() => _boss.Tick(_state);
            public override void StateFixedUpdate() => _boss.FixedTick(_state);
            public override void Exit() { }
        }
    }
}
