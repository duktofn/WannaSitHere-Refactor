using UnityEngine;
using Game.Data.People;
using Game.Core.Board;
using Game.Core.People;

namespace Game.Data.Board
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
