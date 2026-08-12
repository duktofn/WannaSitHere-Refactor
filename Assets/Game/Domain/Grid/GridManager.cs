using Game.Domain.SaveAndLoad;
using Game.Shared;
using UnityEngine;

namespace Game.Domain.Grid
{
    public class GridManager
    {
        private Grid<CellRuntimeData> _main;
        private Grid<CellRuntimeData> _wait;
        private LevelRuntimeData currentLevel;

        public void CreateMainGrid()
        {
            Vector2 mainGridPos = GetGridWorldPosition(_main);
            _main = new Grid<CellRuntimeData>(currentLevel.MainGrid);
        }

        public void CreateWaitGrid()
        {
            Vector2 waitGridPos = GetGridWorldPosition(_wait);
            _wait = new Grid<CellRuntimeData>(currentLevel.WaitGrid);
        }

        public Vector2 GetGridWorldPosition(Grid<CellRuntimeData> grid)
        {
            Camera mainCam = Camera.main;

            if (mainCam == null)
            {
                Debug.LogWarning("Cannot find camera, Base Position return to Vector2.zero");
                return Vector2.zero;
            }

            Vector2 originalPosition = mainCam.ViewportToWorldPoint(new Vector3(grid.PosX, 
                                                                                grid.PosY, 
                                                                                mainCam.nearClipPlane + 1f));

            Vector2 offSet = new Vector2(
                -0.5f * (grid.CellSize.x + grid.CellDistance.x) * (grid.GridSize.x - 1),
                -0.5f * (grid.CellSize.y + grid.CellDistance.y) * (grid.GridSize.y - 1)
            );

            return originalPosition + offSet;
        }
    }
}