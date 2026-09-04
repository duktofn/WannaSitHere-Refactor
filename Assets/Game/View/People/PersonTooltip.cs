using System;
using Cysharp.Threading.Tasks;
using Game.Core.People;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Game.View.People
{
    public class PersonTooltip : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject tooltipsRoot;
        [SerializeField] private SpriteRenderer top;
        [SerializeField] private SpriteRenderer mid;
        [SerializeField] private SpriteRenderer bot;
        [SerializeField] private string textSortingLayer;

        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro traitText;
        [SerializeField] private TextMeshPro conditionText;

        [Header("Show/Hide Tween")]
        [SerializeField] private float duration;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        [Header("Mid Resize")]
        [SerializeField, FormerlySerializedAs("padding")] private float topPadding = 0.08f;
        [SerializeField] private float bottomPadding = 0.08f;
        [SerializeField] private float horizontalPadding = 0.15f;

        private const int MaxConditions = 2;

        private bool _isShowTooltips;
        private Vector3 _originalLocalPos;
        private InputAction _pointerPressAction;
        private float _botAnchorY;
        private bool _hasBotAnchor;
        private float _midBaseScaleY = 1f;

        private void Awake()
        {
            _originalLocalPos = tooltipsRoot.transform.localPosition;
            if (bot != null)
            {
                _botAnchorY = bot.transform.localPosition.y;
                _hasBotAnchor = true;
            }

            if (mid != null)
                _midBaseScaleY = Mathf.Abs(mid.transform.localScale.y);

            _pointerPressAction = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");

            nameText.GetComponent<MeshRenderer>().sortingLayerName = textSortingLayer;
            traitText.GetComponent<MeshRenderer>().sortingLayerName = textSortingLayer;
            conditionText.GetComponent<MeshRenderer>().sortingLayerName = textSortingLayer;
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
            if (conditionText == null || top == null || top.sprite == null ||
                mid == null || mid.sprite == null || bot == null || bot.sprite == null)
            {
                return;
            }

            float midWidth = mid.sprite.bounds.size.x * Mathf.Abs(mid.transform.localScale.x);
            float textWidth = Mathf.Max(0.01f, midWidth - horizontalPadding * 2f);
            conditionText.rectTransform.sizeDelta = new Vector2(textWidth, 100f);
            conditionText.ForceMeshUpdate();
            float textHeight = conditionText.GetPreferredValues(
                conditionText.text,
                textWidth,
                Mathf.Infinity
            ).y;
            conditionText.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);

            float midBaseHeight = mid.sprite.bounds.size.y * _midBaseScaleY;
            float desiredMidHeight = Mathf.Max(
                textHeight + topPadding + bottomPadding,
                midBaseHeight
            );
            float scaleY = desiredMidHeight / mid.sprite.bounds.size.y;
            mid.transform.localScale = new Vector3(
                mid.transform.localScale.x,
                scaleY,
                mid.transform.localScale.z
            );

            float actualMidHeight = mid.sprite.bounds.size.y * Mathf.Abs(scaleY);
            float botHeight = bot.sprite.bounds.size.y * Mathf.Abs(bot.transform.localScale.y);
            float topHeight = top.sprite.bounds.size.y * Mathf.Abs(top.transform.localScale.y);

            // Bot remains fixed. Mid stacks above Bot and Top stacks above Mid.
            float botCenterY = _hasBotAnchor ? _botAnchorY : bot.transform.localPosition.y;
            float botTopY = botCenterY + botHeight * 0.5f;
            float midCenterY = botTopY + actualMidHeight * 0.5f;
            float topCenterY = botTopY + actualMidHeight + topHeight * 0.5f;

            bot.transform.localPosition = new Vector3(
                bot.transform.localPosition.x,
                botCenterY,
                bot.transform.localPosition.z
            );
            mid.transform.localPosition = new Vector3(
                mid.transform.localPosition.x,
                midCenterY,
                mid.transform.localPosition.z
            );
            top.transform.localPosition = new Vector3(
                top.transform.localPosition.x,
                topCenterY,
                top.transform.localPosition.z
            );

            SetHeaderPosition(nameText, topCenterY);
            SetHeaderPosition(traitText, topCenterY);

            Vector3 conditionPosition = conditionText.transform.localPosition;
            conditionText.verticalAlignment = VerticalAlignmentOptions.Top;
            conditionText.rectTransform.pivot = new Vector2(
                conditionText.rectTransform.pivot.x,
                1f
            );
            conditionText.transform.localPosition = new Vector3(
                conditionPosition.x,
                botTopY + actualMidHeight - topPadding,
                conditionPosition.z
            );
        }

        private static void SetHeaderPosition(TextMeshPro text, float y)
        {
            if (text == null)
                return;

            Vector3 position = text.transform.localPosition;
            text.transform.localPosition = new Vector3(position.x, y, position.z);
        }

        private void ResizeLegacyToFitConditionText()
        {
            conditionText.ForceMeshUpdate();
            float textHeight = conditionText.GetRenderedValues(true).y;

            // Scale Y của mid cho vừa text
            float midOriginalHeight = mid.bounds.size.y / mid.transform.localScale.y;
            float scaleY = (textHeight + topPadding + bottomPadding) / midOriginalHeight;
            mid.transform.localScale = new Vector3(mid.transform.localScale.x, scaleY, mid.transform.localScale.z);

            
            float halfMid = mid.bounds.size.y / 2f;
            top.transform.localPosition = new Vector3(
                top.transform.localPosition.x, halfMid, top.transform.localPosition.z);
            bot.transform.localPosition = new Vector3(
                bot.transform.localPosition.x, -halfMid, bot.transform.localPosition.z);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || 
                Vector2.Distance(eventData.pressPosition, eventData.position) > 5f)
            {
                return;                
            }

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
