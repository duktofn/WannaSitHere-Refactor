using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Game.View.UI
{
    public class LevelEndBackground : MonoBehaviour
    {
        [SerializeField] private List<Image> images;
        [SerializeField] private float revealDuration;
        [SerializeField] private Ease revealEase;
        [SerializeField] private float revealDelay;

        private void Awake()
        {
            foreach (Image img in images)
            {
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
            }
        }

        private void OnEnable()
        {
            Revealing().Forget();
        }

        [ContextMenu("Revealing")]
        public async UniTask Revealing()
        {
            foreach (Image img in images)
            {
                await UniTask.Delay((int) (revealDelay * 1000));

                _ = Tween.Custom(
                    0f,
                    1f,
                    revealDuration,
                    value =>
                    {
                        Color color = img.color;
                        color.a = value;
                        img.color = color;
                    },
                    ease: revealEase
                );
            }
        }
    }
}