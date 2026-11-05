using UnityEngine;

namespace SDW.Scripts.CoreSystem
{
    // IInteraction을 구현하는 상호작용 오브젝트들의 공통 기반.
    // 상호작용 문구는 인스펙터에서 직접 지정하며, 자식 클래스에서 override해 다른 값을 돌려줄 수 있다.
    public abstract class InteractableBase : MonoBehaviour, IInteraction
    {
        [SerializeField] private string interactionPrompt;

        public virtual bool CanInteract => true;
        public virtual string InteractionPrompt => interactionPrompt;
        public abstract void Interaction();
    }
}
