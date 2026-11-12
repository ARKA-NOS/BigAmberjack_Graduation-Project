using System;

namespace _Scripts.UI.Dialogue
{
    // 대화창 ViewModel이 읽는 Model. 게임 어디서든 연 대화(DialogueEvents)를 받는다. null이면 대화창을 닫는다.
    public interface IDialogueSource
    {
        event Action<DialogueData> DialogueChanged;
    }
}
