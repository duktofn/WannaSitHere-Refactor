using UnityEngine;
using Game.Core.Board;
using Game.Core.People;
using Game.View.Input;
using Game.View.People;
using PrimeTween;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.InputSystem;

namespace Game.View.Board
{
    public class CellView : MonoBehaviour
    {
        private CellRuntimeData _cell;
        [SerializeField] private GameObject personViewPrefabs;
        [SerializeField] private PersonView personView;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField] private GameObject foodTooltips;
        [SerializeField] private TextMeshPro foodName;

        [Header("Show/Hide Tween")]
        [SerializeField] private float duration;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        private Vector3 _originalLocalPos;
        private InputAction _pointerPressAction;

        public CellRuntimeData RuntimeData => _cell;
        public PersonView CurrentPersonView => personView;

        public CellType GetCellType() => _cell.Type;

        private void Awake()
        {
            foodTooltips.SetActive(false);
            _originalLocalPos = foodTooltips.transform.localPosition;
            _pointerPressAction = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");
        }

        private void EnablePointerAction()
        {
            if (_pointerPressAction == null) return;
            _pointerPressAction.performed -= OnPointerPressed;
            _pointerPressAction.performed += OnPointerPressed;
            _pointerPressAction.Enable();
        }

        private void DisablePointerAction()
        {
            if (_pointerPressAction == null) return;
            _pointerPressAction.performed -= OnPointerPressed;
            _pointerPressAction.Disable();
        }

        private void OnEnable()
        {
            if (_cell != null && _cell.Type == CellType.Food)
            {
                EnablePointerAction();
            }
        }

        private void OnDisable()
        {
            DisablePointerAction();
        }

        private void OnDestroy()
        {
            _pointerPressAction?.Dispose();
        }

        private void InitCell(PersonMover personMoveManager)
        {
            if (_cell.Type == CellType.Food)
            {
                spriteRenderer.sprite = _cell.Sprite;
                foodName.text = _cell.Food.ToString();
                EnablePointerAction();
                return;
            }

            if (_cell.DefaultPerson != null)
            {
                if (personViewPrefabs == null)
                {
                    Debug.LogError($"[CellView] personViewPrefabs chưa được gán trên {gameObject.name}! Vui lòng kéo Prefab vào Inspector.", this);
                    return;
                }

                GameObject tmp = Instantiate(personViewPrefabs, transform.position, Quaternion.identity, transform.root);
                personView = tmp.GetComponent<PersonView>();
                personView.BindData(_cell.DefaultPerson);
                tmp.GetComponent<PersonDragManager>()
                    .Initialize(personMoveManager, _cell.DefaultPerson, this);
            }
        }

        public void BindData(CellRuntimeData cell, PersonMover personMoveManager)
        {
            if (cell == null) return;
            _cell = cell;
            personView = null;
            InitCell(personMoveManager);
        }

        public void SetPersonView(PersonView view)
        {
            personView = view;
        }

        public Vector2Int GetCellIndex()
        {
            if (_cell == null)
            {
                Debug.LogWarning("No Cell valid to get index");
                return Vector2Int.zero;
            }
            return _cell.Index;
        }
        
        public void AssignPersonToCell(PersonRuntimeData person)
        {
            _cell.SetPerson(person);
        }

        private void OnPointerPressed(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.OverlapPoint(worldPos);

            if (hit != null && hit.transform == transform)
            {
                ToggleTooltips();
            }
            else if (foodTooltips.activeSelf)
            {
                HideTween().Forget();
            }
        }

        private void ToggleTooltips()
        {
            if (GetCellType() != CellType.Food) return;
            if (!foodTooltips.activeSelf)
            {
                ShowTween().Forget();
            }
            else
            {
                HideTween().Forget();
            }
        }

        private async UniTask ShowTween()
        {
            Tween.StopAll(foodTooltips.transform);

            foodTooltips.transform.localPosition = Vector3.zero;
            foodTooltips.transform.localScale = Vector3.zero;
            foodTooltips.SetActive(true);

            _ = Tween.Scale(foodTooltips.transform, endValue: 1f, duration: duration, ease: showEase);
            await Tween.LocalPosition(foodTooltips.transform, endValue: _originalLocalPos, duration: duration, ease: showEase);
        }

        private async UniTask HideTween()
        {
            Tween.StopAll(foodTooltips.transform);

            _ = Tween.Scale(foodTooltips.transform, endValue: 0f, duration: duration, ease: hideEase);
            await Tween.LocalPosition(foodTooltips.transform, endValue: Vector3.zero, duration: duration, ease: hideEase);

            foodTooltips.SetActive(false);
        }
    }
}
