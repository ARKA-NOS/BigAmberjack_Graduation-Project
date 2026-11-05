using Lrw.Script.UI.UpgradeUI;
using System.Collections.Generic;
using Lrw.Script.Upgrade;
using Lrw.Script.Upgrade.SO;
using UnityEngine;
using UnityEngine.UI;

namespace SDW.Scripts.UI.UpgradeUI
{
    public class TestUpgradeLineDrawer : MonoBehaviour
    {
        [SerializeField] private RectTransform lineRoot;
        [SerializeField] private float thickness = 8f;
        [SerializeField] private float gap = 4f;
        [SerializeField] private Color activeColor = new Color(1f, 0.78f, 0.2f, 1f);
        [SerializeField] private Color inactiveColor = new Color(1f, 0.78f, 0.2f, 0.35f);

        private readonly List<Line> _lines = new();

        private struct Line
        {
            public Image Image;
            public UpgradeNodeSO From;
            public UpgradeNodeSO To;
        }

        public void Draw(TestUpgradeNodeButton[] buttons)
        {
            Clear();

            Dictionary<UpgradeNodeSO, RectTransform> rects = new();
            foreach (TestUpgradeNodeButton button in buttons)
            {
                if (button.Node != null) rects[button.Node] = (RectTransform)button.transform;
            }

            foreach (TestUpgradeNodeButton button in buttons)
            {
                UpgradeNodeSO node = button.Node;
                if (node == null || node.PrevNodes == null) continue;

                foreach (UpgradeNodeSO prev in node.PrevNodes)
                {
                    if (prev == null || !rects.TryGetValue(prev, out RectTransform from)) continue;
                    _lines.Add(CreateLine(from, rects[node], prev, node));
                }
            }
        }

        public void Refresh(UpgradeTree tree)
        {
            foreach (Line line in _lines)
            {
                bool active = tree != null && tree.IsUpgrade(line.From) && tree.IsUpgrade(line.To);
                line.Image.color = active ? activeColor : inactiveColor;
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

        private void Clear()
        {
            foreach (Line line in _lines)
            {
                if (line.Image != null) Destroy(line.Image.gameObject);
            }
            _lines.Clear();
        }
    }
}
