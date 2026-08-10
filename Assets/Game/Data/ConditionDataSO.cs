using Game.Shared;
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
    }
}