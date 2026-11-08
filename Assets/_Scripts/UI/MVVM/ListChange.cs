namespace _Scripts.UI.MVVM
{
    public enum ListChangeKind
    {
        Add,
        Remove,
        Replace,
        // 목록 전체가 바뀌었다(Clear, SetAll, 구독 직후). View는 현재 목록으로 전부 다시 그린다.
        Reset,
    }

    // ObservableList가 구독자에게 보내는 변경 내용.
    public readonly struct ListChange<T>
    {
        public readonly ListChangeKind Kind;
        // Reset이면 -1.
        public readonly int Index;
        // Remove, Replace에서 빠진 항목.
        public readonly T OldItem;
        // Add, Replace에서 들어온 항목.
        public readonly T NewItem;

        private ListChange(ListChangeKind kind, int index, T oldItem, T newItem)
        {
            Kind = kind;
            Index = index;
            OldItem = oldItem;
            NewItem = newItem;
        }

        internal static ListChange<T> Add(int index, T item) => new(ListChangeKind.Add, index, default, item);
        internal static ListChange<T> Remove(int index, T item) => new(ListChangeKind.Remove, index, item, default);
        internal static ListChange<T> Replace(int index, T oldItem, T newItem) => new(ListChangeKind.Replace, index, oldItem, newItem);
        internal static ListChange<T> Reset() => new(ListChangeKind.Reset, -1, default, default);
    }
}
