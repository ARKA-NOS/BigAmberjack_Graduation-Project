using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.UI.MVVM.Observables
{
    // 값이 바뀔 때만 구독자에게 알리는 값 상자. ViewModel 안에서 private으로 들고, 밖에는 IReadOnlyObservable로 노출한다.
    // [Serializable]이 아니라서 ScriptableObject ViewModel에 넣어도 에셋에 저장되지 않는다.
    public sealed class Observable<T> : IReadOnlyObservable<T>
    {
        private readonly SubscriberList<Action<T, T>> _subscribers = new();
        private readonly IEqualityComparer<T> _comparer;
        private T _value;

        public Observable(T initialValue = default, IEqualityComparer<T> comparer = null)
        {
            _value = initialValue;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        // 같은 값이면 알리지 않는다.
        public T Value
        {
            get => _value;
            set
            {
                if (_comparer.Equals(_value, value)) return;

                T prev = _value;
                _value = value;
                Notify(prev, value);
            }
        }

        public void SetWithoutNotify(T value) => _value = value;

        // 참조 타입의 내부만 바뀌어서 Value setter가 변화를 알 수 없을 때 직접 알린다.
        public void ForceNotify() => Notify(_value, _value);

        public IDisposable Subscribe(Action<T> onChanged, bool invokeNow = true)
        {
            if (onChanged == null) throw new ArgumentNullException(nameof(onChanged));
            return Subscribe((_, current) => onChanged(current), invokeNow);
        }

        public IDisposable Subscribe(Action<T, T> onChanged, bool invokeNow = true)
        {
            if (onChanged == null) throw new ArgumentNullException(nameof(onChanged));

            IDisposable subscription = _subscribers.Add(onChanged);
            if (invokeNow) Invoke(onChanged, _value, _value);
            return subscription;
        }

        private void Notify(T prev, T current)
        {
            int count = _subscribers.BeginNotify();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Action<T, T> subscriber = _subscribers[i];
                    if (subscriber != null) Invoke(subscriber, prev, current);
                }
            }
            finally
            {
                _subscribers.EndNotify();
            }
        }

        // 구독자 하나가 예외를 던져도 나머지 구독자는 알림을 받는다.
        private static void Invoke(Action<T, T> subscriber, T prev, T current)
        {
            try
            {
                subscriber(prev, current);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
