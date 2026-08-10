using Game.Shared;
using UnityEngine;

namespace Game.Data {
    [CreateAssetMenu(fileName = "New Person", menuName = "Game/New Person")]
    public class PersonDataSO : ScriptableObject
    {
        public string personName;
        public PersonTrait trait;
    }
}
