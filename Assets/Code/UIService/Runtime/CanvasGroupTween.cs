using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Code.UIService
{
    public static class CanvasGroupTween
    {
        public static async UniTask FadeAsync(
            CanvasGroup canvasGroup,
            float targetAlpha,
            float duration,
            CancellationToken token)
        {
            if (canvasGroup == null)
                throw new ArgumentNullException(nameof(canvasGroup));
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));

            token.ThrowIfCancellationRequested();
            DOTween.Kill(canvasGroup);

            var tween = DOTween.To(
                    () => canvasGroup.alpha,
                    value => canvasGroup.alpha = value,
                    Mathf.Clamp01(targetAlpha),
                    duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(canvasGroup)
                .SetLink(canvasGroup.gameObject, LinkBehaviour.KillOnDestroy);

            try
            {
                await UniTask.WaitUntil(
                    () => !tween.IsActive(),
                    cancellationToken: token);
            }
            catch
            {
                tween.Kill();
                throw;
            }
        }

        public static void Kill(CanvasGroup canvasGroup)
        {
            if (canvasGroup != null)
                DOTween.Kill(canvasGroup);
        }
    }
}
