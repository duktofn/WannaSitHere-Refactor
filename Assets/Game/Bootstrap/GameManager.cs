using System.Collections.Generic;
using UnityEngine;
using Game.Core.Economy;
using Game.App.SaveAndLoad;
using Game.Data.Economy;
using Game.Data.Levels;
using Game.View.Board;
using Game.View.UI;
using Game.Events;
using Game.Data.People;

namespace Game.Bootstrap
{
    public class GameManager : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField] private List<LevelDataSO> _levelData;

        [Header("Economy")]
        [SerializeField] private EconomyConfigSO _economyConfig;

        [Header("Scene References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelView _levelView;

        [Header("Events - Game Flow")]
        [SerializeField] private VoidEventChannelSO _onPlayGameEvent;
        [SerializeField] private VoidEventChannelSO _onWinEvent;
        [SerializeField] private VoidEventChannelSO _onLoseEvent;
        [SerializeField] private VoidEventChannelSO _onNextLevelEvent;
        [SerializeField] private VoidEventChannelSO _onRestartLevelEvent;

        [Header("Events - Rewards")]
        [SerializeField] private VoidEventChannelSO _onClaimWinRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimAdsRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimDailyRewardEvent;

        [Header("Events - Shop")]
        [SerializeField] private VoidEventChannelSO _onBuyRemoveEvent;
        [SerializeField] private VoidEventChannelSO _onBuyUndoEvent;
        [SerializeField] private VoidEventChannelSO _onBuyMoreMovesEvent;

        [Header("Events - Economy Feedback")]
        [SerializeField] private OnItemReceiveSO _onItemReceive;
        [SerializeField] private OnItemSpendSO _onItemSpend;

        private LevelBootstrapper _levelBootstrapper;
        private SaveLoadManager _saveLoad;
        private Inventory _inventory;
        private GameData _gameData;
        private readonly EventListener _listener = new();

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

            if (_uiManager != null)
                _uiManager.Initialize(_inventory);

            _levelBootstrapper = new LevelBootstrapper(_levelData, _gridManager, _levelView);
        }

        private void OnEnable()
        {
            // Game Flow
            _listener.Listen(_onPlayGameEvent, HandlePlayGame);
            _listener.Listen(_onWinEvent, HandleWin);
            _listener.Listen(_onLoseEvent, HandleLose);
            _listener.Listen(_onNextLevelEvent, NextLevel);
            _listener.Listen(_onRestartLevelEvent, RestartLevel);

            // Rewards
            _listener.Listen(_onClaimWinRewardEvent, HandleClaimWinReward);
            _listener.Listen(_onClaimAdsRewardEvent, HandleClaimAdsReward);
            _listener.Listen(_onClaimDailyRewardEvent, HandleClaimDailyReward);

            // Shop
            _listener.Listen(_onBuyRemoveEvent, HandleBuyRemove);
            _listener.Listen(_onBuyUndoEvent, HandleBuyUndo);
            _listener.Listen(_onBuyMoreMovesEvent, HandleBuyMoreMoves);
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
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

        // ── Rewards ────────────────────────────────────────

        private void HandleClaimWinReward()
        {
            if (_economyConfig == null) return;

            Reward reward = _economyConfig.levelWinReward;
            _inventory.UpdateInventory(reward);
            _onItemReceive?.Raise(reward);
            SaveGame();

            Debug.Log($"[GameManager] Claimed win reward: {reward.amount} {reward.type}");
        }

        private void HandleClaimAdsReward()
        {
            if (_economyConfig == null) return;

            Reward reward = _economyConfig.levelAdsWinReward;
            _inventory.UpdateInventory(reward);
            _onItemReceive?.Raise(reward);
            SaveGame();

            Debug.Log($"[GameManager] Claimed ads reward: {reward.amount} {reward.type}");
        }

        private void HandleClaimDailyReward()
        {
            if (_economyConfig == null) return;

            Reward reward = _economyConfig.dailyReward;
            _inventory.UpdateInventory(reward);
            _onItemReceive?.Raise(reward);
            SaveGame();

            Debug.Log($"[GameManager] Claimed daily reward: {reward.amount} {reward.type}");
        }

        // ── Shop ───────────────────────────────────────────

        private bool TryPurchase(Reward cost, Reward item)
        {
            if (!_inventory.TrySpendItem(cost))
            {
                _onItemSpend?.Raise(cost);
                Debug.LogWarning($"[GameManager] Not enough {cost.type}! Need {cost.amount}, have {_inventory.GetAmount(cost.type)}");
                return false;
            }

            _inventory.UpdateInventory(item);
            _onItemReceive?.Raise(item);
            SaveGame();

            Debug.Log($"[GameManager] Purchased {item.amount} {item.type} for {cost.amount} {cost.type}");
            return true;
        }

        private void HandleBuyRemove()
        {
            TryPurchase(
                cost: new Reward { type = ItemType.Gem, amount = 50 },
                item: new Reward { type = ItemType.Remove, amount = 1 }
            );
        }

        private void HandleBuyUndo()
        {
            TryPurchase(
                cost: new Reward { type = ItemType.Gem, amount = 50 },
                item: new Reward { type = ItemType.Undo, amount = 1 }
            );
        }

        private void HandleBuyMoreMoves()
        {
            TryPurchase(
                cost: new Reward { type = ItemType.Gold, amount = 100 },
                item: new Reward { type = ItemType.MoreMoves, amount = GameConfig.MORE_MOVE_AMOUNT }
            );
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
