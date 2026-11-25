using System.Collections.Generic;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.DevLib.BattleSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.Agent.Enemies.Bosses
{
    [DisallowMultipleComponent]
    public sealed class ProductionControlDrone : BossBase
    {
        private enum State { Dormant, ChooseGround, GroundAttack, Ascending, ChooseAir, AirAttack, Descend, Recover, Return,
            LastStandEntry, EntryLaser, EntryFall, Groggy, GroggyRecover, GroggyRest }

        [SerializeField] private DroneChargeSkill charge;
        [SerializeField] private DroneSalvoSkill rapidFire;
        [SerializeField] private DroneAirTransitionSkill airTransition;
        [SerializeField] private DroneDownwardLaserSkill laser;
        [SerializeField] private DroneBombSkill bomb;
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private DroneLastStandEntrySkill lastStandEntry;

        [Header("Last Stand Landing")]
        [SerializeField, Min(0.1f)] private float entryFallSpeed = 20f;
        [Tooltip("정지 상태에서 낙하를 시작해 최고 낙하 속도까지 가속합니다.")]
        [SerializeField, Min(0.1f)] private float entryFallAcceleration = 32f;
        [SerializeField, Min(0f)] private float groggyDuration = 5f;
        [SerializeField, Min(1f)] private float groggyDamageMultiplier = 2f;
        [Tooltip("일어나는 모션이 끝난 다음 공격을 재개하기 전의 여유 시간입니다.")]
        [SerializeField, Min(0f)] private float groggyRecoveryPause = 0.65f;

        [Header("Pattern / Flight")]
        [SerializeField, Range(0f, 1f)] private float lastStandHealthRatio = 0.35f;
        [SerializeField, Min(0.1f)] private float lowAltitude = 0.9f;
        [Tooltip("레이저와 폭격이 함께 사용하는 바닥 기준 공중 높이입니다.")]
        [SerializeField, Min(0.1f)] private float airAttackAltitude = 4.5f;
        [SerializeField, Min(0.1f)] private float ascentSpeed = 10f;
        [SerializeField, Min(0.1f)] private float descentSpeed = 10f;
        [SerializeField, Min(0.1f)] private float arenaEdgeMargin = 1.05f;
        [Tooltip("각 패턴이 끝난 뒤 기본 자세에서 멈춰 플레이어에게 반격 시간을 줍니다.")]
        [SerializeField, FormerlySerializedAs("airRecoveryTime"), Min(0f)] private float patternRecoveryTime = 1.25f;
        [Tooltip("스킬 사이 지상 이동은 아레나 중심으로 향합니다. 도착하기 전에도 다음 스킬을 시작합니다.")]
        [SerializeField, Min(0.01f)] private float centerArrivalTolerance = 0.15f;
        [SerializeField] private LayerMask terrainLayers = (1 << 6) | (1 << 10);

        private readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[16];
        private readonly List<EnemyAttackSkill> _candidates = new List<EnemyAttackSkill>(3);
        private BossArenaModule _arena;
        private DroneLastStandFeedbackModule _lastStandFeedback;
        private State _state;
        private Vector2 _bodyOffset;
        private Vector2 _bodySize;
        private bool _lastStandEntryPending;
        private bool _entryLaserActive;
        private bool _airPatternIsBomb;
        private bool _resumeAirAfterRecovery;
        private float _recoverUntil;
        private float _nextNavigationAt;
        private float _groggyUntil;
        private float _entryFallVelocity;
        private bool _entryLanded;

        protected override bool UpdatesWithoutCombatTarget => true;
        public bool IsLastStand { get; private set; }
        public bool IsGroggy => _state == State.Groggy;
        internal bool IsEntryShieldReady => _lastStandFeedback != null && _lastStandFeedback.IsShieldReady;
        internal bool IsEntryShieldForming => _lastStandFeedback != null && _lastStandFeedback.IsShieldForming;
        internal bool UseLastStandPatterns => IsLastStand && (!_lastStandEntryPending || _entryLaserActive);
        internal bool IsLastStandEntry => _lastStandEntryPending || _entryLaserActive;
        internal float FloorY => _arena.FloorY;
        internal LayerMask TerrainLayers => terrainLayers;
        internal BossArenaModule Arena => _arena;
        internal bool IsMoving => (_state == State.EntryFall && !_entryLanded) || (Flight != null && Flight.IsMoving);
        internal bool IsSalvoMoving => IsMoving && rapidFire != null && rapidFire.IsUsing &&
            !rapidFire.IsRecovering && rapidFire.DidFire;
        internal Bounds ContactBounds => new Bounds((Vector2)transform.position + _bodyOffset, _bodySize);
        private Vector2 Home => new Vector2(_arena.Center.x, FloorY + lowAltitude);

        protected override void InitializeBoss()
        {
            _arena = GetModule<BossArenaModule>();
            _lastStandFeedback = GetModule<DroneLastStandFeedbackModule>();
            var body = GetComponent<BoxCollider2D>();

            if (_arena == null || Flight == null || Skills == null || visual == null || body == null ||
                lastStandEntry == null || laser == null || _lastStandFeedback == null)
            {
                Debug.LogError("ProductionControlDrone requires Arena, Flight, Skill, Visual, LastStandEntry, Laser and BoxCollider2D.", this);
                enabled = false;

                return;
            }

            _bodyOffset = transform.TransformVector(body.offset);
            Vector3 scale = transform.lossyScale;

            _bodySize = new Vector2(body.size.x * Mathf.Abs(scale.x), body.size.y * Mathf.Abs(scale.y));
            _arena.SetLaserHeightFromFloor(airAttackAltitude);
            Flight.Place(Home);
        }

        protected override void UpdateBoss()
        {
            if (_arena == null || Flight == null || Skills == null) return;

            if (!IsLastStand && NewHealthModule != null && NewHealthModule.MaxHealth > 0f &&
                (NewHealthModule.CurrentHealth <= NewHealthModule.MaxHealth * lastStandHealthRatio ||
                 Mathf.Approximately(NewHealthModule.CurrentHealth, NewHealthModule.MaxHealth * lastStandHealthRatio)))
            {
                IsLastStand = true;
                _lastStandEntryPending = true;
                Skills.StopAttack();
                Flight.Stop();
                Change(State.LastStandEntry);
            }

            bool canPursue = HasCombatTarget && IsTargetInRange(ChaseReleaseRange);

            switch (_state)
            {
                case State.LastStandEntry:
                    if (lastStandEntry != null && lastStandEntry.DidComplete && !Skills.IsBusy)
                        Change(State.EntryLaser);
                    else if (!Skills.IsBusy && Ready(lastStandEntry))
                        Skills.TryUseSpecifiedSkill(lastStandEntry);

                    break;

                case State.EntryLaser:
                    if (_entryLaserActive && !Skills.IsBusy)
                    {
                        _entryLaserActive = false;
                        _lastStandEntryPending = false;
                        Change(State.EntryFall);
                    }
                    else if (!_entryLaserActive && Ready(laser))
                    {
                        _entryLaserActive = true;
                        if (!Skills.TryUseSpecifiedSkill(laser)) _entryLaserActive = false;
                    }

                    break;

                case State.EntryFall:
                    // 빠른 하강이 실제로 바닥 높이에 도착한 다음부터 5초를 셉니다.
                    if (_entryLanded) Change(State.Groggy);

                    break;

                case State.Groggy:
                    if (Time.time >= _groggyUntil) Change(State.GroggyRecover);

                    break;

                case State.GroggyRecover:
                    if (visual.HasFinished("GroggyRecover")) Change(State.GroggyRest);

                    break;

                case State.GroggyRest:
                    if (Time.time >= _recoverUntil) Change(canPursue ? State.ChooseGround : State.Return);

                    break;

                case State.Dormant:
                    if (HasCombatTarget && IsTargetInRange(DetectionRange)) Change(State.ChooseGround);

                    break;

                case State.ChooseGround:
                    if (!canPursue) Change(State.Return);
                    else ChooseGroundPattern();

                    break;

                case State.GroundAttack:
                    if (!Skills.IsBusy)
                    {
                        _resumeAirAfterRecovery = false;
                        Change(canPursue ? State.Recover : State.Return);
                    }

                    break;

                case State.Ascending:
                    if (!Skills.IsBusy)
                    {
                        if (airTransition == null || !airTransition.DidArrive || !canPursue) Change(State.Descend);
                        else
                        {
                            _resumeAirAfterRecovery = true;
                            Change(State.Recover);
                        }
                    }

                    break;

                case State.ChooseAir:
                    if (!canPursue) Change(State.Descend);
                    else ChooseAirPattern();

                    break;

                case State.AirAttack:
                    if (!Skills.IsBusy)
                    {
                        _resumeAirAfterRecovery = false;
                        Change(_airPatternIsBomb && bomb != null && bomb.DidReturnToGround
                            ? (canPursue ? State.Recover : State.Return) : State.Descend);
                    }

                    break;

                case State.Descend:
                    if (Flight.HasArrived && visual.HasFinished("Descend"))
                    {
                        _resumeAirAfterRecovery = false;
                        Change(State.Recover);
                    }

                    break;

                case State.Recover:
                    if (!_resumeAirAfterRecovery) Reposition();
                    if (Time.time >= _recoverUntil)
                        Change(canPursue ? (_resumeAirAfterRecovery ? State.ChooseAir : State.ChooseGround) :
                            (_resumeAirAfterRecovery ? State.Descend : State.Return));

                    break;

                case State.Return:
                    if (canPursue) Change(State.ChooseGround);
                    else if (!Flight.IsMoving) Change(State.Dormant);

                    break;
            }
        }

        private void ChooseGroundPattern()
        {
            _candidates.Clear();

            if (Ready(charge)) _candidates.Add(charge);
            if (Ready(rapidFire)) _candidates.Add(rapidFire);
            if (Ready(airTransition) && AirAttackReadyAtDestination(false)) _candidates.Add(airTransition);

            if (_candidates.Count > 0) StartGround(_candidates[Random.Range(0, _candidates.Count)]);
            else Reposition();
        }

        private bool Ready(EnemyAttackSkill skill) =>
            skill != null && Skills.IsSkillEnabled(skill) && Skills.CanUseSkill(skill.SkillSo);

        private bool AirAttackReadyAtDestination(bool entry)
        {
            if (!TryPlanAirTransition(out Vector2 point)) return false;
            if (laser != null && Skills.IsSkillEnabled(laser) && laser.RemainingCoolTime <= 0f && laser.CanPlanFrom(point)) return true;

            return !entry && bomb != null && Skills.IsSkillEnabled(bomb) && bomb.RemainingCoolTime <= 0f &&
                bomb.CanPlanFrom(point, Target);
        }

        private void StartGround(EnemyAttackSkill skill)
        {
            if (!Skills.TryUseSpecifiedSkill(skill) || !Skills.IsBusy) return;

            Change(skill == airTransition ? State.Ascending : State.GroundAttack);
        }

        private void ChooseAirPattern()
        {
            _candidates.Clear();

            if (Ready(laser)) _candidates.Add(laser);
            if (!_lastStandEntryPending && Ready(bomb)) _candidates.Add(bomb);

            if (_candidates.Count == 0) return;

            var selected = _candidates[Random.Range(0, _candidates.Count)];
            if (!Skills.TryUseSpecifiedSkill(selected) || !Skills.IsBusy) return;

            _entryLaserActive = _lastStandEntryPending && selected == laser;
            _airPatternIsBomb = selected == bomb;
            Change(State.AirAttack);
        }

        internal bool TryPlanAirTransition(out Vector2 destination)
        {
            destination = new Vector2(_arena.Center.x, FloorY + airAttackAltitude);

            return HasAirPath(destination);
        }

        internal bool HasAirPath(Vector2 destination)
        {
            Vector2 from = transform.position;
            Vector2 delta = destination - from;
            var filter = new ContactFilter2D { useTriggers = false };

            filter.SetLayerMask(terrainLayers);
            int count = Physics2D.CircleCast(from, 0.4f, delta.normalized, filter, _terrainHits, delta.magnitude);

            for (int i = 0; i < count; i++)
                if (!OneWay(_terrainHits[i].collider) && _terrainHits[i].collider.GetComponentInParent<Agent>() == null) return false;

            return true;
        }

        internal bool TryPlanGroundMove(float direction, float distance, out Vector2 destination)
        {
            Vector2 from = transform.position;
            Vector2 requested = new Vector2(ClampX(from.x + Mathf.Sign(direction) * Mathf.Max(0f, distance)), Home.y);

            destination = ConstrainGroundSegment(from, requested);

            return Mathf.Abs(destination.x - from.x) > 0.05f;
        }

        private Vector2 ConstrainGroundSegment(Vector2 from, Vector2 requested)
        {
            float offset = ClampX(requested.x) - from.x;
            float direction = Mathf.Sign(offset);
            float allowed = Mathf.Abs(offset);
            if (allowed < 0.0001f) return new Vector2(from.x, Home.y);

            var filter = new ContactFilter2D { useTriggers = false };

            filter.SetLayerMask(terrainLayers);
            int count = Physics2D.BoxCast(from + _bodyOffset, _bodySize * 0.95f, 0f, Vector2.right * direction,
                filter, _terrainHits, allowed);

            for (int i = 0; i < count; i++)
            {
                var hit = _terrainHits[i];
                if (OneWay(hit.collider) || hit.collider.GetComponentInParent<Agent>() != null) continue;

                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.025f));
            }

            float safe = 0f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(allowed / 0.15f));

            for (int i = 1; i <= steps; i++)
            {
                float travel = allowed * i / steps;
                float probeX = from.x + direction * (travel + _bodySize.x * 0.35f);
                int hits = Physics2D.Raycast(new Vector2(probeX, FloorY + 0.3f), Vector2.down, filter, _terrainHits, 0.65f);
                bool supported = false;

                for (int h = 0; h < hits; h++)
                    if (_terrainHits[h].normal.y > 0.5f) { supported = true; break; }

                if (!supported) break;

                safe = travel;
            }

            return new Vector2(from.x + direction * safe, Home.y);
        }

        private static bool OneWay(Collider2D collider) => collider != null && collider.usedByEffector &&
            collider.TryGetComponent(out PlatformEffector2D effector) && effector.useOneWay;

        internal void MoveGround(Vector2 destination, float speed, float? ease = null) => Flight.MoveTo(destination, speed, ease);

        internal void MoveGroundForDuration(Vector2 destination, float duration) => Flight.MoveToDuration(destination, duration);

        internal void MoveToAir(Vector2 destination, float speedMultiplier = 1f) => Flight.MoveTo(destination, ascentSpeed * speedMultiplier);

        internal void BeginPatternDescent()
        {
            Vector2 destination = new Vector2(transform.position.x, Home.y);
            Flight.MoveTo(destination, descentSpeed);
            visual.Play("Descend");
        }

        internal void BeginEntryShield()
        {
            Flight.Stop();
            visual.SnapToBody();
            _lastStandFeedback.BeginShieldFormation();
        }

        protected override float ModifyIncomingDamage(DamageData damage, Vector2 hitPoint, Vector2 hitDirection,
            out bool suppressHitControl)
        {
            suppressHitControl = true;

            return damage.DamageAmount * (IsGroggy ? groggyDamageMultiplier : 1f);
        }

        internal void StopMovement() => Flight?.Stop();

        internal void TickAirMovement() => Flight?.Tick();

        internal void TickGroundMovement()
        {
            Vector2 before = transform.position;

            Flight.Tick();
            Vector2 after = transform.position;
            if (Mathf.Abs(after.x - before.x) < 0.0001f) return;

            Vector2 safe = ConstrainGroundSegment(before, after);

            if (Mathf.Abs(safe.x - after.x) > 0.001f) Flight.Place(safe);
        }

        private void Reposition()
        {
            if (Flight.IsMoving || Time.time < _nextNavigationAt) return;

            float difference = Home.x - transform.position.x;

            if (Mathf.Abs(difference) <= centerArrivalTolerance) { FaceTarget(); return; }

            if (TryPlanGroundMove(Mathf.Sign(difference), Mathf.Abs(difference), out Vector2 destination))
            {
                OnMoveDirectionChanged(destination.x - transform.position.x);
                MoveGround(destination, MoveSpeed);
            }

            _nextNavigationAt = Time.time + 0.15f;
        }

        internal float ClampX(float x)
        {
            float margin = Mathf.Min(arenaEdgeMargin, (_arena.LaserRight.x - _arena.LaserLeft.x) * 0.45f);

            return Mathf.Clamp(x, _arena.LaserLeft.x + margin, _arena.LaserRight.x - margin);
        }

        private void Change(State state)
        {
            _state = state;

            if (state == State.LastStandEntry)
            {
                IsInvulnerable = true;
                _lastStandFeedback.ResetShield();
                _resumeAirAfterRecovery = false;
                visual.Play("Idle");
            }
            else if (state == State.EntryLaser)
            {
                // PrepareAttack에서 강화 수치로 읽도록 지정 실행 전에 설정합니다.
                _entryLaserActive = true;
                if (!Skills.TryUseSpecifiedSkill(laser)) _entryLaserActive = false;
            }
            else if (state == State.EntryFall)
            {
                Flight.Stop();
                _entryFallVelocity = 0f;
                _entryLanded = false;
                visual.Play("Idle");
            }
            else if (state == State.Groggy)
            {
                IsInvulnerable = false;
                _lastStandFeedback.BeginShieldShutdown();
                Flight.Stop();
                visual.SnapToBody();
                visual.Play("GroggyHold");
                _groggyUntil = Time.time + groggyDuration;
                _lastStandFeedback?.PlayLandingImpact();
            }
            else if (state == State.GroggyRecover)
            {
                Flight.Stop();
                visual.Play("GroggyRecover");
            }
            else if (state == State.GroggyRest)
            {
                Flight.Stop();
                visual.Play("Idle");
                _recoverUntil = Time.time + groggyRecoveryPause;
            }
            else if (state == State.Descend)
            {
                Flight.MoveTo(new Vector2(transform.position.x, Home.y), descentSpeed);
                visual.Play("Descend");
            }
            else if (state == State.Recover)
            {
                Flight.Stop();
                visual.Play(_resumeAirAfterRecovery ? "AirIdle" : "Idle");
                _recoverUntil = Time.time + patternRecoveryTime;
                if (!_resumeAirAfterRecovery) Reposition();
            }
            else if (state == State.Return)
            {
                Vector2 destination = ConstrainGroundSegment(transform.position, Home);

                Flight.MoveTo(destination, MoveSpeed);
                visual.Play("Idle");
            }
            else if (state == State.Dormant)
            {
                Flight.Stop();
                visual.Play("Idle");
            }
        }

        protected override void FixedUpdateBoss()
        {
            if (_state == State.EntryFall)
            {
                _entryFallVelocity = Mathf.MoveTowards(_entryFallVelocity, entryFallSpeed, entryFallAcceleration * Time.fixedDeltaTime);
                Vector2 destination = new Vector2(transform.position.x, Home.y);
                Vector2 position = Vector2.MoveTowards(transform.position, destination, _entryFallVelocity * Time.fixedDeltaTime);
                Flight.Place(position);
                _entryLanded = Vector2.Distance(position, destination) <= 0.001f;

                return;
            }

            if (_state == State.Descend || _state == State.AirAttack || _state == State.ChooseAir ||
                _state == State.LastStandEntry || _state == State.EntryLaser) TickAirMovement();
            else TickGroundMovement();
        }

        protected override void OnCombatInterrupted()
        {
            if (_state == State.LastStandEntry || _state == State.EntryLaser) _lastStandEntryPending = false;
            _entryLaserActive = false;
            IsInvulnerable = false;
            _lastStandFeedback?.ResetShield();

            if (isActiveAndEnabled && !IsDead && _arena != null && Flight != null && visual != null)
                Change(transform.position.y > Home.y + 0.2f ? State.Descend : State.Return);
        }
    }
}
