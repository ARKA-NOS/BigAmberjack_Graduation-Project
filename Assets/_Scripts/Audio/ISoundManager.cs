using UnityEngine;

namespace _Scripts.Audio
{
    public interface ISoundManager
    {
        void PlayMusic(AudioClip clip);

        void StopMusic();

        void PlaySfx(SoundCueSO cue, Vector3 position);

        void PlayUi(SoundCueSO cue);
    }
}
