using System.Collections.Generic;
using Game.Domain.Grid;
using Game.Shared;
using UnityEngine;
using UnityEngine.Analytics;

namespace Game.Domain.Condition
{
    public class ConditionEvaluator
    {
        private Grid<CellRuntimeData> _grid;
        
        public bool Check(Vector2Int CellPos, ConditionRuntimeData condition)
        {
            List <Vector2Int> adjacent = new();

            if (CellPos.y < _grid.GridSize.y) 
                adjacent.Add(new Vector2Int(CellPos.x, CellPos.y + 1));
            
            if (CellPos.y > 0) 
                adjacent.Add(new Vector2Int(CellPos.x, CellPos.y - 1));

            if (CellPos.x < _grid.GridSize.y)
                adjacent.Add(new Vector2Int(CellPos.x + 1, CellPos.y));
            
            if (CellPos.x > 0) 
                adjacent.Add(new Vector2Int(CellPos.x - 1, CellPos.y));

            foreach (var a in adjacent) 
            {
                if (condition.Target == ConditionTarget.Food && 
                    condition.Type == ConditionType.Hate &&
                    condition.FoodTarget == _grid.Get(a.x, a.y).Food)
                {
                    return false;
                }

                if (condition.Target == ConditionTarget.Person && 
                    condition.Type == ConditionType.Hate &&
                    condition.TargetTrait == _grid.Get(a.x, a.y).Person.Trait)
                {
                    return false;
                }
            }

            return true;
        }
    }
}