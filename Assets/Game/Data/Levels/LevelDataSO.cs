using UnityEngine;
using Game.Data.Board;
using Game.Core.Board;
using Game.Core.Levels;

namespace Game.Data.Levels
{
    [CreateAssetMenu(fileName = "New Level Data", menuName = "Game/New Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public int levelMove;

        public Grid<CellDataSO> mainGrid;
        public Grid<CellDataSO> waitGrid;

        private Grid<CellRuntimeData> ConvertGrid(Grid<CellDataSO> source, GridId id)
        {
            if (source == null)
                return null;

            var result = new Grid<CellRuntimeData>(
                source.GridSize,
                source.CellSize,
                source.CellDistance,
                source.PosX,
                source.PosY
            );

            for (int x = 0; x < source.GridSize.x; x++)
            {
                for (int y = 0; y < source.GridSize.y; y++)
                {
                    CellDataSO cellData = source.Get(x, y);

                    if (cellData == null)
                        continue;

                    result.Set(x, y, cellData.ToRuntimeData(x, y, source.CellSize, id));
                }
            }

            return result;
        }

        public LevelRuntimeData ToRuntimeData()
        {
            return new LevelRuntimeData(levelMove, 
                                        ConvertGrid(mainGrid, GridId.MainGrid), 
                                        ConvertGrid(waitGrid, GridId.WaitGrid));
        }
    }
}
