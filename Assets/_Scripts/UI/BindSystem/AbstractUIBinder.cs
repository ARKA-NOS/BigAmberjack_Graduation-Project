using _Scripts._Core._Debug;
using UnityEngine;

namespace _Scripts.UI.BindSystem
{
    public abstract class AbstractUIBinder<T> : MonoBehaviour
    {
        [SerializeField] private AbstractUIViewModel<T> bindData;

        protected T CurrentValue => bindData != null ? bindData.BindData : default;

        protected virtual void Awake()
        {
            if (bindData != null)
            {
                bindData.OnBind += OnDataChanged;
            }
            else FDebug.LogError($"[{name}] BindData is null");
        }

        protected virtual void OnDestroy()
        {
            if (bindData != null)
            {
                bindData.OnBind -= OnDataChanged;
            }
        }

        protected virtual void Update()
        {
            if (bindData != null)
                DataUpdate();
        }
        
        protected abstract void DataUpdate();

        protected virtual void OnDataChanged() {}

    }
}