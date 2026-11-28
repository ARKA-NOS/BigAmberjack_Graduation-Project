using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.Enemy;
using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Bosses
{
    [DisallowMultipleComponent]
    public sealed class DroneLastStandFeedbackModule : Module
    {
        private enum ShieldPhase { Hidden, Forming, Active, Discharging }

        [SerializeField] private DroneShieldGraphic shield;
        [SerializeField] private ParticleSystem malfunctionSparks;

        [Header("Shield Formation")]
        [SerializeField, Min(0.01f)] private float centerFadeDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float arcFadeDuration = 0.22f;
        [SerializeField, Min(0f)] private float layerDelay = 0.08f;
        [SerializeField] private float innerRotationSpeed = 42f;
        [SerializeField] private float outerRotationSpeed = -32f;

        [Header("Shield Shutdown")]
        [SerializeField, Min(0.01f)] private float shutdownDuration = 0.28f;
        [SerializeField, Min(1f)] private float flickerFrequency = 36f;

        [Header("Landing Impact")]
        [SerializeField, Min(0f)] private float landingShakeStrength = 1.1f;
        [SerializeField, Min(0.01f)] private float landingShakeDuration = 0.45f;
        [SerializeField, Min(0f)] private float landingShakeRadius = 20f;

        private ProductionControlDrone _drone;
        private ShieldPhase _shieldPhase;
        private float _shieldElapsed, _innerAngle, _outerAngle;

        public bool IsShieldReady => _shieldPhase == ShieldPhase.Active;
        public bool IsShieldForming => _shieldPhase == ShieldPhase.Forming;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _drone = owner as ProductionControlDrone;
            Hide();
        }

        internal void BeginShieldFormation()
        {
            _shieldPhase = ShieldPhase.Forming;
            _shieldElapsed = 0f;
            _innerAngle = 20f;
            _outerAngle = 200f;
            if (shield == null) return;

            shield.SetPresentation(0f, 0f, 0f, _innerAngle, _outerAngle);
            shield.enabled = true;
        }

        internal void BeginShieldShutdown()
        {
            if (_shieldPhase == ShieldPhase.Hidden) return;

            _shieldPhase = ShieldPhase.Discharging;
            _shieldElapsed = 0f;
        }

        internal void ResetShield()
        {
            _shieldPhase = ShieldPhase.Hidden;
            _shieldElapsed = 0f;
            if (shield == null) return;

            shield.SetPresentation(0f, 0f, 0f, 0f, 0f);
            shield.enabled = false;
        }

        public void PlayLandingImpact()
        {
            if (!isActiveAndEnabled || _drone == null || landingShakeStrength <= 0f) return;

            var definition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = BossLaserShake.ImpulseChannel,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Dissipating,
                ImpulseDuration = Mathf.Max(0.01f, landingShakeDuration),
                DissipationDistance = 10f
            };
            var impulse = definition.CreateAndReturnEvent(_drone.transform.position, Vector3.down * landingShakeStrength);
            if (impulse != null) impulse.Radius = landingShakeRadius;
        }

        private void LateUpdate()
        {
            bool alive = _drone != null && _drone.isActiveAndEnabled && !_drone.IsDead;
            if (!alive) { Hide(); return; }

            UpdateShield();
            if (malfunctionSparks == null) return;

            bool groggy = alive && _drone.IsGroggy;
            if (groggy && !malfunctionSparks.isPlaying) malfunctionSparks.Play();
            else if (!groggy && malfunctionSparks.isPlaying)
                malfunctionSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void UpdateShield()
        {
            if (_shieldPhase == ShieldPhase.Hidden) return;
            if (!_drone.IsInvulnerable && _shieldPhase != ShieldPhase.Discharging)
            {
                ResetShield();
                return;
            }

            _shieldElapsed += Time.deltaTime;
            _innerAngle = Mathf.Repeat(_innerAngle + innerRotationSpeed * Time.deltaTime, 360f);
            _outerAngle = Mathf.Repeat(_outerAngle + outerRotationSpeed * Time.deltaTime, 360f);
            float center = 1f, inner = 1f, outer = 1f, jitter = 0f;

            if (_shieldPhase == ShieldPhase.Forming)
            {
                float centerDuration = Mathf.Max(0.01f, centerFadeDuration);
                float arcDuration = Mathf.Max(0.01f, arcFadeDuration);
                float innerStart = centerDuration + Mathf.Max(0f, layerDelay);
                float outerStart = innerStart + arcDuration + Mathf.Max(0f, layerDelay);
                center = Fade(_shieldElapsed, centerDuration);
                inner = Fade(_shieldElapsed - innerStart, arcDuration);
                outer = Fade(_shieldElapsed - outerStart, arcDuration);

                if (_shieldElapsed >= outerStart + arcDuration) _shieldPhase = ShieldPhase.Active;
            }
            else if (_shieldPhase == ShieldPhase.Discharging)
            {
                float t = Mathf.Clamp01(_shieldElapsed / Mathf.Max(0.01f, shutdownDuration));
                float wave = _shieldElapsed * flickerFrequency * 2f * Mathf.PI;
                float flicker = Mathf.Sin(wave) + Mathf.Sin(wave * 0.61f) > 0f ? 1f : 0.12f;
                center = inner = outer = (1f - t) * flicker;
                jitter = Mathf.Sin(wave * 0.77f) * 8f;

                if (t >= 1f) { ResetShield(); return; }
            }

            if (shield != null) shield.SetPresentation(center, inner, outer, _innerAngle + jitter, _outerAngle - jitter);
        }

        private static float Fade(float elapsed, float duration) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

        private void Hide()
        {
            ResetShield();
            if (malfunctionSparks != null)
                malfunctionSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable() => Hide();
    }
}
