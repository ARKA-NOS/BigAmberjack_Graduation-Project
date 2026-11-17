using UnityEngine;

namespace _Scripts.UI.UI_System
{
    public abstract class AbstractWindow : MonoBehaviour,IWindow
    {
        public abstract void Open(object data);
        public abstract void Close();

    }
}