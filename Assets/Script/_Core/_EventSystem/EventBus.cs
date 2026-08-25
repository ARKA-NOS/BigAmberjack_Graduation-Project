using System;

namespace Script._Core._EventSystem
{
    public static class EventBus<T> where T : IEvent
    {
        public static event Action<T> Event;
        public static void Invoke(T value) => Event?.Invoke(value);
    }
}