using UnityEngine;

namespace Lrw.Script.UI
{
    public abstract class AbstractWindow : MonoBehaviour,IWindow
    {
        public abstract void Open(object data);
        public abstract void Close();

    }
}