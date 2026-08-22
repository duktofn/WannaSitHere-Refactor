using System.Collections.Generic;
using UnityEngine;
using System;
using Game.Core.Conditions;

namespace Game.Core.People
{
    public class PersonRuntimeData
    {
        public readonly string PersonName;
        public readonly PersonTrait Trait;
        public readonly IReadOnlyList<ConditionRuntimeData> Conditions;
        public readonly Sprite BaseSprite;
        public PersonState State { get; private set; }

        public event Action<PersonState> OnPersonStateChanged;
        
        public PersonRuntimeData(string personName, 
                                PersonTrait trait, 
                                IReadOnlyList<ConditionRuntimeData> conditions,
                                Sprite baseSprite) 
        {
            PersonName = personName;
            Trait = trait;
            Conditions = conditions;
            BaseSprite = baseSprite;
            SetState(PersonState.Normal);
        }

        public void SetState(PersonState state)
        {
            if (State == state) return;
            State = state;
            OnPersonStateChanged?.Invoke(State);
        }
    }
}
