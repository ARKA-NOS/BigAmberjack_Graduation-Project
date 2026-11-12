using _Scripts._Core._Debug;
using Lean.Pool;
using UnityEngine;

namespace _Scripts._Core.PoolSystem
{
    [CreateAssetMenu(fileName = "Pool SO", menuName = "PoolSystem/GameObject Pool SO", order = 0)]
    public class PoolSO : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        
        public GameObject Spawn()
            => LeanPool.Spawn(prefab);
        
        public T Spawn<T>()
        {
            if (LeanPool.Spawn(prefab).TryGetComponent(out T result))
                return result;
            
            FDebug.LogError("Pool Prefab doesn't have a " + typeof(T).Name, this);
            return default;
        }
        
        public void Despawn(GameObject go)
            => LeanPool.Despawn(go);
        
        public static void DespawnAll()
            => LeanPool.DespawnAll();
        
    }
}