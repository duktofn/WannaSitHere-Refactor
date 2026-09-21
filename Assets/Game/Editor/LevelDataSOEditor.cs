using UnityEngine;
using UnityEditor;
using Game.Data.Board;
using Game.Data.Levels;
using Game.Data.People;
using Game.Core.Board;
using System.Collections.Generic;

namespace Game.Editor
{
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : UnityEditor.Editor
    {
        private bool _mainGridFoldout = true;
        private bool _waitGridFoldout = true;
        private bool _personConfigsFoldout = true;
        private bool _validationFoldout = true;

        private Vector2 _mainScrollPos;
        private Vector2 _waitScrollPos;

        private Vector2Int _prevMainSize;
        private Vector2Int _prevWaitSize;
        private Vector2Int _placementPosition;
        private GridId _placementGrid = GridId.MainGrid;
        private int _selectedConfigIndex = -1;
        private GUIStyle _personMarkerStyle;

        private CellDataSO _mainFillCellData;
        private CellDataSO _waitFillCellData;

        private void OnEnable()
        {
            var mainSizeProp = serializedObject.FindProperty("mainGrid._gridSize");
            var waitSizeProp = serializedObject.FindProperty("waitGrid._gridSize");

            if (mainSizeProp != null) _prevMainSize = mainSizeProp.vector2IntValue;
            if (waitSizeProp != null) _prevWaitSize = waitSizeProp.vector2IntValue;

            _personMarkerStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                wordWrap = false,
                padding = new RectOffset(2, 2, 1, 1)
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("levelMove"), new GUIContent("Level Move Limit"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            var environmentPrefabsProp = serializedObject.FindProperty("levelEnvironmentPrefabs");
            if (environmentPrefabsProp != null)
            {
                EditorGUILayout.PropertyField(
                    environmentPrefabsProp,
                    new GUIContent("Level Environment Prefabs"),
                    true);
            }

            var mainGridProp = serializedObject.FindProperty("mainGrid");
            var waitGridProp = serializedObject.FindProperty("waitGrid");
            var personConfigsProp = serializedObject.FindProperty("personConfigs");

            DrawGridMatrixSection("Main Grid Matrix", mainGridProp, GridId.MainGrid, true, personConfigsProp, ref _mainGridFoldout, ref _mainScrollPos, ref _prevMainSize, ref _mainFillCellData);
            EditorGUILayout.Space(15);
            DrawGridMatrixSection("Wait Grid Matrix", waitGridProp, GridId.WaitGrid, false, personConfigsProp, ref _waitGridFoldout, ref _waitScrollPos, ref _prevWaitSize, ref _waitFillCellData);

            EditorGUILayout.Space(15);
            DrawPersonConfigSection(personConfigsProp, mainGridProp, waitGridProp);

            serializedObject.ApplyModifiedProperties();
            DrawValidationSummary();
        }

        private void DrawGridMatrixSection(string label, SerializedProperty gridProp, GridId gridId, bool isMainGrid,
                                            SerializedProperty personConfigsProp,
                                            ref bool foldout, ref Vector2 scrollPos, ref Vector2Int prevSize,
                                            ref CellDataSO fillCellData)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foldout = EditorGUILayout.Foldout(foldout, label, true, EditorStyles.foldoutHeader);
            if (!foldout)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);

            var sizeProp     = gridProp.FindPropertyRelative("_gridSize");
            var cellSizeProp = gridProp.FindPropertyRelative("_cellSize");
            var cellDistProp = gridProp.FindPropertyRelative("_cellDistance");
            var posXProp     = gridProp.FindPropertyRelative("_posX");
            var posYProp     = gridProp.FindPropertyRelative("_posY");
            var contentProp  = gridProp.FindPropertyRelative("_gridContent");

            // --- Grid Settings ---
            EditorGUILayout.LabelField("Grid Properties", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(sizeProp, new GUIContent("Matrix Size (X x Y)"));
            bool sizeChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.PropertyField(cellSizeProp, new GUIContent("Cell Size"));
            EditorGUILayout.PropertyField(cellDistProp, new GUIContent("Cell Distance"));
            EditorGUILayout.Slider(posXProp, 0f, 1f, new GUIContent("Viewport Pos X"));
            EditorGUILayout.Slider(posYProp, 0f, 1f, new GUIContent("Viewport Pos Y"));
            EditorGUILayout.LabelField("Allowed Cell Types", GetAllowedCellTypesLabel(isMainGrid), EditorStyles.miniLabel);

            // Clamp minimum grid size
            Vector2Int gridSize = sizeProp.vector2IntValue;
            gridSize.x = Mathf.Max(1, gridSize.x);
            gridSize.y = Mathf.Max(1, gridSize.y);
            sizeProp.vector2IntValue = gridSize;

            int totalCells = gridSize.x * gridSize.y;

            // Resize array, preserve cell positions
            if (sizeChanged && prevSize != gridSize && prevSize.x > 0 && prevSize.y > 0)
            {
                ResizeGridContent(contentProp, prevSize, gridSize);
                prevSize = gridSize;
            }
            else if (contentProp.arraySize != totalCells)
            {
                contentProp.arraySize = totalCells;
                prevSize = gridSize;
            }

            EditorGUILayout.Space(10);

            // --- Batch Operations / Quick Fill ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Batch Tools:", GUILayout.Width(75));
            CellDataSO selectedFillCell = (CellDataSO)EditorGUILayout.ObjectField(
                fillCellData,
                typeof(CellDataSO),
                false,
                GUILayout.Width(140));
            bool rejectedFillCell = selectedFillCell != null && !IsAllowedCellData(selectedFillCell, isMainGrid);
            if (!rejectedFillCell)
                fillCellData = selectedFillCell;

            using (new EditorGUI.DisabledGroupScope(fillCellData == null))
            {
                if (GUILayout.Button("Fill Matrix", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    for (int i = 0; i < contentProp.arraySize; i++)
                        contentProp.GetArrayElementAtIndex(i).objectReferenceValue = fillCellData;
                }
            }

            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                for (int i = 0; i < contentProp.arraySize; i++)
                {
                    contentProp.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (rejectedFillCell)
            {
                EditorGUILayout.HelpBox(
                    $"'{selectedFillCell.name}' is not valid for this grid. Allowed: {GetAllowedCellTypesLabel(isMainGrid)}.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"2D Matrix View ({gridSize.x} x {gridSize.y})", EditorStyles.boldLabel);

            // --- Matrix Rendering ---
            const float headerHeight = 20f;
            const float rowHeaderWidth = 35f;
            float cellWidth = GetMatrixCellWidth(gridSize.x, rowHeaderWidth);
            bool allowHorizontalScroll = gridSize.x > 6;

            scrollPos = EditorGUILayout.BeginScrollView(
                scrollPos,
                allowHorizontalScroll,
                false,
                GUILayout.Height(Mathf.Min(350f, (gridSize.y + 2) * 42f + 30f)));

            // Column Index Header (X)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(rowHeaderWidth);
            for (int x = 0; x < gridSize.x; x++)
            {
                GUILayout.Box($"X: {x}", EditorStyles.miniButton, GUILayout.Width(cellWidth), GUILayout.Height(headerHeight));
            }
            EditorGUILayout.EndHorizontal();

            // Rows (y descending so y = gridSize.y - 1 is top row visually)
            int invalidCellCount = 0;
            for (int y = gridSize.y - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                // Row Index Header (Y)
                GUILayout.Box($"Y:{y}", EditorStyles.miniButton, GUILayout.Width(rowHeaderWidth), GUILayout.Height(22f));

                for (int x = 0; x < gridSize.x; x++)
                {
                    int index = x + y * gridSize.x;
                    if (index < contentProp.arraySize)
                    {
                        SerializedProperty elementProp = contentProp.GetArrayElementAtIndex(index);

                        EditorGUILayout.BeginVertical(GUILayout.Width(cellWidth));
                        if (DrawCellDataField(elementProp, isMainGrid, cellWidth))
                            invalidCellCount++;

                        DrawPersonMarker(personConfigsProp, gridId, new Vector2Int(x, y), cellWidth);
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (invalidCellCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"This grid contains {invalidCellCount} invalid cell reference(s). Replace them with: {GetAllowedCellTypesLabel(isMainGrid)}.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPersonConfigSection(
            SerializedProperty configsProp,
            SerializedProperty mainGridProp,
            SerializedProperty waitGridProp)
        {
            if (configsProp == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _personConfigsFoldout = EditorGUILayout.Foldout(
                _personConfigsFoldout,
                $"Level Person Configurations ({configsProp.arraySize})",
                true,
                EditorStyles.foldoutHeader);

            if (!_personConfigsFoldout)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Add or select a person at a Seat", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Person names are shown directly inside occupied matrix cells. " +
                "Click a name in the matrix to select its configuration.",
                MessageType.Info);
            _placementGrid = (GridId)EditorGUILayout.EnumPopup("Grid", _placementGrid);
            _placementPosition = EditorGUILayout.Vector2IntField("Position", _placementPosition);

            SerializedProperty selectedGridProp =
                _placementGrid == GridId.MainGrid ? mainGridProp : waitGridProp;
            CellDataSO placementCell = GetCellData(selectedGridProp, _placementPosition);
            int existingIndex = GetConfigIndexAt(configsProp, _placementGrid, _placementPosition);

            if (placementCell == null)
            {
                EditorGUILayout.HelpBox(
                    "The selected position is outside the grid or has no cell.",
                    MessageType.Warning);
            }
            else if (placementCell.type != CellType.Seat)
            {
                EditorGUILayout.HelpBox(
                    $"The selected cell is {placementCell.type}; only Seat cells can contain a person.",
                    MessageType.Warning);
            }
            else if (existingIndex >= 0)
            {
                EditorGUILayout.HelpBox(
                    $"A person configuration already exists at this position (#{existingIndex}).",
                    MessageType.Info);
                if (GUILayout.Button("Select Existing Configuration"))
                    _selectedConfigIndex = existingIndex;
            }
            else if (GUILayout.Button("Add Person Configuration"))
            {
                AddOrSelectPersonConfig(configsProp, _placementGrid, _placementPosition);
            }

            if (_selectedConfigIndex >= 0 && _selectedConfigIndex < configsProp.arraySize)
            {
                EditorGUILayout.LabelField(
                    $"Selected configuration: #{_selectedConfigIndex}",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Definitions and Conditions", EditorStyles.boldLabel);

            for (int i = 0; i < configsProp.arraySize; i++)
            {
                SerializedProperty configProp = configsProp.GetArrayElementAtIndex(i);
                SerializedProperty definitionProp = configProp.FindPropertyRelative("definition");
                SerializedProperty conditionsProp = configProp.FindPropertyRelative("conditions");
                SerializedProperty gridIdProp = configProp.FindPropertyRelative("gridId");
                SerializedProperty positionProp = configProp.FindPropertyRelative("position");

                bool isSelected = i == _selectedConfigIndex;
                EditorGUILayout.BeginVertical(isSelected ? "selectionRect" : "helpBox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"Person #{i}: {GetDefinitionName(definitionProp)}",
                    EditorStyles.boldLabel);
                if (GUILayout.Button("Select Cell", EditorStyles.miniButton, GUILayout.Width(72f)))
                {
                    _selectedConfigIndex = i;
                    _placementGrid = (GridId)gridIdProp.enumValueIndex;
                    _placementPosition = positionProp.vector2IntValue;
                }
                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    Undo.RecordObject(target, "Remove Level Person Configuration");
                    configsProp.DeleteArrayElementAtIndex(i);
                    if (_selectedConfigIndex == i)
                        _selectedConfigIndex = -1;
                    else if (_selectedConfigIndex > i)
                        _selectedConfigIndex--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(
                    "Matrix location",
                    GetPlacementDescription(gridIdProp, positionProp),
                    EditorStyles.miniLabel);

                EditorGUILayout.PropertyField(definitionProp, new GUIContent("Definition"));
                EditorGUILayout.PropertyField(gridIdProp, new GUIContent("Grid"));
                EditorGUILayout.PropertyField(positionProp, new GUIContent("Position"));

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(conditionsProp, GUIContent.none, true);
                DrawConditionHints(conditionsProp);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationSummary()
        {
            LevelDataSO level = target as LevelDataSO;
            if (level == null)
                return;

            List<string> errors = new();
            bool valid = level.Validate(errors);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _validationFoldout = EditorGUILayout.Foldout(
                _validationFoldout,
                valid ? "Level Validation: Valid" : $"Level Validation: {errors.Count} issue(s)",
                true,
                EditorStyles.foldoutHeader);

            if (_validationFoldout)
            {
                if (valid)
                {
                    EditorGUILayout.HelpBox(
                        "All configured person placements and conditions are valid.",
                        MessageType.Info);
                }
                else
                {
                    foreach (string error in errors)
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void AddOrSelectPersonConfig(
            SerializedProperty configsProp,
            GridId gridId,
            Vector2Int position)
        {
            int existingIndex = GetConfigIndexAt(configsProp, gridId, position);
            if (existingIndex >= 0)
            {
                _selectedConfigIndex = existingIndex;
                _placementGrid = gridId;
                _placementPosition = position;
                return;
            }

            Undo.RecordObject(target, "Add Level Person Configuration");
            int newIndex = configsProp.arraySize;
            configsProp.InsertArrayElementAtIndex(newIndex);

            SerializedProperty configProp = configsProp.GetArrayElementAtIndex(newIndex);
            configProp.FindPropertyRelative("definition").objectReferenceValue = null;
            configProp.FindPropertyRelative("gridId").enumValueIndex = (int)gridId;
            configProp.FindPropertyRelative("position").vector2IntValue = position;
            configProp.FindPropertyRelative("conditions").arraySize = 0;

            _selectedConfigIndex = newIndex;
            _placementGrid = gridId;
            _placementPosition = position;
        }

        private void DrawPersonMarker(
            SerializedProperty configsProp,
            GridId gridId,
            Vector2Int position,
            float cellWidth)
        {
            int count = GetConfigCountAt(configsProp, gridId, position);
            if (count == 0)
            {
                GUILayout.Label("", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(cellWidth));
                return;
            }

            int configIndex = GetConfigIndexAt(configsProp, gridId, position);
            string marker = count > 1
                ? $"People ({count})"
                : GetDefinitionName(
                    configsProp
                        .GetArrayElementAtIndex(configIndex)
                        .FindPropertyRelative("definition"));
            string tooltip = GetConfigTooltip(configsProp, gridId, position);
            if (GUILayout.Button(
                    new GUIContent(marker, tooltip),
                    _personMarkerStyle,
                    GUILayout.Width(cellWidth),
                    GUILayout.Height(22f)))
            {
                _selectedConfigIndex = configIndex;
                _placementGrid = gridId;
                _placementPosition = position;
            }
        }

        private static void DrawConditionHints(SerializedProperty conditionsProp)
        {
            if (conditionsProp == null)
                return;

            if (conditionsProp.arraySize > GameConfig.MAX_CONDITION_PER_PERSON)
            {
                EditorGUILayout.HelpBox(
                    $"This person has {conditionsProp.arraySize} conditions; " +
                    $"the maximum is {GameConfig.MAX_CONDITION_PER_PERSON}.",
                    MessageType.Error);
            }

            for (int i = 0; i < conditionsProp.arraySize; i++)
            {
                if (conditionsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(
                        $"Condition reference at index {i} is missing.",
                        MessageType.Error);
                }
            }
        }

        private static int GetConfigIndexAt(
            SerializedProperty configsProp,
            GridId gridId,
            Vector2Int position)
        {
            if (configsProp == null)
                return -1;

            for (int i = 0; i < configsProp.arraySize; i++)
            {
                SerializedProperty config = configsProp.GetArrayElementAtIndex(i);
                SerializedProperty gridProp = config.FindPropertyRelative("gridId");
                SerializedProperty positionProp = config.FindPropertyRelative("position");
                if (gridProp != null && positionProp != null &&
                    gridProp.enumValueIndex == (int)gridId &&
                    positionProp.vector2IntValue == position)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetConfigCountAt(
            SerializedProperty configsProp,
            GridId gridId,
            Vector2Int position)
        {
            if (configsProp == null)
                return 0;

            int count = 0;
            for (int i = 0; i < configsProp.arraySize; i++)
            {
                SerializedProperty config = configsProp.GetArrayElementAtIndex(i);
                SerializedProperty gridProp = config.FindPropertyRelative("gridId");
                SerializedProperty positionProp = config.FindPropertyRelative("position");
                if (gridProp != null && positionProp != null &&
                    gridProp.enumValueIndex == (int)gridId &&
                    positionProp.vector2IntValue == position)
                {
                    count++;
                }
            }
            return count;
        }

        private static string GetConfigTooltip(
            SerializedProperty configsProp,
            GridId gridId,
            Vector2Int position)
        {
            if (configsProp == null)
                return string.Empty;

            string tooltip =
                $"{GetGridDisplayName(gridId)} · row {position.y + 1}, column {position.x + 1}";
            for (int i = 0; i < configsProp.arraySize; i++)
            {
                SerializedProperty config = configsProp.GetArrayElementAtIndex(i);
                SerializedProperty gridProp = config.FindPropertyRelative("gridId");
                SerializedProperty positionProp = config.FindPropertyRelative("position");
                if (gridProp == null || positionProp == null ||
                    gridProp.enumValueIndex != (int)gridId ||
                    positionProp.vector2IntValue != position)
                {
                    continue;
                }

                string personName = GetDefinitionName(config.FindPropertyRelative("definition"));
                tooltip += $"\n{personName}";
            }

            return tooltip;
        }

        private static string GetPlacementDescription(
            SerializedProperty gridProp,
            SerializedProperty positionProp)
        {
            if (gridProp == null || positionProp == null)
                return "Unknown";

            GridId gridId = (GridId)gridProp.enumValueIndex;
            Vector2Int position = positionProp.vector2IntValue;
            return
                $"{GetGridDisplayName(gridId)} · row {position.y + 1}, column {position.x + 1}";
        }

        private static string GetGridDisplayName(GridId gridId)
        {
            return gridId == GridId.WaitGrid ? "Wait Grid" : "Main Grid";
        }

        private static string GetDefinitionName(SerializedProperty definitionProp)
        {
            PersonDefinitionSO definition = definitionProp?.objectReferenceValue as PersonDefinitionSO;
            if (definition == null)
                return "Missing Definition";

            return string.IsNullOrEmpty(definition.personName) ? definition.name : definition.personName;
        }

        private static CellDataSO GetCellData(SerializedProperty gridProp, Vector2Int position)
        {
            if (gridProp == null)
                return null;

            SerializedProperty sizeProp = gridProp.FindPropertyRelative("_gridSize");
            SerializedProperty contentProp = gridProp.FindPropertyRelative("_gridContent");
            if (sizeProp == null || contentProp == null)
                return null;

            Vector2Int size = sizeProp.vector2IntValue;
            if (position.x < 0 || position.y < 0 || position.x >= size.x || position.y >= size.y)
                return null;

            int index = position.x + position.y * size.x;
            if (index < 0 || index >= contentProp.arraySize)
                return null;

            return contentProp.GetArrayElementAtIndex(index).objectReferenceValue as CellDataSO;
        }

        private static float GetMatrixCellWidth(int columnCount, float rowHeaderWidth)
        {
            const float preferredCellWidth = 90f;
            const float minimumCellWidth = 40f;
            const float inspectorPadding = 24f;
            const int columnsWithoutHorizontalScroll = 6;

            if (columnCount <= 0 || columnCount > columnsWithoutHorizontalScroll)
                return preferredCellWidth;

            float availableWidth = EditorGUIUtility.currentViewWidth - rowHeaderWidth - inspectorPadding;
            float responsiveCellWidth = availableWidth / columnCount;
            return Mathf.Clamp(responsiveCellWidth, minimumCellWidth, preferredCellWidth);
        }

        private static bool DrawCellDataField(SerializedProperty elementProp, bool isMainGrid, float cellWidth)
        {
            CellDataSO currentCell = elementProp.objectReferenceValue as CellDataSO;
            CellDataSO selectedCell = (CellDataSO)EditorGUILayout.ObjectField(
                currentCell,
                typeof(CellDataSO),
                false,
                GUILayout.Width(cellWidth),
                GUILayout.Height(20f));

            if (selectedCell != currentCell && IsAllowedCellData(selectedCell, isMainGrid))
                elementProp.objectReferenceValue = selectedCell;

            return currentCell != null && !IsAllowedCellData(currentCell, isMainGrid);
        }

        private static bool IsAllowedCellData(CellDataSO cellData, bool isMainGrid)
        {
            if (cellData == null)
                return true;

            if (isMainGrid)
                return cellData.type == Game.Core.Board.CellType.Seat ||
                       cellData.type == Game.Core.Board.CellType.Food ||
                       cellData.type == Game.Core.Board.CellType.Block;

            return cellData.type == Game.Core.Board.CellType.Seat;
        }

        private static string GetAllowedCellTypesLabel(bool isMainGrid)
        {
            return isMainGrid ? "Seat, Food, Block" : "Seat";
        }

        /// <summary>
        /// Resize grid content array while preserving existing cell coordinates.
        /// </summary>
        private void ResizeGridContent(SerializedProperty contentProp,
                                       Vector2Int oldSize, Vector2Int newSize)
        {
            int oldTotal = oldSize.x * oldSize.y;

            // Cache old references
            Object[] oldValues = new Object[Mathf.Min(oldTotal, contentProp.arraySize)];
            for (int i = 0; i < oldValues.Length; i++)
                oldValues[i] = contentProp.GetArrayElementAtIndex(i).objectReferenceValue;

            // Resize & clear
            int newTotal = newSize.x * newSize.y;
            contentProp.arraySize = newTotal;
            for (int i = 0; i < newTotal; i++)
                contentProp.GetArrayElementAtIndex(i).objectReferenceValue = null;

            // Remap: copy overlapping region
            int minX = Mathf.Min(oldSize.x, newSize.x);
            int minY = Mathf.Min(oldSize.y, newSize.y);

            for (int y = 0; y < minY; y++)
            {
                for (int x = 0; x < minX; x++)
                {
                    int oldIdx = x + y * oldSize.x;
                    int newIdx = x + y * newSize.x;
                    if (oldIdx < oldValues.Length)
                        contentProp.GetArrayElementAtIndex(newIdx).objectReferenceValue = oldValues[oldIdx];
                }
            }
        }
    }
}

