using UnityEngine;
using TMPro;
using PrimeTween;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Game.View.Board
{
    public class FoodTooltips : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipVisual;
        [SerializeField] private TextMeshPro foodName;

        [Header("Show/Hide Tween")]
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        private Vector3 _originalLocalPos;
        private InputAction _pointerPressAction;
        private Collider2D _cellCollider;
        private bool _isInitialized;

        public bool IsVisible => tooltipVisual != null ? tooltipVisual.activeSelf : gameObject.activeSelf;

        private void Awake()
        {
            if (tooltipVisual == null)
                tooltipVisual = gameObject;

            _originalLocalPos = tooltipVisual.transform.localPosition;
            tooltipVisual.SetActive(false);

            if (foodName == null)
                foodName = GetComponentInChildren<TextMeshPro>(true);

            _pointerPressAction = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");
        }

        public void Initialize(string name, Collider2D cellCollider)
        {
            _cellCollider = cellCollider;
            if (foodName != null)
                foodName.text = name;

            _isInitialized = true;
            EnablePointerAction();
        }

        public void EnablePointerAction()
        {
            if (!_isInitialized || _pointerPressAction == null) return;
            _pointerPressAction.performed -= OnPointerPressed;
            _pointerPressAction.performed += OnPointerPressed;
            _pointerPressAction.Enable();
        }

        public void DisablePointerAction()
        {
            if (_pointerPressAction == null) return;
            _pointerPressAction.performed -= OnPointerPressed;
            _pointerPressAction.Disable();
        }

        private void OnEnable()
        {
            if (_isInitialized)
                EnablePointerAction();
        }

        private void OnDisable()
        {
            DisablePointerAction();
        }

        private void OnDestroy()
        {
            _pointerPressAction?.Dispose();
        }

        private void OnPointerPressed(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.OverlapPoint(worldPos);

            bool isHitSelf = hit != null && (
                (_cellCollider != null && hit == _cellCollider) ||
                hit.transform == transform ||
                (_cellCollider != null && hit.transform == _cellCollider.transform)
            );

            if (isHitSelf)
            {
                ToggleTooltips();
            }
            else if (IsVisible)
            {
                HideTween().Forget();
            }
        }

        public void ToggleTooltips()
        {
            if (!IsVisible)
            {
                ShowTween().Forget();
            }
            else
            {
                HideTween().Forget();
            }
        }

        public async UniTask ShowTween()
        {
            GameObject target = tooltipVisual != null ? tooltipVisual : gameObject;
            Tween.StopAll(target.transform);

            target.transform.localPosition = Vector3.zero;
            target.transform.localScale = Vector3.zero;
            target.SetActive(true);

            _ = Tween.Scale(target.transform, endValue: 1f, duration: duration, ease: showEase);
            await Tween.LocalPosition(target.transform, endValue: _originalLocalPos, duration: duration, ease: showEase);
        }

        public async UniTask HideTween()
        {
            GameObject target = tooltipVisual != null ? tooltipVisual : gameObject;
            Tween.StopAll(target.transform);

            _ = Tween.Scale(target.transform, endValue: 0f, duration: duration, ease: hideEase);
            await Tween.LocalPosition(target.transform, endValue: Vector3.zero, duration: duration, ease: hideEase);

            target.SetActive(false);
        }
    }
}