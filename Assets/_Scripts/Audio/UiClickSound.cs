using _Scripts._Core._ServiceLocator;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Audio
{
    // 이 오브젝트와 자식의 모든 Button(비활성 포함) 클릭 시 UI 사운드를 낸다.
    // 런타임에 생성되는 버튼은 포함되지 않는다. 그런 경우 해당 버튼에 따로 붙인다.
    [DisallowMultipleComponent]
    public sealed class UiClickSound : MonoBehaviour
    {
        [SerializeField] private SoundCueSO _cue;

        private Button[] _buttons;

        private void Awake() => _buttons = GetComponentsInChildren<Button>(true);

        private void OnEnable()
        {
            foreach (Button button in _buttons)
                if (button != null) button.onClick.AddListener(Play);
        }

        private void OnDisable()
        {
            foreach (Button button in _buttons)
                if (button != null) button.onClick.RemoveListener(Play);
        }

        private void Play() => ServiceLocator.Get<ISoundManager>()?.PlayUi(_cue);
    }
}
