using System.Collections.Generic;
using Lrw.Script.UI.BindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SDW.Scripts.UI.Dialogue
{
    // 화면 하단 대화창(이름표 + 대사 + 선택지). DialogueViewModel에 Bind된 DialogueData를 표시한다.
    // 이 컴포넌트가 붙은 오브젝트는 항상 켜 두고, 대화창 표시는 dialogueRoot로 켜고 끈다.
    // (꺼져 있으면 Awake가 실행되지 않아 ViewModel 구독이 빠진다)
    public class DialogueUIBinder : AbstractUIBinder<DialogueData>
    {
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private Transform choiceContainer;
        [Tooltip("선택지 버튼 템플릿. 비활성 상태로 두고 선택지 수만큼 복제한다.")]
        [SerializeField] private Button choiceButtonTemplate;

        private readonly List<Button> _choiceButtons = new();

        protected override void Awake()
        {
            base.Awake();
            choiceButtonTemplate.gameObject.SetActive(false);
            OnDataChanged();
        }

        // 대화 내용은 Bind 시점에만 바뀌므로 매 프레임 갱신할 것이 없다.
        protected override void DataUpdate()
        {
        }

        protected override void OnDataChanged()
        {
            DialogueData data = CurrentValue;
            ClearChoices();

            if (data == null)
            {
                dialogueRoot.SetActive(false);
                return;
            }

            nameText.text = data.Speaker;
            lineText.text = data.Line;

            for (int i = 0; i < data.Choices.Count; i++)
            {
                Button button = Instantiate(choiceButtonTemplate, choiceContainer);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = data.Choices[i];

                int index = i;
                button.onClick.AddListener(() => data.OnSelected?.Invoke(index));
                _choiceButtons.Add(button);
            }

            choiceContainer.gameObject.SetActive(data.Choices.Count > 0);
            dialogueRoot.SetActive(true);
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
