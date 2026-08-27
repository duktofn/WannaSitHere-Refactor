using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Core.Conditions;
using Game.Core.People;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Game.View.Board;
using Game.View.Input;

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

        [Header("Layout")]
        [SerializeField] private float verticalPadding = 0.15f;
        [SerializeField] private float horizontalPadding = 0.15f;

        private const int MaxConditions = 2;

        private static PersonTooltip _activeTooltip;
        private bool _isShowTooltips;
        private Vector3 _originalLocalPos;
        private InputAction _pointerPressAction;
        private Transform _animatedVisuals;
        private SortingGroup _sortingGroup;
        private PersonDragManager _dragManager;
        private GridManager _gridManager;
        private Color _topBaseColor;
        private Color _midBaseColor;
        private Color _botBaseColor;
        private Color _nameBaseColor;
        private Color _traitBaseColor;
        private Color _conditionBaseColor;
        private bool _hasCachedColors;
        private float _botAnchorY;
        private bool _hasBotAnchor;
        private float _midBaseScaleY = 1f;

        private void Awake()
        {
            _dragManager = GetComponent<PersonDragManager>();
            _originalLocalPos = tooltipsRoot.transform.localPosition;
            if (bot != null)
            {
                _botAnchorY = bot.transform.localPosition.y;
                _hasBotAnchor = true;
            }
            if (mid != null)
                _midBaseScaleY = Mathf.Abs(mid.transform.localScale.y);
            _pointerPressAction = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");
            ConfigureAnimationHierarchy();
            CacheColors();
            ConfigureSorting();
        }

        private void OnDestroy()
        {
            _pointerPressAction?.Dispose();
        }

        private void OnEnable()
        {
            _isShowTooltips = false;
            SetAnimationScale(0f);
            SetVisualAlpha(0f);
            tooltipsRoot.transform.localPosition = Vector3.zero;
            tooltipsRoot.SetActive(false);
        }

        private void OnDisable()
        {
            StopListeningForOutsideClick();
            if (_activeTooltip == this)
                _activeTooltip = null;
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
            if (Pointer.current == null)
                return;

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
            if (person == null)
                return;

            if (nameText != null)
                nameText.text = person.PersonName;
            if (traitText != null)
                traitText.text = person.Trait.ToString();

            if (conditionText == null)
                return;

            conditionText.text = BuildConditionText(person);

            conditionText.textWrappingMode = TextWrappingModes.Normal;
            conditionText.overflowMode = TextOverflowModes.Overflow;

            ResizeLayout();
        }

        private void ResizeLayout()
        {
            if (conditionText == null || mid == null || mid.sprite == null ||
                top == null || top.sprite == null || bot == null || bot.sprite == null ||
                tooltipsRoot == null)
            {
                return;
            }

            // Measure against the actual width of Mid, not the prefab's placeholder rect.
            float midWidth = mid.sprite.bounds.size.x * Mathf.Abs(mid.transform.localScale.x);
            float textWidth = Mathf.Max(0.01f, midWidth - horizontalPadding * 2f);
            conditionText.rectTransform.sizeDelta = new Vector2(textWidth, 100f);
            conditionText.ForceMeshUpdate();
            float textHeight = conditionText.GetPreferredValues(
                conditionText.text,
                textWidth,
                Mathf.Infinity
            ).y;

            float midBaseH = mid.sprite.bounds.size.y * _midBaseScaleY;
            float desiredMidHeight = Mathf.Max(
                textHeight + verticalPadding * 2f,
                midBaseH
            );
            conditionText.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);

            // Keep the cap and tail at their original sizes.
            float botH = bot.sprite.bounds.size.y * Mathf.Abs(bot.transform.localScale.y);
            float topH = top.sprite.bounds.size.y * Mathf.Abs(top.transform.localScale.y);

            // Scale mid theo Y
            float scaleY = desiredMidHeight / mid.sprite.bounds.size.y;
            mid.transform.localScale = new Vector3(
                mid.transform.localScale.x, scaleY, mid.transform.localScale.z);

            float actualMidH = midBaseH * scaleY;

            // Keep Bot (and its tail) fixed. Mid grows upward from Bot, then Top follows.
            float botCenterY = _hasBotAnchor
                ? _botAnchorY
                : bot.transform.localPosition.y;
            float botTopY = botCenterY + botH * 0.5f;
            float midCenterY = botTopY + actualMidH * 0.5f;
            float topCenterY = botTopY + actualMidH + topH * 0.5f;
            float midTopY = botTopY + actualMidH;

            bot.transform.localPosition = new Vector3(
                bot.transform.localPosition.x, botCenterY, bot.transform.localPosition.z);

            mid.transform.localPosition = new Vector3(
                mid.transform.localPosition.x, midCenterY, mid.transform.localPosition.z);

            top.transform.localPosition = new Vector3(
                top.transform.localPosition.x, topCenterY, top.transform.localPosition.z);

            // Chỉ cập nhật Y cho text, giữ nguyên X, pivot, alignment, sizeDelta từ prefab
            if (nameText != null)
            {
                var namePos = nameText.transform.localPosition;
                nameText.transform.localPosition = new Vector3(namePos.x, topCenterY, namePos.z);
            }

            if (traitText != null)
            {
                var traitPos = traitText.transform.localPosition;
                traitText.transform.localPosition = new Vector3(traitPos.x, topCenterY, traitPos.z);
            }

            var conditionPos = conditionText.transform.localPosition;
            conditionText.verticalAlignment = VerticalAlignmentOptions.Top;
            conditionText.rectTransform.pivot = new Vector2(
                conditionText.rectTransform.pivot.x,
                1f
            );
            conditionText.transform.localPosition = new Vector3(
                conditionPos.x,
                midTopY - verticalPadding,
                conditionPos.z
            );
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleTooltips();
        }

        public void Hide()
        {
            if (!_isShowTooltips)
                return;

            _isShowTooltips = false;
            StopListeningForOutsideClick();
            HideTween().Forget();
        }

        private void ToggleTooltips()
        {
            if (_isShowTooltips)
            {
                Hide();
            }
            else
            {
                if (_activeTooltip != null && _activeTooltip != this)
                    _activeTooltip.Hide();

                _activeTooltip = this;
                _isShowTooltips = true;
                StartListeningForOutsideClick();
                ShowTween().Forget();
            }
        }

        private async UniTask ShowTween()
        {
            if (tooltipsRoot == null)
                return;

            Tween.StopAll(tooltipsRoot.transform);
            if (_animatedVisuals != null)
                Tween.StopAll(_animatedVisuals);

            tooltipsRoot.transform.localPosition = Vector3.zero;
            tooltipsRoot.transform.localScale = Vector3.one;
            SetAnimationScale(0f);
            SetVisualAlpha(0f);
            tooltipsRoot.SetActive(true);

            _ = Tween.Scale(_animatedVisuals, endValue: 1f, duration: duration, ease: showEase);
            _ = Tween.Custom(
                0f,
                1f,
                duration,
                onValueChange: SetVisualAlpha,
                ease: showEase
            );
            await Tween.LocalPosition(tooltipsRoot.transform, endValue: _originalLocalPos, duration: duration, ease: showEase);
        }

        private async UniTask HideTween()
        {
            if (tooltipsRoot == null)
                return;

            Tween.StopAll(tooltipsRoot.transform);
            if (_animatedVisuals != null)
                Tween.StopAll(_animatedVisuals);

            _ = Tween.Scale(_animatedVisuals, endValue: 0f, duration: duration, ease: hideEase);
            _ = Tween.Custom(
                GetCurrentAlpha(),
                0f,
                duration,
                onValueChange: SetVisualAlpha,
                ease: hideEase
            );
            await Tween.LocalPosition(tooltipsRoot.transform, endValue: Vector3.zero, duration: duration, ease: hideEase);

            tooltipsRoot.SetActive(false);
            if (_activeTooltip == this)
                _activeTooltip = null;
        }

        private void ConfigureAnimationHierarchy()
        {
            if (tooltipsRoot == null)
                return;

            Transform tooltipTransform = tooltipsRoot.transform;
            _animatedVisuals = tooltipTransform.Find("TooltipVisuals");
            if (_animatedVisuals == null)
            {
                GameObject visualContainer = new("TooltipVisuals");
                _animatedVisuals = visualContainer.transform;
                _animatedVisuals.SetParent(tooltipTransform, false);
                _animatedVisuals.SetSiblingIndex(0);
            }

            _animatedVisuals.localPosition = Vector3.zero;
            _animatedVisuals.localRotation = Quaternion.identity;
            _animatedVisuals.localScale = Vector3.one;

            MoveToParent(top != null ? top.transform : null, _animatedVisuals);
            MoveToParent(mid != null ? mid.transform : null, _animatedVisuals);
            MoveToParent(bot != null ? bot.transform : null, _animatedVisuals);

            // Keep text outside the animated background so its font size stays stable.
            MoveToParent(nameText != null ? nameText.transform : null, tooltipTransform);
            MoveToParent(traitText != null ? traitText.transform : null, tooltipTransform);
            MoveToParent(conditionText != null ? conditionText.transform : null, tooltipTransform);
        }

        private void ConfigureSorting()
        {
            if (tooltipsRoot == null)
                return;

            _sortingGroup = tooltipsRoot.GetComponent<SortingGroup>();
            if (_sortingGroup == null)
                _sortingGroup = tooltipsRoot.AddComponent<SortingGroup>();

            SpriteRenderer topmostPersonRenderer = null;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || renderer.transform.IsChildOf(tooltipsRoot.transform))
                    continue;

                if (topmostPersonRenderer == null ||
                    renderer.sortingOrder > topmostPersonRenderer.sortingOrder)
                {
                    topmostPersonRenderer = renderer;
                }
            }

            if (topmostPersonRenderer != null)
            {
                _sortingGroup.sortingLayerID = topmostPersonRenderer.sortingLayerID;
                _sortingGroup.sortingOrder = topmostPersonRenderer.sortingOrder + 1;
            }
        }

        private void CacheColors()
        {
            if (_hasCachedColors)
                return;

            _topBaseColor = GetOpaqueBaseColor(top != null ? top.color : Color.white);
            _midBaseColor = GetOpaqueBaseColor(mid != null ? mid.color : Color.white);
            _botBaseColor = GetOpaqueBaseColor(bot != null ? bot.color : Color.white);
            _nameBaseColor = GetOpaqueBaseColor(nameText != null ? nameText.color : Color.white);
            _traitBaseColor = GetOpaqueBaseColor(traitText != null ? traitText.color : Color.white);
            _conditionBaseColor = GetOpaqueBaseColor(conditionText != null ? conditionText.color : Color.white);
            _hasCachedColors = true;
        }

        private float GetCurrentAlpha()
        {
            if (top == null || _topBaseColor.a <= Mathf.Epsilon)
                return 1f;

            return top.color.a / _topBaseColor.a;
        }

        private void SetVisualAlpha(float alpha)
        {
            CacheColors();
            SetSpriteAlpha(top, _topBaseColor, alpha);
            SetSpriteAlpha(mid, _midBaseColor, alpha);
            SetSpriteAlpha(bot, _botBaseColor, alpha);
            SetTextAlpha(nameText, _nameBaseColor, alpha);
            SetTextAlpha(traitText, _traitBaseColor, alpha);
            SetTextAlpha(conditionText, _conditionBaseColor, alpha);
        }

        private void SetAnimationScale(float scale)
        {
            if (_animatedVisuals != null)
                _animatedVisuals.localScale = Vector3.one * scale;
        }

        private static void SetSpriteAlpha(SpriteRenderer renderer, Color baseColor, float alpha)
        {
            if (renderer == null)
                return;

            baseColor.a *= alpha;
            renderer.color = baseColor;
        }

        private static void SetTextAlpha(TextMeshPro text, Color baseColor, float alpha)
        {
            if (text == null)
                return;

            baseColor.a *= alpha;
            text.color = baseColor;
        }

        private static Color GetOpaqueBaseColor(Color color)
        {
            if (color.a <= Mathf.Epsilon)
                color.a = 1f;

            return color;
        }

        private static void MoveToParent(Transform child, Transform parent)
        {
            if (child != null && parent != null && child.parent != parent)
                child.SetParent(parent, true);
        }

        private string BuildConditionText(PersonRuntimeData person)
        {
            if (person?.Conditions == null || person.Conditions.Count == 0)
                return string.Empty;

            StringBuilder builder = new();
            int displayedCount = 0;
            foreach (ConditionRuntimeData condition in person.Conditions)
            {
                if (condition == null)
                    continue;

                string description = string.IsNullOrWhiteSpace(condition.Description)
                    ? condition.AngryDescription
                    : condition.Description;
                if (string.IsNullOrWhiteSpace(description))
                    continue;

                if (IsConditionSatisfied(condition))
                    description = $"<s>{description}</s>";

                if (builder.Length > 0)
                    builder.Append("\n\n");

                builder.Append(description);
                displayedCount++;
                if (displayedCount >= MaxConditions)
                    break;
            }

            return builder.ToString();
        }

        private bool IsConditionSatisfied(ConditionRuntimeData condition)
        {
            if (condition == null || _dragManager?.CurrentCell == null)
                return false;

            if (_gridManager == null)
                _gridManager = FindFirstObjectByType<GridManager>();

            return _gridManager != null &&
                   _gridManager.IsConditionSatisfied(_dragManager.CurrentCell, condition);
        }
    }
}
