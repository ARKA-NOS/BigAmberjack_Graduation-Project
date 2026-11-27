using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Audio
{
    // 씬의 배경 음악을 정한다. 씬마다 하나 두고 클립을 넣으면 씬이 시작될 때 재생한다.
    // 같은 클립이면 SoundManager가 끊지 않고 이어서 재생한다. 비워 두면 이전 음악이 그대로 이어진다.
    [DisallowMultipleComponent]
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;
        
        // SoundManager는 GameManager의 Awake에서 등록되므로 Start에서 부른다.
        private void Start() => ServiceLocator.Get<ISoundManager>()?.PlayMusic(_clip);
    }
}
