using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Dialogue
{
    // 화면 하단 대화창(이름표 + 대사 + 선택지). DialogueViewModel의 대화를 표시하고, 없으면(null) dialogueRoot를 끈다.
    public class DialogueView : ViewBase<DialogueViewModel>
    {
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
            dialogueRoot.SetActive(false);
        }

        protected override void OnBind(DialogueViewModel vm)
        {
            vm.Dialogue.Subscribe(Show).AddTo(Disposables);
        }

        // 꺼질 때는 닫아 둔다. 다시 켜지면 구독하면서 지금 대화로 다시 그린다.
        protected override void OnUnbind()
        {
            ClearChoices();
            dialogueRoot.SetActive(false);
        }

        private void Show(DialogueData data)
        {
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
                if (button == null) continue;

                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            _choiceButtons.Clear();
        }
    }
}
