using UnityEngine;

namespace _Scripts._Core._Manager
{
    public abstract class AbstractManager : MonoBehaviour
    {
        public abstract void Initialize();
        public virtual void AfterInitialize() {}

        public abstract void GameEnd();
    }
}