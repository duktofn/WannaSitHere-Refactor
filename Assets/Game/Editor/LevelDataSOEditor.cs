using UnityEngine;
using UnityEditor;
using Game.Data;

namespace Game.Editor
{
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : UnityEditor.Editor
    {
        private bool _mainGridFoldout = true;
        private bool _waitGridFoldout = true;

        private Vector2 _mainScrollPos;
        private Vector2 _waitScrollPos;

        private Vector2Int _prevMainSize;
        private Vector2Int _prevWaitSize;

        private CellDataSO _fillCellData;

        private void OnEnable()
        {
            var mainSizeProp = serializedObject.FindProperty("mainGrid._gridSize");
            var waitSizeProp = serializedObject.FindProperty("waitGrid._gridSize");

            if (mainSizeProp != null) _prevMainSize = mainSizeProp.vector2IntValue;
            if (waitSizeProp != null) _prevWaitSize = waitSizeProp.vector2IntValue;
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

            var mainGridProp = serializedObject.FindProperty("mainGrid");
            var waitGridProp = serializedObject.FindProperty("waitGrid");

            DrawGridMatrixSection("Main Grid Matrix", mainGridProp, ref _mainGridFoldout, ref _mainScrollPos, ref _prevMainSize);
            EditorGUILayout.Space(15);
            DrawGridMatrixSection("Wait Grid Matrix", waitGridProp, ref _waitGridFoldout, ref _waitScrollPos, ref _prevWaitSize);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGridMatrixSection(string label, SerializedProperty gridProp,
                                            ref bool foldout, ref Vector2 scrollPos, ref Vector2Int prevSize)
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
            _fillCellData = (CellDataSO)EditorGUILayout.ObjectField(_fillCellData, typeof(CellDataSO), false, GUILayout.Width(140));

            if (GUILayout.Button("Fill Matrix", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                for (int i = 0; i < contentProp.arraySize; i++)
                {
                    contentProp.GetArrayElementAtIndex(i).objectReferenceValue = _fillCellData;
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

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"2D Matrix View ({gridSize.x} x {gridSize.y})", EditorStyles.boldLabel);

            // --- Matrix Rendering ---
            const float cellWidth = 90f;
            const float headerHeight = 20f;
            const float rowHeaderWidth = 35f;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(Mathf.Min(350f, (gridSize.y + 2) * 26f + 30f)));

            // Column Index Header (X)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(rowHeaderWidth);
            for (int x = 0; x < gridSize.x; x++)
            {
                GUILayout.Box($"X: {x}", EditorStyles.miniButton, GUILayout.Width(cellWidth), GUILayout.Height(headerHeight));
            }
            EditorGUILayout.EndHorizontal();

            // Rows (y descending so y = gridSize.y - 1 is top row visually)
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
                        
                        // Draw Cell ObjectField in matrix slot
                        EditorGUILayout.PropertyField(
                            elementProp,
                            GUIContent.none,
                            GUILayout.Width(cellWidth),
                            GUILayout.Height(20f)
                        );
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
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

