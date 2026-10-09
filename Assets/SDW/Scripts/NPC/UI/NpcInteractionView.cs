using System;
using System.Collections.Generic;
using SDW.Scripts.NPC.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SDW.Scripts.NPC.UI
{
    // NPC 머리 위 프롬프트와 화면 하단 대화창(이름표 + 대사 + 선택지)을 제어한다.
    public class NpcInteractionView : MonoBehaviour
    {
        [Header("프롬프트")]
        [SerializeField] private GameObject promptRoot;

        [Header("대화창")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private Transform choiceContainer;
        [Tooltip("선택지 버튼 템플릿. 비활성 상태로 두고 선택지 수만큼 복제한다.")]
        [SerializeField] private Button choiceButtonTemplate;

        private readonly List<Button> _choiceButtons = new();

        private void Awake()
        {
            choiceButtonTemplate.gameObject.SetActive(false);
            HidePrompt();
            CloseDialogue();
        }

        public void ShowPrompt()
        {
            promptRoot.SetActive(true);
        }

        public void HidePrompt()
        {
            promptRoot.SetActive(false);
        }

        public void OpenDialogue(NpcDataSO data, Action<int> onSelected)
        {
            ClearChoices();

            nameText.text = data.NpcName;
            lineText.text = data.FirstDialogue;

            for (int i = 0; i < data.Choices.Count; i++)
            {
                Button button = Instantiate(choiceButtonTemplate, choiceContainer);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = data.Choices[i].ChoiceText;

                int index = i;
                button.onClick.AddListener(() => onSelected?.Invoke(index));
                _choiceButtons.Add(button);
            }

            choiceContainer.gameObject.SetActive(true);
            HidePrompt();
            dialogueRoot.SetActive(true);
        }

        // 대사만 교체하고 선택지는 숨긴다. (취소 대사 등)
        public void ShowLine(string line)
        {
            lineText.text = line;
            choiceContainer.gameObject.SetActive(false);
        }

        public void CloseDialogue()
        {
            dialogueRoot.SetActive(false);
            ClearChoices();
        }

        private void ClearChoices()
        {
            // Destroy는 프레임 끝에 처리되므로, 같은 프레임에 다시 열어도 겹쳐 보이지 않게 먼저 끈다.
            foreach (Button button in _choiceButtons)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            _choiceButtons.Clear();
        }
    }
}
