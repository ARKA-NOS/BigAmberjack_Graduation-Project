using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Audio
{
    public sealed class SoundEmitter : MonoBehaviour
    {
        [SerializeField] private SoundCueSO _cue;

        public void PlaySfx() => ServiceLocator.Get<ISoundManager>()?.PlaySfx(_cue, transform.position);
        public void PlayUi() => ServiceLocator.Get<ISoundManager>()?.PlayUi(_cue);
    }
}
