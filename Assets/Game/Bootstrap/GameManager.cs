using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core.Booster;
using Game.Core.Economy;
using Game.App;
using Game.App.SaveAndLoad;
using Game.Data.Economy;
using Game.Data.Conditions;
using Game.Data.Levels;
using Game.View.Board;
using Game.View.People;
using Game.View.UI;
using Game.View.VFX;
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

        [Header("Conditions")]
        [SerializeField] private ConditionDataSO _canSitAnywhereCondition;

        [Header("Scene References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private WeeklyLogin _weeklyLogin;

        [Header("Visual Effects")]
        [SerializeField] private VfxPlayer _vfxPlayer;

        [Header("Events - Game Flow")]
        [SerializeField] private VoidEventChannelSO _onPlayGameEvent;
        [SerializeField] private VoidEventChannelSO _onWinEvent;
        [SerializeField] private VoidEventChannelSO _onLoseEvent;
        [SerializeField] private VoidEventChannelSO _onNextLevelEvent;
        [SerializeField] private VoidEventChannelSO _onRestartLevelEvent;
        [SerializeField] private IntEventChannelSO _onLevelChangedEvent;

        [Header("Events - Rewards")]
        [SerializeField] private VoidEventChannelSO _onClaimWinRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimAdsRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimDailyRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimWeeklyRewardEvent;

        [Header("Events - Shop")]
        [SerializeField] private VoidEventChannelSO _onBuyRemoveEvent;
        [SerializeField] private VoidEventChannelSO _onBuyUndoEvent;
        [SerializeField] private VoidEventChannelSO _onBuyMoreMovesEvent;

        [Header("Events - Booster Use (In-Game)")]
        [SerializeField] private VoidEventChannelSO _onUseMoreMovesEvent;
        [SerializeField] private VoidEventChannelSO _onUseUndoEvent;
        [SerializeField] private VoidEventChannelSO _onUseRemoveEvent;

        [Header("Events - Economy Feedback")]
        [SerializeField] private OnItemReceiveSO _onItemReceive;
        [SerializeField] private OnItemSpendSO _onItemSpend;

        private LevelBootstrapper _levelBootstrapper;
        private SaveLoadManager _saveLoad;
        private Inventory _inventory;
        private EconomyManager _economyManager;
        private GameData _gameData;
        private readonly EventListener _listener = new();

        public Inventory Inventory => _inventory;
        public EconomyManager EconomyManager => _economyManager;
        public int CurrentLevel => _gameData.currentLevel;
        public int TotalLevels => _levelData != null ? _levelData.Count : 0;

        public void SetLevel(int level)
        {
            _gameData.currentLevel = Math.Max(1, level);
            SaveGame();
            _onLevelChangedEvent?.Raise(_gameData.currentLevel);
        }

        public void TriggerWin()
        {
            _onWinEvent?.Raise();
        }

        public void TriggerLose()
        {
            _onLoseEvent?.Raise();
        }

        public void LoadLevel(int level)
        {
            _gameData.currentLevel = Math.Max(1, level);
            SaveGame();
            _levelBootstrapper?.LoadLevel(_gameData.currentLevel);
            _onLevelChangedEvent?.Raise(_gameData.currentLevel);
        }

        public void PlayLevel(int level)
        {
            _gameData.currentLevel = Math.Max(1, level);
            SaveGame();
            _onLevelChangedEvent?.Raise(_gameData.currentLevel);
            if (_onPlayGameEvent != null)
            {
                _onPlayGameEvent.Raise();
            }
            else
            {
                _levelBootstrapper?.LoadLevel(_gameData.currentLevel);
            }
        }

        public void RestartCurrentLevel()
        {
            if (_onRestartLevelEvent != null)
            {
                _onRestartLevelEvent.Raise();
            }
            else
            {
                RestartLevel();
            }
        }

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

            if (_gameData.goldShopPurchaseCountToday == null || _gameData.goldShopPurchaseCountToday.Length != 3)
            {
                _gameData.goldShopPurchaseCountToday = new int[3];
            }

            int[] goldShopLimits = _economyConfig != null && _economyConfig.goldShopLimit != null
                ? _economyConfig.goldShopLimit
                : new int[3] { 5, 5, 5 };

            _economyManager = new EconomyManager(
                _inventory,
                _gameData.currentLoginDay,
                _gameData.isDailyRewardClaimed,
                _gameData.isWeeklyRewardClaimed,
                goldShopLimits,
                _gameData.goldShopPurchaseCountToday
            );

            DateTime nowUtc = DateTime.UtcNow;
            DateTime lastLoginUtc = DateTime.MinValue;
            if (!string.IsNullOrEmpty(_gameData.lastLoginDateUtc) &&
                DateTime.TryParse(_gameData.lastLoginDateUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedDate))
            {
                lastLoginUtc = parsedDate;
            }

            bool isNewDay = _economyManager.EvaluateLoginState(lastLoginUtc, nowUtc);
            if (isNewDay || string.IsNullOrEmpty(_gameData.lastLoginDateUtc))
            {
                _gameData.lastLoginDateUtc = nowUtc.ToString("o");
                SaveGame();
            }

            if (_uiManager != null)
                _uiManager.Initialize(_inventory);

            UpdateWeeklyLoginUI();

            _levelBootstrapper = new LevelBootstrapper(_levelData, _gridManager, _levelView);
        }

        private void Start()
        {
            _onLevelChangedEvent?.Raise(CurrentLevel);
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
            _listener.Listen(_onClaimWeeklyRewardEvent, HandleClaimWeeklyReward);

            // Shop
            _listener.Listen(_onBuyRemoveEvent, HandleBuyRemove);
            _listener.Listen(_onBuyUndoEvent, HandleBuyUndo);
            _listener.Listen(_onBuyMoreMovesEvent, HandleBuyMoreMoves);

            // Booster Use (In-Game)
            _listener.Listen(_onUseMoreMovesEvent, HandleUseMoreMoves);
            _listener.Listen(_onUseUndoEvent, HandleUseUndo);
            _listener.Listen(_onUseRemoveEvent, HandleUseRemove);
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
        }

        public void UpdateWeeklyLoginUI()
        {
            if (_weeklyLogin != null && _economyConfig != null && _economyConfig.weeklyReward != null && _economyManager != null)
            {
                _weeklyLogin.SetRewardData(
                    _economyConfig.weeklyReward.ToList(),
                    _economyManager.CurrentLoginDay,
                    _economyManager.IsWeeklyRewardClaimed,
                    _economyConfig.dailyReward,
                    _economyManager.IsDailyRewardClaimed
                );
            }
        }

        // ── Game Flow ──────────────────────────────────────

        public void HandlePlayGame()
        {
            int levelToLoad = _gameData.currentLevel > 0 ? _gameData.currentLevel : 1;
            _levelBootstrapper.LoadLevel(levelToLoad);

            if (_levelView != null)
                _levelView.BindBoosters(_inventory);

            _onLevelChangedEvent?.Raise(levelToLoad);
            Debug.Log($"[GameManager] Started Level: {levelToLoad}");
        }

        private void HandleWin()
        {
            _gameData.currentLevel++;
            SaveGame();
            _onLevelChangedEvent?.Raise(_gameData.currentLevel);
            Debug.Log($"[GameManager] Win! Next level: {_gameData.currentLevel}");
        }

        private void HandleLose()
        {
            Debug.Log("[GameManager] Lose! Player can retry.");
        }

        public void NextLevel()
        {
            _levelBootstrapper.LoadLevel(_gameData.currentLevel);
            _onLevelChangedEvent?.Raise(_gameData.currentLevel);
        }

        public void RestartLevel()
        {
            int levelToLoad = _gameData.currentLevel > 0 ? _gameData.currentLevel : 1;
            _levelBootstrapper.LoadLevel(levelToLoad);
            _onLevelChangedEvent?.Raise(levelToLoad);
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
            if (_economyConfig == null || _economyManager == null) return;

            Reward reward = _economyConfig.dailyReward;
            if (_economyManager.ClaimDailyReward(reward))
            {
                _onItemReceive?.Raise(reward);
                UpdateWeeklyLoginUI();
                SaveGame();
                Debug.Log($"[GameManager] Claimed daily reward: {reward.amount} {reward.type}");
            }
            else
            {
                Debug.LogWarning("[GameManager] Daily reward already claimed today!");
            }
        }

        private void HandleClaimWeeklyReward()
        {
            if (_economyConfig == null || _economyManager == null) return;
            if (_economyConfig.weeklyReward == null || _economyConfig.weeklyReward.Length == 0) return;

            int currentDay = _economyManager.CurrentLoginDay;
            if (currentDay < 0 || currentDay >= _economyConfig.weeklyReward.Length)
                currentDay = 0;

            Reward reward = _economyConfig.weeklyReward[currentDay];
            if (_economyManager.ClaimWeeklyReward(reward))
            {
                _onItemReceive?.Raise(reward);
                UpdateWeeklyLoginUI();
                SaveGame();
                Debug.Log($"[GameManager] Claimed weekly reward (Day {currentDay + 1}): {reward.amount} {reward.type}");
            }
            else
            {
                Debug.LogWarning($"[GameManager] Weekly reward for Day {currentDay + 1} already claimed today!");
            }
        }

        // ── Shop ───────────────────────────────────────────

        private bool TryPurchase(Reward cost, Reward item, int goldShopSlotIndex = -1)
        {
            if (_economyManager == null) return false;

            if (goldShopSlotIndex >= 0 && !_economyManager.CanPurchaseGoldShopItem(goldShopSlotIndex))
            {
                Debug.LogWarning($"[GameManager] Purchase limit reached for slot {goldShopSlotIndex} today!");
                return false;
            }

            if (!_economyManager.TryPurchase(cost, item, goldShopSlotIndex))
            {
                _onItemSpend?.Raise(cost);
                Debug.LogWarning($"[GameManager] Not enough {cost.type}! Need {cost.amount}, have {_inventory.GetAmount(cost.type)}");
                return false;
            }

            _onItemReceive?.Raise(item);
            SaveGame();

            Debug.Log($"[GameManager] Purchased {item.amount} {item.type} for {cost.amount} {cost.type}");
            return true;
        }

        private void HandleBuyRemove()
        {
            Reward cost = (_economyConfig != null && _economyConfig.gemShopPrice != null && _economyConfig.gemShopPrice.Length > 0)
                ? _economyConfig.gemShopPrice[0]
                : new Reward { type = ItemType.Gem, amount = 50 };

            TryPurchase(
                cost: cost,
                item: new Reward { type = ItemType.Remove, amount = 1 }
            );
        }

        private void HandleBuyUndo()
        {
            Reward cost = (_economyConfig != null && _economyConfig.gemShopPrice != null && _economyConfig.gemShopPrice.Length > 1)
                ? _economyConfig.gemShopPrice[1]
                : new Reward { type = ItemType.Gem, amount = 50 };

            TryPurchase(
                cost: cost,
                item: new Reward { type = ItemType.Undo, amount = 1 }
            );
        }

        private void HandleBuyMoreMoves()
        {
            Reward cost = (_economyConfig != null && _economyConfig.goldShopPrice != null && _economyConfig.goldShopPrice.Length > 2)
                ? _economyConfig.goldShopPrice[2]
                : new Reward { type = ItemType.Gold, amount = 100 };

            TryPurchase(
                cost: cost,
                item: new Reward { type = ItemType.MoreMoves, amount = GameConfig.MORE_MOVE_AMOUNT },
                goldShopSlotIndex: 2
            );
        }

        // ── Booster Use (In-Game) ─────────────────────────────

        private void HandleUseMoreMoves()
        {
            LevelManager levelManager = _gridManager?.LevelManager;
            if (levelManager?.CurrentLevel == null) return;
            if (levelManager.CurrentLevel.IsOutOfMove) return;
            if (!_inventory.HasEnough(ItemType.MoreMoves, 1)) return;

            var booster = new MoreMoveBooster(levelManager.CurrentLevel, GameConfig.MORE_MOVE_AMOUNT);
            if (!booster.TryUse()) return;

            _inventory.TrySpendItem(ItemType.MoreMoves);
            SaveGame();
            Debug.Log($"[GameManager] Used MoreMoves booster: +{GameConfig.MORE_MOVE_AMOUNT} moves");
        }

        private void HandleUseUndo()
        {
            LevelManager levelManager = _gridManager?.LevelManager;
            if (levelManager?.CurrentLevel == null) return;
            if (levelManager.CurrentLevel.IsOutOfMove) return;
            if (!_inventory.HasEnough(ItemType.Undo, 1)) return;

            var booster = new UndoBooster(levelManager.MoveHistory, levelManager.CurrentLevel);
            if (!booster.TryUse()) return;

            _inventory.TrySpendItem(ItemType.Undo);

            // Sync view: animate persons back to their previous cells
            if (booster.LastUndoneRecord.HasValue)
            {
                _gridManager.RevertMoveView(booster.LastUndoneRecord.Value);
            }

            // Re-evaluate all conditions after the revert
            levelManager.CheckAllPersonConditions();

            SaveGame();
            Debug.Log("[GameManager] Used Undo booster: reverted last move");
        }

        private void HandleUseRemove()
        {
            LevelManager levelManager = _gridManager?.LevelManager;
            if (levelManager?.CurrentLevel == null) return;
            if (levelManager.CurrentLevel.IsOutOfMove) return;
            if (!_inventory.HasEnough(ItemType.Remove, 1)) return;
            if (_canSitAnywhereCondition == null)
            {
                Debug.LogError("[GameManager] CanSitAnywhere condition is not configured.", this);
                return;
            }

            var booster = new RemoveBooster(
                levelManager.CurrentLevel,
                _canSitAnywhereCondition.ToRuntimeData()
            );
            if (!booster.TryUse()) return;

            PersonView targetPersonView = _gridManager?.FindPersonView(booster.TargetPerson);
            _vfxPlayer?.PlayAtWorld(VfxId.RemoveBooster, targetPersonView?.transform);

            _inventory.TrySpendItem(ItemType.Remove);

            // Re-evaluate all conditions — the cleared person should now be Happy
            levelManager.CheckAllPersonConditions();

            SaveGame();
            Debug.Log($"[GameManager] Used Remove booster: replaced conditions with CanSitAnywhere for {booster.TargetPerson?.PersonName}");
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

            if (_economyManager != null)
            {
                _gameData.currentLoginDay = _economyManager.CurrentLoginDay;
                _gameData.isDailyRewardClaimed = _economyManager.IsDailyRewardClaimed;
                _gameData.isWeeklyRewardClaimed = _economyManager.IsWeeklyRewardClaimed;
                _gameData.goldShopPurchaseCountToday = _economyManager.GoldShopPurchaseCounts.ToArray();
            }

            _saveLoad.SaveGameData(_gameData);
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
