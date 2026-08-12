using Game.Domain.SaveAndLoad;
using Game.Shared;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Domain.Grid
{
    public class GridManager : MonoBehaviour
    {
        private Grid<CellRuntimeData> _main;
        private Grid<CellRuntimeData> _wait;
        private LevelRuntimeData _currentLevel;
        [SerializeField] private GameObject cellPrefabs;
        [SerializeField] private List<Vector2> adjacent;
        
        public void CreateMainGrid()
        {
            _main = new Grid<CellRuntimeData>(_currentLevel.MainGrid);

            foreach (CellRuntimeData c in _main.GridContent)
            {
                Vector2 step = _main.CellDistance + _main.CellSize;
                Vector2 cellOffset = new Vector2((c.Index.x - (_main.GridSize.x - 1) / 2f) * step.x,
                                                 (c.Index.y - (_main.GridSize.y - 1) / 2f) * step.y);

                GameObject tmpCell = Instantiate(cellPrefabs);
                tmpCell.transform.position = new Vector3(cellOffset.x, cellOffset.y);
            }
        }

        public void CreateWaitGrid()
        {
            _wait = new Grid<CellRuntimeData>(_currentLevel.WaitGrid);
            
            foreach(CellRuntimeData c in _wait.GridContent) {
                Vector2 step = _wait.CellDistance + _wait.CellSize;
                Vector2 cellOffset = new Vector2((c.Index.x - (_wait.GridSize.x - 1) / 2f) * step.x,
                                                 (c.Index.y - (_wait.GridSize.y - 1) / 2f) * step.y);

                GameObject tmpCell = Instantiate(cellPrefabs);
                tmpCell.transform.position = new Vector3(cellOffset.x, cellOffset.y);
            }
        }
        
        public List<CellRuntimeData> GetAdjacentCells(Vector2 index, Grid<CellRuntimeData> grid)
        {
            List<CellRuntimeData> res = new();

            foreach(Vector2 v in adjacent) {
                foreach(CellRuntimeData c in grid.GridContent)
                {
                    if (c.Index == index + v) res.Add(c);
                }
            }

            return res;
        }
    }
}