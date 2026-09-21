using UnityEngine;
using Game.Data.Levels;
using Game.Core.Levels;
using Game.View.Board;
using Game.View.UI;
using System.Collections.Generic;

namespace Game.Bootstrap
{
    public class LevelBootstrapper
    {
        private readonly List<LevelDataSO> _levelData;
        private readonly GridManager _gridManager;
        private readonly LevelView _levelView;

        public LevelBootstrapper(List<LevelDataSO> levelData, GridManager gridManager, LevelView levelView)
        {
            _levelData = levelData;
            _gridManager = gridManager;
            _levelView = levelView;
        }

        public void LoadLevel(int levelNumber)
        {
            if (_levelData == null)
            {
                Debug.LogWarning("[LevelBootstrapper] Missing Level Data reference.");
                return;
            } 
            else if (_gridManager == null)
            {
                Debug.LogWarning("[LevelBootstrapper] Missing Grid Manager reference.");
                return;
            }

            if (_levelData.Count == 0)
            {
                Debug.LogWarning("[LevelBootstrapper] Level Data list is empty.");
                return;
            }

            int index = (levelNumber > 0 ? levelNumber - 1 : 0) % _levelData.Count;
            LevelDataSO targetLevelSO = _levelData[index];

            if (targetLevelSO == null)
            {
                Debug.LogError($"[LevelBootstrapper] LevelData at index {index} is null.");
                return;
            }

            if (!targetLevelSO.TryToRuntimeData(out LevelRuntimeData runtime, out _))
            {
                Debug.LogError(
                    $"[LevelBootstrapper] Level '{targetLevelSO.name}' is invalid; " +
                    "the current grids were left untouched.",
                    targetLevelSO);
                return;
            }

            _gridManager.ClearGrids();
            SpawnLevelEnvironment(targetLevelSO);
            _gridManager.Initialize(runtime);
            _gridManager.CreateMainGrid();
            _gridManager.CreateWaitGrid();

            if (_levelView != null)
                _levelView.BindData(runtime);

            Debug.Log($"[LevelBootstrapper] Level {levelNumber} (index {index}) loaded successfully");
        }

        private void SpawnLevelEnvironment(LevelDataSO level)
        {
            if (level.levelEnvironmentPrefabs == null || level.levelEnvironmentPrefabs.Count == 0)
                return;

            Transform worldRoot = _gridManager.WorldRoot;
            if (worldRoot == null)
            {
                Debug.LogWarning(
                    $"[LevelBootstrapper] Cannot spawn environment for level '{level.name}': WorldRoot is missing.",
                    level);
                return;
            }

            foreach (GameObject environmentPrefab in level.levelEnvironmentPrefabs)
            {
                if (environmentPrefab == null)
                    continue;

                GameObject environmentInstance = Object.Instantiate(
                    environmentPrefab,
                    worldRoot,
                    false);
                environmentInstance.transform.localPosition = Vector3.zero;
            }
        }
    }
}
