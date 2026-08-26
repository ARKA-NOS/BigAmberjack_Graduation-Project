using System;
using DG.Tweening;
using UnityEngine;

namespace Agents.Players
{
    public class AfterImageGhost : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Material customSpriteMaterial;

        private Tween _fadeTween;

        public void Show(
            SpriteRenderer source,
            Color color,
            float lifeTime,
            Action<AfterImageGhost> onFinished)
        {
            _fadeTween?.Kill();

            transform.position = source.transform.position;
            transform.rotation = source.transform.rotation;
            transform.localScale = source.transform.lossyScale;

            spriteRenderer.sprite = source.sprite;
            spriteRenderer.sharedMaterial = customSpriteMaterial == null ? source.sharedMaterial : customSpriteMaterial;

            spriteRenderer.flipX = source.flipX;
            spriteRenderer.flipY = source.flipY;

            spriteRenderer.sortingLayerID = source.sortingLayerID;
            spriteRenderer.sortingOrder = source.sortingOrder - 1;

            spriteRenderer.color = color;

            _fadeTween = spriteRenderer
                .DOFade(0f, lifeTime)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _fadeTween = null;
                    onFinished?.Invoke(this);
                });
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
        }
    }
}