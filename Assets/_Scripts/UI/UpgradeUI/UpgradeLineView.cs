using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using _Scripts.Upgrade.SO;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 선행 노드 → 다음 노드 사이에 선을 긋는다. 양쪽 노드를 모두 샀으면 활성 색으로 칠한다.
    // 선 위치는 노드 버튼의 배치에 따라 정해지므로 레이아웃이 끝난 뒤 UpgradeTreeWindow가 Draw를 부른다.
    // ViewModel은 UpgradeTreeWindow가 SetViewModel로 넣는다.
    public class UpgradeLineView : ViewBase<UpgradeTreeViewModel>
    {
        [SerializeField] private RectTransform lineRoot;
        [SerializeField] private float thickness = 8f;
        [SerializeField] private float gap = 4f;
        [SerializeField] private Color activeColor = new Color(1f, 0.78f, 0.2f, 1f);
        [SerializeField] private Color inactiveColor = new Color(1f, 0.78f, 0.2f, 0.35f);

        private readonly List<Line> _lines = new();
        // 선은 Draw마다 다시 만들어지므로 선 색 구독은 View 구독(Disposables)과 따로 관리한다.
        private readonly DisposableBag _lineSubscriptions = new();

        private struct Line
        {
            public Image Image;
            public UpgradeNodeSO From;
            public UpgradeNodeSO To;
        }

        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            SubscribeLines(vm);
        }

        protected override void OnUnbind()
        {
            _lineSubscriptions.Dispose();
        }

        public void Draw(IReadOnlyList<UpgradeNodeView> nodes)
        {
            _lineSubscriptions.Dispose();
            ClearLines();

            Dictionary<UpgradeNodeSO, RectTransform> rects = new();
            foreach (UpgradeNodeView node in nodes)
            {
                if (node.Node != null) rects[node.Node] = (RectTransform)node.transform;
            }

            foreach (UpgradeNodeView view in nodes)
            {
                UpgradeNodeSO node = view.Node;
                if (node == null || node.PrevNodes == null) continue;

                foreach (UpgradeNodeSO prev in node.PrevNodes)
                {
                    if (prev == null || !rects.TryGetValue(prev, out RectTransform from)) continue;
                    _lines.Add(CreateLine(from, rects[node], prev, node));
                }
            }

            // 이미 바인드된 상태면 새 선들의 색을 바로 맞춘다. 아직이면 OnBind에서 맞춘다.
            if (ViewModel != null && isActiveAndEnabled)
                SubscribeLines(ViewModel);
        }

        private void SubscribeLines(UpgradeTreeViewModel vm)
        {
            foreach (Line line in _lines)
            {
                IReadOnlyObservable<UpgradeNodeState> fromState = vm.GetNode(line.From).State;
                IReadOnlyObservable<UpgradeNodeState> toState = vm.GetNode(line.To).State;
                Image image = line.Image;

                void Recolor(UpgradeNodeState _)
                {
                    bool active = fromState.Value == UpgradeNodeState.Purchased && toState.Value == UpgradeNodeState.Purchased;
                    image.color = active ? activeColor : inactiveColor;
                }

                fromState.Subscribe(Recolor).AddTo(_lineSubscriptions);
                toState.Subscribe(Recolor).AddTo(_lineSubscriptions);
            }
        }

        private Line CreateLine(RectTransform from, RectTransform to, UpgradeNodeSO fromNode, UpgradeNodeSO toNode)
        {
            GameObject go = new($"Line_{fromNode.name}_{toNode.name}", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(lineRoot, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 a = lineRoot.InverseTransformPoint(from.TransformPoint(from.rect.center));
            Vector2 b = lineRoot.InverseTransformPoint(to.TransformPoint(to.rect.center));
            Vector2 dir = b - a;
            Vector2 unit = dir.normalized;

            a += unit * (EdgeDistance(from, unit) + gap);
            b -= unit * (EdgeDistance(to, unit) + gap);
            float length = Mathf.Max(0f, Vector2.Dot(b - a, unit));

            rect.localPosition = (a + b) * 0.5f;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            Image image = go.GetComponent<Image>();
            image.color = inactiveColor;
            image.raycastTarget = false;
            return new Line { Image = image, From = fromNode, To = toNode };
        }

        private float EdgeDistance(RectTransform node, Vector2 unit)
        {
            float scale = node.lossyScale.x / lineRoot.lossyScale.x;
            Vector2 half = node.rect.size * (0.5f * scale);
            float tx = Mathf.Abs(unit.x) < 0.0001f ? float.MaxValue : half.x / Mathf.Abs(unit.x);
            float ty = Mathf.Abs(unit.y) < 0.0001f ? float.MaxValue : half.y / Mathf.Abs(unit.y);
            return Mathf.Min(tx, ty);
        }

        private void ClearLines()
        {
            foreach (Line line in _lines)
            {
                if (line.Image != null) Destroy(line.Image.gameObject);
            }
            _lines.Clear();
        }
    }
}
