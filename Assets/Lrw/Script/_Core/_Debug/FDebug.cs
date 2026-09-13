using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine
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
    
        public static void Assert([DoesNotReturnIf(false)] bool value,object text)
        {
            if(value) return;
            if(text == null) throw new Exception("test is null");
            throw new Exception(text.ToString());
        }
    }
}
