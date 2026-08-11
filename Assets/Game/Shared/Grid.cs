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

        [SerializeField,Range(0,1)] private float PosX;
        [SerializeField,Range(0,1)] private float PosY;

        public T[,] GridContent { get; }

        public T Get(int x, int y)
        {
            if (x > GridSize.x || y > GridSize.y || x < 0 || y < 0) return default;
            return GridContent[x,y];
        }

        public void Set(int x, int y, T value)
        {
            if (x > GridSize.x || y > GridSize.y || x < 0 || y < 0) return;
            GridContent[x,y] = value;
        }
    }
}