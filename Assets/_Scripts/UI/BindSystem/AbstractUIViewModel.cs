using System;
using UnityEngine;

namespace _Scripts.UI.BindSystem
{
    /*
    [CreateAssetMenu(fileName = "??? View Module", menuName = "UI/??? View Module")]
    */
    public abstract class AbstractUIViewModel<T> : ScriptableObject
    {
        public T BindData { get; private set; }
        
        public event Action OnBind;
        public virtual void Bind(T data)
        {
            BindData = data;
            OnBind?.Invoke();
        }
        
        
    }
}