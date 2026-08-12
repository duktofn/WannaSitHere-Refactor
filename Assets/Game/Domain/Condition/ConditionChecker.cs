using System.Collections.Generic;
using Game.Domain.Grid;
using Game.Shared;

namespace Game.Domain.Condition
{
    public class ConditionChecker
    {
        public bool Check(List<CellRuntimeData> adjacent, ConditionRuntimeData condition)
        {
            foreach (var a in adjacent) 
            {
                if (condition.Target == ConditionTarget.Food &&
                    condition.Type == ConditionType.Hate &&
                    condition.FoodTarget == a.Food)
                {
                    return false;
                }

                if (condition.Target == ConditionTarget.Person && 
                    condition.Type == ConditionType.Hate &&
                    condition.TargetTrait == a.CurrentPerson.Trait)
                {
                    return false;
                }
            }

            return true;
        }

        
    }
}