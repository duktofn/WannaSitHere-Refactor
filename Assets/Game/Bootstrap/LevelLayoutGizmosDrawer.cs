using System.Collections.Generic;
using Game.Core.Board;
using Game.Data.Board;
using Game.Data.Levels;
using UnityEngine;

namespace Game.Bootstrap
{
    [ExecuteAlways]
    [AddComponentMenu("Game/Debug/Level Layout Gizmos Drawer")]
    public class LevelLayoutGizmosDrawer : MonoBehaviour
    {
        [SerializeField] private List<LevelDataSO> levels = new();
        [Min(0)] [SerializeField] private int levelIndex;

        private void OnDrawGizmos()
        {
            LevelDataSO level = GetSelectedLevel();
            if (level == null)
            {
                return;
            }

            DrawGrid(level.mainGrid, Color.cyan);
            DrawGrid(level.waitGrid, Color.yellow);
        }

        private void OnValidate()
        {
            if (levels == null || levels.Count == 0)
            {
                levelIndex = 0;
            }
            else
            {
                levelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
            }
        }

        private LevelDataSO GetSelectedLevel()
        {
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Count)
            {
                return null;
            }

            return levels[levelIndex];
        }

        private void DrawGrid(Grid<CellDataSO> grid, Color color)
        {
            if (grid == null || grid.GridSize.x <= 0 || grid.GridSize.y <= 0)
            {
                return;
            }

            Vector3 gridWorldPosition = GetGridWorldPosition(grid);
            Vector2 cellSize = new(Mathf.Abs(grid.CellSize.x), Mathf.Abs(grid.CellSize.y));
            Vector2 step = grid.CellDistance + grid.CellSize;

            Gizmos.color = color;
            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector3 cellPosition = gridWorldPosition + new Vector3(
                        (x - (grid.GridSize.x - 1) / 2f) * step.x,
                        (y - (grid.GridSize.y - 1) / 2f) * step.y,
                        0f);

                    Gizmos.DrawWireCube(
                        cellPosition,
                        new Vector3(Mathf.Max(cellSize.x, 0.01f), Mathf.Max(cellSize.y, 0.01f), 0.01f));
                }
            }
        }

        private Vector3 GetGridWorldPosition(Grid<CellDataSO> grid)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                return transform.position;
            }

            float distanceToCamera = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 worldPosition = mainCamera.ViewportToWorldPoint(
                new Vector3(grid.PosX, grid.PosY, distanceToCamera));
            worldPosition.z = 0f;
            return worldPosition;
        }
    }
}
