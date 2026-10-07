using System;

namespace Lrw.Script._Core.EnumSystem
{
    public static class EnumManager
    {
        public static T[] GetAllValue<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}