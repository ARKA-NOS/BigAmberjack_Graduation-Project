using Lean.Pool;
using UnityEngine;

namespace _Scripts.VFXSystem.Effect
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PooledAnimationEffect : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float lifetime;

        private Animator _animator;
        private float _releaseTime;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            Schedule(1f);
        }

        public void Play(float speed)
        {
            Schedule(speed);
        }

        private void Schedule(float speed)
        {
            float safeSpeed = Mathf.Max(0.01f, speed);
            _animator.speed = safeSpeed;
            _releaseTime = Time.time + GetLength() / safeSpeed;
        }

        private float GetLength()
        {
            if (lifetime > 0f) return lifetime;

            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
            if (controller == null) return 0f;

            float length = 0f;
            foreach (AnimationClip clip in controller.animationClips)
                length = Mathf.Max(length, clip.length);
            return length;
        }

        private void Update()
        {
            if (Time.time >= _releaseTime)
                Release();
        }

        private void Release()
        {
            if (LeanPool.Links.TryGetValue(gameObject, out LeanGameObjectPool pool) && pool != null)
                LeanPool.Despawn(gameObject);
            else
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (LeanPool.Links.TryGetValue(gameObject, out LeanGameObjectPool pool))
                LeanPool.Detach(gameObject, pool != null);
        }
    }
}
