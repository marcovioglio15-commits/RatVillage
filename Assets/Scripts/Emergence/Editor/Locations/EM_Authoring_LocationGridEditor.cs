using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    [CustomEditor(typeof(EM_Authoring_LocationGrid))]
    public sealed class EM_Authoring_LocationGridEditor : Editor
    {
        #region Constants
        private const float MinCellSize = 8f;
        private const float MaxCellSize = 32f;
        private const float GridHeight = 300f;
        #endregion

        #region Fields
        private SerializedProperty widthProperty;
        private SerializedProperty heightProperty;
        private SerializedProperty nodeSizeProperty;
        private SerializedProperty originOffsetProperty;
        private SerializedProperty drawGizmosProperty;
        private SerializedProperty showCellLabelsProperty;
        private SerializedProperty walkableColorProperty;
        private SerializedProperty blockedColorProperty;
        private SerializedProperty gridLineColorProperty;
        private SerializedProperty nodesProperty;

        private int paintMode;
        private EM_LocationDefinition paintLocation;
        private float cellSize = 16f;
        private Vector2 scrollPosition;
        private GUIStyle cellLabelStyle;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            widthProperty = serializedObject.FindProperty("width");
            heightProperty = serializedObject.FindProperty("height");
            nodeSizeProperty = serializedObject.FindProperty("nodeSize");
            originOffsetProperty = serializedObject.FindProperty("originOffset");
            drawGizmosProperty = serializedObject.FindProperty("drawGizmos");
            showCellLabelsProperty = serializedObject.FindProperty("showCellLabels");
            walkableColorProperty = serializedObject.FindProperty("walkableColor");
            blockedColorProperty = serializedObject.FindProperty("blockedColor");
            gridLineColorProperty = serializedObject.FindProperty("gridLineColor");
            nodesProperty = serializedObject.FindProperty("nodes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(widthProperty);
            EditorGUILayout.PropertyField(heightProperty);
            EditorGUILayout.PropertyField(nodeSizeProperty);
            EditorGUILayout.PropertyField(originOffsetProperty);
            EditorGUILayout.PropertyField(drawGizmosProperty);

            if (drawGizmosProperty.boolValue)
            {
                EditorGUILayout.PropertyField(showCellLabelsProperty);
                EditorGUILayout.PropertyField(gridLineColorProperty);
            }

            EditorGUILayout.PropertyField(walkableColorProperty);
            EditorGUILayout.PropertyField(blockedColorProperty);

            EditorGUILayout.Space();
            DrawPaintToolbar();

            int width = Mathf.Max(1, widthProperty.intValue);
            int height = Mathf.Max(1, heightProperty.intValue);
            EnsureNodeArray(width, height);
            DrawGrid(width, height);

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region Paint UI
        private void DrawPaintToolbar()
        {
            EditorGUILayout.LabelField("Paint", EditorStyles.boldLabel);

            string[] labels = new string[] { "Walkable", "Blocked", "Location" };
            paintMode = GUILayout.Toolbar(paintMode, labels);
            cellSize = EditorGUILayout.Slider("Cell Size", cellSize, MinCellSize, MaxCellSize);

            if (paintMode != 2)
                return;

            paintLocation = (EM_LocationDefinition)EditorGUILayout.ObjectField("Location", paintLocation, typeof(EM_LocationDefinition), false);

            if (paintLocation == null)
                EditorGUILayout.HelpBox("Assign a Location Definition to paint location nodes.", MessageType.Info);
        }
        #endregion

        #region Grid Drawing
        private void DrawGrid(int width, int height)
        {
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

            float gridWidth = width * cellSize;
            float gridHeight = height * cellSize;
            float viewHeight = Mathf.Min(GridHeight, gridHeight + 16f);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(viewHeight));
            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

            DrawGridCells(gridRect, width, height);
            HandlePaintEvent(gridRect, width, height);

            EditorGUILayout.EndScrollView();
        }

        private void DrawGridCells(Rect gridRect, int width, int height)
        {
            EnsureLabelStyle();

            for (int y = 0; y < height; y++)
            {
                float drawY = (height - 1 - y) * cellSize;

                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(index);
                    SerializedProperty walkableProperty = nodeProperty.FindPropertyRelative("walkable");
                    SerializedProperty locationProperty = nodeProperty.FindPropertyRelative("locationDefinition");
                    bool isWalkable = walkableProperty != null && walkableProperty.boolValue;
                    EM_LocationDefinition location = locationProperty != null
                        ? locationProperty.objectReferenceValue as EM_LocationDefinition
                        : null;

                    Color color = isWalkable
                        ? walkableColorProperty.colorValue
                        : blockedColorProperty.colorValue;

                    if (location != null)
                        color = location.Color;

                    Rect cellRect = new Rect(gridRect.x + x * cellSize, gridRect.y + drawY, cellSize - 1f, cellSize - 1f);
                    EditorGUI.DrawRect(cellRect, color);
                    GUI.Label(cellRect, string.Format("{0},{1}", x, y), cellLabelStyle);
                }
            }
        }

        private void HandlePaintEvent(Rect gridRect, int width, int height)
        {
            Event current = Event.current;

            if (current.button != 0)
                return;

            if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag)
                return;

            if (!gridRect.Contains(current.mousePosition))
                return;

            Vector2 local = current.mousePosition - gridRect.position;
            int cellX = Mathf.FloorToInt(local.x / cellSize);
            int cellY = Mathf.FloorToInt(local.y / cellSize);

            if (cellX < 0 || cellX >= width)
                return;

            if (cellY < 0 || cellY >= height)
                return;

            int gridY = height - 1 - cellY;
            int index = cellX + gridY * width;
            SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(index);

            ApplyPaint(nodeProperty);
            current.Use();
        }
        #endregion

        #region Helpers
        private void EnsureNodeArray(int width, int height)
        {
            if (nodesProperty == null)
                return;

            int expected = width * height;

            if (nodesProperty.arraySize == expected)
                return;

            int previous = nodesProperty.arraySize;
            nodesProperty.arraySize = expected;

            for (int i = previous; i < expected; i++)
                InitializeNode(nodesProperty.GetArrayElementAtIndex(i));
        }

        private void InitializeNode(SerializedProperty nodeProperty)
        {
            if (nodeProperty == null)
                return;

            SerializedProperty walkableProperty = nodeProperty.FindPropertyRelative("walkable");
            SerializedProperty locationProperty = nodeProperty.FindPropertyRelative("locationDefinition");

            if (walkableProperty != null)
                walkableProperty.boolValue = true;

            if (locationProperty != null)
                locationProperty.objectReferenceValue = null;
        }

        private void EnsureLabelStyle()
        {
            if (cellLabelStyle != null)
                return;

            cellLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            cellLabelStyle.normal.textColor = Color.white;
        }

        private void ApplyPaint(SerializedProperty nodeProperty)
        {
            if (nodeProperty == null)
                return;

            SerializedProperty walkableProperty = nodeProperty.FindPropertyRelative("walkable");
            SerializedProperty locationProperty = nodeProperty.FindPropertyRelative("locationDefinition");

            if (paintMode == 0)
            {
                if (walkableProperty != null)
                    walkableProperty.boolValue = true;

                if (locationProperty != null)
                    locationProperty.objectReferenceValue = null;
            }
            else if (paintMode == 1)
            {
                if (walkableProperty != null)
                    walkableProperty.boolValue = false;

                if (locationProperty != null)
                    locationProperty.objectReferenceValue = null;
            }
            else if (paintMode == 2)
            {
                if (paintLocation == null)
                    return;

                if (walkableProperty != null)
                    walkableProperty.boolValue = true;

                if (locationProperty != null)
                    locationProperty.objectReferenceValue = paintLocation;
            }
        }
        #endregion
    }
}
