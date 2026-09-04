using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Core.People;
using Game.View.Board;
using Game.View.People;

namespace Game.View.Input
{
    public class PersonDragManager : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("Scale Tween")]
        [SerializeField] private float scaleFactor;
        [SerializeField] private float playTime;
        [SerializeField] private Ease scaleEase;

        [Header("Tooltip")]
        [SerializeField] private PersonTooltip personTooltip;

        private Collider2D col;
        private PersonMover personMove;
        private PersonRuntimeData person;
        private CellView currentCell;
        private Vector3 lastDragWorldPos;
        private bool hasLastDragWorldPos;

        public CellView CurrentCell => currentCell;

        public void Initialize(
            PersonMover moveManager,
            PersonRuntimeData runtimePerson,
            CellView startCell)
        {
            personMove = moveManager;
            person = runtimePerson;
            currentCell = startCell;
        }

        public void SetCurrentCell(CellView cell)
        {
            currentCell = cell;
        }

        private void Awake()
        {
            col = GetComponent<Collider2D>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (personMove == null)
                return;

            if (personTooltip != null)
                personTooltip.Hide();

            personMove.BeginMove(transform, col, transform.position, currentCell);
            lastDragWorldPos = GetPointerWorldPos(eventData);
            hasLastDragWorldPos = true;
            personMove.DragTo(transform, lastDragWorldPos);
            Tween.Scale(transform, scaleFactor, playTime, scaleEase);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (personMove == null)
                return;

            Vector3 targetWorldPos = GetPointerWorldPos(eventData);

            if (hasLastDragWorldPos &&
                (targetWorldPos - lastDragWorldPos).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            lastDragWorldPos = targetWorldPos;
            hasLastDragWorldPos = true;
            personMove.DragTo(transform, targetWorldPos);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            hasLastDragWorldPos = false;
            Tween.Scale(transform, 1f, playTime, scaleEase);

            if (personMove == null)
                return;

            CellView targetCell = personMove.GetOverlappingCell(transform);
            personMove.MoveToCell(transform, person, targetCell);
        }

        private Vector3 GetPointerWorldPos(PointerEventData eventData)
        {
            Vector2 screenPos = eventData.position;
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return transform.position;

            return mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPos.x,
                    screenPos.y,
                    -mainCamera.transform.position.z
                )
            );
        }
    }
}
