namespace _Scripts._Core
{
    public struct ChangeData<T>
    {
        public T OldValue { get; private set; }
        public T NewValue { get; private set; }
        
        public ChangeData(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        
    }
}