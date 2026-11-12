namespace _Scripts._Core
{
    public struct ChangeData<T>
    {
        public T OldValue;
        public T NewValue;
        
        public ChangeData(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        
    }
}