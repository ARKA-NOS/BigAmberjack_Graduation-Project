using System;
using UnityEngine;

namespace Lrw.Script._Core._Manager
{
    [DefaultExecutionOrder(-20)]
    public class GameManager : MonoBehaviour
    {
        private AbstractManager[] _managers;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            _managers = GetComponentsInChildren<AbstractManager>();
            
            foreach (AbstractManager manager in _managers)
            {
                manager.Initialize();
            }
        }

        private void OnDestroy()
        {
            GameEnd();
        }

        private void GameEnd()
        {
            foreach (AbstractManager manager in _managers)
            {
                manager.GameEnd();
            }
        }
    }
}