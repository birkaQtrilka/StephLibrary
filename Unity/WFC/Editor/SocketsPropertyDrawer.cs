using UnityEditor;
using UnityEngine;

namespace steph.Unity.WFC.Editor
{

    [CustomPropertyDrawer(typeof(Sockets))]
    public class SocketsPropertyDrawer : PropertyDrawer
    {
        string[] _labels = new string[4] { "Up", "Right", "Down", "Left" }; 

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight*4;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty array = property.FindPropertyRelative("_edges");

            if (array.arraySize != 4) array.arraySize = 4;
            EditorGUI.BeginProperty(position, label, property);
            // Retrieve the array element value, display it in a TextField, and assign the edited text back
            DrawTextFields(position, array);
        
            EditorGUI.EndProperty();
            property.serializedObject.ApplyModifiedProperties(); // Apply changes to the serialized object

        }
    
        void DrawTextFields(Rect position, SerializedProperty array)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            for (int i = 0; i < _labels.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                Rect textArea = new Rect(position.x, position.y + lineHeight * i, position.width, lineHeight);
                element.stringValue = EditorGUI.TextField(textArea, _labels[i], element.stringValue);
            }
        }
    }
}
