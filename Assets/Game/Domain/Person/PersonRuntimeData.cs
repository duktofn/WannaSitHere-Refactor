using System.Collections.Generic;
using Game.Shared;
using Game.Domain.Condition;
using UnityEngine;

namespace Game.Domain.Person
{
    public class PersonRuntimeData
    {
        public readonly string PersonName;
        public readonly PersonTrait Trait;
        public readonly IReadOnlyList<ConditionRuntimeData> Conditions;
        public readonly Sprite BaseSprite;
        
        public PersonRuntimeData(string personName, 
                                PersonTrait trait, 
                                IReadOnlyList<ConditionRuntimeData> conditions,
                                Sprite baseSprite) 
        {
            PersonName = personName;
            Trait = trait;
            Conditions = conditions;
            BaseSprite = baseSprite;
        }
    }
}