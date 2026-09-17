using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.App;
using Game.Core.Booster;
using Game.Core.Economy;
using Game.Data.Conditions;
using Game.Data.Economy;
using Game.Data.Levels;
using Game.Data.People;
using Game.Events;
using Game.View.Board;
using Game.View.People;
using Game.View.UI;
using Game.View.VFX;

namespace Game.Bootstrap
{
    /// <summary>
    /// Scene composition root. Owns Unity lifecycle, serialized references, event-channel
    /// subscriptions, and presentation orchestration while delegating application state and
    /// transactions to <see cref="GameManager"/>.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
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

        private readonly EventListener _listener = new();
        private GameManager _gameManager;
        private LevelBootstrapper _levelBootstrapper;

        public Inventory Inventory => _gameManager?.Inventory;
        public EconomyManager EconomyManager => _gameManager?.EconomyManager;
        public int CurrentLevel => _gameManager?.CurrentLevel ?? 1;
        public int TotalLevels => _levelData != null ? _levelData.Count : 0;

        public void SetLevel(int level)
        {
            if (_gameManager == null) return;

            _gameManager.SetLevel(level);
            RaiseCurrentLevelChanged();
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
            if (_gameManager == null) return;

            _gameManager.SetLevel(level);
            _levelBootstrapper?.LoadLevel(CurrentLevel);
            RaiseCurrentLevelChanged();
        }

        public void PlayLevel(int level)
        {
            if (_gameManager == null) return;

            _gameManager.SetLevel(level);
            RaiseCurrentLevelChanged();

            if (_onPlayGameEvent != null)
                _onPlayGameEvent.Raise();
            else
                _levelBootstrapper?.LoadLevel(CurrentLevel);
        }

        public void RestartCurrentLevel()
        {
            if (_onRestartLevelEvent != null)
                _onRestartLevelEvent.Raise();
            else
                RestartLevel();
        }

        public void UpdateWeeklyLoginUI()
        {
            if (_weeklyLogin == null || _economyConfig == null || _economyConfig.weeklyReward == null || EconomyManager == null)
                return;

            _weeklyLogin.SetRewardData(
                _economyConfig.weeklyReward.ToList(),
                EconomyManager.CurrentLoginDay,
                EconomyManager.IsWeeklyRewardClaimed,
                _economyConfig.dailyReward,
                EconomyManager.IsDailyRewardClaimed
            );
        }

        public void SaveGame()
        {
            _gameManager?.SaveGame();
        }

        private void Awake()
        {
            int[] goldShopLimits = _economyConfig != null && _economyConfig.goldShopLimit != null
                ? _economyConfig.goldShopLimit
                : new[] { 5, 5, 5 };

            _gameManager = new GameManager(goldShopLimits);
            _gameManager.InitializeLoginState(DateTime.UtcNow);
            _levelBootstrapper = new LevelBootstrapper(_levelData, _gridManager, _levelView);

            _uiManager?.Initialize(Inventory);
            UpdateWeeklyLoginUI();
        }

        private void Start()
        {
            RaiseCurrentLevelChanged();
        }

        private void OnEnable()
        {
            _listener.Listen(_onPlayGameEvent, HandlePlayGame);
            _listener.Listen(_onWinEvent, HandleWin);
            _listener.Listen(_onLoseEvent, HandleLose);
            _listener.Listen(_onNextLevelEvent, NextLevel);
            _listener.Listen(_onRestartLevelEvent, RestartLevel);

            _listener.Listen(_onClaimWinRewardEvent, HandleClaimWinReward);
            _listener.Listen(_onClaimAdsRewardEvent, HandleClaimAdsReward);
            _listener.Listen(_onClaimDailyRewardEvent, HandleClaimDailyReward);
            _listener.Listen(_onClaimWeeklyRewardEvent, HandleClaimWeeklyReward);

            _listener.Listen(_onBuyRemoveEvent, HandleBuyRemove);
            _listener.Listen(_onBuyUndoEvent, HandleBuyUndo);
            _listener.Listen(_onBuyMoreMovesEvent, HandleBuyMoreMoves);

            _listener.Listen(_onUseMoreMovesEvent, HandleUseMoreMoves);
            _listener.Listen(_onUseUndoEvent, HandleUseUndo);
            _listener.Listen(_onUseRemoveEvent, HandleUseRemove);
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
        }

