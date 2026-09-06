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

            int index = (levelNumber > 0 ? levelNumber - 1 : 0) % _levelData.Count;
            LevelDataSO targetLevelSO = _levelData[index];

            if (targetLevelSO == null)
            {
                Debug.LogError($"[LevelBootstrapper] LevelData at index {index} is null.");
                return;
            }

            LevelRuntimeData runtime = targetLevelSO.ToRuntimeData();

            _gridManager.ClearGrids();
            _gridManager.Initialize(runtime);
            _gridManager.CreateMainGrid();
            _gridManager.CreateWaitGrid();

            if (_levelView != null)
                _levelView.BindData(runtime);

            Debug.Log($"[LevelBootstrapper] Level {levelNumber} (index {index}) loaded successfully");
        }
    }
}
