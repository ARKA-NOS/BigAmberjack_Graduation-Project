using System;
using System.Collections.Generic;

namespace Lrw.Script._Core._EventSystem
{
    public static class EventBus<T> where T : IEvent
    {
        public delegate void Event(T value);

        private static readonly Dictionary<object, Event> Events = new();
        private static readonly object Lock = new object();
        private static readonly object DefaultKey = new object();
        
        public static void Subscribe(Event callback) => Subscribe(DefaultKey, callback);
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
        public static void UnSubscribe(Event callback) => Subscribe(DefaultKey, callback);
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
        
        public static void Raise(T value) => Raise(DefaultKey, value);
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