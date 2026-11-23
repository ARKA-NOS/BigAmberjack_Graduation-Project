using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.SettingsUI
{
    // "정말 포기하시겠습니까?" 확인 팝업. 프리팹으로 따로 두고, 보여 줄 때 확인 시 할 일을 넘긴다.
    // 문구는 messageText에 직접 적혀 있고, 게임 로직은 모르고 확인/취소만 알린다.
    public class GiveUpConfirmPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action _onConfirm;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(Confirm);
            cancelButton.onClick.RemoveListener(Hide);
        }

        public void Show(Action onConfirm)
        {
            _onConfirm = onConfirm;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            _onConfirm = null;
            gameObject.SetActive(false);
        }

        private void Confirm()
        {
            Action onConfirm = _onConfirm;
            Hide();
            onConfirm?.Invoke();
        }
    }
}
