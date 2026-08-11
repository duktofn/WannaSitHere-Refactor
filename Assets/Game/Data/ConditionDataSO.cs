using Game.Model;
using Game.Shared;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Condition", menuName = "Game/New Condition")]
    public class ConditionDataSO : ScriptableObject
    {
        public ConditionType type;
        public ConditionTarget target;
        public PersonTrait targetTrait; // for Person ConditionType
        public Food foodTarget;         // for Food ConditionType
        public string description;
        public string angryDescription;

        public ConditionRuntimeData ToRuntimeData()
        {
            return new ConditionRuntimeData(type, target, targetTrait, foodTarget, description, angryDescription);
        }
    }
}