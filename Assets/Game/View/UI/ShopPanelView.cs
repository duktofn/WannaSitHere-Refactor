using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core.Economy;

namespace Game.View.UI
{
    public readonly struct ShopPurchaseRequest
    {
        public bool UsesGold { get; }
        public int SlotIndex { get; }
        public ItemType ItemType { get; }

        public ShopPurchaseRequest(bool usesGold, int slotIndex, ItemType itemType)
        {
            UsesGold = usesGold;
            SlotIndex = slotIndex;
            ItemType = itemType;
        }
    }

    /// <summary>
    /// Binds EconomyConfig-derived offers to the six shop slots and keeps the UI synchronized
    /// with inventory changes and gold-shop purchase counts.
    /// </summary>
    public sealed class ShopPanelView : MonoBehaviour
    {
        [Header("Shop Slots")]
        [SerializeField] private ShopItemView[] goldShopItems;
        [SerializeField] private ShopItemView[] gemShopItems;

        private Reward[] _goldShopPrices = Array.Empty<Reward>();
        private int[] _goldShopLimits = Array.Empty<int>();
        private Reward[] _gemShopPrices = Array.Empty<Reward>();
        private EconomyManager _economyManager;
        private Action<ShopPurchaseRequest> _onPurchaseRequested;
        private bool _isSubscribed;

        private void Awake()
        {
            EnsureSlotViews();
            DiscoverSlotViewsIfNeeded();
        }

        public void Bind(
            IReadOnlyList<Reward> goldShopPrices,
            IReadOnlyList<int> goldShopLimits,
            IReadOnlyList<Reward> gemShopPrices,
            EconomyManager economyManager,
            Action<ShopPurchaseRequest> onPurchaseRequested)
        {
            DiscoverSlotViewsIfNeeded();

            _goldShopPrices = Copy(goldShopPrices);
            _goldShopLimits = Copy(goldShopLimits);
            _gemShopPrices = Copy(gemShopPrices);
            _economyManager = economyManager;
            _onPurchaseRequested = onPurchaseRequested;

            SubscribeToInventory();
            Refresh();
        }

        public void Refresh()
        {
            RefreshSlots(goldShopItems, _goldShopPrices, true);
            RefreshSlots(gemShopItems, _gemShopPrices, false);
        }

        private void OnEnable()
        {
            SubscribeToInventory();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromInventory();
        }

        private void RefreshSlots(
            ShopItemView[] slots,
            IReadOnlyList<Reward> prices,
            bool usesGold)
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                ShopItemView slot = slots[i];
                if (slot == null || !TryGetItemType(i, out ItemType itemType) || i >= prices.Count)
                {
                    slot?.SetUnavailable();
                    continue;
                }

                bool hasGoldLimit = usesGold && i < _goldShopLimits.Length;
                if (usesGold && !hasGoldLimit)
                {
                    slot.SetUnavailable();
                    continue;
                }

                int purchaseCount = usesGold && _economyManager != null
                    ? _economyManager.GetGoldShopPurchaseCount(i)
                    : 0;
                int limit = hasGoldLimit ? _goldShopLimits[i] : 0;
                var request = new ShopPurchaseRequest(usesGold, i, itemType);

                slot.Bind(
                    request,
                    prices[i],
                    _economyManager,
                    purchaseCount,
                    limit,
                    _onPurchaseRequested
                );
            }
        }

        private void DiscoverSlotViewsIfNeeded()
        {
            ShopItemView[] discovered = GetComponentsInChildren<ShopItemView>(true);

            if (goldShopItems == null || goldShopItems.Length == 0)
                goldShopItems = FindSlots(discovered, "GoldShopItem");

            if (gemShopItems == null || gemShopItems.Length == 0)
                gemShopItems = FindSlots(discovered, "GemShopItem");
        }

        private void EnsureSlotViews()
        {
            Transform[] descendants = GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant == transform ||
                    (!descendant.name.StartsWith("GoldShopItem", StringComparison.OrdinalIgnoreCase) &&
                     !descendant.name.StartsWith("GemShopItem", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (descendant.GetComponent<ShopItemView>() == null)
                    descendant.gameObject.AddComponent<ShopItemView>();
            }
        }

        private static ShopItemView[] FindSlots(ShopItemView[] candidates, string prefix)
        {
            return candidates
                .Where(candidate => candidate != null && candidate.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(GetSlotIndex)
                .ThenBy(candidate => candidate.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static int GetSlotIndex(ShopItemView slot)
        {
            int separator = slot.name.LastIndexOf(' ');
            if (separator >= 0 && int.TryParse(slot.name.Substring(separator + 1), out int index))
                return index - 1;

            return int.MaxValue;
        }

        private static bool TryGetItemType(int slotIndex, out ItemType itemType)
        {
            switch (slotIndex)
            {
                case 0:
                    itemType = ItemType.MoreMoves;
                    return true;
                case 1:
                    itemType = ItemType.Remove;
                    return true;
                case 2:
                    itemType = ItemType.Undo;
                    return true;
                default:
                    itemType = default;
                    return false;
            }
        }

        private void SubscribeToInventory()
        {
            if (_isSubscribed || !isActiveAndEnabled || _economyManager?.Inventory == null)
                return;

            _economyManager.Inventory.OnInventoryUpdate += HandleInventoryUpdate;
            _isSubscribed = true;
        }

        private void UnsubscribeFromInventory()
        {
            if (!_isSubscribed || _economyManager?.Inventory == null)
                return;

            _economyManager.Inventory.OnInventoryUpdate -= HandleInventoryUpdate;
            _isSubscribed = false;
        }

        private void HandleInventoryUpdate()
        {
            Refresh();
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            return source == null ? Array.Empty<T>() : source.ToArray();
        }
    }
}
