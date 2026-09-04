using System;
using Game.Core.Board;

namespace Game.Core.Levels
{
    public class LevelRuntimeData
    {
        private int _levelMove;
        public int CurrentMove => _levelMove;
        public bool IsOutOfMove => _levelMove <= 0;

        public event Action<int> OnMoveChanged;

        public Grid<CellRuntimeData> MainGrid { get; private set; }
        public Grid<CellRuntimeData> WaitGrid { get; private set; }

        public void ModifyMove(int amount)
        {
            _levelMove += amount;
            OnMoveChanged?.Invoke(_levelMove);
        }

        public LevelRuntimeData(int move, Grid<CellRuntimeData> main, Grid<CellRuntimeData> wait)
        {
            _levelMove = move;
            MainGrid = main;
            WaitGrid = wait;
        }
    }
}
