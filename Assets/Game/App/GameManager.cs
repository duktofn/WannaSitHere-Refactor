using System;
using System.Linq;
using Game.App.SaveAndLoad;
using Game.Core.Booster;
using Game.Core.Conditions;
using Game.Core.Economy;
using Game.Core.People;

namespace Game.App
{
    /// <summary>
    /// Application-level owner for persisted game progress, inventory, economy transactions,
    /// and core booster execution. Unity scene wiring and presentation orchestration belong to
    /// <c>Game.Bootstrap.GameBootstrapper</c>.
    /// </summary>
    public sealed class GameManager
    {
        private readonly SaveLoadManager _saveLoad;
        private readonly Inventory _inventory;
        private readonly EconomyManager _economyManager;
        private GameData _gameData;

        public Inventory Inventory => _inventory;
        public EconomyManager EconomyManager => _economyManager;
        public int CurrentLevel => _gameData.currentLevel;

        public GameManager(int[] goldShopLimits, SaveLoadManager saveLoad = null)
        {
            _saveLoad = saveLoad ?? new SaveLoadManager();
            _gameData = _saveLoad.GetGameData();

            _inventory = new Inventory(
                _gameData.currentGold,
                _gameData.currentGem,
                _gameData.currentRemove,
                _gameData.currentMoreMoves,
                _gameData.currentUndo
            );

            if (_gameData.goldShopPurchaseCountToday == null || _gameData.goldShopPurchaseCountToday.Length != 3)
                _gameData.goldShopPurchaseCountToday = new int[3];

            _economyManager = new EconomyManager(
                _inventory,
                _gameData.currentLoginDay,
                _gameData.isDailyRewardClaimed,
                _gameData.isWeeklyRewardClaimed,
                goldShopLimits,
                _gameData.goldShopPurchaseCountToday
            );
        }

        public void InitializeLoginState(DateTime nowUtc)
        {
            DateTime lastLoginUtc = DateTime.MinValue;
            if (!string.IsNullOrEmpty(_gameData.lastLoginDateUtc) &&
                DateTime.TryParse(
                    _gameData.lastLoginDateUtc,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime parsedDate))
            {
                lastLoginUtc = parsedDate;
            }

            bool isNewDay = _economyManager.EvaluateLoginState(lastLoginUtc, nowUtc);
            if (isNewDay || string.IsNullOrEmpty(_gameData.lastLoginDateUtc))
            {
                _gameData.lastLoginDateUtc = nowUtc.ToString("o");
                SaveGame();
            }
        }

        public void SetLevel(int level)
        {
            _gameData.currentLevel = Math.Max(1, level);
            SaveGame();
        }

        public void AdvanceLevel()
        {
            _gameData.currentLevel++;
            SaveGame();
        }

        public void GrantReward(Reward reward)
        {
            _inventory.UpdateInventory(reward);
            SaveGame();
        }

        public bool TryClaimDailyReward(Reward reward)
        {
            if (!_economyManager.ClaimDailyReward(reward))
                return false;

            SaveGame();
            return true;
        }

        public bool TryClaimWeeklyReward(Reward reward)
        {
            if (!_economyManager.ClaimWeeklyReward(reward))
                return false;

            SaveGame();
            return true;
        }

        public bool TryPurchase(Reward cost, Reward item, int goldShopSlotIndex = -1)
        {
            if (!_economyManager.TryPurchase(cost, item, goldShopSlotIndex))
                return false;

            SaveGame();
            return true;
        }

        public bool TryUseMoreMoves(LevelManager levelManager, int amount)
        {
            if (!CanUseBooster(levelManager, ItemType.MoreMoves))
                return false;

            var booster = new MoreMoveBooster(levelManager.CurrentLevel, amount);
            if (!booster.TryUse())
                return false;

            _inventory.TrySpendItem(ItemType.MoreMoves);
            SaveGame();
            return true;
        }

        public bool TryUseUndo(LevelManager levelManager, out MoveRecord record)
        {
            record = default;
            if (!CanUseBooster(levelManager, ItemType.Undo))
                return false;

            var booster = new UndoBooster(levelManager.MoveHistory, levelManager.CurrentLevel);
            if (!booster.TryUse() || !booster.LastUndoneRecord.HasValue)
                return false;

            _inventory.TrySpendItem(ItemType.Undo);
            SaveGame();
            record = booster.LastUndoneRecord.Value;
            return true;
        }

        public bool TryUseRemove(
            LevelManager levelManager,
            ConditionRuntimeData canSitAnywhereCondition,
            out PersonRuntimeData targetPerson)
        {
            targetPerson = null;
            if (!CanUseBooster(levelManager, ItemType.Remove))
                return false;

            var booster = new RemoveBooster(levelManager.CurrentLevel, canSitAnywhereCondition);
            if (!booster.TryUse())
                return false;

            _inventory.TrySpendItem(ItemType.Remove);
            SaveGame();
            targetPerson = booster.TargetPerson;
            return true;
        }

        public void SaveGame()
        {
            _gameData.currentGold = _inventory.Gold;
            _gameData.currentGem = _inventory.Gem;
            _gameData.currentRemove = _inventory.Remove;
            _gameData.currentMoreMoves = _inventory.MoreMoves;
            _gameData.currentUndo = _inventory.Undo;
            _gameData.currentLoginDay = _economyManager.CurrentLoginDay;
            _gameData.isDailyRewardClaimed = _economyManager.IsDailyRewardClaimed;
            _gameData.isWeeklyRewardClaimed = _economyManager.IsWeeklyRewardClaimed;
            _gameData.goldShopPurchaseCountToday = _economyManager.GoldShopPurchaseCounts.ToArray();

            _saveLoad.SaveGameData(_gameData);
        }

        private bool CanUseBooster(LevelManager levelManager, ItemType boosterType)
        {
            return levelManager?.CurrentLevel != null &&
                   !levelManager.CurrentLevel.IsOutOfMove &&
                   _inventory.HasEnough(boosterType, 1);
        }
    }
}
