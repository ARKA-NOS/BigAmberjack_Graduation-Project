using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneSalvoSkill : EnemyAttackSkill
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private string fireState = "Shot";
        [SerializeField, Min(0.1f)] private float range = 8f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0f)] private float damage = 14f;
        [SerializeField, Min(1)] private int projectilesPerSalvo = 1;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.13f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.8f;

        private Vector2 _direction;
        private float _preparedAt;
        private float _nextShotAt;
        private float _recoveryUntil;
        private int _remaining;
        public bool DidFire { get; private set; }
        public float Range => range;

        protected override bool CanUseSkillCondition() =>
            base.CanUseSkillCondition() && projectilePrefab != null && muzzle != null &&
            Mathf.Abs(Enemy.Target.position.x - Enemy.transform.position.x) <= range;

        protected override void OnUseSkill()
        {
            float previousCooldown = CoolTimeStartTime;
            base.OnUseSkill();
            CoolTimeStartTime = previousCooldown;
        }

        protected override void PrepareAttack()
        {
            DidFire = false;
            Enemy.FaceTarget();
            _direction = Vector2.right * Enemy.FacingDirection;
            _preparedAt = Time.time;
            visual.Play("Charge");
            visual.SetGunFlash(true);
        }

        protected override void OnReleaseAttack()
        {
            visual.SetGunFlash(false);
            _remaining = projectilesPerSalvo;
            Fire();
        }

        public override void UpdateSkill()
        {
            if (IsPreparing) visual.SetGunFlash(Mathf.FloorToInt((Time.time - _preparedAt) * 16f) % 2 == 0);
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;
            if (_remaining > 0)
            {
                if (Time.time >= _nextShotAt) Fire();
                return;
            }
            if (!IsRecovering)
            {
                if (!visual.HasFinished(fireState)) return;
                visual.Play("Idle");
                BeginRecovery();
                _recoveryUntil = Time.time + recoveryTime;
            }
            if (Time.time >= _recoveryUntil) EndSkill();
        }

        private void Fire()
        {
            var projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(projectile.gameObject, Enemy.gameObject.scene);
            projectile.Speed = projectileSpeed;
            projectile.Lifetime = range / projectileSpeed;
            projectile.Launch(Enemy, CastTarget, damage, _direction);
            if (!DidFire) visual.Play(fireState);
            DidFire = true;
            CoolTimeStartTime = Time.time;
            _nextShotAt = Time.time + shotInterval;
            _remaining--;
        }

        public override void OnAttackFinished() { }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            visual.SetGunFlash(false);
            visual.Play("Idle");
        }
    }
}
