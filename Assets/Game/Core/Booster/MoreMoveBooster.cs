using Game.Core.Levels;

namespace Game.Core.Booster
{
    /// <summary>
    /// Adds extra moves to the current level.
    /// Amount is configured via <see cref="Game.Data.People.GameConfig.MORE_MOVE_AMOUNT"/>.
    /// </summary>
    public class MoreMoveBooster : Booster
    {
        private readonly LevelRuntimeData _levelData;
        private readonly int _amount;

        public MoreMoveBooster(LevelRuntimeData levelData, int amount)
        {
            _levelData = levelData;
            _amount = amount;
        }

        protected override bool CanUse() => _levelData != null && _amount > 0;

        protected override void Execute()
        {
            _levelData.ModifyMove(_amount);
        }
    }
}