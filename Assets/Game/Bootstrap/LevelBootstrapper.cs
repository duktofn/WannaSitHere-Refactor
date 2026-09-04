using UnityEngine;
using Game.Data.Levels;
using Game.Core.Levels;
using Game.View.Board;
using Game.View.UI;
using System.Collections.Generic;

namespace Game.Bootstrap
{
    public class LevelBootstrapper : MonoBehaviour
    {
        [SerializeField] private List<LevelDataSO> levelData;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private LevelView levelView;
        [SerializeField] private int currentLevel;

        private void Start()
        {
            if (levelData == null || gridManager == null)
            {
                Debug.LogWarning("[LevelBootstrapper] Missing levelData or gridManager reference.");
                return;
            }

            LevelRuntimeData runtime = levelData[currentLevel - 1].ToRuntimeData();

            gridManager.Initialize(runtime);
            gridManager.CreateMainGrid();
            gridManager.CreateWaitGrid();

            if (levelView != null)
                levelView.BindData(runtime);

            Debug.Log("[LevelBootstrapper] Successfully initialized and created grid");
        }
    }
}
