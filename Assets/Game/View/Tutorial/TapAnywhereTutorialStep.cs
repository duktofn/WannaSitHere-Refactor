using Game.App.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.View.Tutorial
{
    [RequireComponent(typeof(Image))]
    public sealed class TapAnywhereTutorialStep : TutorialStep, IPointerClickHandler
    {
        private Image _blocker;
        private Color _previousColor;
        private bool _previousGameplayInputEnabled;
        private bool _isWaitingForTap;

        private void Awake()
        {
            if (_blocker == null)
                _blocker = GetComponent<Image>();

            _blocker.enabled = false;
            _blocker.raycastTarget = false;
        }

        public override void StartStep()
        {
            if (_isWaitingForTap)
                EndStep();

            if (_blocker == null)
                _blocker = GetComponent<Image>();

            _previousColor = _blocker.color;
            _previousGameplayInputEnabled = GamePresentation != null && GamePresentation.IsGameplayInputEnabled;

            GamePresentation?.SetGameplayInputEnabled(false);

            Color blockerColor = _blocker.color;
            blockerColor.a = 0f;
            _blocker.color = blockerColor;
            _blocker.raycastTarget = true;
            _blocker.enabled = true;

            RectTransform blockerRect = _blocker.rectTransform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            transform.SetAsLastSibling();

            _isWaitingForTap = true;
        }

        public override void UpdateStep()
        {
        }

        public override void EndStep()
        {
            if (!_isWaitingForTap)
                return;

            _isWaitingForTap = false;
            _blocker.color = _previousColor;
            _blocker.raycastTarget = false;
            _blocker.enabled = false;
            GamePresentation?.SetGameplayInputEnabled(_previousGameplayInputEnabled);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isWaitingForTap)
                CompleteStep();
        }
    }
}
