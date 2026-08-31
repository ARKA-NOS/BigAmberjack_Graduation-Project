using UnityEngine;

namespace SDW.Scripts.CoreSystem
{
    // IInteraction을 구현하는 상호작용 오브젝트들의 공통 기반.
    // 상호작용 문구는 인스펙터에서 직접 지정할 수 있고, 비워두면 DefaultInteractionPrompt를 사용한다.
    public abstract class InteractableBase : MonoBehaviour, IInteraction
    {
        [SerializeField] private string interactionPrompt;

        public virtual bool CanInteract => true;
        public string InteractionPrompt { get; }
        public abstract void Interaction();
    }
}
