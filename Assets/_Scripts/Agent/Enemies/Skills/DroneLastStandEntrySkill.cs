using System.Collections.Generic;
using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    // 일반 공중 전환과 분리된 발악 진입. 후속 레이저와 착지는 보스 FSM이 이어받습니다.
    public sealed class DroneLastStandEntrySkill : EnemyAttackSkill
    {
        private enum Phase { Rising, FormingShield, Attacking, Centering }

        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private DroneBombSkill bombardment;
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform rearMuzzle;
        [SerializeField, Min(0.1f)] private float riseSpeed = 10f;
        [SerializeField, Min(0.1f)] private float returnSpeed = 8f;
        [SerializeField, Min(0.1f)] private float actionInterval = 0.65f;
        [SerializeField, Range(1f, 160f)] private float shotgunAngle = 70f;
        [SerializeField, Min(0f)] private float shotDamage = 10f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
        [SerializeField, Min(0.1f)] private float projectileRange = 20f;

        private readonly List<EnemyLobProjectile> _bombs = new List<EnemyLobProjectile>(4);
        private ProductionControlDrone _drone;
        private Phase _phase;
        private int _actionIndex;
        private float _nextActionAt;
        private string _animation;
        private bool _animationFinished;

        public int FiredVolleys { get; private set; }
        public int DroppedBombs { get; private set; }
        public bool DidComplete { get; private set; }
        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            visual != null && bombardment != null && projectilePrefab != null && muzzle != null &&
            Enemy is ProductionControlDrone drone && drone.TryPlanAirTransition(out _);

        protected override void PrepareAttack()
        {
            _drone = (ProductionControlDrone)Enemy;
            _drone.StopMovement();
            _phase = Phase.Rising;
            _actionIndex = FiredVolleys = DroppedBombs = 0;
            DidComplete = false;
            _animationFinished = true;
            _bombs.Clear();
            // Rise 변신 클립을 사용하지 않고 현재 기본 형태 그대로 떠오릅니다.
            visual.Play("Idle");
        }

        protected override void OnReleaseAttack()
        {
            CoolTimeStartTime = Time.time;
            _drone.TryPlanAirTransition(out Vector2 center);
            Vector2 destination = new Vector2(Enemy.transform.position.x, center.y);
            if (!_drone.HasAirPath(destination)) destination = center;
            _drone.MoveGround(destination, riseSpeed);
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;

            if (_phase == Phase.Rising)
            {
                if (_drone.IsMoving) return;
                _drone.BeginEntryShield();
                _phase = Phase.FormingShield;

                return;
            }

            if (_phase == Phase.FormingShield)
            {
                if (!_drone.IsEntryShieldReady) return;
                _phase = Phase.Attacking;
                _nextActionAt = Time.time;
            }

            if (_phase == Phase.Centering)
            {
                if (_drone.IsMoving) return;
                DidComplete = true;
                EndSkill();

                return;
            }

            if (!_animationFinished && visual.HasFinished(_animation))
            {
                _animationFinished = true;
                visual.Play("Idle");
            }

            if (_actionIndex < 7 && _animationFinished && Time.time >= _nextActionAt)
            {
                Enemy.FaceTarget();
                if (_actionIndex % 2 == 0) DropBomb();
                else FireShotgun();

                _actionIndex++;
                _animationFinished = false;
                _nextActionAt = Time.time + actionInterval;
            }

            if (_actionIndex < 7 || !_animationFinished) return;
            foreach (var bomb in _bombs)
                if (bomb != null && bomb.IsInFlight) return;

            _phase = Phase.Centering;
            _drone.TryPlanAirTransition(out Vector2 destination);
            _drone.MoveGround(destination, returnSpeed);
        }

        private void DropBomb()
        {
            var bomb = bombardment.LaunchTargetBomb(CastTarget, 0f, true);
            if (bomb != null)
            {
                _bombs.Add(bomb);
                DroppedBombs++;
            }

            _animation = "Bomb";
            visual.Play(_animation, 1.2f);
        }

        private void FireShotgun()
        {
            bool rear = FiredVolleys % 2 != 0;
            Transform launcher = rear && rearMuzzle != null ? rearMuzzle : muzzle;
            var body = CastTarget.GetComponent<Collider2D>();
            Vector2 target = body != null ? (Vector2)body.bounds.center : (Vector2)CastTarget.position;
            Vector2 direction = (target - (Vector2)launcher.position).normalized;
            if (direction == Vector2.zero) direction = Vector2.down;

            for (int i = 0; i < 3; i++)
            {
                float angle = (i - 1) * shotgunAngle * 0.5f;
                var projectile = Instantiate(projectilePrefab, launcher.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(projectile.gameObject, Enemy.gameObject.scene);
                projectile.Speed = projectileSpeed;
                projectile.Lifetime = projectileRange / Mathf.Max(0.1f, projectileSpeed);
                projectile.Launch(Enemy, CastTarget, shotDamage, Quaternion.Euler(0f, 0f, angle) * direction);
            }

            FiredVolleys++;
            _animation = rear ? "AirShotRear" : "AirShotFront";
            visual.Play(_animation, 1.2f);
            NotifyAttackPerformed(launcher.position);
        }

        public override void FixedUpdateAttack()
        {
            if (IsUsing && HasReleased) _drone.TickAirMovement();
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            visual?.Play("Idle");
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            _drone?.StopMovement();
            visual?.ResetPlaybackSpeed();
        }
    }
}
