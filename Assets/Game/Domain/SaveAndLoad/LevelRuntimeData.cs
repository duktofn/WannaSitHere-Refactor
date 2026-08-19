using System;
using Game.Domain.Grid;
using Game.Shared;

namespace Game.Domain.SaveAndLoad
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

            OnMoveChanged?.Invoke(_levelMove);
        }
    }
}
