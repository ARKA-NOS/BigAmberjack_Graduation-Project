using System.Collections.Generic;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using _Scripts.Upgrade.SO;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 선행 노드 → 다음 노드 사이에 선을 긋는다. 양쪽 노드를 모두 샀거나 임시 선택했으면 활성 색으로 칠한다.
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
            public List<Image> Images;
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
                List<Image> images = line.Images;

                void Recolor(UpgradeNodeState _)
                {
                    bool active = IsSelected(fromState.Value) && IsSelected(toState.Value);
                    Color color = active ? activeColor : inactiveColor;
                    foreach (Image image in images) image.color = color;
                }

                fromState.Subscribe(Recolor).AddTo(_lineSubscriptions);
                toState.Subscribe(Recolor).AddTo(_lineSubscriptions);
            }
        }

        private static bool IsSelected(UpgradeNodeState state)
            => state is UpgradeNodeState.Pending or UpgradeNodeState.Purchased;

        // 직각 경로: 가로/세로 중 더 먼 쪽으로 나가서 → 중간에서 꺾고 → 반대 축으로 이동 → 다시 꺾어 도착한다.
        private Line CreateLine(RectTransform from, RectTransform to, UpgradeNodeSO fromNode, UpgradeNodeSO toNode)
        {
            Vector2 a = lineRoot.InverseTransformPoint(from.TransformPoint(from.rect.center));
            Vector2 b = lineRoot.InverseTransformPoint(to.TransformPoint(to.rect.center));
            Vector2 halfFrom = HalfSize(from);
            Vector2 halfTo = HalfSize(to);
            Vector2 d = b - a;

            List<Vector2> points = new();
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            {
                float sx = d.x >= 0f ? 1f : -1f;
                Vector2 start = new(a.x + sx * (halfFrom.x + gap), a.y);
                Vector2 end = new(b.x - sx * (halfTo.x + gap), b.y);
                float midX = (start.x + end.x) * 0.5f;
                points.Add(start);
                if (Mathf.Abs(start.y - end.y) > 0.5f)
                {
                    points.Add(new Vector2(midX, start.y));
                    points.Add(new Vector2(midX, end.y));
                }
                points.Add(end);
            }
            else
            {
                float sy = d.y >= 0f ? 1f : -1f;
                Vector2 start = new(a.x, a.y + sy * (halfFrom.y + gap));
                Vector2 end = new(b.x, b.y - sy * (halfTo.y + gap));
                float midY = (start.y + end.y) * 0.5f;
                points.Add(start);
                if (Mathf.Abs(start.x - end.x) > 0.5f)
                {
                    points.Add(new Vector2(start.x, midY));
                    points.Add(new Vector2(end.x, midY));
                }
                points.Add(end);
            }

            Line line = new() { Images = new List<Image>(), From = fromNode, To = toNode };
            for (int i = 0; i < points.Count - 1; i++)
            {
                // 꺾이는 모서리가 비지 않도록 안쪽 끝점은 선 굵기의 절반만큼 늘린다.
                line.Images.Add(CreateSegment($"Line_{fromNode.name}_{toNode.name}_{i}", points[i], points[i + 1], i > 0, i < points.Count - 2));
            }
            return line;
        }

        private Image CreateSegment(string objName, Vector2 p, Vector2 q, bool extendStart, bool extendEnd)
        {
            GameObject go = new(objName, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(lineRoot, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 dir = q - p;
            float length = dir.magnitude;
            Vector2 unit = length > 0.0001f ? dir / length : Vector2.right;
            float half = thickness * 0.5f;
            if (extendStart) { p -= unit * half; length += half; }
            if (extendEnd) { q += unit * half; length += half; }

            rect.localPosition = (p + q) * 0.5f;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(unit.y, unit.x) * Mathf.Rad2Deg);

            Image image = go.GetComponent<Image>();
            image.color = inactiveColor;
            image.raycastTarget = false;
            return image;
        }

        private Vector2 HalfSize(RectTransform node)
        {
            float scale = node.lossyScale.x / lineRoot.lossyScale.x;
            return node.rect.size * (0.5f * scale);
        }

        private void ClearLines()
        {
            foreach (Line line in _lines)
            {
                foreach (Image image in line.Images)
                {
                    if (image != null) Destroy(image.gameObject);
                }
            }
            _lines.Clear();
        }
    }
}
