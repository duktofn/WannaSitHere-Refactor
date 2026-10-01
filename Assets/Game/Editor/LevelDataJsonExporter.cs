using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Game.Core.Board;
using Game.Data.Board;
using Game.Data.Conditions;
using Game.Data.Levels;
using Game.Data.People;

namespace Game.Editor
{
    internal static class LevelDataJsonExporter
    {
        internal static void Export(LevelDataSO level)
        {
            if (level == null)
                return;

            string path = EditorUtility.SaveFilePanel(
                "Export Level Config JSON",
                Application.dataPath,
                level.name + ".json",
                "json");
            if (string.IsNullOrEmpty(path))
                return;

            string json = JsonUtility.ToJson(CreateJson(level), true);
            File.WriteAllText(path, json, new UTF8Encoding(false));
            EditorUtility.RevealInFinder(path);
        }

        private static LevelJson CreateJson(LevelDataSO level)
        {
            return new LevelJson
            {
                levelName = level.name,
                levelAssetPath = AssetDatabase.GetAssetPath(level),
                levelMove = level.levelMove,
                mainGrid = CreateGridJson(level.mainGrid),
                waitGrid = CreateGridJson(level.waitGrid),
                personConfigs = CreatePersonConfigsJson(level.personConfigs)
            };
        }

        private static GridJson CreateGridJson(Grid<CellDataSO> grid)
        {
            GridJson result = new GridJson
            {
                gridSize = grid != null ? grid.GridSize : Vector2Int.zero,
                cells = new CellJson[grid != null ? grid.GridSize.x * grid.GridSize.y : 0]
            };

            if (grid == null)
                return result;

            for (int y = 0; y < grid.GridSize.y; y++)
            {
                for (int x = 0; x < grid.GridSize.x; x++)
                {
                    int index = x + y * grid.GridSize.x;
                    CellDataSO cell = grid.GridContent != null && index < grid.GridContent.Length
                        ? grid.GridContent[index]
                        : null;
                    result.cells[index] = CreateCellJson(x, y, cell);
                }
            }

            return result;
        }

        private static CellJson CreateCellJson(int x, int y, CellDataSO cell)
        {
            CellJson result = new CellJson
            {
                x = x,
                y = y,
                isEmpty = cell == null,
                type = cell != null ? cell.type.ToString() : string.Empty,
                food = cell != null ? cell.food.ToString() : string.Empty
            };

            return result;
        }

        private static PersonConfigJson[] CreatePersonConfigsJson(List<LevelPersonConfig> configs)
        {
            if (configs == null)
                return new PersonConfigJson[0];

            PersonConfigJson[] result = new PersonConfigJson[configs.Count];
            for (int i = 0; i < configs.Count; i++)
            {
                LevelPersonConfig config = configs[i];
                PersonConfigJson json = new PersonConfigJson();
                result[i] = json;
                if (config == null)
                    continue;

                PersonDefinitionSO definition = config.definition;
                json.personName = definition != null ? definition.personName : string.Empty;
                json.trait = definition != null ? definition.trait.ToString() : string.Empty;
                json.gridId = config.gridId.ToString();
                json.position = config.position;
                json.conditions = CreateConditionsJson(config.conditions);
            }

            return result;
        }

        private static ConditionJson[] CreateConditionsJson(List<ConditionDataSO> conditions)
        {
            if (conditions == null)
                return new ConditionJson[0];

            ConditionJson[] result = new ConditionJson[conditions.Count];
            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionDataSO condition = conditions[i];
                ConditionJson json = new ConditionJson();
                result[i] = json;
                if (condition == null)
                    continue;

                json.type = condition.type.ToString();
                json.target = condition.target.ToString();
                json.targetTrait = condition.targetTrait.ToString();
                json.foodTarget = condition.foodTarget.ToString();
                json.description = condition.description;
            }

            return result;
        }

        [Serializable]
        private sealed class LevelJson
        {
            public string levelName;
            public string levelAssetPath;
            public int levelMove;
            public GridJson mainGrid;
            public GridJson waitGrid;
            public PersonConfigJson[] personConfigs;
        }

        [Serializable]
        private sealed class GridJson
        {
            public Vector2Int gridSize;
            public CellJson[] cells;
        }

        [Serializable]
        private sealed class CellJson
        {
            public int x;
            public int y;
            public bool isEmpty;
            public string type;
            public string food;
        }

        [Serializable]
        private sealed class PersonConfigJson
        {
            public string personName;
            public string trait;
            public string gridId;
            public Vector2Int position;
            public ConditionJson[] conditions;
        }

        [Serializable]
        private sealed class ConditionJson
        {
            public string type;
            public string target;
            public string targetTrait;
            public string foodTarget;
            public string description;
        }
    }
}
