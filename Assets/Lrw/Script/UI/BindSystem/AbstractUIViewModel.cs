using UnityEngine;

namespace Lrw.Script.UI.BindSystem
{
    [CreateAssetMenu(fileName = "??? View Module", menuName = "UI/??? View Module", order = 0)]
    public abstract class AbstractUIViewModel<T> : ScriptableObject
    {
        public T BindData { get; private set; }
        
        public void Bind(T data)
        {
            BindData = data;
        }
        
    }
}