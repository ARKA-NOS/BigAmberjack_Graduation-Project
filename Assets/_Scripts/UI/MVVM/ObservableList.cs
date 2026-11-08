using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.UI.MVVM
{
    // 추가·삭제·교체·초기화를 구독자에게 알리는 목록. ViewModel 안에서 private으로 들고, 밖에는 IReadOnlyObservableList로 노출한다.
    // 인덱서로 넣으면 같은 값이어도 Replace를 알린다(칸 내용을 다시 그리라는 뜻으로 쓴다).
    public sealed class ObservableList<T> : IReadOnlyObservableList<T>
    {
        private readonly List<T> _items;
        private readonly SubscriberList<Action<ListChange<T>>> _subscribers = new();

        public ObservableList()
        {
            _items = new List<T>();
        }

        public ObservableList(int capacity)
        {
            _items = new List<T>(capacity);
        }

        public int Count => _items.Count;

        public T this[int index]
        {
            get => _items[index];
            set
            {
                T oldItem = _items[index];
                _items[index] = value;
                Notify(ListChange<T>.Replace(index, oldItem, value));
            }
        }

        public void Add(T item)
        {
            _items.Add(item);
            Notify(ListChange<T>.Add(_items.Count - 1, item));
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            Notify(ListChange<T>.Add(index, item));
        }

        public void RemoveAt(int index)
        {
            T item = _items[index];
            _items.RemoveAt(index);
            Notify(ListChange<T>.Remove(index, item));
        }

        public bool Remove(T item)
        {
            int index = _items.IndexOf(item);
            if (index < 0) return false;

            RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            _items.Clear();
            Notify(ListChange<T>.Reset());
        }

        // 목록을 통째로 바꾸고 Reset을 한 번만 알린다.
        public void SetAll(IEnumerable<T> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            Notify(ListChange<T>.Reset());
        }

        // 칸에 든 참조 타입의 내부만 바뀌었을 때 그 칸을 다시 그리라고 알린다.
        public void NotifyItemChanged(int index)
        {
            T item = _items[index];
            Notify(ListChange<T>.Replace(index, item, item));
        }

        public int IndexOf(T item) => _items.IndexOf(item);

        public bool Contains(T item) => _items.Contains(item);

        public IDisposable Subscribe(Action<ListChange<T>> onChanged, bool invokeResetNow = true)
        {
            if (onChanged == null) throw new ArgumentNullException(nameof(onChanged));

            IDisposable subscription = _subscribers.Add(onChanged);
            if (invokeResetNow) Invoke(onChanged, ListChange<T>.Reset());
            return subscription;
        }

        public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        private void Notify(ListChange<T> change)
        {
            int count = _subscribers.BeginNotify();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Action<ListChange<T>> subscriber = _subscribers[i];
                    if (subscriber != null) Invoke(subscriber, change);
                }
            }
            finally
            {
                _subscribers.EndNotify();
            }
        }

        // 구독자 하나가 예외를 던져도 나머지 구독자는 알림을 받는다.
        private static void Invoke(Action<ListChange<T>> subscriber, ListChange<T> change)
        {
            try
            {
                subscriber(change);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
