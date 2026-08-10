using UnityEngine;
using Game.Shared;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Level Data", menuName = "Game/New Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public int levelMove;

        public Vector2Int mainGridSize;
        public Vector2Int waitGridSize;
        public Vector2Int mainGridPos;
        public Vector2Int waitGridPos;

        public CellGrid mainGrid;
        public CellGrid waitGrid;
    }
}