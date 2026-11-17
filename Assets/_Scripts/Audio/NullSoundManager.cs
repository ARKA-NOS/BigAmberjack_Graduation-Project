using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Audio
{
    internal sealed class NullSoundManager : ISoundManager
    {
        public static readonly NullSoundManager Instance = new();

        public void PlayMusic(AudioClip clip) { }
        public void StopMusic() { }
        public void PlaySfx(SoundCueSO cue, Vector3 position) { }
        public void PlayUi(SoundCueSO cue) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            ServiceLocator.Register<ISoundManager>(Instance);
        }
    }
}
