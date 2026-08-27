using System;
using Cysharp.Threading.Tasks;
using Game.Core.People;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.View
{
    public class PersonTooltip : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject tooltipsRoot;
        [SerializeField] private SpriteRenderer top;
        [SerializeField] private SpriteRenderer mid;
        [SerializeField] private SpriteRenderer bot;

        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro traitText;
        [SerializeField] private TextMeshPro conditionText;

        [Header("Show/Hide Tween")]
        [SerializeField] private float duration;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        [Header("Mid Resize")]
        [SerializeField] private float padding = 0.2f;

        private const int MaxConditions = 2;

        private bool _isShowTooltips;
        private Vector3 _originalLocalPos;
        private InputAction _pointerPressAction;

        private void Awake()
        {
            _originalLocalPos = tooltipsRoot.transform.localPosition;

            _pointerPressAction = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");
        }

        private void OnDestroy()
        {
            _pointerPressAction?.Dispose();
        }

        private void OnEnable()
        {
            _isShowTooltips = false;
            tooltipsRoot.transform.localScale = Vector3.zero;
            tooltipsRoot.transform.localPosition = Vector3.zero;
            tooltipsRoot.SetActive(false);
        }

        private void OnDisable()
        {
            StopListeningForOutsideClick();
        }

        private void StartListeningForOutsideClick()
        {
            _pointerPressAction.performed += OnPointerPressed;
            _pointerPressAction.Enable();
        }

        private void StopListeningForOutsideClick()
        {
            _pointerPressAction.performed -= OnPointerPressed;
            _pointerPressAction.Disable();
        }

        private void OnPointerPressed(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.OverlapPoint(worldPos);

            if (hit == null || hit.transform != transform)
            {
                Hide();
            }
        }

        public void BindData(PersonRuntimeData person)
        {
            if (person == null) return;

            nameText.text = person.PersonName;
            traitText.text = person.Trait.ToString();

            if (person.Conditions != null && person.Conditions.Count > 0)
            {
                int count = Math.Min(person.Conditions.Count, MaxConditions);
                var descriptions = new string[count];

                for (int i = 0; i < count; i++)
                {
                    descriptions[i] = person.Conditions[i].Description;
                }

                conditionText.text = string.Join("\n\n", descriptions);
            }
            else
            {
                conditionText.text = string.Empty;
            }

            ResizeToFitConditionText();
        }

        private void ResizeToFitConditionText()
        {
            conditionText.ForceMeshUpdate();
            float textHeight = conditionText.GetRenderedValues(true).y;

            // Scale Y của mid cho vừa text
            float midOriginalHeight = mid.bounds.size.y / mid.transform.localScale.y;
            float scaleY = (textHeight + padding) / midOriginalHeight;
            mid.transform.localScale = new Vector3(mid.transform.localScale.x, scaleY, mid.transform.localScale.z);

            
            float halfMid = mid.bounds.size.y / 2f;
            top.transform.localPosition = new Vector3(
                top.transform.localPosition.x, halfMid, top.transform.localPosition.z);
            bot.transform.localPosition = new Vector3(
                bot.transform.localPosition.x, -halfMid, bot.transform.localPosition.z);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleTooltips();
        }

        public void Hide()
        {
            if (!_isShowTooltips) return;
            _isShowTooltips = false;
            StopListeningForOutsideClick();
            HideTween().Forget();
        }

        private void ToggleTooltips()
        {
            if (_isShowTooltips)
            {
                _isShowTooltips = false;
                StopListeningForOutsideClick();
                HideTween().Forget();
            }
            else
            {
                _isShowTooltips = true;
                StartListeningForOutsideClick();
                ShowTween().Forget();
            }
        }

        private async UniTask ShowTween()
        {
            Tween.StopAll(tooltipsRoot.transform);

            tooltipsRoot.transform.localPosition = Vector3.zero;
            tooltipsRoot.transform.localScale = Vector3.zero;
            tooltipsRoot.SetActive(true);

            _ = Tween.Scale(tooltipsRoot.transform, endValue: 1f, duration: duration, ease: showEase);
            await Tween.LocalPosition(tooltipsRoot.transform, endValue: _originalLocalPos, duration: duration, ease: showEase);
        }

        private async UniTask HideTween()
        {
            Tween.StopAll(tooltipsRoot.transform);

            _ = Tween.Scale(tooltipsRoot.transform, endValue: 0f, duration: duration, ease: hideEase);
            await Tween.LocalPosition(tooltipsRoot.transform, endValue: Vector3.zero, duration: duration, ease: hideEase);

            tooltipsRoot.SetActive(false);
        }
    }
}
