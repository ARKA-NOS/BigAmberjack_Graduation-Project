using UnityEngine;

namespace Lrw.Script._Core._Manager
{
    public abstract class AbstractManager : MonoBehaviour
    {
        public abstract void Initialize();
        public virtual void AfterInitialize() {}

        public virtual void GameEnd() {}
    }
}