using System;
using System.Linq;
using UnityEngine;

namespace Lrw.Script._Core._Manager
{
    [DefaultExecutionOrder(-20)]
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private string[] managerTypeNames;
        
        private AbstractManager[] _managers;
        
        private void Awake()
        {
            EditorInitialize();
            RuntimeInitialize();
        }
        
        private void EditorInitialize()
        {
#if UNITY_EDITOR
            managerTypeNames = UnityEditor.TypeCache.GetTypesDerivedFrom<AbstractManager>()
                .Where(type => type.IsClass && !type.IsAbstract).Select(x => x.FullName).ToArray();
#endif
        }

        private void RuntimeInitialize()
        {
            Type[] managerTypes = managerTypeNames.Select(x => Type.GetType(x)).ToArray();
            GameObject[] managerObjects = managerTypes.Select(x => new GameObject(x.Name, x)).ToArray();

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
        
        
        
        
    }
}