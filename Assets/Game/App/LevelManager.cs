using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.Levels;
using Game.Core.Conditions;
using Game.Core.Board;
using Game.Core.Booster;
using Game.Core.People;

namespace Game.App
{
    public class LevelManager
    {
        private readonly LevelRuntimeData _currentLevel;
        private readonly LevelConditionEvaluator _conditionEvaluator;
        private readonly MoveHistory _moveHistory;
        public event Action<LevelManager, LevelOutcome> OutcomeRaised;
        public event Action MoveSucceeded;

        public LevelRuntimeData CurrentLevel => _currentLevel;
        public LevelConditionEvaluator ConditionEvaluator => _conditionEvaluator;
        public MoveHistory MoveHistory => _moveHistory;

        public LevelManager(
            LevelRuntimeData currentLevel,
            List<Vector2Int> adjacentOffsets)
        {
            _currentLevel = currentLevel;
            _conditionEvaluator = new LevelConditionEvaluator(adjacentOffsets);
            _moveHistory = new MoveHistory();
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
                return moveSucceeded;
            }

            if (sourceCell != null && sourceCell.CurrentPerson != person)
                return false;

            PersonRuntimeData targetPerson = targetCell.CurrentPerson;

            if (targetPerson != null && sourceCell == null)
                return false;

            targetCell.SetPerson(person);
            sourceCell?.SetPerson(targetPerson);

            bool isWaitLineToWaitLine = sourceCell != null &&
                                        sourceCell.OwnGrid == GridId.WaitGrid &&
                                        targetCell.OwnGrid == GridId.WaitGrid;

            if (sourceCell != null && !isWaitLineToWaitLine)
            {
                _moveHistory.Record(new MoveRecord(sourceCell, targetCell, person, targetPerson));
            }

            _currentLevel.ModifyMove(-1);
            CheckAllPersonConditions();
            MoveSucceeded?.Invoke();

            return true;
        }

        public void CheckAllPersonConditions()
        {
            if (_currentLevel == null) return;

            _conditionEvaluator.UpdateAllPersonStates(_currentLevel.MainGrid, _currentLevel.WaitGrid);

            if (_conditionEvaluator.AreAllPersonConditionsSatisfied(_currentLevel.MainGrid, _currentLevel.WaitGrid))
            {
                OutcomeRaised?.Invoke(this, LevelOutcome.Won);
                return;
            }

            if (_currentLevel.IsOutOfMove)
            {
                OutcomeRaised?.Invoke(this, LevelOutcome.Lost);
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

    public enum LevelOutcome
    {
        Won,
        Lost
    }
}
