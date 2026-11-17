using System;

namespace _Scripts._Core {
    public interface IReadOnlyNotifyValue<T> {
        public event Action<ChangeData<T>> OnValueChanged;
        public T Value { get; }
    }

    public class NotifyValue<T> : IReadOnlyNotifyValue<T> {
        public event Action<ChangeData<T>> OnValueChanged;
        private T _value;

        public T Value {
            get => _value;
            set
            {
                T before = _value;
                _value = value;
                if ((before == null && value != null) || (before != null && value == null) || !before.Equals(_value))
                    OnValueChanged?.Invoke(new ChangeData<T>(before, value));
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