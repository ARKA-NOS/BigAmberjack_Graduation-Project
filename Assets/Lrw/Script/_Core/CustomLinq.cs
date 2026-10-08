using System;
using System.Collections.Generic;

namespace Lrw.Script._Core
{
    public static class CustomLinq
    {
        public static void Foreach<T>(this IEnumerable<T> arr,Action<T> action)
        {
            foreach (var item in arr)
            {
                action(item);
            }
        }
    }
}