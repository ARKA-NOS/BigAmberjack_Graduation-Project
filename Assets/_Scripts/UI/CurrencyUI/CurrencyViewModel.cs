using _Scripts.Currency;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.CurrencyUI
{
    // 재화 하나(골드, 다크스톤 등)의 표시용 ViewModel. 재화마다 에셋을 하나씩 만들고 Currency에 보여줄 재화를 넣는다.
    // 재화 매니저에 연결되면 그 재화의 수량 변경 이벤트를 구독한다. 매니저가 없으면(Null 매니저) 아이콘과 0을 보여준다.
    [CreateAssetMenu(fileName = "Currency View Model", menuName = "UI/MVVM/Currency View Model")]
    public class CurrencyViewModel : ViewModelBase<ICurrencyManager>
    {
        [field: SerializeField] public CurrencySO Currency { get; private set; }

        private readonly Observable<Sprite> _icon = new();
        private readonly Observable<string> _currencyName = new(string.Empty);
        private readonly Observable<int> _count = new();
        private CurrencyData _data;

        public IReadOnlyObservable<Sprite> Icon => _icon;
        public IReadOnlyObservable<string> CurrencyName => _currencyName;
        public IReadOnlyObservable<int> Count => _count;

        protected override void OnConnect(ICurrencyManager manager)
        {
            _icon.Value = Currency != null ? Currency.CurrencyIcon : null;
            _currencyName.Value = Currency != null ? Currency.CurrencyName : string.Empty;

            _data = Currency != null ? manager.GetCurrency(Currency) : null;
            if (_data == null)
            {
                _count.Value = 0;
                return;
            }

            _data.OnCurrencyChanged += HandleCurrencyChanged;
            _count.Value = _data.Count;
        }

        protected override void OnDisconnect()
        {
            if (_data != null)
                _data.OnCurrencyChanged -= HandleCurrencyChanged;

            _data = null;
        }

        protected override void ResetState()
        {
            _icon.Value = null;
            _currencyName.Value = string.Empty;
            _count.Value = 0;
        }

        private void HandleCurrencyChanged(int current, int prev) => _count.Value = current;
    }
}
