using _Scripts._Core._EventSystem;

namespace _Scripts.UI.Dialogue
{
    // 대화창을 열고 닫는다. NPC 등 게임 쪽은 이것만 부르고, 대화창(ViewModel/View)은 모른다.
    public static class DialogueEvents
    {
        private static readonly DialogueChangeEvent DialogueChange = new();

        // 대화창을 이 내용으로 연다. 이미 열려 있으면 내용을 바꾼다.
        public static void Show(DialogueData dialogue)
            => EventBus<DialogueChangeEvent>.Raise(DialogueChange.Init(dialogue));

        public static void Hide()
            => EventBus<DialogueChangeEvent>.Raise(DialogueChange.Init(null));
    }

    // 값이 null이면 대화창을 닫는다.
    public class DialogueChangeEvent : TemplateEvent<DialogueChangeEvent, DialogueData>
    {
    }
}
