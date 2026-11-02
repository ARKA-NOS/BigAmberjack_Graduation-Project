using Lrw.Script._Core._EventSystem;
using UnityEngine;

namespace Lrw.Script.UI.Template
{
    public abstract class EventListener<T> : MonoBehaviour where T : IEvent
    {
        
        protected virtual void Awake()
        {
            EventBus<T>.Subscribe(HandleEvent);
        }
        
        protected virtual void OnDestroy()
        {
            EventBus<T>.UnSubscribe(HandleEvent);
        }
        
        protected abstract void HandleEvent(T eventData);
        
    }
}