using UnityEngine;
using Game.Data.Levels;
using Game.Core.Levels;
using Game.View.Board;
using Game.View.UI;
using System.Collections.Generic;
using System;
using Game.App;

namespace Game.Bootstrap
{
    public class LevelBootstrapper : ILevelLoader
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

        public int TotalLevels => _levelData?.Count ?? 0;

        public bool TryPrepare(int levelNumber, out IPreparedLevel preparedLevel, out string error)
        {
            preparedLevel = null;
            error = null;

            if (_levelData == null)
            {
                error = "Missing Level Data reference.";
            } 
            else if (_gridManager == null)
            {
                error = "Missing Grid Manager reference.";
            }
            else if (_levelData.Count == 0)
            {
                error = "Level Data list is empty.";
            }

            if (error != null)
                return false;

            int index = (levelNumber > 0 ? levelNumber - 1 : 0) % _levelData.Count;
            LevelDataSO targetLevelSO = _levelData[index];

            if (targetLevelSO == null)
            {
                error = $"LevelData at index {index} is null.";
                return false;
            }

            if (!targetLevelSO.TryToRuntimeData(out LevelRuntimeData runtime, out List<string> validationErrors))
            {
                error = $"Level '{targetLevelSO.name}' is invalid: {string.Join(" ", validationErrors)}";
                return false;
            }

            preparedLevel = new PreparedUnityLevel(index, targetLevelSO, runtime);
            return true;
        }

        public void Activate(IPreparedLevel preparedLevel, LevelManager levelManager)
        {
            PreparedUnityLevel prepared = preparedLevel as PreparedUnityLevel;
            if (prepared == null)
                throw new ArgumentException("Prepared level was not created by this level loader.", nameof(preparedLevel));

            _gridManager.ClearGrids();
            SpawnLevelEnvironment(prepared.LevelAsset);
            _gridManager.Initialize(prepared.RuntimeData, levelManager);
            _gridManager.CreateMainGrid();
            _gridManager.CreateWaitGrid();

            if (_levelView != null)
                _levelView.BindData(prepared.RuntimeData);

            Debug.Log($"[LevelBootstrapper] Level index {prepared.CatalogIndex} activated successfully");
        }

        private sealed class PreparedUnityLevel : IPreparedLevel
        {
            public int CatalogIndex { get; }
            public LevelRuntimeData RuntimeData { get; }
            public LevelDataSO LevelAsset { get; }

            public PreparedUnityLevel(int catalogIndex, LevelDataSO levelAsset, LevelRuntimeData runtimeData)
            {
                CatalogIndex = catalogIndex;
                LevelAsset = levelAsset;
                RuntimeData = runtimeData;
            }
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

                GameObject environmentInstance = UnityEngine.Object.Instantiate(
                    environmentPrefab,
                    worldRoot,
                    false);
                environmentInstance.transform.localPosition = Vector3.zero;
            }
        }
    }
}
