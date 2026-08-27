using System.Collections.Generic;
using UnityEngine;
using Game.Core.Board;
using Game.Core.People;

namespace Game.Core.Conditions
{
    public class LevelConditionEvaluator
    {
        private readonly ConditionChecker _conditionChecker = new();
        private readonly List<Vector2> _adjacentOffsets;

        public LevelConditionEvaluator(List<Vector2> adjacentOffsets)
        {
            _adjacentOffsets = adjacentOffsets;
        }

        public bool AreAllPersonConditionsSatisfied(
            Grid<CellRuntimeData> mainGrid,
            Grid<CellRuntimeData> waitGrid)
        {
            return CheckGridPersonConditions(mainGrid, mainGrid) &&
                   CheckGridPersonConditions(waitGrid, mainGrid);
        }

        public List<CellRuntimeData> GetAdjacentCells(
            Vector2Int index,
            Grid<CellRuntimeData> grid)
        {
            List<CellRuntimeData> result = new();

            foreach (Vector2 offset in _adjacentOffsets)
            {
                foreach (CellRuntimeData cell in grid.GridContent)
                {
                    if (cell.Index == index + offset)
                        result.Add(cell);
                }
            }

            return result;
        }

        public bool IsConditionSatisfied(
            CellRuntimeData containCell,
            ConditionRuntimeData condition,
            Grid<CellRuntimeData> mainGrid)
        {
            if (containCell == null || mainGrid == null)
                return false;

            if (condition == null)
                return true;

            if (containCell.OwnGrid == GridId.WaitGrid)
                return false;

            return _conditionChecker.Check(
                GetAdjacentCells(containCell.Index, mainGrid),
                condition
            );
        }

        public void CheckPersonCondition(
            CellRuntimeData containCell,
            PersonRuntimeData person,
            GridId cellGrid,
            Grid<CellRuntimeData> mainGrid)
        {
            if (containCell == null || person == null)
                return;

            if (cellGrid == GridId.WaitGrid)
            {
                person.SetState(PersonState.Normal);
                return;
            }

            bool isConditionOk = true;
            foreach (ConditionRuntimeData condition in person.Conditions)
            {
                if (!IsConditionSatisfied(containCell, condition, mainGrid))
                    isConditionOk = false;

                if (!isConditionOk)
                    break;
            }

            person.SetState(isConditionOk ? PersonState.Happy : PersonState.Angry);
        }

        private bool CheckGridPersonConditions(
            Grid<CellRuntimeData> grid,
            Grid<CellRuntimeData> mainGrid)
        {
            if (grid == null)
                return false;

            foreach (CellRuntimeData cell in grid.GridContent)
            {
                if (cell?.CurrentPerson == null)
                    continue;

                CheckPersonCondition(cell, cell.CurrentPerson, cell.OwnGrid, mainGrid);
                if (cell.CurrentPerson.State != PersonState.Happy)
                    return false;
            }

            return true;
        }
    }
}
