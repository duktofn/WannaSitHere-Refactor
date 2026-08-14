using System.Collections.Generic;
using Game.Domain.Person;
using Game.Shared;
using Game.View.Person;
using PrimeTween;
using UnityEngine;

namespace Game.Manager
{
    public class PersonMoveManager : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private LayerMask cellLayer;
        [SerializeField] private float snapTime;
        [SerializeField] private Ease moveEase;

        private readonly Dictionary<Transform, DragContext> dragContexts = new();

        private sealed class DragContext
        {
            public Collider2D Collider;
            public Vector3 StartPosition;
            public CellView SourceCell;
        }

        public void BeginMove(
            Transform personTransform,
            Collider2D personCollider,
            Vector3 startPosition,
            CellView sourceCell = null)
        {
            dragContexts[personTransform] = new DragContext
            {
                Collider = personCollider,
                StartPosition = startPosition,
                SourceCell = sourceCell
            };
        }

        public void DragTo(Transform personTransform, Vector3 targetWorldPosition)
        {
            Tween.Position(personTransform, targetWorldPosition, snapTime, moveEase);
        }

        public CellView GetOverlappingCell(Transform personTransform)
        {
            if (!dragContexts.TryGetValue(personTransform, out DragContext context) ||
                context.Collider == null)
            {
                return null;
            }

            List<Collider2D> overlapCells = new();

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(cellLayer);

            Physics2D.OverlapCollider(context.Collider, contactFilter, overlapCells);

            Collider2D result = null;
            float closestDistance = float.MaxValue;
            foreach (Collider2D collider in overlapCells)
            {
                float distance = Vector2.Distance(
                    collider.bounds.center,
                    context.Collider.bounds.center
                );

                if (distance < closestDistance || result == null)
                {
                    result = collider;
                    closestDistance = distance;
                }
            }

            return result == null ? null : result.GetComponent<CellView>();
        }

        public bool MoveToCell(
            Transform personTransform,
            PersonRuntimeData person,
            CellView targetCell)
        {
            if (!dragContexts.TryGetValue(personTransform, out DragContext context))
                return false;

            if (targetCell == null || targetCell.GetCellType() != CellType.Seat)
            {
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            if (gridManager == null ||
                !gridManager.TryMovePerson(context.SourceCell, targetCell, person))
            {
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            if (context.SourceCell == targetCell)
            {
                Tween.Position(personTransform, targetCell.transform.position, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return true;
            }

            PersonView movingPersonView = personTransform.GetComponent<PersonView>();
            PersonView displacedPersonView = targetCell.CurrentPersonView;

            context.SourceCell?.SetPersonView(displacedPersonView);
            targetCell.SetPersonView(movingPersonView);

            if (displacedPersonView != null && context.SourceCell != null)
            {
                Tween.Position(
                    displacedPersonView.transform,
                    context.SourceCell.transform.position,
                    snapTime,
                    moveEase);

                displacedPersonView
                    .GetComponent<PersonDragManager>()
                    ?.SetCurrentCell(context.SourceCell);
            }

            Tween.Position(personTransform, targetCell.transform.position, snapTime, moveEase);
            personTransform.GetComponent<PersonDragManager>()?.SetCurrentCell(targetCell);
            dragContexts.Remove(personTransform);
            return true;
        }
    }
}
