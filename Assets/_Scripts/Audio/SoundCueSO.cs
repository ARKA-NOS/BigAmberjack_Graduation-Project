using UnityEngine;

namespace _Scripts.Audio
{
    [CreateAssetMenu(menuName = "Audio/Sound Cue", fileName = "SoundCue")]
    public sealed class SoundCueSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField] private Vector2 _pitchRange = Vector2.one;
        [SerializeField, Range(0, 255)] private int _priority = 128;

        internal float Volume => Mathf.Clamp01(_volume);
        internal int Priority => Mathf.Clamp(_priority, 0, 255);

        internal AudioClip PickClip()
        {
            AudioClip selected = null;
            int validCount = 0;

            if (_clips == null) return null;

            // 빈 슬롯을 건너뛰면서 각 유효 클립을 같은 확률로 고른다.
            foreach (AudioClip clip in _clips)
            {
                if (clip == null) continue;
                if (Random.Range(0, ++validCount) == 0) selected = clip;
            }

            return selected;
        }

        internal float PickPitch()
        {
            float min = Mathf.Clamp(Mathf.Min(_pitchRange.x, _pitchRange.y), 0.1f, 3f);
            float max = Mathf.Clamp(Mathf.Max(_pitchRange.x, _pitchRange.y), 0.1f, 3f);
            return Random.Range(min, max);
        }
    }
}
