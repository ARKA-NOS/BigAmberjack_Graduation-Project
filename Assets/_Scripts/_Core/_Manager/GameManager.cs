using UnityEngine;

namespace _Scripts._Core._Manager
{
    [DefaultExecutionOrder(-20)]
    public class GameManager : MonoBehaviour
    {
        private AbstractManager[] _managers;

        private static GameManager _instance;
        
        private void Awake()
        {
            if (_instance != null)
            {
                // 파괴는 프레임 끝에 처리되므로, 자식 매니저와 UI가 켜지지 않게 먼저 끈다.
                gameObject.SetActive(false);
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
            // 중복이라 초기화하지 않은 GameManager는 정리할 것이 없다.
            if (_managers == null) return;

            foreach (AbstractManager manager in _managers)
            {
                manager.GameEnd();
            }
        }
    }
}