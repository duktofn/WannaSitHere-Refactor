using Game.Domain.RuntimeData;
using Game.Shared;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Cell", menuName = "Game/New Cell")]
    public class CellDataSO : ScriptableObject
    {
        public CellType type;
        public PersonDataSO defaultPerson;  // For Wait Seat
        public Food food;                   // For food type
        public Sprite sprite;

        public CellRuntimeData ToRuntimeData()
        {
            return new CellRuntimeData(type, defaultPerson, food, sprite);
        }
    }
}