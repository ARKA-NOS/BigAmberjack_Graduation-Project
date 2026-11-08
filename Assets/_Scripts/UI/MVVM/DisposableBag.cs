using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.UI.MVVM
{
    // 구독 핸들을 모아 두었다가 Dispose로 한 번에 해제한다. 해제한 뒤에는 비워지므로 다시 쓸 수 있다.
    public sealed class DisposableBag : IDisposable
    {
        private readonly List<IDisposable> _items = new();

        public int Count => _items.Count;

        public void Add(IDisposable item)
        {
            if (item != null) _items.Add(item);
        }

        // 넣은 순서의 역순으로 해제한다. 하나가 예외를 던져도 나머지는 해제한다.
        public void Dispose()
        {
            if (_items.Count == 0) return;

            IDisposable[] items = _items.ToArray();
            _items.Clear();

            for (int i = items.Length - 1; i >= 0; i--)
            {
                try
                {
                    items[i].Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
