using System;
using System.Collections.Generic;

namespace _Scripts._Core
{
    public static class CustomLinq
    {
        public static void Foreach<T>(this IEnumerable<T> arr, Action<T> action)
        {
            foreach (var item in arr)
            {
                action(item);
            }
        }
        
        public static T FirstOrDefault<T>(this IEnumerable<T> source,
            Func<T, bool> check, T defaultValue)
        {
            foreach (var t in source)
                if(check(t))
                    return t;
            return defaultValue;
        }
    }
}