using System;
using System.Linq;

namespace Lrw.Script._Core.EnumSystem
{
    public static class EnumManager
    {
        public static T[] GetAllValue<T>() where T : Enum
        {
            var arr = Enum.GetValues(typeof(T));
            return arr as T[];
        }
    }
}