using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using PrimeTween;
using Cysharp.Threading.Tasks;

namespace Game.View.UI
{
    public class LevelEndPanel : MonoBehaviour
    {
        [FormerlySerializedAs("images")]
        [SerializeField] private List<Graphic> graphics;
        [SerializeField] private GameObject navigation;
        [SerializeField] private GameObject reward;
        [SerializeField] private float revealDuration;
        [SerializeField] private Ease revealEase;
        [SerializeField] private float revealDelay;

        private void Awake()
        {
            ResetAlpha();
        }

        private void OnEnable()
        {
            Revealing().Forget();
            navigation.SetActive(false);
        }

        private void OnDisable()
        {
            if (graphics == null) return;
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                {
                    Tween.StopAll(graphic);
                }
            }
        }

        public void ResetAlpha()
        {
            if (graphics == null) return;
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                {
                    Tween.StopAll(graphic);
                    graphic.SetAlpha(0f);
                }
            }
        }

        [ContextMenu("Revealing")]
        public async UniTask Revealing()
        {
            if (graphics == null || graphics.Count == 0) return;

            ResetAlpha();

            foreach (Graphic graphic in graphics)
            {
                if (!gameObject.activeInHierarchy) return;

                if (revealDelay > 0f)
                {
                    await UniTask.WaitForSeconds(revealDelay);
                }

                if (!gameObject.activeInHierarchy) return;

                if (graphic != null)
                {
                    _ = graphic.TweenAlpha(0f, 1f, revealDuration, revealEase);
                }
            }
        }
    }
}