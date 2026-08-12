using Game.Domain.Grid;
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

        public CellRuntimeData ToRuntimeData(int x, int y)
        {
            return new CellRuntimeData(new Vector2(x, y), type, defaultPerson, food, sprite);
        }
    }
}