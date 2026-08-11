using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Model
{
    public class PersonRuntimeData
    {
        public readonly string PersonName;
        public readonly PersonTrait Trait;
        public readonly IReadOnlyList<ConditionRuntimeData> Conditions;
        
        public PersonRuntimeData(string personName, 
                                PersonTrait trait, 
                                IReadOnlyList<ConditionRuntimeData> conditions) 
        {
            PersonName = personName;
            Trait = trait;
            Conditions = conditions;
        }
    }
}