using System;
using System.Collections.Generic;

namespace Lrw.Script._Core._ServiceLocator
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _service = new();

        public static void Register<T>(T service)
        {
            _service[typeof(T)] = service;
        }
        
        public static T Get<T>()
        {
            if(_service.TryGetValue(typeof(T), out object service))
                return (T)service;
            
            return default;
        }
        
    }
}