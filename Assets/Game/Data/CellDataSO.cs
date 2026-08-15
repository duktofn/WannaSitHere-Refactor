using Game.Domain.Grid;
using Game.Domain.Person;
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

        public CellRuntimeData ToRuntimeData(int x, int y, Vector2 size, GridId ownGrid)
        {
            PersonRuntimeData runtimePerson = defaultPerson != null ? defaultPerson.ToRuntimeData() : null;
            
            return new CellRuntimeData(new Vector2Int(x, y), type, size, runtimePerson, food, sprite, ownGrid);
        }
    }
}