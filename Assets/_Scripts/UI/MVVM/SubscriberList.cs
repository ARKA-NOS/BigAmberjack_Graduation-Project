using System;
using System.Collections.Generic;

namespace _Scripts.UI.MVVM
{
    // Observable / ObservableList가 공유하는 구독자 목록.
    // 알림 도중에 구독·해제가 일어나도 안전하게, 알림 중에는 해제를 null 표시로 미뤘다가 알림이 끝나면 정리한다.
    internal sealed class SubscriberList<TCallback> where TCallback : class
    {
        private readonly List<TCallback> _callbacks = new();
        private int _notifyDepth;
        private bool _needsCompact;

        public IDisposable Add(TCallback callback)
        {
            _callbacks.Add(callback);
            return Disposable.Create(() => Remove(callback));
        }

        // 반환값까지만 순회한다. 알림 도중에 추가된 구독자는 이번 알림을 받지 않는다(구독할 때 현재 값을 이미 받는다).
        public int BeginNotify()
        {
            _notifyDepth++;
            return _callbacks.Count;
        }

        // 해제된 칸은 null이다.
        public TCallback this[int index] => _callbacks[index];

        public void EndNotify()
        {
            _notifyDepth--;
            if (_notifyDepth > 0 || !_needsCompact) return;

            _callbacks.RemoveAll(callback => callback == null);
            _needsCompact = false;
        }

        private void Remove(TCallback callback)
        {
            int index = _callbacks.IndexOf(callback);
            if (index < 0) return;

            if (_notifyDepth > 0)
            {
                _callbacks[index] = null;
                _needsCompact = true;
            }
            else
            {
                _callbacks.RemoveAt(index);
            }
        }
    }
}
