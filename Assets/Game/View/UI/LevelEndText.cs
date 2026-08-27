using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Game.View.UI
{
    public class LevelEndText : MonoBehaviour
    {
        [SerializeField] private List<TextMeshProUGUI> letters;

        [Header("Scale Up (0 -> ScaleUp)")]
        [SerializeField] private float scaleUpFactor;
        [SerializeField] private float scaleUpTime;
        [SerializeField] private Ease scaleUpEase;

        [Header("Scale Down (ScaleUp -> 1.0)")]
        [SerializeField] private float scaleDownTime;
        [SerializeField] private Ease scaleDownEase;

        [Header("Sequence Settings")]
        [SerializeField] private float delayBetweenLetter;

        private void OnEnable()
        {
            _ = RevealingEffect();
        }

        [ContextMenu("Play Revealing Effect")]
        public void PlayRevealingEffect()
        {
            _ = RevealingEffect();
        }

        public async Awaitable RevealingEffect()
        {
            if (letters == null || letters.Count == 0) return;

            foreach (var let in letters)
            {
                if (let != null)
                {
                    Tween.StopAll(let.transform);
                    let.transform.localScale = Vector3.zero;
                }
            }

            await Awaitable.NextFrameAsync();

            foreach (var let in letters)
            {
                if (let != null && let.gameObject.activeSelf)
                {
                    _ = ScaleLetterPopAsync(let.transform);

                    if (delayBetweenLetter > 0f)
                    {
                        await Awaitable.WaitForSecondsAsync(delayBetweenLetter);
                    }
                }
            }
        }

        private async Awaitable ScaleLetterPopAsync(Transform target)
        {
            Tween.StopAll(target);
            target.localScale = Vector3.zero;

            await Tween.Scale(target, startValue: 0f, endValue: scaleUpFactor, duration: scaleUpTime, ease: scaleUpEase);
            await Tween.Scale(target, startValue: scaleUpFactor, endValue: 1f, duration: scaleDownTime, ease: scaleDownEase);
        }
    }
}
