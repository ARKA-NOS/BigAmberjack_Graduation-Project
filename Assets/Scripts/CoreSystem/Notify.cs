using System;

namespace CoreSystem {
    public interface IReadOnlyNotifyValue<T> {
        public event Action<T, T> OnValueChanged;
        public T Value { get; }
    }

    public class NotifyValue<T> : IReadOnlyNotifyValue<T> {
        public event Action<T, T> OnValueChanged;
        private T _value;

        public T Value {
            get => _value;
            set {
                T before = _value;
                _value = value;
                if ((before == null && value != null) || before!.Equals(_value) == false)
                    OnValueChanged?.Invoke(before, value);
            }
        }

        public NotifyValue() {
            _value = default(T);
        }

        public NotifyValue(T value) {
            _value = value;
        }
    }
}