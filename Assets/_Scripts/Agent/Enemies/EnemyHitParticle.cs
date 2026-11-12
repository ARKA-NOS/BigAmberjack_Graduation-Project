using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 한 번 생성된 효과의 수명만 관리합니다. 적이 먼저 제거되어도 재생과 정리가 계속됩니다.
    [DisallowMultipleComponent]
    public sealed class EnemyHitParticle : MonoBehaviour
    {
        private ParticleSystem _root;
        private float _startedAt;
        private float _expiresAt;

        internal void Play(ParticleSystem root, float maximumLifetime)
        {
            _root = root;
            _root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (ParticleSystem particle in GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = false;
                main.playOnAwake = false;
                // 루트가 먼저 종료되어 자식 파편까지 제거하는 설정을 방지합니다.
                main.stopAction = ParticleSystemStopAction.None;
            }
            _startedAt = Time.time;
            _expiresAt = _startedAt + Mathf.Max(0.1f, maximumLifetime);
            _root.Play(true);
            if (TryGetComponent(out EnemyBloodParticle blood))
            {
                // 흔적 유지 시간은 방출이 아닌 표면 접촉부터 세므로 일반 효과보다 수명이 깁니다.
                _expiresAt = _startedAt + Mathf.Max(maximumLifetime, blood.MaximumLifetime);
                blood.Emit();
            }
            else if (TryGetComponent(out EnemyMetalFragmentParticle metal))
            {
                _expiresAt = _startedAt + Mathf.Max(maximumLifetime, metal.MaximumLifetime);
                metal.Emit(_root);
            }
        }

        private void Update()
        {
            if (_root == null || Time.time >= _expiresAt ||
                (Time.time > _startedAt + 0.05f && !_root.IsAlive(true)))
                Destroy(gameObject);
        }
    }
}
