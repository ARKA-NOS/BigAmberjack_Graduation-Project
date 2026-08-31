using UnityEngine;

namespace Lrw.Script._Core
{
    public static class FDebug
    {
        public static void Log(object text, LogType type = LogType.Log)
        {
#if UNITY_EDITOR
            Debug.unityLogger.Log(type,text);
#endif
        }

        public static void LogWarning(object text)
            => Log(text, LogType.Warning);

        public static void LogError(object text)
            => Log(text, LogType.Error);
        
        public static void Assert(bool value,object text)
        {
            if(value) return;
            Log(text, LogType.Assert);
        }
    }
}