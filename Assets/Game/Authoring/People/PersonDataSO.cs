using System.Collections.Generic;
using UnityEngine;
using Game.Authoring.Conditions;
using Game.Core.Conditions;
using Game.Core.People;

namespace Game.Authoring.People
{
    [CreateAssetMenu(fileName = "New Person", menuName = "Game/New Person")]
    public class PersonDataSO : ScriptableObject
    {
        public string personName;
        public PersonTrait trait;
        public List<ConditionDataSO> conditions;
        public Sprite baseSprite;

        public PersonRuntimeData ToRuntimeData()
        {
            List<ConditionRuntimeData> conditionRuntime = new();

            foreach(var c in conditions)
            {
                conditionRuntime.Add(c.ToRuntimeData());
            }

            IReadOnlyList<ConditionRuntimeData> conditionReadOnly = conditionRuntime.ToArray();

            return new PersonRuntimeData(personName, trait, conditionReadOnly, baseSprite);
        }

        private void OnValidate()
        {
            if (conditions.Count > GameConfig.MAX_CONDITION_PER_PERSON) 
                conditions.RemoveRange(GameConfig.MAX_CONDITION_PER_PERSON, 
                                       conditions.Count - GameConfig.MAX_CONDITION_PER_PERSON);
        }
    }
}
