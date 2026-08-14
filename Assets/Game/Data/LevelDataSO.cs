using UnityEngine;
using Game.Shared;
using Game.Domain.SaveAndLoad;
using Game.Domain.Grid;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "New Level Data", menuName = "Game/New Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public int levelMove;

        public Grid<CellDataSO> mainGrid;
        public Grid<CellDataSO> waitGrid;

        private Grid<CellRuntimeData> ConvertGrid(Grid<CellDataSO> source)
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

                    result.Set(x, y, cellData.ToRuntimeData(x, y, source.CellSize));
                }
            }

            return result;
        }

        public LevelRuntimeData ToRuntimeData()
        {
            return new LevelRuntimeData(levelMove, ConvertGrid(mainGrid), ConvertGrid(waitGrid));
        }
    }
}