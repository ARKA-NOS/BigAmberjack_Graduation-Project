using System.Collections.Generic;
using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneDownwardLaserSkill : EnemyAttackSkill
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private SpriteRenderer beam;
        [SerializeField, Min(0.1f)] private float length = 4.5f;
        [SerializeField, Min(0.1f)] private float width = 0.8f;
        [SerializeField, Min(0f)] private float damage = 18f;
        [SerializeField, Min(0.05f)] private float activeDuration = 0.45f;
        private readonly List<Collider2D> _overlaps = new();
        private readonly RaycastHit2D[] _groundHit = new RaycastHit2D[1];
        private Bounds _area;
        private bool _didHit;
        private float _beamStartedAt;

        protected override bool CanUseSkillCondition() =>
            base.CanUseSkillCondition() && Enemy is ProductionControlDrone drone && muzzle != null && beam != null &&
            beam.sprite != null && muzzle.position.y > drone.FloorY;

        protected override void OnUseSkill()
        {
            float previousCooldown = CoolTimeStartTime;
            base.OnUseSkill();
            CoolTimeStartTime = previousCooldown;
        }

        protected override void PrepareAttack()
        {
            var drone = (ProductionControlDrone)Enemy;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(drone.TerrainLayers);
            RaycastHit2D ground = EnemyLobProjectile.FindSurface(muzzle.position, Vector2.down, length, filter, _groundHit);
            float distance = ground.collider != null ? ground.distance : Mathf.Min(length, muzzle.position.y - drone.FloorY);
            _area = new Bounds((Vector2)muzzle.position + Vector2.down * (distance * 0.5f), new Vector3(width, distance, 0f));
            _didHit = false;
            visual.Play("LaserCharge");
        }

        protected override void UpdateTelegraph() => Telegraph.SetBounds(_area);

        protected override void OnReleaseAttack()
        {
            CoolTimeStartTime = _beamStartedAt = Time.time;
            Telegraph?.Hide();
            visual.Play("Laser");
            beam.transform.position = _area.center;
            beam.transform.localScale = new Vector3(_area.size.x / beam.sprite.bounds.size.x,
                _area.size.y / beam.sprite.bounds.size.y, 1f);
            beam.enabled = true;
            UpdateBeam(0f);
            TryHit();
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;
            float elapsed = Time.time - _beamStartedAt;
            if (elapsed >= activeDuration)
            {
                beam.enabled = false;
                if (visual.HasFinished("Laser")) EndSkill();
                return;
            }
            UpdateBeam(elapsed);
            TryHit();
        }

        private void UpdateBeam(float elapsed)
        {
            Color tint = beam.color;
            tint.a = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(elapsed / 0.055f)) *
                Mathf.Clamp01((activeDuration - elapsed) / 0.10f);
            beam.color = tint;
        }

        private void TryHit()
        {
            if (_didHit) return;
            Physics2D.OverlapBox(_area.center, _area.size, 0f, new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (var hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                if (!EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, damage, hit.ClosestPoint(_area.center), Vector2.down))
                    continue;
                _didHit = true;
                break;
            }
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            if (beam != null) beam.enabled = false;
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            if (beam != null) beam.enabled = false;
        }
    }
}
