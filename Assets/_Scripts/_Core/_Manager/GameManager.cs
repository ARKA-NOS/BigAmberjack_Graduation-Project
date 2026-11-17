using UnityEngine;

namespace _Scripts._Core._Manager
{
    [DefaultExecutionOrder(-20)]
    public class GameManager : MonoBehaviour
    {
        private AbstractManager[] _managers;

        private GameManager _instance;
        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
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

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
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