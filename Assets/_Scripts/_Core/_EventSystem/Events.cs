namespace _Scripts._Core._EventSystem
{
    public abstract class TemplateEvent<T,V> : IEvent where T : TemplateEvent<T,V>
    {
        public V Value { get; private set; }

        public T Init(V value)
        {
            Value = value;
            return this as T;
        }
    }
}