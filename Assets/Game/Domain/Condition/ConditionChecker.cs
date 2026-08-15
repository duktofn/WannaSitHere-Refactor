using System.Collections.Generic;
using Game.Domain.Grid;
using Game.Shared;

namespace Game.Domain.Condition
{
    public class ConditionChecker
    {
        public bool Check(List<CellRuntimeData> adjacent, ConditionRuntimeData condition)
        {
            if (condition == null)
                return true;

            if (adjacent == null)
                return condition.Type == ConditionType.Hate;

            bool hasMatchingTarget = false;

            foreach (CellRuntimeData cell in adjacent)
            {
                if (MatchTarget(cell, condition))
                {
                    hasMatchingTarget = true;
                    break;
                }
            }

            return condition.Type == ConditionType.Hate ? !hasMatchingTarget : hasMatchingTarget;
        }

        private bool MatchTarget(CellRuntimeData cell, ConditionRuntimeData condition)
        {
            if (cell == null)
                return false;

            if (condition.Target == ConditionTarget.Food)
            {
                return cell.Type == CellType.Food &&
                       cell.Food == condition.FoodTarget;
            }

            if (condition.Target == ConditionTarget.Person)
            {
                return cell.Type == CellType.Seat &&
                       cell.CurrentPerson != null &&
                       cell.CurrentPerson.Trait == condition.TargetTrait;
            }

            return false;
        }
    }
}
