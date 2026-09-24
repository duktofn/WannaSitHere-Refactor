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
        [SerializeField] private Sprite checkedSprite;
        [SerializeField] private Sprite uncheckedSprite;

        [Header("Show/Hide Tween")]
        [SerializeField] private float duration;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        [Header("Mid Resize")]
        [SerializeField, FormerlySerializedAs("padding")] private float topPadding = 0.08f;
        [SerializeField] private float bottomPadding = 0.08f;
        [SerializeField] private float conditionSpacing = 0.04f;
        [SerializeField] private float horizontalPadding = 0.15f;

        private const int MaxConditions = 2;

        private bool _isShowTooltips;
        private Vector3 _originalLocalPos;
        private InputAction _pointerPressAction;
        private float _botAnchorY;
        private bool _hasBotAnchor;
        private float _midBaseScaleY = 1f;
        private PersonRuntimeData _boundPerson;
        private readonly TextMeshPro[] _conditionRows = new TextMeshPro[MaxConditions];
        private readonly SpriteRenderer[] _conditionCheckboxes = new SpriteRenderer[MaxConditions];

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
            _conditionRows[0] = conditionText;
            for (int i = 0; i < MaxConditions; i++)
            {
                if (i > 0)
                {
                    GameObject row = Instantiate(conditionText.gameObject, conditionText.transform.parent);
                    row.name = "ConditionText" + (i + 1);
                    _conditionRows[i] = row.GetComponent<TextMeshPro>();
                    _conditionRows[i].GetComponent<MeshRenderer>().sortingLayerName = textSortingLayer;
                }

                GameObject checkbox = new GameObject("ConditionCheckbox" + (i + 1));
                checkbox.transform.SetParent(tooltipsRoot.transform, false);
                _conditionCheckboxes[i] = checkbox.AddComponent<SpriteRenderer>();
                _conditionCheckboxes[i].sortingLayerName = textSortingLayer;
                _conditionCheckboxes[i].sortingOrder = conditionText.GetComponent<MeshRenderer>().sortingOrder + 1;
                checkbox.SetActive(false);
            }
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
            if (_boundPerson != null)
            {
                _boundPerson.OnConditionsCleared -= RefreshConditions;
                _boundPerson.OnConditionStatusChanged -= RefreshConditions;
            }
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
            if (_boundPerson != null)
            {
                _boundPerson.OnConditionsCleared -= RefreshConditions;
                _boundPerson.OnConditionStatusChanged -= RefreshConditions;
            }

            _boundPerson = person;

            if (person == null) return;

            person.OnConditionsCleared += RefreshConditions;
            person.OnConditionStatusChanged += RefreshConditions;

            nameText.text = person.PersonName;
            traitText.text = person.Trait.ToString();

            UpdateConditionText(person);
            ResizeToFitConditionText();
        }

        private void RefreshConditions()
        {
            if (_boundPerson == null) return;
            UpdateConditionText(_boundPerson);
            ResizeToFitConditionText();
        }

        private void UpdateConditionText(PersonRuntimeData person)
        {
            if (person.Conditions != null && person.Conditions.Count > 0)
            {
                int count = Math.Min(person.Conditions.Count, MaxConditions);
                for (int i = 0; i < MaxConditions; i++)
                {
                    bool hasCondition = i < count;
                    _conditionRows[i].text = hasCondition ? person.Conditions[i].Description : string.Empty;
                    _conditionCheckboxes[i].sprite = hasCondition && person.ConditionSatisfied[i]
                        ? checkedSprite
                        : uncheckedSprite;
                    _conditionCheckboxes[i].gameObject.SetActive(hasCondition);
                }
            }
            else
            {
                for (int i = 0; i < MaxConditions; i++)
                {
                    _conditionRows[i].text = string.Empty;
                    _conditionCheckboxes[i].gameObject.SetActive(false);
                }
            }
        }

        private void ResizeToFitConditionText()
        {
            if (conditionText == null || top == null || top.sprite == null ||
                mid == null || mid.sprite == null || bot == null || bot.sprite == null)
            {
                return;
            }

            float midWidth = mid.sprite.bounds.size.x * Mathf.Abs(mid.transform.localScale.x);
            const float checkboxSize = 0.18f;
            const float checkboxGap = 0.04f;
            float checkboxX = -midWidth * 0.5f + horizontalPadding + checkboxSize * 0.5f;
            float textWidth = Mathf.Max(0.01f, midWidth - horizontalPadding * 2f - checkboxSize - checkboxGap);
            float[] rowHeights = new float[MaxConditions];
            float textHeight = 0f;
            for (int i = 0; i < MaxConditions; i++)
            {
                TextMeshPro row = _conditionRows[i];
                row.horizontalAlignment = HorizontalAlignmentOptions.Left;
                row.verticalAlignment = VerticalAlignmentOptions.Top;
                row.rectTransform.pivot = new Vector2(0f, 1f);
                row.rectTransform.sizeDelta = new Vector2(textWidth, 100f);
                row.ForceMeshUpdate();
                rowHeights[i] = string.IsNullOrEmpty(row.text)
                    ? 0f
                    : row.GetPreferredValues(row.text, textWidth, Mathf.Infinity).y;
                textHeight += rowHeights[i];
            }
            if (rowHeights[0] > 0f && rowHeights[1] > 0f)
                textHeight += conditionSpacing;

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

            float rowTopY = botTopY + actualMidHeight - topPadding;
            for (int i = 0; i < MaxConditions; i++)
            {
                TextMeshPro row = _conditionRows[i];
                row.rectTransform.sizeDelta = new Vector2(textWidth, rowHeights[i]);
                row.transform.localPosition = new Vector3(
                    checkboxX + checkboxSize * 0.5f + checkboxGap,
                    rowTopY,
                    row.transform.localPosition.z
                );

                if (rowHeights[i] > 0f)
                {
                    SpriteRenderer checkbox = _conditionCheckboxes[i];
                    checkbox.transform.localPosition = new Vector3(
                        checkboxX,
                        rowTopY - checkboxSize * 0.5f,
                        row.transform.localPosition.z - 0.01f
                    );
                    if (checkbox.sprite != null && uncheckedSprite != null)
                    {
                        // Both frames use the same pixel size; the checked sprite also includes the protruding tick.
                        float scale = checkboxSize * checkbox.sprite.pixelsPerUnit / uncheckedSprite.rect.width;
                        checkbox.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }

                float spacingAfterRow = i < MaxConditions - 1 && rowHeights[i + 1] > 0f
                    ? conditionSpacing
                    : 0f;
                rowTopY -= rowHeights[i] + spacingAfterRow;
            }
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
