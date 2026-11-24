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
        [SerializeField, Min(0.1f)] private float radius = 1.3f;
        [SerializeField, Min(0f)] private float damage = 20f;

        [SerializeField, Min(1)] private int normalDrops = 3;
        [SerializeField, Min(1)] private int lastStandDrops = 6;
        [SerializeField, Min(0.05f)] private float normalInterval = 1f;
        [SerializeField, Min(0.05f)] private float lastStandInterval = 0.5f;

        private readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[16];
        private readonly List<EnemyLobProjectile> _drops = new List<EnemyLobProjectile>(6);
        private int _dropCount;
        private float _interval;
        private float _nextDropAt;
        private bool _enhanced;

        public float Range => range;
        public int DroppedCount { get; private set; }
        protected override bool ShouldShowTelegraph => false;

        internal bool CanPlanFrom(Vector2 root, Transform target) => muzzle != null &&
            TryPlan(root + (Vector2)(muzzle.position - Enemy.transform.position), target, out _);

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && muzzle != null &&
            TryPlan(muzzle.position, Enemy.Target, out _);

        private bool TryPlan(Vector2 origin, Transform target, out Vector2 landing)
        {
            landing = default;

            if (target == null || bombPrefab == null || Enemy is not ProductionControlDrone drone ||
                Mathf.Abs(target.position.x - origin.x) > range) return false;

            var terrain = new ContactFilter2D { useTriggers = false };

            terrain.SetLayerMask(drone.TerrainLayers);
            float x = Mathf.Clamp(target.position.x, drone.Arena.LaserLeft.x, drone.Arena.LaserRight.x);
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
            _enhanced = ((ProductionControlDrone)Enemy).UseLastStandPatterns;
            _dropCount = Mathf.Max(1, _enhanced ? lastStandDrops : normalDrops);
            _interval = Mathf.Max(0.05f, _enhanced ? lastStandInterval : normalInterval);
            DroppedCount = 0;
            _drops.Clear();

            if (preview != null) preview.enabled = true;

            visual.Play("BombCharge");
        }

        protected override void OnReleaseAttack()
        {
            if (preview != null) preview.enabled = false;

            CoolTimeStartTime = Time.time;
            _nextDropAt = Time.time;
            Drop();
        }

        private void Drop()
        {
            _nextDropAt += _interval;

            if (!TryPlan(muzzle.position, CastTarget, out Vector2 landing))
            {
                // 대상이 유효한 바닥을 벗어나면 새 투하만 중단하고, 이미 날아가는 폭탄은 유지합니다.
                DroppedCount = _dropCount;

                return;
            }

            var drone = (ProductionControlDrone)Enemy;
            var bomb = Instantiate(bombPrefab, muzzle.position, Quaternion.identity);

            SceneManager.MoveGameObjectToScene(bomb.gameObject, Enemy.gameObject.scene);
            bomb.Launch(Enemy, CastTarget, landing, damage, radius, drone.TerrainLayers, null);
            _drops.Add(bomb);

            if (warningPrefab != null)
            {
                var warning = Instantiate(warningPrefab);

                SceneManager.MoveGameObjectToScene(warning.gameObject, Enemy.gameObject.scene);
                warning.Bind(bomb, landing, radius);
            }

            DroppedCount++;
            CoolTimeStartTime = Time.time;
            visual.Play("Bomb", _enhanced ? 1.5f : 1f);
            NotifyAttackPerformed(muzzle.position);
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();

            if (!IsUsing || !HasReleased) return;

            while (DroppedCount < _dropCount && Time.time >= _nextDropAt) Drop();

            if (DroppedCount < _dropCount || !visual.HasFinished("Bomb")) return;

            foreach (var drop in _drops)
                if (drop != null && drop.IsInFlight) return;

            EndSkill();
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            visual?.ResetPlaybackSpeed();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            visual?.ResetPlaybackSpeed();

            if (preview != null) preview.enabled = false;
        }
    }
}
