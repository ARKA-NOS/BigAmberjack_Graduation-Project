using System;

namespace Lrw.Script._Core.EnumSystem
{
    public static class EnumManager<T> where T : Enum
    {
        public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
    }
}