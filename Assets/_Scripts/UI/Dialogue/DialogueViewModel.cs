using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.Dialogue
{
    // 화면 하단 대화창의 ViewModel. DialogueEvents로 연 대화를 Dialogue에 넣는다.
    // 선택지 클릭은 DialogueData.OnSelected(Action)로 대화를 연 쪽(NPC)에 바로 전달된다.
    [CreateAssetMenu(fileName = "Dialogue View Model", menuName = "UI/MVVM/Dialogue View Model")]
    public class DialogueViewModel : ViewModelBase<IDialogueSource>
    {
        private readonly Observable<DialogueData> _dialogue = new();
        private IDialogueSource _source;

        // 보여줄 대화. null이면 대화창을 닫는다.
        public IReadOnlyObservable<DialogueData> Dialogue => _dialogue;

        protected override void OnConnect(IDialogueSource source)
        {
            _source = source;
            _source.DialogueChanged += HandleDialogueChanged;
        }

        protected override void OnDisconnect()
        {
            _source.DialogueChanged -= HandleDialogueChanged;
            _source = null;
        }

        protected override void ResetState()
        {
            _dialogue.Value = null;
        }

        private void HandleDialogueChanged(DialogueData dialogue) => _dialogue.Value = dialogue;
    }
}
