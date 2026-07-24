

namespace steph.Unity.WFC.Editor
{
    using steph.Unity.WFC.Runtime;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(WorldGenConfig))]
    public class WorldGenConfigDrawer : Editor
    {
        WorldGenConfig SO;
        Vector2 _scrollPosition;
        string[] _tileNames;
        int _oldTileNamesCount;

        void OnEnable()
        {
            SO = (WorldGenConfig)target;
            UpdateTileNames();
            _oldTileNamesCount = _tileNames.Length;
            if(SO.AvailableTiles == null)
            {
                SO.AvailableTiles = new AvailableTiles(SO);
                SaveObject();
            }
        }

        public override void OnInspectorGUI()
        {
            SO = (WorldGenConfig)target;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawField("Columns");
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            SerializedProperty apply = serializedObject.FindProperty("_apply");
            EditorGUILayout.PropertyField(apply);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (apply.boolValue)
            {
                apply.boolValue = false;
                serializedObject.ApplyModifiedProperties();

                EditorApplication.delayCall += () =>
                {
                    if (SO == null) return;

                    Undo.RecordObject(SO, "Resetting grid");
                    SO.Grid = new GridCellCollection(SO.Columns);
                    SaveObject();
                };
                return;
            }

            EditorGUI.BeginChangeCheck();
            DrawField("SocketsCount");
            if (EditorGUI.EndChangeCheck())
            {
                SO.AvailableTiles = new AvailableTiles(SO);
                serializedObject.ApplyModifiedProperties();
            }

            bool isGridValid = SO.Grid != null && SO.Grid.GetHorizontalLength() == SO.Columns;

            EditorGUI.BeginChangeCheck();
            SerializedProperty availableTilesProp = DrawField("AvailableTiles", true);
            if (EditorGUI.EndChangeCheck())
            {

                serializedObject.ApplyModifiedProperties();

                UpdateTileNames();
                int newCount = _tileNames.Length;
                if (_oldTileNamesCount != newCount)
                {
                    _oldTileNamesCount = newCount;
                    // Apply properties right before wiping out the map
                    serializedObject.ApplyModifiedProperties();
                    SO.DestroyMap();
                    SaveObject();
                }
                return;
            }

            DrawField("_seed");

            // Since we didn't return early, your buttons will remain visible even if the grid is null!
            if (GUILayout.Button("GenerateGridRandom"))
            {
                Undo.RecordObject(SO, "generated grid");
                SO.GenerateGrid(UnityEngine.Random.Range(0, 1000));
                SaveObject();
                return;
            }

            if (GUILayout.Button("GenerateGrid"))
            {
                Undo.RecordObject(SO, "generated grid");
                SO.GenerateGrid();
                SaveObject();
                return;
            }

            if (GUILayout.Button("DestroyGrid"))
            {
                Undo.RecordObject(SO, "Destroyed grid");
                SO.DestroyMap();
                SaveObject();
                return;
            }

            DrawField("drawGrid");
            DrawField("drawSockets");
            DrawField("drawNames");
            DrawField("allowHollowTiles");
            DrawField("SocketColors", true);
            DrawField("_behaviors", true);
            DrawBehaviors();

            if (isGridValid)
            {
                if (SO.drawGrid)
                {
                    DrawGrid();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Grid is empty or mismatched. Please check 'Apply' or click 'Generate Grid'.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void UpdateTileNames()
        {
            _tileNames = SO.AvailableTiles.GetTiles().Where(t => t.Name != null).Select(t => t.Name).Prepend("None").ToArray();

        }

        void SaveObject()
        {
            //Debug.Log("Saving");
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void DrawBehaviors()
        {
            SerializedProperty prop = serializedObject.FindProperty("_behaviors");
            EditorGUILayout.PropertyField(prop, true);

            // Show inspectors for each element
            if (!prop.isExpanded) return;
            for (int i = 0; i < prop.arraySize; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);

                if (element.objectReferenceValue == null)
                    continue;

                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField(
                    element.objectReferenceValue.name,
                    EditorStyles.boldLabel);

                GetEditor(element.objectReferenceValue).OnInspectorGUI();

                EditorGUILayout.EndVertical();
            }
        }

        private readonly Dictionary<Object, Editor> _editors = new();

        Editor GetEditor(Object obj)
        {
            if (!_editors.TryGetValue(obj, out var editor))
            {
                Editor.CreateCachedEditor(obj, null, ref editor);
                _editors[obj] = editor;
            }

            return editor;
        }

        void DrawGrid()
        {
            float screenWidth = EditorGUIUtility.currentViewWidth;
            float screenHeight = Screen.height;
            float cellSize = (screenWidth - (GUI.skin.button.margin.horizontal * SO.Columns)) / SO.Columns - EditorStyles.inspectorDefaultMargins.margin.horizontal;
            float minCellSize = 50;
            bool showScroll = cellSize < minCellSize;
            cellSize = Mathf.Max(cellSize, minCellSize);

            if (showScroll)
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(screenWidth - 18), GUILayout.Height(screenHeight / 2)); // Adjust height as needed

            GUILayout.BeginVertical();
            for (int y = 0; y < SO.Columns; y++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < SO.Columns; x++)
                {
                    ColorTiles(x, y);
                    if (SO.Grid[y, x].PopUpIndex >= _tileNames.Length)
                    {
                        SO.Grid[y, x] = new GridCell();
                        SaveObject();
                        return;
                    }

                    bool buttonIsPressed = GUILayout.Button(new GUIContent(SO.drawNames ? _tileNames[SO.Grid[y, x].PopUpIndex] : ""), GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                    if (SO.drawSockets)
                        DrawSocketsRects(GUILayoutUtility.GetLastRect(), SO.Grid[y, x].tile.Sockets);

                    Rect buttonRect = GUILayoutUtility.GetLastRect();

                    if (!buttonIsPressed) continue;

                    if (IsMiddleClick())
                    {
                        GridCell result = SO.Grid[y, x];
                        Debug.Log(result);
                        continue;
                    }

                    if (IsRightClick())
                    {
                        SO.Grid[y, x].tile?.Rotate();
                        SaveObject();
                        return;
                    }
                    var selection = GetCellProperty(x, y);

                    PopupWindow.Show(buttonRect, new TileSelectionPopup(
                        _tileNames,
                        selection.FindPropertyRelative("CellData"),
                        SO.Grid[y, x].tile,
                        (selectedIndex) => ApplyTileSelection(selectedIndex, x, y)
                    ));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (showScroll)
                EditorGUILayout.EndScrollView();
        }

        SerializedProperty GetCellProperty(int x, int y)
        {
            return serializedObject
                .FindProperty("Grid")           // GridCellCollection
                .FindPropertyRelative("_cellRows")          // GridCellRow[]
                .GetArrayElementAtIndex(y)                  // GridCellRow
                .FindPropertyRelative("Cells")              // GridCell[]
                .GetArrayElementAtIndex(x);                 // GridCell
        }

        void DrawSocketsRects(Rect container, Sockets sockets)
        {
            if (sockets == null || string.IsNullOrEmpty(sockets.GetSocket(0))) return;

            float rectWidth = container.width / SO.SocketsCount;
            foreach (string edge in sockets)//always 4 edges
            {
                for (int i = 0; i < edge.Length; i++)
                {
                    EditorGUI.DrawRect(new Rect(container.position + i * rectWidth * Vector2.right, new(rectWidth, 5)), GetColor(edge[i]));
                }

                EditorGUIUtility.RotateAroundPivot(90f, container.center);
            }

        }

        Color GetColor(char socket)
        {
            return SO.SocketColors.FirstOrDefault(sc => sc.socket == socket).color;
        }

        void ApplyTileSelection(int popUpIndex, int x, int y)
        {
            Undo.RecordObject(SO, "AddedItem");

            GridCell newCell = new()
            {
                PopUpIndex = popUpIndex,
                tile = popUpIndex == 0 ? null : SO.AvailableTiles.GetTiles()[popUpIndex - 1].Clone(),
                X = x,
                Y = y,
            };
            SO.Grid[y, x] = newCell;
            SaveObject();
        }

        void ColorTiles(int x, int y)
        {
            Tile tile = SO.Grid[y, x].tile;
            //showing if selected tiles have valid connections
            if (!tile.HasEmptySockets())
            {
                if (!AllValidConnections(x, y, tile, SO.Grid))
                    GUI.backgroundColor = Color.red;
                else if (tile.TileData is BaseTileData data && data.Manual)
                    GUI.backgroundColor = Color.blue;
                else
                    GUI.backgroundColor = Color.green;
            }
            else
                GUI.backgroundColor = new Color(0.76f, 0.76f, 0.76f); ;//default color
        }
        SerializedProperty DrawField(string propertyName, bool includeChildren = false)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(prop, includeChildren);
            return prop;
        }

        bool IsRightClick()
        {
            return Event.current.button == 1;
        }

        bool IsMiddleClick()
        {
            return Event.current.button == 2;
        }

        bool AllValidConnections(int x, int y, Tile currentTile, GridCellCollection grid)
        {
            bool IsNotConnected(Tile otherTile, NeighbourDir dir)
            {
                return !otherTile.HasEmptySockets() && !currentTile.CanConnect(otherTile, dir);
            }

            //check if I'm bounds and then if  the tile is not connected
            if
            (
                (x - 1 >= 0 && IsNotConnected(grid[y, x - 1].tile, NeighbourDir.Left))
                ||
                (x + 1 < grid.GetHorizontalLength() && IsNotConnected(grid[y, x + 1].tile, NeighbourDir.Right))
                ||
                (y - 1 >= 0 && IsNotConnected(grid[y - 1, x].tile, NeighbourDir.Up))
                ||
                (y + 1 < grid.GetVerticalLength() && IsNotConnected(grid[y + 1, x].tile, NeighbourDir.Down))
            )
                return false;

            return true;
        }

    }
}