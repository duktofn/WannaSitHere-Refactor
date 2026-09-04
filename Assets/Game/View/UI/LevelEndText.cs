using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
            RevealingEffect().Forget();
        }

        private void OnDisable()
        {
            if (letters == null) return;
            foreach (var let in letters)
            {
                if (let != null)
                {
                    Tween.StopAll(let.transform);
                }
            }
        }

        [ContextMenu("Play Revealing Effect")]
        public void PlayRevealingEffect()
        {
            RevealingEffect().Forget();
        }

        public async UniTask RevealingEffect()
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

            await UniTask.NextFrame();
            if (!gameObject.activeInHierarchy) return;

            foreach (var let in letters)
            {
                if (!gameObject.activeInHierarchy) return;

                if (let != null && let.gameObject.activeInHierarchy)
                {
                    ScaleLetterPopAsync(let.transform).Forget();

                    if (delayBetweenLetter > 0f)
                    {
                        await UniTask.WaitForSeconds(delayBetweenLetter);
                    }
                }
            }
        }

        private async UniTask ScaleLetterPopAsync(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;

            Tween.StopAll(target);
            target.localScale = Vector3.zero;

            await Tween.Scale(target, startValue: 0f, endValue: scaleUpFactor, duration: scaleUpTime, ease: scaleUpEase);

            if (target == null || !target.gameObject.activeInHierarchy) return;

            await Tween.Scale(target, startValue: scaleUpFactor, endValue: 1f, duration: scaleDownTime, ease: scaleDownEase);
        }
    }
}
