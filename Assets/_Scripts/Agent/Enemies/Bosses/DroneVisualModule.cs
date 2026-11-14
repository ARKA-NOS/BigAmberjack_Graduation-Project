using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Bosses
{
    public sealed class DroneVisualModule : Module
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private SpriteRenderer alarmLamp;
        [SerializeField] private SpriteRenderer processNumber;
        [SerializeField] private SpriteRenderer gunFlash;
        private ProductionControlDrone _drone;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _drone = owner as ProductionControlDrone;
        }

        internal void Play(string state)
        {
            if (_drone == null || _drone.IsDead || animator == null || !animator.isActiveAndEnabled) return;
            int hash = Animator.StringToHash("Base Layer." + state);
            if (animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
        }

        internal bool HasFinished(string state)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                return true;

            int hash = Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, hash)) return true;
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            // Play가 이번 프레임의 Animator 평가에 아직 반영되지 않았으면 기다립니다.
            return current.fullPathHash == hash && current.normalizedTime >= 1f;
        }

        internal void SetGunFlash(bool visible)
        {
            if (gunFlash != null) gunFlash.enabled = visible;
        }

        private void LateUpdate()
        {
            if (_drone == null) return;
            bool alive = !_drone.IsDead;
            if (alarmLamp != null)
                alarmLamp.enabled = alive && (!_drone.IsAirPhase || Time.time % 0.55f < 0.29f);
            if (processNumber != null)
            {
                processNumber.enabled = alive;
                processNumber.flipX = _drone.FacingDirection < 0f;
            }
            if (shadow != null)
            {
                shadow.enabled = alive;
                float height = _drone.transform.position.y - _drone.FloorY;
                shadow.transform.position = new Vector3(_drone.transform.position.x, _drone.FloorY + 0.04f,
                    _drone.transform.position.z);
                float falloff = Mathf.InverseLerp(1f, 5f, height);
                shadow.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, falloff);
                shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.45f, 0.19f, falloff));
            }
        }

        private void OnDisable() => SetGunFlash(false);
    }
}
