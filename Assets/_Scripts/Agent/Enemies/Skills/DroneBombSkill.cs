using System.Collections.Generic;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.UI.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneBombSkill : EnemyAttackSkill
    {
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private EnemyLobProjectile bombPrefab;
        [SerializeField] private DroneBombTargetGraphic warningPrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private SpriteRenderer preview;
        [SerializeField, Min(0.1f)] private float range = 20f;
        [SerializeField, Min(0f)] private float damage = 22f;
        [SerializeField, Min(0f)] private float lastStandDamage = 26f;
        [SerializeField, Min(1f)] private float pixelsPerUnit = 28f;
        [SerializeField, Min(1f)] private float normalRadiusPixels = 52.5f;
        [SerializeField, Min(0f)] private float predictionLead = 0.35f;
        [SerializeField, Min(0f)] private float maxPredictionDistance = 1.5f;
        [Tooltip("폭격 중 표적 옆으로 위치를 조정하는 속도와 간격입니다. 일반과 강화가 같은 이동을 사용합니다.")]
        [SerializeField, Min(0.1f)] private float normalHoverSpeed = 3.2f;
        [SerializeField, Min(0f)] private float normalHoverOffset = 1.2f;
        [SerializeField, Min(0.1f)] private float lastStandMovementSpeedMultiplier = 1.2f;

        [Header("Normal Timeline")]
        [SerializeField, Min(1)] private int normalDrops = 3;
        [SerializeField, Min(0f)] private float firstDropTime = 0.8f;
        [SerializeField, Min(0.05f)] private float normalInterval = 1.05f;
        [SerializeField, Min(0.05f)] private float warningDuration = 1.65f;

        [Header("Last Stand Timeline")]
        [SerializeField, Min(1)] private int lastStandDrops = 6;
        [SerializeField, Min(0f)] private float lastStandFirstDropTime = 0.65f;
        [SerializeField, Min(0.05f)] private float lastStandInterval = 0.508f;
        [SerializeField, Min(0.05f)] private float lastStandWarningDuration = 1.15f;

        private readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[16];
        private readonly List<EnemyLobProjectile> _bombs = new List<EnemyLobProjectile>(6);
        private ProductionControlDrone _drone;
        private Vector2 _previousTargetPosition, _targetVelocity;
        private int _dropCount;
        private float _nextDropAt, _interval, _warningTime, _radius, _damage, _animationSpeed;
        private bool _descending;
        private bool _enhanced;
        private float _hoverSide;

        public float Range => range;
        public int DroppedCount { get; private set; }
        public bool DidReturnToGround { get; private set; }
        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;

        internal bool CanPlanFrom(Vector2 root, Transform target) => muzzle != null &&
            TryPlan(root + (Vector2)(muzzle.position - Enemy.transform.position), target, 0f, out _);

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && muzzle != null &&
            TryPlan(muzzle.position, Enemy.Target, 0f, out _);

        private bool TryPlan(Vector2 origin, Transform target, float prediction, out Vector2 landing)
        {
            landing = default;
            if (target == null || bombPrefab == null || Enemy is not ProductionControlDrone drone ||
                Mathf.Abs(target.position.x - origin.x) > range) return false;

            var terrain = new ContactFilter2D { useTriggers = false };
            terrain.SetLayerMask(drone.TerrainLayers);
            float x = Mathf.Clamp(target.position.x + prediction, drone.Arena.LaserLeft.x, drone.Arena.LaserRight.x);
            var targetBody = target.GetComponent<Collider2D>();
            float feet = targetBody != null ? targetBody.bounds.min.y : target.position.y;
            float y = Mathf.Min(origin.y - 0.05f, Mathf.Max(drone.FloorY + 0.25f, feet + 0.25f));
            int count = Physics2D.Raycast(new Vector2(x, y), Vector2.down, terrain, _terrainHits, 30f);

            for (int i = 0; i < count; i++)
            {
                if (_terrainHits[i].normal.y < 0.5f) continue;
                landing = _terrainHits[i].point;
                return bombPrefab.HasClearArc(origin, landing, terrain, _terrainHits);
            }

            return false;
        }

        protected override void PrepareAttack()
        {
            _drone = (ProductionControlDrone)Enemy;
            bool enhanced = _drone.UseLastStandPatterns;
            _enhanced = enhanced;
            _dropCount = Mathf.Max(1, enhanced ? lastStandDrops : normalDrops);
            _interval = Mathf.Max(0.05f, enhanced ? lastStandInterval : normalInterval);
            _warningTime = Mathf.Max(0.05f, enhanced ? lastStandWarningDuration : warningDuration);
            _radius = normalRadiusPixels / Mathf.Max(1f, pixelsPerUnit);
            _damage = enhanced ? lastStandDamage : damage;
            _animationSpeed = enhanced ? 2f : 1f;
            float first = enhanced ? lastStandFirstDropTime : firstDropTime;
            _nextDropAt = Time.time + first;
            _bombs.Clear();
            DroppedCount = 0;
            _descending = DidReturnToGround = false;
            _previousTargetPosition = CastTarget.position;
            _targetVelocity = Vector2.zero;
            _drone.StopMovement();
            _hoverSide = Enemy.transform.position.x < CastTarget.position.x ? -1f : 1f;

            if (preview != null) preview.enabled = true;
            visual.Play("BombCharge", enhanced ? 1.2f : 1f);
        }

        protected override void OnReleaseAttack() => CoolTimeStartTime = Time.time;

        private void Drop()
        {
            _nextDropAt += _interval;
            float prediction = Mathf.Clamp(_targetVelocity.x * predictionLead, -maxPredictionDistance, maxPredictionDistance);
            if (!TryPlan(muzzle.position, CastTarget, prediction, out Vector2 landing))
            {
                DroppedCount++;
                return;
            }

            var bomb = Instantiate(bombPrefab, muzzle.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(bomb.gameObject, Enemy.gameObject.scene);
            bomb.Launch(Enemy, CastTarget, landing, _damage, _radius, _drone.TerrainLayers, null, _warningTime);
            _bombs.Add(bomb);

            if (warningPrefab != null)
            {
                var warning = Instantiate(warningPrefab);
                SceneManager.MoveGameObjectToScene(warning.gameObject, Enemy.gameObject.scene);
                warning.Bind(bomb, landing, _radius);
            }

            DroppedCount++;
            CoolTimeStartTime = Time.time;
            if (preview != null) preview.enabled = false;
            visual.Play("Bomb", _animationSpeed);
            NotifyAttackPerformed(muzzle.position);
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;

            Vector2 target = CastTarget.position;
            if (Time.deltaTime > 0f)
                _targetVelocity = Vector2.Lerp(_targetVelocity, (target - _previousTargetPosition) / Time.deltaTime, 0.5f);
            _previousTargetPosition = target;

            if (!_descending && !_drone.IsMoving && DroppedCount < _dropCount)
            {
                Vector2 destination = new Vector2(_drone.ClampX(target.x + _hoverSide * normalHoverOffset), Enemy.transform.position.y);
                if (Mathf.Abs(destination.x - Enemy.transform.position.x) > 0.35f && _drone.HasAirPath(destination))
                    _drone.MoveGround(destination, normalHoverSpeed * (_enhanced ? lastStandMovementSpeedMultiplier : 1f));
            }

            while (DroppedCount < _dropCount && Time.time >= _nextDropAt) Drop();

            if (!_descending && DroppedCount >= _dropCount && !_drone.IsMoving &&
                (_bombs.Count == 0 || visual.HasFinished("Bomb")))
            {
                _descending = true;
                _drone.BeginPatternDescent();
            }

            if (_descending && !_drone.IsMoving && !DidReturnToGround)
            {
                DidReturnToGround = true;
                visual.Play("Idle");
                BeginRecovery();
            }

            if (!DidReturnToGround) return;
            foreach (var bomb in _bombs)
                if (bomb != null && bomb.IsInFlight) return;

            EndSkill();
        }

        public override void FixedUpdateAttack()
        {
            if (IsUsing) _drone.TickAirMovement();
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
            if (preview != null) preview.enabled = false;
        }
    }
}
