namespace Game.Core.Economy
{
    public class EconomyManager
    {
        private int removeLimit;
        private int undoLimit;
        private int moreMovesLimit;

        public bool IsWeeklyRewardClaimed { get; private set; }
        public bool IsDailyRewardClaimed { get; private set; }

        public EconomyManager(int removeLimit, int undoLimit, int moreMovesLimit)
        {
            this.removeLimit = removeLimit;
            this.undoLimit = undoLimit;
            this.moreMovesLimit = moreMovesLimit;
        }
    }
}