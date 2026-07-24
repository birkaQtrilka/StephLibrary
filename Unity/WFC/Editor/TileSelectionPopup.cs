namespace steph.Unity.WFC.Editor
{
    using steph.Unity.Editor;
    using steph.Unity.WFC.Runtime;
    using System;
    using UnityEditor;
    using UnityEngine;

    public class TileSelectionPopup : PopupWindowContent
    {
        private readonly string[] _tileNames;
        private readonly SerializedProperty _cellDataProp;
        private readonly Tile _tile;
        private readonly Action<int> _onTileSelected;

        private Vector2 _scrollPosition;

        public TileSelectionPopup(string[] tileNames, SerializedProperty cellDataProp, Tile tile, Action<int> onTileSelected)
        {
            _tileNames = tileNames;
            _cellDataProp = cellDataProp;
            _onTileSelected = onTileSelected;
            _tile = tile;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(250, 350);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(5);

            if (_cellDataProp != null)
            {
                EditorGUILayout.LabelField("Cell Data", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                EdtrUtil.DrawPolymorphicField<CellData>(_cellDataProp);
                if(EditorGUI.EndChangeCheck())
                {
                    _cellDataProp.serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.Space();
                DrawUILine(Color.gray);
                EditorGUILayout.Space();
            }

            if(GUILayout.Button("Print tile data") )
            {
                Debug.Log(_tile.ToString());
            }

            EditorGUILayout.LabelField("Available Tiles", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _tileNames.Length; i++)
            {
                if (GUILayout.Button(_tileNames[i], EditorStyles.miniButton))
                {
                    // Trigger the method in your main script, then close the window
                    _onTileSelected?.Invoke(i);
                    editorWindow.Close();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // Optional helper to draw a dividing line
        private void DrawUILine(Color color, int thickness = 1, int padding = 5)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2f;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}
