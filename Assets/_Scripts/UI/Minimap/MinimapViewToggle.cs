using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 작은 미니맵과 전체 맵을 서로 바꿔 보여준다.
    // TODO: PlayerInputSo에 미니맵 키(M)가 생기면 그 이벤트에서 Toggle을 호출한다. 지금은 ContextMenu로만 확인한다.
    public class MinimapViewToggle : MonoBehaviour
    {
        [SerializeField] private GameObject miniMapView;
        [SerializeField] private GameObject fullMapView;

        public bool IsFullMapOpen { get; private set; }

        private void Start()
        {
            SetFullMapOpen(false);
        }

        [ContextMenu("Toggle")]
        public void Toggle()
        {
            SetFullMapOpen(!IsFullMapOpen);
        }

        public void SetFullMapOpen(bool open)
        {
            IsFullMapOpen = open;

            if (miniMapView != null)
                miniMapView.SetActive(!open);

            if (fullMapView != null)
                fullMapView.SetActive(open);
        }
    }
}
