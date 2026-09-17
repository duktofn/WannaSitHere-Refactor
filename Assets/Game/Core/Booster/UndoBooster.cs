using Game.Core.Levels;

namespace Game.Core.Booster
{
    /// <summary>
    /// Reverts the last recorded move (MainGrid-to-MainGrid only).
    /// Swaps the persons back to their original cells and refunds one move.
    /// After execution, read <see cref="LastUndoneRecord"/> to sync the view layer.
    /// </summary>
    public class UndoBooster : Booster
    {
        private readonly MoveHistory _moveHistory;
        private readonly LevelRuntimeData _levelData;

        /// <summary>
        /// The move record that was undone. Read this after <see cref="Booster.TryUse"/>
        /// returns true to animate the view revert.
        /// </summary>
        public MoveRecord? LastUndoneRecord { get; private set; }

        public UndoBooster(MoveHistory moveHistory, LevelRuntimeData levelData)
        {
            _moveHistory = moveHistory;
            _levelData = levelData;
        }

        protected override bool CanUse()
        {
            return _moveHistory != null && _moveHistory.HasHistory && _levelData != null;
        }

        protected override void Execute()
        {
            if (!_moveHistory.TryPop(out MoveRecord record))
            {
                LastUndoneRecord = null;
                return;
            }

            // Revert cell person assignments to pre-move state
            record.TargetCell.SetPerson(record.DisplacedPerson);
            record.SourceCell.SetPerson(record.MovedPerson);

            // Refund the consumed move
            _levelData.ModifyMove(1);

            LastUndoneRecord = record;
        }
    }
}

