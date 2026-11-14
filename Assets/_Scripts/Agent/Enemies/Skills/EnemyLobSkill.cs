using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class EnemyLobSkill : EnemyAttackSkill
    {
        private static readonly int PreparationState = Animator.StringToHash("Base Layer.ThrowPreparation");
        private static readonly int ThrowState = Animator.StringToHash("Base Layer.Throw");
        private static readonly int RecoveryState = Animator.StringToHash("Base Layer.Recovery");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

        [SerializeField] private Animator attackAnimator;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private EnemyLobProjectile projectilePrefab;
        [SerializeField] private LayerMask terrainLayers = (1 << 6) | (1 << 10);
        [Tooltip("대상 위치 아래에서 착탄할 바닥을 찾는 최대 높이입니다.")]
        [SerializeField, Min(0.1f)] private float maximumGroundDrop = 3f;
        [SerializeField, Min(0.01f)] private float impactRadius = 1.2f;
        [SerializeField, Min(0f)] private float damage = 16f;
        [SerializeField, Min(0f)] private float recoveryTime = 1f;

        private readonly RaycastHit2D[] _surfaceHit = new RaycastHit2D[1];
        private Vector2 _landingPoint;
        private float _recoveryEnd;

        internal override bool FinishesOutsideChaseRange => IsUsing;
        public override bool CompletesAfterTargetLost => HasReleased;

        protected override bool CanUseSkillCondition() =>
            base.CanUseSkillCondition() && TryPlanThrow(Enemy.Target, out _);

        protected override void OnUseSkill()
        {
            float previousCooldown = CoolTimeStartTime;
            base.OnUseSkill();
            CoolTimeStartTime = previousCooldown;
        }

        protected override void PrepareAttack()
        {
            Enemy.FaceTarget();
            if (!TryPlanThrow(CastTarget, out _landingPoint))
            {
                StopSkill();
                return;
            }
            Play(PreparationState);
        }

        private bool TryPlanThrow(Transform target, out Vector2 landingPoint)
        {
            landingPoint = default;
            if (projectilePrefab == null || throwPoint == null) return false;
            var terrain = new ContactFilter2D { useTriggers = false };
            terrain.SetLayerMask(terrainLayers);
            RaycastHit2D floor = EnemyLobProjectile.FindSurface(
                (Vector2)target.position + Vector2.up * 0.15f, Vector2.down, maximumGroundDrop + 0.15f, terrain, _surfaceHit);
            if (floor.collider == null || floor.normal.y <= 0.5f) return false;

            // 스킬 선택 시 아직 회전하기 전일 수 있으므로 타깃을 바라본 발사점으로 검사합니다.
            Vector2 launchPoint = throwPoint.position;
            float side = target.position.x - Enemy.transform.position.x;
            if (!Mathf.Approximately(side, 0f) && Mathf.Sign(side) != Enemy.FacingDirection)
                launchPoint.x = 2f * Enemy.transform.position.x - launchPoint.x;
            landingPoint = floor.point;
            return projectilePrefab.HasClearArc(launchPoint, landingPoint, terrain, _surfaceHit);
        }

        protected override void UpdateTelegraph() =>
            Telegraph.SetBounds(new Bounds(_landingPoint, Vector3.one * (2f * impactRadius)));

        protected override void OnReleaseAttack()
        {
            Vector2 origin = throwPoint.position;
            var projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(projectile.gameObject, Enemy.gameObject.scene);
            projectile.Launch(Enemy, CastTarget, _landingPoint, damage, impactRadius, terrainLayers, TakeTelegraph());
            CoolTimeStartTime = Time.time;
            Play(ThrowState);
        }

        public override void UpdateSkill()
        {
            // Animator.Play 직후의 프레임에는 이전 상태가 조회될 수 있습니다.
            bool wasReleased = HasReleased;
            base.UpdateSkill();
            if (!IsUsing || !wasReleased || !HasReleased) return;
            if (!IsRecovering)
            {
                if (attackAnimator != null && attackAnimator.isActiveAndEnabled)
                {
                    AnimatorStateInfo state = attackAnimator.GetCurrentAnimatorStateInfo(0);
                    if (state.fullPathHash == ThrowState && state.normalizedTime < 1f)
                        return;
                }
                BeginRecovery();
                Play(RecoveryState);
                _recoveryEnd = Time.time + recoveryTime;
            }
            if (Time.time >= _recoveryEnd) EndSkill();
        }

        public override void OnAttackFinished() { }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            Play(IdleState);
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            Play(IdleState);
        }

        private void Play(int state)
        {
            if (Enemy != null && !Enemy.IsDead && attackAnimator != null && attackAnimator.isActiveAndEnabled &&
                attackAnimator.runtimeAnimatorController != null && attackAnimator.HasState(0, state))
                attackAnimator.Play(state, 0, 0f);
        }
    }
}
