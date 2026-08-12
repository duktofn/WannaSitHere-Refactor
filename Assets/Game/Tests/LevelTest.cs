using Game.Data;
using Game.Domain.Grid;
using Game.Domain.SaveAndLoad;
using UnityEngine;

namespace Game.Tests
{
    public class LevelTest : MonoBehaviour
    {
        [SerializeField] private LevelDataSO levelData;
        [SerializeField] private GridManager gridManager;

        private void Start()
        {
            if (levelData == null || gridManager == null)
            {
                Debug.LogWarning("[LevelTest] Missing levelData or gridManager reference.");
                return;
            }

            LevelRuntimeData runtime = levelData.ToRuntimeData();

            gridManager.Initialize(runtime);
            gridManager.CreateMainGrid();
            gridManager.CreateWaitGrid();

            Debug.Log($"[LevelTest] Successfully initialized and created grid. Level moves: {runtime.LevelMove}");
        }
    }
}
