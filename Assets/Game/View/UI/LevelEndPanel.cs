using UnityEngine;
using PrimeTween; 

namespace Game.View.UI
{
    public class LevelEndPanel : MonoBehaviour
    {
        [SerializeField] private float revealDuration;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Ease revealEase;

        private void OnEnable()
        {
            Tween.Custom(0f, 1f, revealDuration, onValueChange: value => canvasGroup.alpha = value, revealEase);
        }
    }
}
