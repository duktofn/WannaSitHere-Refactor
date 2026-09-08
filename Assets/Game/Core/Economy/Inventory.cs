using System;

namespace Game.Core.Economy
{
    public class Inventory
    {
        private int _currentGold;
        private int _currentGem;
        private int _currentRemove;
        private int _currentMoreMoves;
        private int _currentUndo;

        public int Gold => _currentGold;
        public int Gem => _currentGem;
        public int Remove => _currentRemove;
        public int MoreMoves => _currentMoreMoves;
        public int Undo => _currentUndo;

        public event Action OnInventoryUpdate;

        public Inventory(int gold, int gem, int remove, int moreMoves, int undo)
        {
            _currentGold = gold;
            _currentGem = gem;
            _currentRemove = remove;
            _currentMoreMoves = moreMoves;
            _currentUndo = undo;
        }

        public int GetAmount(ItemType type) => type switch
        {
            ItemType.Gold => _currentGold,
            ItemType.Gem => _currentGem,
            ItemType.Remove => _currentRemove,
            ItemType.Undo => _currentUndo,
            ItemType.MoreMoves => _currentMoreMoves,
            _ => 0
        };

        public bool HasEnough(ItemType type, int amount) => amount > 0 && GetAmount(type) >= amount;

        public void UpdateInventory(Reward reward)
        {
            if (reward.amount <= 0) return;

            switch (reward.type)
            {
                case ItemType.Gold:
                    _currentGold += reward.amount;
                    break;
                case ItemType.Gem:
                    _currentGem += reward.amount;
                    break;
                case ItemType.Remove:
                    _currentRemove += reward.amount;
                    break;
                case ItemType.Undo:
                    _currentUndo += reward.amount;
                    break;
                case ItemType.MoreMoves:
                    _currentMoreMoves += reward.amount;
                    break;
            }
        }

        public bool TrySpendItem(Reward item) => TrySpendItem(item.type, item.amount);

        public bool TrySpendItem(ItemType type, int amount = 1)
        {
            if (!HasEnough(type, amount))
                return false;

            switch (type)
            {
                case ItemType.Gold:
                    _currentGold -= amount;
                    break;
                case ItemType.Gem:
                    _currentGem -= amount;
                    break;
                case ItemType.Remove:
                    _currentRemove -= amount;
                    break;
                case ItemType.Undo:
                    _currentUndo -= amount;
                    break;
                case ItemType.MoreMoves:
                    _currentMoreMoves -= amount;
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}