        private void HandlePlayGame()
        {
            int levelToLoad = CurrentLevel > 0 ? CurrentLevel : 1;
            _levelBootstrapper?.LoadLevel(levelToLoad);
            _levelView?.BindBoosters(Inventory);
            _onLevelChangedEvent?.Raise(levelToLoad);

            Debug.Log($"[GameBootstrapper] Started Level: {levelToLoad}");
        }

        private void HandleWin()
        {
            if (_gameManager == null) return;

            _gameManager.AdvanceLevel();
            RaiseCurrentLevelChanged();
            Debug.Log($"[GameBootstrapper] Win! Next level: {CurrentLevel}");
        }

        private static void HandleLose()
        {
            Debug.Log("[GameBootstrapper] Lose! Player can retry.");
        }

        private void NextLevel()
        {
            _levelBootstrapper?.LoadLevel(CurrentLevel);
            RaiseCurrentLevelChanged();
        }

        private void RestartLevel()
        {
            int levelToLoad = CurrentLevel > 0 ? CurrentLevel : 1;
            _levelBootstrapper?.LoadLevel(levelToLoad);
            _onLevelChangedEvent?.Raise(levelToLoad);
        }

        private void HandleClaimWinReward()
        {
            if (_economyConfig == null || _gameManager == null) return;

            Reward reward = _economyConfig.levelWinReward;
            _gameManager.GrantReward(reward);
            _onItemReceive?.Raise(reward);
            Debug.Log($"[GameBootstrapper] Claimed win reward: {reward.amount} {reward.type}");
        }

        private void HandleClaimAdsReward()
        {
            if (_economyConfig == null || _gameManager == null) return;

            Reward reward = _economyConfig.levelAdsWinReward;
            _gameManager.GrantReward(reward);
            _onItemReceive?.Raise(reward);
            Debug.Log($"[GameBootstrapper] Claimed ads reward: {reward.amount} {reward.type}");
        }

        private void HandleClaimDailyReward()
        {
            if (_economyConfig == null || _gameManager == null) return;

            Reward reward = _economyConfig.dailyReward;
            if (!_gameManager.TryClaimDailyReward(reward))
            {
                Debug.LogWarning("[GameBootstrapper] Daily reward already claimed today!");
                return;
            }

            _onItemReceive?.Raise(reward);
            UpdateWeeklyLoginUI();
            Debug.Log($"[GameBootstrapper] Claimed daily reward: {reward.amount} {reward.type}");
        }

        private void HandleClaimWeeklyReward()
        {
            if (_economyConfig == null || _gameManager == null || _economyConfig.weeklyReward == null || _economyConfig.weeklyReward.Length == 0)
                return;

            int currentDay = EconomyManager.CurrentLoginDay;
            if (currentDay < 0 || currentDay >= _economyConfig.weeklyReward.Length)
                currentDay = 0;

            Reward reward = _economyConfig.weeklyReward[currentDay];
            if (!_gameManager.TryClaimWeeklyReward(reward))
            {
                Debug.LogWarning($"[GameBootstrapper] Weekly reward for Day {currentDay + 1} already claimed today!");
                return;
            }

            _onItemReceive?.Raise(reward);
            UpdateWeeklyLoginUI();
            Debug.Log($"[GameBootstrapper] Claimed weekly reward (Day {currentDay + 1}): {reward.amount} {reward.type}");
        }

