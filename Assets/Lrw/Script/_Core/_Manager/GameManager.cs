using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lrw.Script._Core._Manager
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private string[] managerTypeNames;
        
        private AbstractManager[] _managers;
        
        private void Awake()
        {
            EditorInitialize();
            RuntimeInitialize();
        }
        
        [ContextMenu("EditorInitialize")]
        private void EditorInitialize()
        {
#if UNITY_EDITOR
            managerTypeNames = TypeCache.GetTypesDerivedFrom<AbstractManager>()
                .Where(type => type.IsClass && !type.IsAbstract).Select(x => x.FullName).ToArray();
#endif
        }

        private void RuntimeInitialize()
        {
            var managerTypes = managerTypeNames.Select(x => Type.GetType(x));
            var managerObjects = managerTypes.Select(x => new GameObject(x.Name, x));

            foreach (var obj in managerObjects)
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