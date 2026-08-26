using UnityEngine;

namespace CoreSystem.Effect
{
    public class PlayParticleVFX : MonoBehaviour, IPlayableVFX
    {
        [field: SerializeField] public AssetNameSo VfxName { get; private set; }
        [field: SerializeField] public float VfxDuration { get; private set; }
        [SerializeField] private ParticleSystem[] particles;
        
        public void PlayVFX(Vector3 position, Quaternion rotation)
        {
            // transform의 position, rotation은 c++ 구조체로 구현되어 있어, getter setter를 빈번하게 호출하면 성능 저하의 원인이 될 수 있음.
            transform.SetPositionAndRotation(position, rotation); // 이렇게 할 수 있으면 이렇게 하기
            PlayVFX();
        }

        public void PlayVFX()
        {
            foreach (ParticleSystem particle in particles)
            {
                particle.Play();
            }
        }

        public void StopVFX()
        {
            foreach (ParticleSystem particle in particles)
            {
                particle.Stop();
            }
        }
    }
}