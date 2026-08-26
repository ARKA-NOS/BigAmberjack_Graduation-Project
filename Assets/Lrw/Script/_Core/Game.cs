using System;

namespace Lrw.Script._Core
{
    public static class Game
    {
        public static event Action OnGameExit;
        
        public static void Exit()
        {
            OnGameExit?.Invoke();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}