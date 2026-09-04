using System.Collections.Generic;
using UnityEngine;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.Levels;
using Game.Core.People;
using Game.Events;

namespace Game.App
{
    public class LevelManager
    {
        private readonly LevelRuntimeData _currentLevel;
        private readonly LevelConditionEvaluator _conditionEvaluator;
        private readonly VoidEventChannelSO _onWinEvent;
        private readonly VoidEventChannelSO _onLoseEvent;

        public LevelRuntimeData CurrentLevel => _currentLevel;
        public LevelConditionEvaluator ConditionEvaluator => _conditionEvaluator;

        public LevelManager(
            LevelRuntimeData currentLevel,
            List<Vector2> adjacentOffsets,
            VoidEventChannelSO onWinEvent = null,
            VoidEventChannelSO onLoseEvent = null)
        {
            _currentLevel = currentLevel;
            _conditionEvaluator = new LevelConditionEvaluator(adjacentOffsets);
            _onWinEvent = onWinEvent;
            _onLoseEvent = onLoseEvent;
        }

        public bool TryMovePerson(
            CellRuntimeData sourceCell,
            CellRuntimeData targetCell,
            PersonRuntimeData person)
        {
            if (_currentLevel == null || targetCell == null || person == null)
                return false;

            if (targetCell.Type != CellType.Seat)
                return false;

            if (sourceCell == targetCell)
            {
                bool moveSucceeded = targetCell.CurrentPerson == person;
                if (moveSucceeded)
                    _currentLevel.ModifyMove(-1);

                return moveSucceeded;
            }

            if (sourceCell != null && sourceCell.CurrentPerson != person)
                return false;

            PersonRuntimeData targetPerson = targetCell.CurrentPerson;

            if (targetPerson != null && sourceCell == null)
                return false;

            targetCell.SetPerson(person);
            sourceCell?.SetPerson(targetPerson);

            // Consume one move after a successful move operation.
            _currentLevel.ModifyMove(-1);
            CheckAllPersonConditions();

            return true;
        }

        public void CheckAllPersonConditions()
        {
            if (_currentLevel == null) return;

            _conditionEvaluator.UpdateAllPersonStates(_currentLevel.MainGrid, _currentLevel.WaitGrid);

            if (_conditionEvaluator.AreAllPersonConditionsSatisfied(_currentLevel.MainGrid, _currentLevel.WaitGrid))
            {
                _onWinEvent?.Raise();
                return;
            }

            if (_currentLevel.IsOutOfMove)
            {
                _onLoseEvent?.Raise();
            }
        }

        public void CheckPersonCondition(CellRuntimeData containCell, PersonRuntimeData person, GridId cellGrid)
        {
            if (_currentLevel?.MainGrid == null) return;
            _conditionEvaluator.CheckPersonCondition(containCell, person, cellGrid, _currentLevel.MainGrid);
        }

        public bool IsConditionSatisfied(CellRuntimeData cell, ConditionRuntimeData condition)
        {
            if (cell == null || _currentLevel?.MainGrid == null || _conditionEvaluator == null)
                return false;

            return _conditionEvaluator.IsConditionSatisfied(cell, condition, _currentLevel.MainGrid);
        }

        public List<CellRuntimeData> GetAdjacentCells(Vector2Int index, Grid<CellRuntimeData> grid)
        {
            return _conditionEvaluator.GetAdjacentCells(index, grid);
        }
    }
}

