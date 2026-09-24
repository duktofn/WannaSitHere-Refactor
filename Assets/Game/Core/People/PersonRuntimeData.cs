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
        private readonly List<bool> _conditionSatisfied;
        public IReadOnlyList<ConditionRuntimeData> Conditions => _conditions;
        public IReadOnlyList<bool> ConditionSatisfied => _conditionSatisfied;
        public readonly Sprite BaseSprite;
        public PersonState State { get; private set; }

        public event Action<PersonState> OnPersonStateChanged;
        public event Action OnConditionsCleared;
        public event Action OnConditionStatusChanged;
        
        public PersonRuntimeData(string personName, 
                                PersonTrait trait, 
                                IReadOnlyList<ConditionRuntimeData> conditions,
                                Sprite baseSprite) 
        {
            PersonName = personName;
            Trait = trait;
            _conditions = conditions == null
                ? new List<ConditionRuntimeData>()
                : new List<ConditionRuntimeData>(conditions);
            BaseSprite = baseSprite;
            _conditionSatisfied = new List<bool>(new bool[_conditions.Count]);
            SetState(PersonState.Normal);
        }

        /// <summary>
        /// Removes all conditions from this person and notifies presentation listeners.
        /// </summary>
        public void ClearConditions()
        {
            _conditions.Clear();
            _conditionSatisfied.Clear();
            OnConditionsCleared?.Invoke();
        }

        /// <summary>
        /// Replaces the current condition list with one condition and notifies presentation listeners.
        /// </summary>
        public void ReplaceConditions(ConditionRuntimeData condition)
        {
            _conditions.Clear();
            _conditionSatisfied.Clear();
            if (condition != null)
            {
                _conditions.Add(condition);
                _conditionSatisfied.Add(false);
            }

            OnConditionsCleared?.Invoke();
        }

        public void SetConditionSatisfied(int index, bool satisfied)
        {
            if (_conditionSatisfied[index] == satisfied) return;
            _conditionSatisfied[index] = satisfied;
            OnConditionStatusChanged?.Invoke();
        }

        public void SetState(PersonState state)
        {
            if (State == state) return;
            State = state;
            OnPersonStateChanged?.Invoke(State);
        }
    }
}
