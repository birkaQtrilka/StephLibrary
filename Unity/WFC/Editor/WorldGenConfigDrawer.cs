

namespace steph.Unity.WFC.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using steph.Unity.Editor;
    using steph.Unity.WFC.Runtime;

    [CustomEditor(typeof(WorldGenConfig))]
    public class WorldGenConfigDrawer : Editor
    {
        WorldGenConfig SO;
        Vector2 _scrollPosition;
        string[] _tileNames;
        int _oldTileNamesCount;

        SerializedProperty _selectedCellData;

        void OnEnable()
        {
            SO = (WorldGenConfig)target;
            UpdateTileNames();
            _oldTileNamesCount = _tileNames.Length;
        }

        public override void OnInspectorGUI()
        {
            SO = (WorldGenConfig)target;
            EditorGUI.BeginChangeCheck();
            DrawGridColumnsField();
            EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();
            SerializedProperty apply = serializedObject.FindProperty("_apply");
            EditorGUILayout.PropertyField(apply);
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

            if (SO.Grid == null || SO.Grid.GetHorizontalLength() != SO.Columns)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            SerializedProperty availableTilesProp = DrawAvailableTiles();
            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("changed tiles");
                LimitSocketLength(availableTilesProp);
                serializedObject.ApplyModifiedProperties();

                UpdateTileNames();
                int newCount = _tileNames.Length;
                if (_oldTileNamesCount != newCount)
                {
                    _oldTileNamesCount = newCount;
                    serializedObject.ApplyModifiedProperties();
                    SO.DestroyMap();
                    SaveObject();
                }
                return;
            }

            DrawSeedField();
            DrawSocketCountField();

            if (_selectedCellData != null)
            {
                EdtrUtil.DrawPolymorphicField<CellData>(_selectedCellData);
            }

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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("drawGrid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("drawSockets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("drawNames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowHollowTiles"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SocketColors"));

            if (SO.drawGrid)
                DrawGrid();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        void UpdateTileNames()
        {
            _tileNames = SO.AvailableTiles.Where(t => t.Prefab != null).Select(t => t.Prefab.name).Prepend("None").ToArray();

        }

        void SaveObject()
        {
            Debug.Log("Saving");
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
                    DrawMenu(x, y, _tileNames);
                    var selection = GetCellProperty(x, y);
                    _selectedCellData = selection.FindPropertyRelative("CellData");
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

        void DrawMenu(int x, int y, string[] tileNames)
        {
            GenericMenu menu = new();
            for (int i = 0; i < tileNames.Length; i++) AddMenuItem(menu, tileNames[i], i, x, y);
            menu.ShowAsContext();
        }

        // a method to simplify adding menu items
        void AddMenuItem(GenericMenu menu, string menuPath, int popUpIndex, int x, int y)
        {
            // the menu item is marked as selected if it matches the current popUpIndex
            menu.AddItem(new GUIContent(menuPath), SO.Grid[y, x].PopUpIndex == popUpIndex, OnMenuButtonPress, new MenuData(x, y, popUpIndex));
        }

        void OnMenuButtonPress(object menuDataObj)
        {
            Undo.RecordObject(SO, "AddedItem");

            MenuData data = (MenuData)menuDataObj;
            GridCell newCell = new()
            {
                PopUpIndex = data.PopUpIndex,
                tile = data.PopUpIndex == 0 ? null : SO.AvailableTiles[data.PopUpIndex - 1].Clone(),
                X = data.X,
                Y = data.Y,
            };
            SO.Grid[data.Y, data.X] = newCell;
            SaveObject();

        }

        void ColorTiles(int x, int y)
        {
            Tile tile = SO.Grid[y, x].tile;
            //showing if selected tiles have valid connections
            if (!IsEmpty(tile))
            {
                if (!AllValidConnections(x, y, tile, SO.Grid))
                    GUI.backgroundColor = Color.red;
                else if (tile.Manual)
                    GUI.backgroundColor = Color.blue;
                else
                    GUI.backgroundColor = Color.green;
            }
            else
                GUI.backgroundColor = new Color(0.76f, 0.76f, 0.76f); ;//default color
        }

        void DrawSeedField()
        {
            SerializedProperty seedProp = serializedObject.FindProperty("_seed");
            seedProp.intValue = Mathf.Clamp(seedProp.intValue, 1, 20);
            EditorGUILayout.PropertyField(seedProp);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_framed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_framingTileIndex"));

        }

        void DrawGridColumnsField()
        {
            SerializedProperty columnsProp = serializedObject.FindProperty("Columns");
            columnsProp.intValue = Mathf.Clamp(columnsProp.intValue, 1, 50);
            EditorGUILayout.PropertyField(columnsProp);
        }

        void DrawSocketCountField()
        {
            SerializedProperty socketCountProp = serializedObject.FindProperty("SocketsCount");
            socketCountProp.intValue = Mathf.Clamp(socketCountProp.intValue, 1, 5);
            EditorGUILayout.PropertyField(socketCountProp);
        }

        SerializedProperty DrawAvailableTiles()
        {
            SerializedProperty availableTiles = serializedObject.FindProperty("AvailableTiles");
            EditorGUILayout.PropertyField(availableTiles, true);
            return availableTiles;
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
                return !IsEmpty(otherTile) && !currentTile.CanConnect(otherTile, dir);
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

        bool IsEmpty(Tile tile)
        {
            return tile.Prefab == null;
        }

        /// <summary>
        /// Sets the string from the inspector to be length of allowed socket length (if string is bigger than allowed)
        /// </summary>
        /// <param name="tilesListProperty"></param>
        void LimitSocketLength(SerializedProperty tilesListProperty)
        {
            for (int i = 0; i < tilesListProperty.arraySize; i++)
            {
                SerializedProperty edges = tilesListProperty
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_sockets")
                    .FindPropertyRelative("_edges");
                for (int j = 0; j < edges.arraySize; j++)
                {
                    SerializedProperty edge = edges.GetArrayElementAtIndex(j);
                    edge.stringValue = LimitLength(SO.SocketsCount, edge.stringValue);
                }
            }
        }

        string LimitLength(int length, string text)
        {
            if (text.Length > length)
                return text[..length];
            return text;
        }

        class MenuData
        {
            //public readonly SerializedProperty GridCellProp;
            public readonly int X;
            public readonly int Y;
            public readonly int PopUpIndex;

            public MenuData(/*SerializedProperty cellProp,*/int x, int y, int popUpIndex)
            {
                X = x;
                Y = y;
                //GridCellProp = cellProp;
                PopUpIndex = popUpIndex;
            }
        }
    }
}