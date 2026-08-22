using System;
using UnityEngine;

namespace Game.Core.Board
{
    [Serializable]
    public class Grid<T>
    {
        [SerializeField] private Vector2Int _gridSize;
        [SerializeField] private Vector2 _cellSize;
        [SerializeField] private Vector2 _cellDistance;
        [SerializeField, Range(0, 1)] private float _posX;
        [SerializeField, Range(0, 1)] private float _posY;
        [SerializeField] private T[] _gridContent;

        public Vector2Int GridSize => _gridSize;
        public Vector2 CellSize => _cellSize;
        public Vector2 CellDistance => _cellDistance;
        public float PosX => _posX;
        public float PosY => _posY;
        public T[] GridContent => _gridContent;

        public Grid(
            Vector2Int gridSize,
            Vector2 cellSize,
            Vector2 cellDistance,
            float posX,
            float posY)
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
                throw new ArgumentOutOfRangeException(nameof(gridSize));

            _gridSize = gridSize;
            _cellSize = cellSize;
            _cellDistance = cellDistance;
            _posX = Mathf.Clamp01(posX);
            _posY = Mathf.Clamp01(posY);
            _gridContent = new T[gridSize.x * gridSize.y];
        }

        public Grid(Grid<T> other)
        {
            _gridSize = other._gridSize;
            _cellSize = other._cellSize;
            _cellDistance = other._cellDistance;
            _posX = other._posX;
            _posY = other._posY;
            _gridContent = other._gridContent;
        }

        public Grid(Vector2Int size)
        {
            _gridSize = size;
            _gridContent = new T[size.x * size.y];
        }

        public T Get(int x, int y)
        {
            if (x >= _gridSize.x || y >= _gridSize.y || x < 0 || y < 0)
                return default;

            return _gridContent[x + y * _gridSize.x];
        }

        public void Set(int x, int y, T value)
        {
            if (x >= _gridSize.x || y >= _gridSize.y || x < 0 || y < 0)
                return;

            _gridContent[x + y * _gridSize.x] = value;
        }
    }
}
