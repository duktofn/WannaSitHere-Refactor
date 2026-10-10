using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using Game.Core.Board;
using Game.Core.People;
using Game.View.Board;
using Game.View.Input;

namespace Game.View.People
{
    public class PersonMover : MonoBehaviour
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
            public Tween DragTween;
        }

        public void BeginMove(
            Transform personTransform,
            Collider2D personCollider,
            Vector3 startPosition,
            CellView sourceCell = null)
        {
            if (dragContexts.TryGetValue(personTransform, out DragContext previousContext))
                previousContext.DragTween.Stop();
            personTransform.GetComponent<PersonView>()?.CancelFeedback();
            dragContexts[personTransform] = new DragContext
            {
                Collider = personCollider,
                StartPosition = startPosition,
                SourceCell = sourceCell
            };
        }

        public void DragTo(Transform personTransform, Vector3 targetWorldPosition)
        {
            if (!dragContexts.TryGetValue(personTransform, out DragContext context))
                return;
            context.DragTween.Stop();
            context.DragTween = Tween.Position(personTransform, targetWorldPosition, snapTime, moveEase);
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
            {
                Debug.LogWarning("[MoveToCell] FAIL: No drag context found for person.");
                return false;
            }

            context.DragTween.Stop();

            if (targetCell == null)
            {
                Debug.LogWarning("[MoveToCell] FAIL: targetCell is null — no overlapping cell found.");
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            if (targetCell.RuntimeData == null)
            {
                Debug.LogWarning($"[MoveToCell] FAIL: targetCell '{targetCell.name}' has null RuntimeData.");
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            if (targetCell.GetCellType() != CellType.Seat)
            {
                Debug.LogWarning($"[MoveToCell] FAIL: targetCell type is {targetCell.GetCellType()}, not Seat.");
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            if (gridManager == null)
            {
                Debug.LogWarning("[MoveToCell] FAIL: gridManager is null.");
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            PersonView movingPersonView = personTransform.GetComponent<PersonView>();
            PersonView displacedPersonView = targetCell.CurrentPersonView;
            bool changesSeat = context.SourceCell != targetCell;
            if (changesSeat)
            {
                movingPersonView?.PrepareForPlacement();
                displacedPersonView?.PrepareForPlacement();
            }

            if (!gridManager.TryMovePerson(context.SourceCell, targetCell, person))
            {
                movingPersonView?.CancelFeedback();
                if (changesSeat)
                    displacedPersonView?.CancelFeedback();
                Debug.LogWarning($"[MoveToCell] FAIL: TryMovePerson rejected. Source={context.SourceCell?.name}, Target={targetCell.name}");
                Tween.Position(personTransform, context.StartPosition, snapTime, moveEase);
                dragContexts.Remove(personTransform);
                return false;
            }

            if (context.SourceCell == targetCell)
            {
                movingPersonView.PrepareForPlacement();
                movingPersonView.MoveToSeat(targetCell.transform.position, snapTime, moveEase, playReaction: false);
                dragContexts.Remove(personTransform);
                return true;
            }

            context.SourceCell?.SetPersonView(displacedPersonView);
            targetCell.SetPersonView(movingPersonView);

            if (displacedPersonView != null && context.SourceCell != null)
            {
                displacedPersonView.MoveToSeat(
                    context.SourceCell.transform.position,
                    snapTime,
                    moveEase);

                displacedPersonView
                    .GetComponent<PersonDragManager>()
                    ?.SetCurrentCell(context.SourceCell);
            }

            movingPersonView.MoveToSeat(targetCell.transform.position, snapTime, moveEase);
            personTransform.GetComponent<PersonDragManager>()?.SetCurrentCell(targetCell);
            dragContexts.Remove(personTransform);
            return true;
        }

        /// <summary>
        /// Visually reverts a move by swapping PersonViews between the source and target cells
        /// and animating them back to their original positions.
        /// Called by <see cref="GridManager.RevertMoveView"/> during an Undo booster.
        /// </summary>
        public void RevertMove(CellView sourceCell, CellView targetCell)
        {
            if (sourceCell == null || targetCell == null) return;

            // The moved person is now at targetCell, needs to go back to sourceCell
            PersonView movedView = targetCell.CurrentPersonView;
            // The displaced person (if any) is now at sourceCell, needs to go back to targetCell
            PersonView displacedView = sourceCell.CurrentPersonView;

            // Swap the PersonView references on the cells
            sourceCell.SetPersonView(movedView);
            targetCell.SetPersonView(displacedView);

            // Animate and update drag references
            if (movedView != null)
            {
                movedView.PrepareForPlacement();
                movedView.MoveToSeat(sourceCell.transform.position, snapTime, moveEase);
                movedView.GetComponent<PersonDragManager>()?.SetCurrentCell(sourceCell);
            }

            if (displacedView != null)
            {
                displacedView.PrepareForPlacement();
                displacedView.MoveToSeat(targetCell.transform.position, snapTime, moveEase);
                displacedView.GetComponent<PersonDragManager>()?.SetCurrentCell(targetCell);
            }
        }
    }
}
