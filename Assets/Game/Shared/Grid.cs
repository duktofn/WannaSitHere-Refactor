using System;
using UnityEngine;

namespace Game.Shared
{
    [Serializable]
    public class Grid<T>
    {
        public Vector2Int GridSize { get; }
        public Vector2 CellSize { get; }
        public Vector2 CellDistance { get; }

        [Range(0, 1)] public float PosX { get; private set; }
        [Range(0, 1)] public float PosY { get; private set; }

        public T[] GridContent { get; }

        public Grid(
            Vector2Int gridSize,
            Vector2 cellSize,
            Vector2 cellDistance,
            float posX,
            float posY)
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
                throw new ArgumentOutOfRangeException(nameof(gridSize));

            GridSize = gridSize;
            CellSize = cellSize;
            CellDistance = cellDistance;

            PosX = Mathf.Clamp01(posX);
            PosY = Mathf.Clamp01(posY);

            GridContent = new T[gridSize.x * gridSize.y];
        }

        public Grid(Grid<T> other)
        {
            GridSize = other.GridSize;
            CellSize = other.CellSize;
            CellDistance = other.CellDistance;
            PosX = other.PosX;
            PosY = other.PosY;
            GridContent = other.GridContent;
        }

        public Grid(Vector2Int size)
        {
            GridSize = size;
            GridContent = new T[size.x * size.y];
        }

        public T Get(int x, int y)
        {
            if (x >= GridSize.x || y >= GridSize.y || x < 0 || y < 0)
                return default;

            return GridContent[x + y * GridSize.x];
        }

        public void Set(int x, int y, T value)
        {
            if (x >= GridSize.x || y >= GridSize.y || x < 0 || y < 0)
                return;

            GridContent[x + y * GridSize.x] = value;
        }
    }
}