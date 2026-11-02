using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lrw.Script._Core._Debug
{
    public static class FDebug
    {
        private static void FLog(object text, Object obj, LogType type)
        {
#if UNITY_EDITOR
            Debug.unityLogger.Log(type,text,obj);
#endif
        }
        
        public static void Log(object text, Object obj = null)
        {
            FLog(text, obj, LogType.Log);
        }

        public static void LogWarning(object text, Object obj = null)
            => FLog(text, obj, LogType.Warning);

        public static void LogError(object text, Object obj = null)
            => FLog(text, obj, LogType.Error);
    
        public static void Assert([DoesNotReturnIf(false)] bool value, object text)
        {
            if(value) return;
            if(text == null) throw new Exception("test is null");
            throw new Exception(text.ToString());
        }
    }
}
