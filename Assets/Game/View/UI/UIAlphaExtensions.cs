using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Game.View.UI
{
    public static class UIAlphaExtensions
    {
        /// <summary>
        /// Sets alpha for any Graphic (Image, Text, TextMeshProUGUI, RawImage, etc.)
        /// </summary>
        public static void SetAlpha(this Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        /// <summary>
        /// Gets alpha for any Graphic (Image, Text, TextMeshProUGUI, RawImage, etc.)
        /// </summary>
        public static float GetAlpha(this Graphic graphic)
        {
            return graphic != null ? graphic.color.a : 0f;
        }

        /// <summary>
        /// Sets alpha for all Graphics in a collection
        /// </summary>
        public static void SetAlpha(this IEnumerable<Graphic> graphics, float alpha)
        {
            if (graphics == null) return;
            foreach (Graphic graphic in graphics)
            {
                graphic?.SetAlpha(alpha);
            }
        }

        /// <summary>
        /// Sets alpha for CanvasGroup
        /// </summary>
        public static void SetAlpha(this CanvasGroup canvasGroup, float alpha)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = alpha;
        }

        /// <summary>
        /// Gets alpha for CanvasGroup
        /// </summary>
        public static float GetAlpha(this CanvasGroup canvasGroup)
        {
            return canvasGroup != null ? canvasGroup.alpha : 0f;
        }

        /// <summary>
        /// Tweens alpha of a Graphic from its current alpha to endValue using PrimeTween
        /// </summary>
        public static Tween TweenAlpha(this Graphic graphic, float endValue, float duration, Ease ease = Ease.Default)
        {
            if (graphic == null) return default;
            return Tween.Alpha(graphic, endValue: endValue, duration: duration, ease: ease);
        }

        /// <summary>
        /// Tweens alpha of a Graphic from startValue to endValue using PrimeTween
        /// </summary>
        public static Tween TweenAlpha(this Graphic graphic, float startValue, float endValue, float duration, Ease ease = Ease.Default)
        {
            if (graphic == null) return default;
            return Tween.Alpha(graphic, startValue: startValue, endValue: endValue, duration: duration, ease: ease);
        }

        /// <summary>
        /// Tweens alpha of a CanvasGroup from its current alpha to endValue using PrimeTween
        /// </summary>
        public static Tween TweenAlpha(this CanvasGroup canvasGroup, float endValue, float duration, Ease ease = Ease.Default)
        {
            if (canvasGroup == null) return default;
            return Tween.Alpha(canvasGroup, endValue: endValue, duration: duration, ease: ease);
        }

        /// <summary>
        /// Tweens alpha of a CanvasGroup from startValue to endValue using PrimeTween
        /// </summary>
        public static Tween TweenAlpha(this CanvasGroup canvasGroup, float startValue, float endValue, float duration, Ease ease = Ease.Default)
        {
            if (canvasGroup == null) return default;
            return Tween.Alpha(canvasGroup, startValue: startValue, endValue: endValue, duration: duration, ease: ease);
        }
    }
}