        private bool TryPurchase(Reward cost, Reward item, int goldShopSlotIndex = -1)
        {
            if (_gameManager == null || EconomyManager == null) return false;

            if (goldShopSlotIndex >= 0 && !EconomyManager.CanPurchaseGoldShopItem(goldShopSlotIndex))
            {
                Debug.LogWarning($"[GameBootstrapper] Purchase limit reached for slot {goldShopSlotIndex} today!");
                return false;
            }

            if (!_gameManager.TryPurchase(cost, item, goldShopSlotIndex))
            {
                _onItemSpend?.Raise(cost);
                Debug.LogWarning($"[GameBootstrapper] Not enough {cost.type}! Need {cost.amount}, have {Inventory.GetAmount(cost.type)}");
                return false;
            }

            _onItemReceive?.Raise(item);
            Debug.Log($"[GameBootstrapper] Purchased {item.amount} {item.type} for {cost.amount} {cost.type}");
            return true;
        }

        private void HandleBuyRemove()
        {
            Reward cost = _economyConfig != null && _economyConfig.gemShopPrice != null && _economyConfig.gemShopPrice.Length > 0
                ? _economyConfig.gemShopPrice[0]
                : new Reward { type = ItemType.Gem, amount = 50 };

            TryPurchase(cost, new Reward { type = ItemType.Remove, amount = 1 });
        }

        private void HandleBuyUndo()
        {
            Reward cost = _economyConfig != null && _economyConfig.gemShopPrice != null && _economyConfig.gemShopPrice.Length > 1
                ? _economyConfig.gemShopPrice[1]
                : new Reward { type = ItemType.Gem, amount = 50 };

            TryPurchase(cost, new Reward { type = ItemType.Undo, amount = 1 });
        }

        private void HandleBuyMoreMoves()
        {
            Reward cost = _economyConfig != null && _economyConfig.goldShopPrice != null && _economyConfig.goldShopPrice.Length > 2
                ? _economyConfig.goldShopPrice[2]
                : new Reward { type = ItemType.Gold, amount = 100 };

            TryPurchase(
                cost,
                new Reward { type = ItemType.MoreMoves, amount = GameConfig.MORE_MOVE_AMOUNT },
                goldShopSlotIndex: 2
            );
        }

        private void HandleUseMoreMoves()
        {
            LevelManager levelManager = _gridManager?.LevelManager;
            if (_gameManager == null || !_gameManager.TryUseMoreMoves(levelManager, GameConfig.MORE_MOVE_AMOUNT))
                return;

            Debug.Log($"[GameBootstrapper] Used MoreMoves booster: +{GameConfig.MORE_MOVE_AMOUNT} moves");
        }

        private void HandleUseUndo()
        {
            LevelManager levelManager = _gridManager?.LevelManager;
            if (_gameManager == null || !_gameManager.TryUseUndo(levelManager, out MoveRecord record))
                return;

            _gridManager?.RevertMoveView(record);
            levelManager.CheckAllPersonConditions();
            Debug.Log("[GameBootstrapper] Used Undo booster: reverted last move");
        }

        private void HandleUseRemove()
        {
            LevelManager levelManager = _gridManager?.LevelManager;
            if (_gameManager == null || _canSitAnywhereCondition == null)
            {
                if (_canSitAnywhereCondition == null)
                    Debug.LogError("[GameBootstrapper] CanSitAnywhere condition is not configured.", this);
                return;
            }

            if (!_gameManager.TryUseRemove(
                    levelManager,
                    _canSitAnywhereCondition.ToRuntimeData(),
                    out var targetPerson))
            {
                return;
            }

            PersonView targetPersonView = _gridManager?.FindPersonView(targetPerson);
            _vfxPlayer?.PlayAtWorld(VfxId.RemoveBooster, targetPersonView?.transform);
            levelManager.CheckAllPersonConditions();
            Debug.Log($"[GameBootstrapper] Used Remove booster: replaced conditions with CanSitAnywhere for {targetPerson?.PersonName}");
        }

        private void RaiseCurrentLevelChanged()
        {
            _onLevelChangedEvent?.Raise(CurrentLevel);
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
