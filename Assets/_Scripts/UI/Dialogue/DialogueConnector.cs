using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.Dialogue
{
    // 대화창에 붙어, DialogueEvents로 연 대화를 DialogueViewModel에 연결한다.
    // 대화는 특정 오브젝트가 아니라 EventBus로 오므로 UI 쪽에 붙인다.
    public class DialogueConnector : ConnectorBase
    {
        [SerializeField] private DialogueViewModel viewModel;

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"[{name}] DialogueViewModel이 지정되지 않았습니다.", this);
                return;
            }

            viewModel.Connect(new EventBusDialogueSource());
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
