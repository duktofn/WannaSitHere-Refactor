using System.Collections.Generic;
using UnityEngine;
using Game.Core.Board;
using Game.Core.People;

namespace Game.Core.Conditions
{
    public class LevelConditionEvaluator
    {
        private readonly ConditionChecker _conditionChecker = new();
        private readonly List<Vector2Int> _adjacentOffsets;

        public LevelConditionEvaluator(List<Vector2Int> adjacentOffsets)
        {
            _adjacentOffsets = adjacentOffsets;
        }

        public void UpdateAllPersonStates(
            Grid<CellRuntimeData> mainGrid,
            Grid<CellRuntimeData> waitGrid)
        {
            UpdateGridPersonStates(mainGrid, mainGrid);
            UpdateGridPersonStates(waitGrid, mainGrid);
        }

        private void UpdateGridPersonStates(
            Grid<CellRuntimeData> grid,
            Grid<CellRuntimeData> mainGrid)
        {
            if (grid == null)
                return;

            foreach (CellRuntimeData cell in grid.GridContent)
            {
                if (cell?.CurrentPerson != null)
                {
                    CheckPersonCondition(cell, cell.CurrentPerson, cell.OwnGrid, mainGrid);
                }
            }
        }

        public bool AreAllPersonConditionsSatisfied(
            Grid<CellRuntimeData> mainGrid,
            Grid<CellRuntimeData> waitGrid)
        {
            return IsGridAllHappy(mainGrid) && IsGridAllHappy(waitGrid);
        }

        private bool IsGridAllHappy(Grid<CellRuntimeData> grid)
        {
            if (grid == null)
                return false;

            foreach (CellRuntimeData cell in grid.GridContent)
            {
                if (cell?.CurrentPerson != null && cell.CurrentPerson.State != PersonState.Happy)
                    return false;
            }

            return true;
        }

        public List<CellRuntimeData> GetAdjacentCells(
            Vector2Int index,
            Grid<CellRuntimeData> grid)
        {
            List<CellRuntimeData> result = new();
            if (grid == null || _adjacentOffsets == null)
                return result;

            foreach (Vector2Int v in _adjacentOffsets)
            {
                Vector2Int tmp = index + v;
                CellRuntimeData cell = grid.Get(tmp.x, tmp.y);
                if (cell != null)
                    result.Add(cell);
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
                for (int i = 0; i < person.Conditions.Count; i++)
                    person.SetConditionSatisfied(i, false);
                person.SetState(PersonState.Normal);
                return;
            }

            bool isConditionOk = true;
            for (int i = 0; i < person.Conditions.Count; i++)
            {
                bool satisfied = IsConditionSatisfied(containCell, person.Conditions[i], mainGrid);
                person.SetConditionSatisfied(i, satisfied);
                isConditionOk &= satisfied;
            }

            person.SetState(isConditionOk ? PersonState.Happy : PersonState.Angry);
        }
    }
}
