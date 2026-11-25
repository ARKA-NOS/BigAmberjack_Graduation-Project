using _Scripts.Maps.New_Map;
using _Scripts.UI.RunEnd;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.SettingsUI
{
    // 설정 창의 "게임 포기하기" 버튼. 누르면 확인 팝업을 띄우고, 확인하면 설정 창을 닫은 뒤 런을 사망과 같은 방식으로 끝낸다.
    // 이후 결과창과 씬 이동은 RunEnd 쪽이 처리한다. 팝업은 프리팹을 popupParent 아래에 만들어 쓴다.
    public class SettingsGiveUpSection : MonoBehaviour
    {
        [SerializeField] private SettingsWindow window;
        [SerializeField] private Button giveUpButton;
        [SerializeField] private GiveUpConfirmPopup popupPrefab;
        [Tooltip("팝업이 만들어질 부모. 설정 창 전체를 덮도록 창 루트 근처의 RectTransform을 넣는다.")]
        [SerializeField] private RectTransform popupParent;

        private GiveUpConfirmPopup _popup;

        public bool IsPopupOpen => _popup != null && _popup.IsOpen;

        private void Awake()
        {
            _popup = Instantiate(popupPrefab, popupParent);
            _popup.Hide();
            giveUpButton.onClick.AddListener(OpenPopup);
        }

        private void OnDestroy()
        {
            giveUpButton.onClick.RemoveListener(OpenPopup);
        }

        // 탐험 중(씬에 맵 생성기가 있을 때, 보스 맵 포함)에만 포기 행을 보여 준다. 설정 창을 열 때마다 갱신한다.
        public void Refresh()
        {
            bool inRun = FindAnyObjectByType<MapGenerator>() != null;
            gameObject.SetActive(inRun);
            if (!inRun) ClosePopup();
        }

        public void ClosePopup()
        {
            if (_popup != null) _popup.Hide();
        }

        private void OpenPopup() => _popup.Show(GiveUp);

        private void GiveUp()
        {
            window.RequestClose();
            RunEndEvents.RaiseRunEnded(RunEndReason.PlayerDead);
        }
    }
}
