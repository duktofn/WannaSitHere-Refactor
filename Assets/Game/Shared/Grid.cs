using System;
using UnityEngine;
using Game.Data;

namespace Game.Shared
{
    [Serializable]
    public struct CellGrid
    {
        public Vector2Int GridSize { get; }
        public Vector2 CellSize { get; }
        public Vector2 CellDistance { get; }

        [SerializeField,Range(0,1)] private float PosX;
        [SerializeField,Range(0,1)] private float PosY;

        public CellDataSO[,] GridContent { get; }
    }
}