using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data.Conditions;
using Game.Data.People;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.People;

namespace Game.Data.Levels
{
    /// <summary>
    /// Level-owned placement and condition configuration for one character
    /// occurrence. The same definition may be used more than once.
    /// </summary>
    [Serializable]
    public class LevelPersonConfig
    {
        public PersonDefinitionSO definition;
        public List<ConditionDataSO> conditions = new();
        public GridId gridId = GridId.MainGrid;
        public Vector2Int position;

        public PersonRuntimeData ToRuntimeData()
        {
            if (definition == null)
                return null;

            List<ConditionRuntimeData> runtimeConditions = new();
            if (conditions != null)
            {
                foreach (ConditionDataSO condition in conditions)
                {
                    if (condition != null)
                        runtimeConditions.Add(condition.ToRuntimeData());
                }
            }

            return definition.ToRuntimeData(runtimeConditions);
        }
    }
}
