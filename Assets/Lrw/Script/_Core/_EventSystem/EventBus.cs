using System;
using System.Collections.Generic;
using System.Linq;

namespace Lrw.Script._Core._EventSystem
{
    public static class EventBus<T> where T : IEvent
    {
        public delegate void Event(T value);

        private static readonly Dictionary<object, Event> Events = new();
        private static readonly object Lock = new();

        public static void Subscribe(object key, Event callback)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            lock (Lock)
            {
                if (Events.TryGetValue(key, out Event current))
                {
                    Events[key] = current + callback;
                }
                else
                {
                    Events.Add(key, callback);
                }
            }
        }

        public static void UnSubscribe(object key, Event callback)
        {
            if (key == null || callback == null)
                return;

            lock (Lock)
            {
                if (!Events.TryGetValue(key, out Event current))
                    return;

                Event remaining = current - callback;

                if (remaining == null)
                {
                    Events.Remove(key);
                }
                else
                {
                    Events[key] = remaining;
                }
            }
        }

        public static void Raise(object key, T value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Event callback;

            lock (Lock)
            {
                if (!Events.TryGetValue(key, out callback))
                    return;
            }
        
            callback.Invoke(value);
        }

        public static void RaiseAll(T value)
        {
            Event[] arr;
            
            lock (Lock)
            {
                arr = Events.Values.ToArray();
            }
            
            foreach (var evt in arr)
            {
                try
                {
                    evt?.Invoke(value);
                }
                catch (Exception e)
                {
                    FDebug.LogError(e);
                }
            }
        }
    
        public static void Clear(object key)
        {
            if (key == null)
                return;

            lock (Lock)
            {
                Events.Remove(key);
            }
        }

        public static void ClearAll()
        {
            lock (Lock)
            {
                Events.Clear();
            }
        }
    }
}