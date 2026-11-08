using System;

namespace _Scripts.UI.MVVM
{
    public static class DisposableExtensions
    {
        // vm.Hp.Subscribe(...).AddTo(Disposables); 처럼 구독과 등록을 한 줄로 쓴다.
        public static T AddTo<T>(this T disposable, DisposableBag bag) where T : IDisposable
        {
            if (bag == null) throw new ArgumentNullException(nameof(bag));

            bag.Add(disposable);
            return disposable;
        }
    }
}
