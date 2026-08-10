using Game.Shared;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Cell", menuName = "Game/New Cell")]
    public class CellDataSO : ScriptableObject
    {
        public CellType type;
        public Food food;
        public Sprite sprite;
    }
}