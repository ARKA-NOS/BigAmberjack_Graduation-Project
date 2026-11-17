using System;

namespace _Scripts.Loading
{
    // LoadingEvents.BeginLoading이 돌려주는 로딩 하나. Dispose하면 끝난다(여러 번 불러도 한 번만 처리된다).
    // async 메서드에서는 using으로 받아 두면 완료·예외·취소 어느 쪽으로 끝나도 로딩이 정리된다.
    public sealed class LoadingScope : IDisposable
    {
        internal LoadingScope() { }

        public void Dispose() => LoadingEvents.EndLoading(this);
    }
}
