using System;
using System.Linq;
using UnityEngine;

namespace Lrw.Script._Core._Manager
{
    /// <summary>
    /// GameManager는 실행시 자동 생성 됩니다.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class GameManager : MonoBehaviour
    {
        private AbstractManager[] _managers;
        
        private void Awake()
        {
            RuntimeInitialize();
        }
        
        private void RuntimeInitialize()
        {
            GameObject[] managerObjects = FindManagerTypes().Select(x => new GameObject(x.Name, x)).ToArray();

            foreach (GameObject obj in managerObjects)
            {
                obj.transform.SetParent(transform);
            }
            
            _managers = managerObjects.Select(x => x.GetComponent<AbstractManager>()).ToArray();
            
            foreach (AbstractManager manager in _managers)
            {
                manager.Initialize();
            }
        }
        
        private static Type[] FindManagerTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.ContainsGenericParameters &&
                    typeof(AbstractManager).IsAssignableFrom(type))
                .ToArray();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            GameObject manager = new GameObject("GameManager",typeof(GameManager));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                FDebug.LogError("GameManager를 수동으로 생성하지 마시오.");
            }
        }
#endif
    }
}