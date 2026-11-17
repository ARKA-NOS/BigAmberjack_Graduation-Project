using System.Collections.Generic;
using _Scripts.DataStructure;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using _Scripts.UI.PlayerUI.HUD_UI.HP_UI.HealthBarUI;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.PlayerUI.HUD_UI.HP_UI
{
    // 플레이어 체력바. 체력 겹마다 막대(HPBarUI)와 글자(HPTextUI)를 하나씩 만들고,
    // 막대 길이는 체력 양에 비례하게(LayoutElement.flexibleWidth), 깎인 만큼은 오른쪽 빈 칸으로 채운다.
    public class HealthView : ViewBase<HealthViewModel>
    {
        [Header("Prefabs")]
        [SerializeField] private HPBarUI healthImagePrefab;
        [SerializeField] private HPTextUI healthTextPrefab;

        [Header("UI")]
        [SerializeField] private Transform barContainer;
        [SerializeField] private LayoutElement barSpaceElement;
        [SerializeField] private Transform textContainer;

        private readonly List<HPBarUI> _bars = new();
        private readonly List<HPTextUI> _texts = new();

        protected override void OnBind(HealthViewModel vm)
        {
            vm.Layers.Subscribe(change => HandleLayersChanged(vm.Layers, change)).AddTo(Disposables);
            vm.MissingHealth.Subscribe(missing => barSpaceElement.flexibleWidth = missing).AddTo(Disposables);
        }

        private void HandleLayersChanged(IReadOnlyList<HealthLayerData> layers, ListChange<HealthLayerData> change)
        {
            if (change.Kind == ListChangeKind.Replace)
            {
                ShowLayer(change.Index, change.NewItem);
                return;
            }

            // 겹 수가 바뀌었으면(연결, 해제) 전부 다시 그린다.
            EnsureCount(layers.Count);

            for (int i = 0; i < _bars.Count; i++)
            {
                if (i < layers.Count)
                {
                    ShowLayer(i, layers[i]);
                }
                else
                {
                    _bars[i].SetActive(false);
                    _texts[i].SetActive(false);
                }
            }
        }

        private void EnsureCount(int count)
        {
            if (_bars.Count >= count) return;

            while (_bars.Count < count)
            {
                _bars.Add(Instantiate(healthImagePrefab, barContainer));
                _texts.Add(Instantiate(healthTextPrefab, textContainer));
            }

            // 빈 칸은 항상 막대들의 오른쪽 끝에 둔다.
            barSpaceElement.transform.SetAsLastSibling();
        }

        // 체력이 남은 겹만 켠다(겹마다 따로 판정).
        private void ShowLayer(int index, HealthLayerData layer)
        {
            if (index >= _bars.Count) return;

            bool visible = layer.IsVisible;
            _bars[index].SetActive(visible);
            _texts[index].SetActive(visible);
            if (!visible) return;

            _bars[index].SetBar(layer.BarColor, layer.Current);
            _texts[index].SetText(layer.TextColor, new Gauge(layer.Current, layer.Max), layer.ShowMax);
        }
    }
}
