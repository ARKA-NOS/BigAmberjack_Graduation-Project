using _Scripts._Core._Debug;
using Lean.Pool;
using UnityEngine;

namespace _Scripts._Core.PoolSystem
{
    [CreateAssetMenu(fileName = "Pool SO", menuName = "PoolSystem/GameObject Pool SO", order = 0)]
    public class PoolSO : ScriptableObject
    {
        [field:SerializeField] public GameObject Prefab { get; private set; }
        
        public GameObject Spawn()
            => LeanPool.Spawn(Prefab);
        
        public void Despawn(GameObject go)
            => LeanPool.Despawn(go);
        
        public T Spawn<T>()
        {
            if (LeanPool.Spawn(Prefab).TryGetComponent(out T result))
                return result;
            
            FDebug.LogError("Pool Prefab doesn't have a " + typeof(T).Name, this);
            return default;
        }
        
    }
}