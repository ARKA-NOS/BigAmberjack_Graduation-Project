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
        [Tooltip("물리 위치 사이를 보간할 비주얼 루트입니다. 콜라이더는 원래 위치를 유지합니다.")]
        [SerializeField] private Transform movementVisual;
        [SerializeField, Min(1f)] private float movementSmoothing = 25f;
        [SerializeField] private ParticleSystem basicFlightExhaust;
        [SerializeField, Min(0f)] private float basicHoverAmplitude = 0.06f;
        [SerializeField, Min(0f)] private float basicHoverFrequency = 1.4f;

        [Header("Salvo Firing Hover")]
        [Tooltip("연사 중 비주얼을 띄우는 높이입니다. 수평 이동이 멈춰도 유지하며, 물리 몸통은 바닥에 둡니다.")]
        [SerializeField, Min(0f)] private float salvoHoverHeight = 0.18f;
        [SerializeField, Min(0f)] private float salvoHoverAmplitude = 0.1f;
        [SerializeField, Min(0f)] private float salvoHoverFrequency = 2f;

        private ProductionControlDrone _drone;
        private Vector3 _visualOffset, _displayPosition;
        private bool _hasDisplayPosition;
        private int _lastPlayedHash;
        private int _lastPlayFrame = -1;
        private float _basicHoverBlend;
        private float _salvoHoverBlend;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _drone = owner as ProductionControlDrone;
            if (movementVisual != null) _visualOffset = movementVisual.localPosition;
        }

        internal void Play(string state, float speed = 1f)
        {
            if (_drone == null || _drone.IsDead || animator == null || !animator.isActiveAndEnabled) return;

            int hash = Animator.StringToHash("Base Layer." + state);

            if (animator.HasState(0, hash))
            {
                animator.speed = Mathf.Max(0.01f, speed);
                _lastPlayedHash = hash;
                _lastPlayFrame = Time.frameCount;
                animator.Play(hash, 0, 0f);
            }
        }

        internal bool HasFinished(string state)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                return true;

            int hash = Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, hash)) return true;
            if (_lastPlayedHash == hash && _lastPlayFrame == Time.frameCount) return false;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

            // Play가 이번 프레임의 Animator 평가에 아직 반영되지 않았으면 기다립니다.
            return current.fullPathHash == hash && current.normalizedTime >= 1f;
        }

        internal void SetGunFlash(bool visible)
        {
            if (gunFlash != null) gunFlash.enabled = visible;
        }

        internal void ResetPlaybackSpeed()
        {
            if (animator != null) animator.speed = 1f;
        }

        internal void SnapToBody()
        {
            if (movementVisual == null || _drone == null) return;

            _displayPosition = _drone.transform.TransformPoint(_visualOffset);
            movementVisual.position = _displayPosition;
            _hasDisplayPosition = true;
            _basicHoverBlend = _salvoHoverBlend = 0f;
        }

        private void LateUpdate()
        {
            if (_drone == null) return;

            bool salvoHover = !_drone.IsDead && _drone.IsSalvoFiring;
            _salvoHoverBlend = Mathf.MoveTowards(_salvoHoverBlend, salvoHover ? 1f : 0f, Time.deltaTime * 6f);

            if (movementVisual != null)
            {
                Vector3 target = _drone.transform.TransformPoint(_visualOffset);
                if (!_hasDisplayPosition) { _displayPosition = target; _hasDisplayPosition = true; }

                float blend = 1f - Mathf.Exp(-movementSmoothing * Time.deltaTime);
                _displayPosition = Vector3.Lerp(_displayPosition, target, blend);
                movementVisual.position = _displayPosition;
                bool hover = !_drone.IsDead && !_drone.IsMoving && !_drone.IsEntryShieldForming &&
                    _drone.transform.position.y - _drone.FloorY > 2f;
                _basicHoverBlend = Mathf.MoveTowards(_basicHoverBlend, hover ? 1f : 0f, Time.deltaTime * 8f);
                movementVisual.position += Vector3.up * (Mathf.Sin(Time.time * basicHoverFrequency * 2f * Mathf.PI) * basicHoverAmplitude * _basicHoverBlend);

                float salvoOffset = salvoHoverHeight + Mathf.Sin(Time.time * salvoHoverFrequency * 2f * Mathf.PI) * salvoHoverAmplitude;
                movementVisual.position += Vector3.up * (salvoOffset * _salvoHoverBlend);
            }

            bool alive = !_drone.IsDead;
            bool emitExhaust = alive && (salvoHover || _drone.transform.position.y - _drone.FloorY > 1.4f);
            if (basicFlightExhaust != null)
            {
                if (emitExhaust && !basicFlightExhaust.isPlaying) basicFlightExhaust.Play();
                else if (!emitExhaust && basicFlightExhaust.isPlaying) basicFlightExhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (alarmLamp != null)
                alarmLamp.enabled = alive && (!_drone.IsLastStand || Time.time % 0.55f < 0.29f);

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

        private void OnDisable()
        {
            _salvoHoverBlend = 0f;
            SetGunFlash(false);
            if (basicFlightExhaust != null) basicFlightExhaust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
