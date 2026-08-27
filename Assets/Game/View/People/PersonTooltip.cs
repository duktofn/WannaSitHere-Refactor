using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.View
{
    public class PersonTooltip : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject tooltipsRoot;
        [SerializeField] private SpriteRenderer top;
        [SerializeField] private SpriteRenderer mid;
        [SerializeField] private SpriteRenderer bot;

        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI traitText;
        [SerializeField] private TextMeshProUGUI conditionText;

        private bool _isShowTooltips;

        private void OnEnable()
        {
            _isShowTooltips = false;
        }

        private void ToggleTooltips()
        {
            if (_isShowTooltips) 
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleTooltips();
        }

        private async void ShowTween()
        {
            await Tween.Scale
        }
    }
}
