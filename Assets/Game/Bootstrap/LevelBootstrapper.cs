using UnityEngine;
using Game.Data.Levels;
using Game.Core.Levels;
using Game.View.Board;

namespace Game.Bootstrap
{
    public class LevelBootstrapper : MonoBehaviour
    {
        [SerializeField] private LevelDataSO levelData;
        [SerializeField] private GridManager gridManager;

        private void Start()
        {
            if (levelData == null || gridManager == null)
            {
                Debug.LogWarning("[LevelBootstrapper] Missing levelData or gridManager reference.");
                return;
            }

            LevelRuntimeData runtime = levelData.ToRuntimeData();

            gridManager.Initialize(runtime);
            gridManager.CreateMainGrid();
            gridManager.CreateWaitGrid();

            Debug.Log("[LevelBootstrapper] Successfully initialized and created grid");
        }
    }
}
