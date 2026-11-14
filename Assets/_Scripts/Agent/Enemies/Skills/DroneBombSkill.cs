using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneBombSkill : EnemyAttackSkill
    {
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private EnemyLobProjectile bombPrefab;
        [SerializeField] private SpriteRenderer preview;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0.1f)] private float range = 6f;
        [SerializeField, Min(0.1f)] private float radius = 1.3f;
        [SerializeField, Min(0f)] private float damage = 20f;

        private readonly RaycastHit2D[] _terrainHits = new RaycastHit2D[1];
        private Vector2 _landingPoint;
        public float Range => range;

        internal bool CanPlanFrom(Vector2 start, Transform target) => muzzle != null && TryPlan(StartPoint(start), target, out _);

        private Vector2 StartPoint(Vector2 root) => root + (Vector2)(muzzle.position - Enemy.transform.position);

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && muzzle != null &&
            TryPlan(muzzle.position, Enemy.Target, out _);

        private bool TryPlan(Vector2 origin, Transform target, out Vector2 landing)
        {
            landing = default;
            if (bombPrefab == null || Enemy is not ProductionControlDrone drone ||
                Mathf.Abs(target.position.x - origin.x) > range) return false;
            var terrain = new ContactFilter2D { useTriggers = false };
            terrain.SetLayerMask(drone.TerrainLayers);
            float targetX = drone.ClampX(target.position.x);
            Vector2 floorProbe = new Vector2(targetX, drone.FloorY + 0.20f);
            var floor = EnemyLobProjectile.FindSurface(floorProbe, Vector2.down, 0.4f, terrain, _terrainHits);
            if (floor.collider == null || floor.normal.y < 0.5f) return false;
            landing = floor.point;
            return bombPrefab.HasClearArc(origin, landing, terrain, _terrainHits);
        }

        protected override void OnUseSkill()
        {
            float previousCooldown = CoolTimeStartTime;
            base.OnUseSkill();
            CoolTimeStartTime = previousCooldown;
        }

        protected override void PrepareAttack()
        {
            if (!TryPlan(muzzle.position, CastTarget, out _landingPoint))
            {
                StopSkill();
                return;
            }
            if (preview != null) preview.enabled = true;
            visual.Play("BombCharge");
        }

        protected override void UpdateTelegraph() =>
            Telegraph.SetBounds(new Bounds(_landingPoint, Vector3.one * (radius * 2f)));

        protected override void OnReleaseAttack()
        {
            if (preview != null) preview.enabled = false;
            var drone = (ProductionControlDrone)Enemy;
            var bomb = Instantiate(bombPrefab, muzzle.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(bomb.gameObject, Enemy.gameObject.scene);
            bomb.Launch(Enemy, CastTarget, _landingPoint, damage, radius, drone.TerrainLayers, TakeTelegraph());
            CoolTimeStartTime = Time.time;
            visual.Play("Bomb");
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (IsUsing && HasReleased && visual.HasFinished("Bomb")) EndSkill();
        }

        public override void OnAttackFinished() { }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            if (preview != null) preview.enabled = false;
        }
    }
}
