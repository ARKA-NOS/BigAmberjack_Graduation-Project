using UnityEngine;

namespace Lrw.Script.UI.BindSystem
{
    public abstract class AbstractUIBinder<T> : MonoBehaviour
    {
        [SerializeField] private AbstractUIViewModel<T> bindData;

        protected T CurrentValue => bindData != null ? bindData.BindData : default;

        protected virtual void Update()
        {
            if (bindData != null)
                DataUpdate();
        }
        
        protected abstract void DataUpdate();
        
    }
}