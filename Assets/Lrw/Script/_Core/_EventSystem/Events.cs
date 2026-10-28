namespace Lrw.Script._Core._EventSystem
{
    public abstract class Event<T,V> : IEvent where T : Event<T,V>
    {
        public V Value { get; private set; }

        public T Init(V value)
        {
            Value = value;
            return this as T;
        }
    }
}