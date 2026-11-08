using System;

namespace _Scripts.UI.MVVM
{
    public static class Disposable
    {
        // 아무 일도 하지 않는 해제 핸들.
        public static readonly IDisposable Empty = new ActionDisposable(null);

        // Dispose하면 onDispose를 한 번만 실행하는 해제 핸들. 두 번째 Dispose부터는 아무 일도 하지 않는다.
        public static IDisposable Create(Action onDispose) => new ActionDisposable(onDispose);

        private sealed class ActionDisposable : IDisposable
        {
            private Action _onDispose;

            public ActionDisposable(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                Action onDispose = _onDispose;
                _onDispose = null;
                onDispose?.Invoke();
            }
        }
    }
}
