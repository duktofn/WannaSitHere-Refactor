using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data.Board;
using Game.Core.Board;
using Game.Core.Levels;
using Game.Core.People;
using Game.Data.People;

namespace Game.Data.Levels
{
    [CreateAssetMenu(fileName = "New Level Data", menuName = "Game/New Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public int levelMove;

        public Grid<CellDataSO> mainGrid;
        public Grid<CellDataSO> waitGrid;
        public List<LevelPersonConfig> personConfigs = new();
        public List<GameObject> levelEnvironmentPrefabs = new();

        /// <summary>
        /// Validates level-owned person placements without mutating serialized
        /// data. The custom Inspector and runtime conversion share this logic.
        /// </summary>
        public bool Validate(List<string> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            errors.Clear();

            HashSet<string> occupiedPlacements = new();
            if (personConfigs != null)
            {
                for (int i = 0; i < personConfigs.Count; i++)
                {
                    LevelPersonConfig config = personConfigs[i];
                    if (config == null)
                    {
                        errors.Add($"Level '{name}': person configuration #{i} is null.");
                        continue;
                    }

                    string location = FormatLocation(config.gridId, config.position);
                    string prefix = $"Level '{name}': person configuration #{i} at {location}";

                    if (config.definition == null)
                        errors.Add($"{prefix} is missing its PersonDefinitionSO.");

                    if (config.conditions != null)
                    {
                        if (config.conditions.Count > GameConfig.MAX_CONDITION_PER_PERSON)
                        {
                            errors.Add(
                                $"{prefix} has {config.conditions.Count} conditions; " +
                                $"the maximum is {GameConfig.MAX_CONDITION_PER_PERSON}.");
                        }

                        for (int conditionIndex = 0; conditionIndex < config.conditions.Count; conditionIndex++)
                        {
                            if (config.conditions[conditionIndex] == null)
                            {
                                errors.Add(
                                    $"{prefix} contains a null condition reference at index {conditionIndex}.");
                            }
                        }
                    }

                    string placementKey = MakePlacementKey(config.gridId, config.position);
                    if (!occupiedPlacements.Add(placementKey))
                        errors.Add($"{prefix} overlaps another person configuration at the same cell.");

                    Grid<CellDataSO> grid = GetGrid(config.gridId);
                    if (grid == null)
                    {
                        errors.Add($"{prefix} targets a missing or unknown grid.");
                        continue;
                    }

                    if (!IsWithinGrid(config.position, grid.GridSize))
                    {
                        errors.Add($"{prefix} is outside the {grid.GridSize.x}x{grid.GridSize.y} grid.");
                        continue;
                    }

                    CellDataSO cell = grid.Get(config.position.x, config.position.y);
                    if (cell == null)
                    {
                        errors.Add($"{prefix} targets an empty cell.");
                    }
                    else if (cell.type != CellType.Seat)
                    {
                        errors.Add($"{prefix} targets a {cell.type} cell; only Seat cells may contain people.");
                    }
                }
            }

            return errors.Count == 0;
        }

        public bool TryToRuntimeData(out LevelRuntimeData runtime, out List<string> validationErrors)
        {
            validationErrors = new List<string>();
            if (!Validate(validationErrors))
            {
                runtime = null;
                foreach (string error in validationErrors)
                    Debug.LogError(error, this);
                return false;
            }

            Dictionary<string, PersonRuntimeData> initialPersons = new();
            if (personConfigs != null)
            {
                foreach (LevelPersonConfig config in personConfigs)
                {
                    PersonRuntimeData person = config.ToRuntimeData();
                    initialPersons.Add(MakePlacementKey(config.gridId, config.position), person);
                }
            }

            runtime = new LevelRuntimeData(
                levelMove,
                ConvertGrid(mainGrid, GridId.MainGrid, initialPersons),
                ConvertGrid(waitGrid, GridId.WaitGrid, initialPersons));
            return true;
        }

        public LevelRuntimeData ToRuntimeData()
        {
            return TryToRuntimeData(out LevelRuntimeData runtime, out _) ? runtime : null;
        }

        private Grid<CellRuntimeData> ConvertGrid(
            Grid<CellDataSO> source,
            GridId id,
            IReadOnlyDictionary<string, PersonRuntimeData> initialPersons)
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

                    initialPersons.TryGetValue(
                        MakePlacementKey(id, new Vector2Int(x, y)),
                        out PersonRuntimeData person);
                    result.Set(x, y, cellData.ToRuntimeData(x, y, source.CellSize, id, person));
                }
            }

            return result;
        }

        private Grid<CellDataSO> GetGrid(GridId id)
        {
            return id switch
            {
                GridId.MainGrid => mainGrid,
                GridId.WaitGrid => waitGrid,
                _ => null
            };
        }

        private static bool IsWithinGrid(Vector2Int position, Vector2Int gridSize)
        {
            return position.x >= 0 && position.y >= 0 &&
                   position.x < gridSize.x && position.y < gridSize.y;
        }

        private static string MakePlacementKey(GridId gridId, Vector2Int position)
        {
            return $"{(int)gridId}:{position.x}:{position.y}";
        }

        private static string FormatLocation(GridId gridId, Vector2Int position)
        {
            return $"{gridId} ({position.x}, {position.y})";
        }
    }
}
