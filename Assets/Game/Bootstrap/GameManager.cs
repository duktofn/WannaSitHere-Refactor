using System.Collections.Generic;
using UnityEngine;
using Game.Core.Economy;
using Game.App.SaveAndLoad;
using Game.Data.Economy;
using Game.Data.Levels;
using Game.View.Board;
using Game.View.UI;
using Game.Events;

namespace Game.Bootstrap
{
    public class GameManager : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField] private List<LevelDataSO> _levelData;

        [Header("Economy")]
        [SerializeField] private EconomyConfigSO _economyConfig;

        [Header("Scene References")]
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelView _levelView;

        [Header("Events - Game Flow")]
        [SerializeField] private VoidEventChannelSO _onPlayGameEvent;
        [SerializeField] private VoidEventChannelSO _onWinEvent;
        [SerializeField] private VoidEventChannelSO _onLoseEvent;
        [SerializeField] private VoidEventChannelSO _onNextLevelEvent;
        [SerializeField] private VoidEventChannelSO _onRestartLevelEvent;

        [Header("Events - Economy")]
        [SerializeField] private OnItemChangedSO _onItemReceive;
        [SerializeField] private OnItemChangedSO _onItemSpend;

        private LevelBootstrapper _levelBootstrapper;
        private SaveLoadManager _saveLoad;
        private Inventory _inventory;
        private GameData _gameData;

        public Inventory Inventory => _inventory;

        private void Awake()
        {
            _saveLoad = new SaveLoadManager();
            _gameData = _saveLoad.GetGameData();

            _inventory = new Inventory(
                _gameData.currentGold,
                _gameData.currentGem,
                _gameData.currentRemove,
                _gameData.currentMoreMoves,
                _gameData.currentUndo
            );

            _levelBootstrapper = new LevelBootstrapper(_levelData, _gridManager, _levelView);
        }

        private void OnEnable()
        {
            if (_onPlayGameEvent != null) _onPlayGameEvent.OnRaised += HandlePlayGame;
            if (_onWinEvent != null) _onWinEvent.OnRaised += HandleWin;
            if (_onLoseEvent != null) _onLoseEvent.OnRaised += HandleLose;
            if (_onNextLevelEvent != null) _onNextLevelEvent.OnRaised += NextLevel;
            if (_onRestartLevelEvent != null) _onRestartLevelEvent.OnRaised += RestartLevel;
            if (_onItemReceive != null) _onItemReceive.OnRaised += HandleItemReceive;
            if (_onItemSpend != null) _onItemSpend.OnRaised += HandleItemSpend;
        }

        private void OnDisable()
        {
            if (_onPlayGameEvent != null) _onPlayGameEvent.OnRaised -= HandlePlayGame;
            if (_onWinEvent != null) _onWinEvent.OnRaised -= HandleWin;
            if (_onLoseEvent != null) _onLoseEvent.OnRaised -= HandleLose;
            if (_onNextLevelEvent != null) _onNextLevelEvent.OnRaised -= NextLevel;
            if (_onRestartLevelEvent != null) _onRestartLevelEvent.OnRaised -= RestartLevel;
            if (_onItemReceive != null) _onItemReceive.OnRaised -= HandleItemReceive;
            if (_onItemSpend != null) _onItemSpend.OnRaised -= HandleItemSpend;
        }

        // ── Game Flow ──────────────────────────────────────

        public void HandlePlayGame()
        {
            int levelToLoad = _gameData.currentLevel > 0 ? _gameData.currentLevel : 1;
            _levelBootstrapper.LoadLevel(levelToLoad);
            Debug.Log($"[GameManager] Started Level: {levelToLoad}");
        }

        private void HandleWin()
        {
            if (_economyConfig != null)
                _inventory.UpdateInventory(_economyConfig.levelWinReward);

            _gameData.currentLevel++;
            SaveGame();

            Debug.Log($"[GameManager] Win! Next level: {_gameData.currentLevel}");
        }

        private void HandleLose()
        {
            Debug.Log("[GameManager] Lose! Player can retry.");
        }

        public void NextLevel()
        {
            _levelBootstrapper.LoadLevel(_gameData.currentLevel);
        }

        public void RestartLevel()
        {
            int levelToLoad = _gameData.currentLevel > 0 ? _gameData.currentLevel : 1;
            _levelBootstrapper.LoadLevel(levelToLoad);
        }

        // ── Economy ────────────────────────────────────────

        private void HandleItemReceive(Reward reward)
        {
            _inventory.UpdateInventory(reward);
            SaveGame();
            Debug.Log($"[GameManager] Received: {reward.amount} {reward.type}. Current: {_inventory.GetAmount(reward.type)}");
        }

        private void HandleItemSpend(Reward reward)
        {
            if (_inventory.TrySpendItem(reward))
            {
                SaveGame();
                Debug.Log($"[GameManager] Spent: {reward.amount} {reward.type}. Remaining: {_inventory.GetAmount(reward.type)}");
            }
            else
            {
                Debug.LogWarning($"[GameManager] Not enough {reward.type}! Need {reward.amount}, have {_inventory.GetAmount(reward.type)}");
            }
        }

        // ── Save/Load ──────────────────────────────────────

        public void SaveGame()
        {
            if (_inventory == null || _saveLoad == null) return;

            _gameData.currentGold = _inventory.Gold;
            _gameData.currentGem = _inventory.Gem;
            _gameData.currentRemove = _inventory.Remove;
            _gameData.currentMoreMoves = _inventory.MoreMoves;
            _gameData.currentUndo = _inventory.Undo;

            _saveLoad.SaveGameData(_gameData);
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}