using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 필요한 발사체에만 붙이는 표시 컴포넌트. 충돌/피해 판정은 EnemyProjectile이 담당합니다.
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyProjectile))]
    public sealed class EnemyProjectileVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField, Min(1f)] private float flightFramesPerSecond = 12f;
        [SerializeField] private Sprite[] impactFrames;
        [SerializeField, Min(1f)] private float impactFramesPerSecond = 18f;
        [SerializeField, Min(0.01f)] private float impactScale = 1.4f;
        private float _startedAt;
        private float _impactRate;
        public bool IsImpactPlaying { get; private set; }

        private void Awake()
        {
            _startedAt = Time.time;
            if (visual != null && flightFrames != null && flightFrames.Length > 0) visual.sprite = flightFrames[0];
        }

        private void Update()
        {
            Sprite[] frames = IsImpactPlaying ? impactFrames : flightFrames;
            if (visual == null || frames == null || frames.Length == 0) return;
            float rate = IsImpactPlaying ? _impactRate : Mathf.Max(1f, flightFramesPerSecond);
            int frame = Mathf.FloorToInt((Time.time - _startedAt) * rate);
            frame = IsImpactPlaying ? Mathf.Min(frame, frames.Length - 1) : frame % frames.Length;
            visual.sprite = frames[frame];
        }

        internal float PlayImpact(Vector2 point)
        {
            if (IsImpactPlaying || visual == null || impactFrames == null || impactFrames.Length == 0) return 0f;
            IsImpactPlaying = true;
            _startedAt = Time.time;
            _impactRate = Mathf.Max(1f, impactFramesPerSecond);
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            visual.transform.localScale *= Mathf.Max(0.01f, impactScale);
            visual.sprite = impactFrames[0];
            return impactFrames.Length / _impactRate;
        }
    }
}
