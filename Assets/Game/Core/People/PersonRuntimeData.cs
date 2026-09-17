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
        private readonly List<ConditionRuntimeData> _conditions;
        public IReadOnlyList<ConditionRuntimeData> Conditions => _conditions;
        public readonly Sprite BaseSprite;
        public PersonState State { get; private set; }

        public event Action<PersonState> OnPersonStateChanged;
        public event Action OnConditionsCleared;
        
        public PersonRuntimeData(string personName, 
                                PersonTrait trait, 
                                IReadOnlyList<ConditionRuntimeData> conditions,
                                Sprite baseSprite) 
        {
            PersonName = personName;
            Trait = trait;
            _conditions = new List<ConditionRuntimeData>(conditions);
            BaseSprite = baseSprite;
            SetState(PersonState.Normal);
        }

        /// <summary>
        /// Removes all conditions from this person and notifies presentation listeners.
        /// </summary>
        public void ClearConditions()
        {
            _conditions.Clear();
            OnConditionsCleared?.Invoke();
        }

        /// <summary>
        /// Replaces the current condition list with one condition and notifies presentation listeners.
        /// </summary>
        public void ReplaceConditions(ConditionRuntimeData condition)
        {
            _conditions.Clear();
            if (condition != null)
                _conditions.Add(condition);

            OnConditionsCleared?.Invoke();
        }

        public void SetState(PersonState state)
        {
            if (State == state) return;
            State = state;
            OnPersonStateChanged?.Invoke(State);
        }
    }
}
