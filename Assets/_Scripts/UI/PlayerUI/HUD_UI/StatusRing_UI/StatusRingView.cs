using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.HUD_UI.StatusRing_UI
{
    // 원형 HP/MP 게이지 HUD. CircleGauge 셰이더에 HP/MP 비율을 넣고, 숫자 글자를 갱신한다.
    // 플레이어가 연결되기 전에는 링을 숨겨 두고, 연결되면 중심에서 바깥으로 나타난다(셰이더 _Hide).
    public class StatusRingView : ViewBase<StatusRingViewModel>
    {
        private static readonly int HpFillId = Shader.PropertyToID("_HpFill");
        private static readonly int MpFillId = Shader.PropertyToID("_MpFill");
        private static readonly int HideId = Shader.PropertyToID("_Hide");

        [Header("Ring")]
        [Tooltip("CircleGauge 셰이더 Material을 쓰는 Image. Material은 이 View가 복제해서 쓴다.")]
        [SerializeField] private Image ringImage;
        [Tooltip("나타나고 사라지는 속도(초당 숨김 값 변화량). 1이면 1초.")]
        [SerializeField, Min(0.01f)] private float hideSpeed = 3f;

        [Header("Text")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text mpText;
        [Tooltip("{0} = 현재값, {1} = 최대값")]
        [SerializeField] private string hpFormat = "HP : {0}";
        [Tooltip("{0} = 현재값, {1} = 최대값")]
        [SerializeField] private string mpFormat = "MP : {0}";

        private Material _material;
        private string _mpDefaultText;
        private float _hide = 1f;
        private float _hideTarget = 1f;

        private void Awake()
        {
            // 마나 모듈이 없으면 프리팹에 써 둔 MP 글자를 그대로 보여준다.
            _mpDefaultText = mpText.text;

            // 에셋 Material을 직접 바꾸면 모든 Image가 같이 바뀌므로 복제본을 쓴다.
            _material = new Material(ringImage.material);
            ringImage.material = _material;
            _material.SetFloat(HideId, _hide);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        protected override void OnBind(StatusRingViewModel vm)
        {
            vm.HpRatio.Subscribe(ratio => _material.SetFloat(HpFillId, ratio)).AddTo(Disposables);
            vm.MpRatio.Subscribe(_ => UpdateMpFill(vm)).AddTo(Disposables);

            vm.HpCurrent.Subscribe(_ => UpdateHpText(vm)).AddTo(Disposables);
            vm.HpMax.Subscribe(_ => UpdateHpText(vm)).AddTo(Disposables);
            vm.MpCurrent.Subscribe(_ => UpdateMpText(vm)).AddTo(Disposables);
            vm.MpMax.Subscribe(_ => UpdateMpText(vm)).AddTo(Disposables);

            // 마나 모듈이 없으면 MP는 가득 찬 링과 프리팹의 글자 그대로 보여준다.
            vm.HasMana.Subscribe(_ =>
            {
                UpdateMpFill(vm);
                UpdateMpText(vm);
            }).AddTo(Disposables);

            // 글자는 플레이어가 연결된 동안만 보인다.
            vm.Connected.Subscribe(_ => UpdateTextActive(vm)).AddTo(Disposables);

            vm.Connected.Subscribe(connected => _hideTarget = connected ? 0f : 1f).AddTo(Disposables);
        }

        // 지연 바처럼 목표값을 따라가는 연출은 View가 한다.
        private void Update()
        {
            if (Mathf.Approximately(_hide, _hideTarget)) return;

            _hide = Mathf.MoveTowards(_hide, _hideTarget, hideSpeed * Time.unscaledDeltaTime);
            _material.SetFloat(HideId, _hide);
        }

        private void UpdateHpText(StatusRingViewModel vm)
            => hpText.text = string.Format(hpFormat, Mathf.CeilToInt(vm.HpCurrent.Value), Mathf.CeilToInt(vm.HpMax.Value));

        private void UpdateMpText(StatusRingViewModel vm)
            => mpText.text = vm.HasMana.Value
                ? string.Format(mpFormat, Mathf.CeilToInt(vm.MpCurrent.Value), Mathf.CeilToInt(vm.MpMax.Value))
                : _mpDefaultText;

        private void UpdateMpFill(StatusRingViewModel vm)
            => _material.SetFloat(MpFillId, vm.HasMana.Value ? vm.MpRatio.Value : 1f);

        private void UpdateTextActive(StatusRingViewModel vm)
        {
            hpText.gameObject.SetActive(vm.Connected.Value);
            mpText.gameObject.SetActive(vm.Connected.Value);
        }
    }
}
