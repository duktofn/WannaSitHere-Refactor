using System.Collections.Generic;
using UnityEngine;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.Levels;
using Game.Core.People;

namespace Game.Core.Booster
{
    /// <summary>
    /// Replaces all conditions of a randomly chosen person with CanSitAnywhere.
    /// Priority: Angry person on MainGrid first; if none, random person on WaitGrid with conditions.
    /// Persons that already have CanSitAnywhere are never eligible targets.
    /// After execution, read <see cref="TargetPerson"/> to identify who was affected for view feedback.
    /// </summary>
    public class RemoveBooster : Booster
    {
        private readonly LevelRuntimeData _levelData;
        private readonly ConditionRuntimeData _canSitAnywhereCondition;
        private PersonRuntimeData _targetPerson;

        /// <summary>The person whose conditions were replaced. Read after <see cref="Booster.TryUse"/> returns true.</summary>
        public PersonRuntimeData TargetPerson => _targetPerson;

        public RemoveBooster(LevelRuntimeData levelData, ConditionRuntimeData canSitAnywhereCondition)
        {
            _levelData = levelData;
            _canSitAnywhereCondition = canSitAnywhereCondition;
        }

        protected override bool CanUse()
        {
            if (_canSitAnywhereCondition == null || !_canSitAnywhereCondition.IsCanSitAnywhere)
            {
                _targetPerson = null;
                return false;
            }

            _targetPerson = FindTarget();
            return _targetPerson != null;
        }

        protected override void Execute()
        {
            _targetPerson.ReplaceConditions(_canSitAnywhereCondition);
        }

        private PersonRuntimeData FindTarget()
        {
            if (_levelData == null) return null;

            // Priority 1: Random Angry person on MainGrid
            PersonRuntimeData result = FindRandomPerson(
                _levelData.MainGrid,
                person => IsEligibleTarget(person) && person.State == PersonState.Angry
            );

            if (result != null) return result;

            // Priority 2: Random person on WaitGrid with conditions
            return FindRandomPerson(
                _levelData.WaitGrid,
                IsEligibleTarget
            );
        }

        private static bool IsEligibleTarget(PersonRuntimeData person)
        {
            if (person == null || person.Conditions == null || person.Conditions.Count == 0)
                return false;

            foreach (ConditionRuntimeData condition in person.Conditions)
            {
                if (condition != null && condition.IsCanSitAnywhere)
                    return false;
            }

            return true;
        }

        private static PersonRuntimeData FindRandomPerson(
            Grid<CellRuntimeData> grid,
            System.Func<PersonRuntimeData, bool> predicate)
        {
            if (grid == null) return null;

            List<PersonRuntimeData> candidates = new();
            foreach (CellRuntimeData cell in grid.GridContent)
            {
                if (cell?.CurrentPerson != null && predicate(cell.CurrentPerson))
                {
                    candidates.Add(cell.CurrentPerson);
                }
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
