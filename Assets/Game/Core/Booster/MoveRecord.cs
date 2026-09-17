using Game.Core.Board;
using Game.Core.People;

namespace Game.Core.Booster
{
    /// <summary>
    /// Immutable snapshot of a single move, used by <see cref="MoveHistory"/> for undo.
    /// Only moves between MainGrid seats are recorded (WaitLine moves are excluded).
    /// </summary>
    public readonly struct MoveRecord
    {
        /// <summary>The cell the person was dragged FROM.</summary>
        public readonly CellRuntimeData SourceCell;

        /// <summary>The cell the person was dropped ONTO.</summary>
        public readonly CellRuntimeData TargetCell;

        /// <summary>The person that was actively moved by the player.</summary>
        public readonly PersonRuntimeData MovedPerson;

        /// <summary>The person that was sitting in the target cell before the move (null if the seat was empty).</summary>
        public readonly PersonRuntimeData DisplacedPerson;

        public MoveRecord(
            CellRuntimeData sourceCell,
            CellRuntimeData targetCell,
            PersonRuntimeData movedPerson,
            PersonRuntimeData displacedPerson)
        {
            SourceCell = sourceCell;
            TargetCell = targetCell;
            MovedPerson = movedPerson;
            DisplacedPerson = displacedPerson;
        }
    }
}

