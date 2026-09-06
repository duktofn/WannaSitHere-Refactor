namespace Game.Core.Economy
{
    public enum ItemType
    {
        Gold = 0, Gem = 1, Remove = 2, Undo = 3, MoreMoves = 4
    }

    [System.Serializable]
    public struct Reward
    {
        public ItemType type;
        public int amount;
    }
}