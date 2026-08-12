using Game.Domain.Grid;
using Game.Shared;

namespace Game.Domain.SaveAndLoad
{
    public class LevelRuntimeData
    {
        public int LevelMove { get; private set; }

        public Grid<CellRuntimeData> MainGrid { get; }
        public Grid<CellRuntimeData> WaitGrid { get; }

        public void ModifyMove(int amount)
        {
            LevelMove += amount;
        }

        public LevelRuntimeData(int move, Grid<CellRuntimeData> main, Grid<CellRuntimeData> wait)
        {
            LevelMove = move;
            MainGrid = main;
            WaitGrid = wait;
        }
    }
}
