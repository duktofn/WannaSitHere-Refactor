using Game.Core.Economy;

namespace Game.App
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
}
