using System;
using _Scripts._Core._EventSystem;

namespace _Scripts.UI.Dialogue
{
    // DialogueEvents로 보낸 대화(EventBus<DialogueChangeEvent>)를 IDialogueSource로 감싼다. DialogueConnector가 만든다.
    public class EventBusDialogueSource : IDialogueSource
    {
        private Action<DialogueData> _dialogueChanged;

        // 구독자가 생기면 EventBus를 구독하고, 모두 빠지면 해제한다. EventBus는 정적이라 씬이 바뀌어도 남으므로 해제가 중요하다.
        public event Action<DialogueData> DialogueChanged
        {
            add
            {
                if (_dialogueChanged == null) EventBus<DialogueChangeEvent>.Subscribe(HandleDialogueChange);
                _dialogueChanged += value;
            }
            remove
            {
                _dialogueChanged -= value;
                if (_dialogueChanged == null) EventBus<DialogueChangeEvent>.UnSubscribe(HandleDialogueChange);
            }
        }

        private void HandleDialogueChange(DialogueChangeEvent evt) => _dialogueChanged?.Invoke(evt.Value);
    }
}
