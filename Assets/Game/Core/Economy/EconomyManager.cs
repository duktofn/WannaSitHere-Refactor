using System;
using System.Collections.Generic;

namespace Game.Core.Economy
{
    public class EconomyManager
    {
        private readonly Inventory _inventory;
        private readonly int[] _goldShopLimits;
        private readonly int[] _goldShopPurchaseCounts;

        public Inventory Inventory => _inventory;
        public int CurrentLoginDay { get; private set; }
        public bool IsDailyRewardClaimed { get; private set; }
        public bool IsWeeklyRewardClaimed { get; private set; }
        public IReadOnlyList<int> GoldShopPurchaseCounts => _goldShopPurchaseCounts;
        public IReadOnlyList<int> GoldShopLimits => _goldShopLimits;

        public EconomyManager(
            Inventory inventory,
            int currentLoginDay,
            bool isDailyRewardClaimed,
            bool isWeeklyRewardClaimed,
            int[] goldShopLimits = null,
            int[] goldShopPurchaseCountsToday = null)
        {
            _inventory = inventory;
            CurrentLoginDay = currentLoginDay >= 0 ? currentLoginDay % 7 : 0;
            IsDailyRewardClaimed = isDailyRewardClaimed;
            IsWeeklyRewardClaimed = isWeeklyRewardClaimed;

            _goldShopLimits = goldShopLimits != null && goldShopLimits.Length == 3
                ? (int[])goldShopLimits.Clone()
                : new int[3] { 5, 5, 5 };

            _goldShopPurchaseCounts = goldShopPurchaseCountsToday != null && goldShopPurchaseCountsToday.Length == 3
                ? (int[])goldShopPurchaseCountsToday.Clone()
                : new int[3];
        }

        public bool EvaluateLoginState(DateTime lastLoginUtc, DateTime nowUtc)
        {
            if (lastLoginUtc == DateTime.MinValue)
            {
                return false;
            }

            // Anti-cheat check: clock rolled backwards
            if (nowUtc.Date < lastLoginUtc.Date)
            {
                return false;
            }

            if (nowUtc.Date > lastLoginUtc.Date)
            {
                // New calendar day
                IsDailyRewardClaimed = false;

                for (int i = 0; i < _goldShopPurchaseCounts.Length; i++)
                {
                    _goldShopPurchaseCounts[i] = 0;
                }

                // Plan A (Non-punishing cumulative):
                // If previous reward was claimed, advance to next day in 7-day cycle
                if (IsWeeklyRewardClaimed)
                {
                    CurrentLoginDay = (CurrentLoginDay + 1) % 7;
                    IsWeeklyRewardClaimed = false;
                }

                return true;
            }

            return false;
        }

        public bool CanClaimDailyReward() => !IsDailyRewardClaimed;

        public bool ClaimDailyReward(Reward dailyReward)
        {
            if (!CanClaimDailyReward())
                return false;

            _inventory.UpdateInventory(dailyReward);
            IsDailyRewardClaimed = true;
            return true;
        }

        public bool CanClaimWeeklyReward() => !IsWeeklyRewardClaimed;

        public bool ClaimWeeklyReward(Reward weeklyReward)
        {
            if (!CanClaimWeeklyReward())
                return false;

            _inventory.UpdateInventory(weeklyReward);
            IsWeeklyRewardClaimed = true;
            return true;
        }

        public bool CanPurchaseGoldShopItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _goldShopLimits.Length)
                return true;

            return _goldShopPurchaseCounts[slotIndex] < _goldShopLimits[slotIndex];
        }

        public int GetGoldShopPurchaseCount(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _goldShopPurchaseCounts.Length)
                return 0;

            return _goldShopPurchaseCounts[slotIndex];
        }

        public int GetGoldShopLimit(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _goldShopLimits.Length)
                return int.MaxValue;

            return _goldShopLimits[slotIndex];
        }

        public bool TryPurchase(Reward cost, Reward item, int goldShopSlotIndex = -1)
        {
            if (goldShopSlotIndex >= 0 && !CanPurchaseGoldShopItem(goldShopSlotIndex))
            {
                return false;
            }

            if (!_inventory.TrySpendItem(cost))
            {
                return false;
            }

            _inventory.UpdateInventory(item);

            if (goldShopSlotIndex >= 0 && goldShopSlotIndex < _goldShopPurchaseCounts.Length)
            {
                _goldShopPurchaseCounts[goldShopSlotIndex]++;
            }

            return true;
        }

        public void SetLoginDay(int day)
        {
            CurrentLoginDay = day >= 0 ? day % 7 : 0;
        }

        public void SetDailyRewardClaimed(bool claimed)
        {
            IsDailyRewardClaimed = claimed;
        }

        public void SetWeeklyRewardClaimed(bool claimed)
        {
            IsWeeklyRewardClaimed = claimed;
        }

        public void ResetShopPurchases()
        {
            for (int i = 0; i < _goldShopPurchaseCounts.Length; i++)
            {
                _goldShopPurchaseCounts[i] = 0;
            }
        }

        public void SetShopPurchaseCount(int slotIndex, int count)
        {
            if (slotIndex >= 0 && slotIndex < _goldShopPurchaseCounts.Length)
            {
                _goldShopPurchaseCounts[slotIndex] = Math.Max(0, count);
            }
        }

        public void SimulateNextDay()
        {
            CurrentLoginDay = (CurrentLoginDay + 1) % 7;
            IsDailyRewardClaimed = false;
            IsWeeklyRewardClaimed = false;
            ResetShopPurchases();
        }

        public void ResetLoginStreak()
        {
            CurrentLoginDay = 0;
            IsDailyRewardClaimed = false;
            IsWeeklyRewardClaimed = false;
            ResetShopPurchases();
        }
    }
}