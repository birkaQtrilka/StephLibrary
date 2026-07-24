using steph.Unity.WFC.Runtime;
using UnityEditor;
using UnityEngine;

namespace steph.Unity.WFC.Editor
{

    [CustomPropertyDrawer(typeof(Sockets))]
    public class SocketsPropertyDrawer : PropertyDrawer
    {
        private static readonly float[] _rotations = { 0f, 90f, 180f, 270f };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int requiredLength = property.FindPropertyRelative("socketLength").intValue;
            return CalcSquareSide(requiredLength) + EditorGUIUtility.singleLineHeight * 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty array = property.FindPropertyRelative("_edges");
            int requiredLength = property.FindPropertyRelative("socketLength").intValue;

            if (array.arraySize != 4) array.arraySize = 4;
            EditorGUI.BeginProperty(position, label, property);
            DrawTextFields(position, array, requiredLength);

            EditorGUI.EndProperty();
            property.serializedObject.ApplyModifiedProperties(); // Apply changes to the serialized object
        }

        float CalcSquareSide(int requiredLength)
        {
            GUIStyle style = EditorStyles.textField;
            return style.CalcSize(new GUIContent(new string('w', Mathf.Max(requiredLength, 1)))).x + 10;
        }

        void DrawTextFields(Rect position, SerializedProperty array, int requiredLength)
        {
            float thickness = EditorGUIUtility.singleLineHeight;
            float fieldLength = CalcSquareSide(requiredLength);
            float outerSide = fieldLength + thickness;

            // Center point of each edge field, relative to the property's top-left corner.
            // Up/Down are horizontal bars, Right/Left are vertical bars (pre-rotation they're
            // all drawn as horizontal rects of size (fieldLength, thickness), then rotated in
            // place). Each field is inset by CornerGap from both corners of its edge so it
            // sits centered on that edge with empty space at the corners.
            Vector2[] centers = new Vector2[4];
            centers[0] = new Vector2(position.x + fieldLength * 0.5f, position.y + thickness * 0.5f);              // Up
            centers[1] = new Vector2(position.x + outerSide - thickness * 0.5f, position.y  + fieldLength * 0.5f);  // Right
            centers[2] = new Vector2(position.x + thickness + fieldLength * 0.5f, position.y + outerSide - thickness * 0.5f);  // Down
            centers[3] = new Vector2(position.x + thickness * 0.5f, position.y + thickness + fieldLength * 0.5f);              // Left

            Matrix4x4 matrixBackup = GUI.matrix;

            for (int i = 0; i < 4; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);

                Rect fieldRect = new Rect(
                    centers[i].x - fieldLength * 0.5f,
                    centers[i].y - thickness * 0.5f,
                    fieldLength,
                    thickness);

                GUIUtility.RotateAroundPivot(_rotations[i], centers[i]);

                string input = EditorGUI.TextField(fieldRect, element.stringValue);

                GUI.matrix = matrixBackup;

                if (input.Length > requiredLength)
                {
                    input = input[..requiredLength].ToLower();
                }
                element.stringValue = input;
            }
        }
    }
}