using Game.Shared;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Cell", menuName = "Game/New Cell")]
    public class CellDataSO : ScriptableObject
    {
        public CellType type;
        public PersonDataSO person;  // For Wait Seat
        public Food food;            // For food type
        public Sprite sprite;
    }
}