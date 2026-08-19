using System.Collections.Generic;
using Game.Domain.Grid;
using Game.Domain.Person;
using Game.Shared;
using UnityEngine;

namespace Game.Domain.Condition
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
            List<CellRuntimeData> adjacentCells = GetAdjacentCells(containCell.Index, mainGrid);

            foreach (ConditionRuntimeData condition in person.Conditions)
            {
                if (!_conditionChecker.Check(adjacentCells, condition))
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
