
namespace steph.Unity.WFC.Editor
{
    using NUnit.Framework;
    using steph.Unity.WFC.Runtime; // Ensure it matches your namespace
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(AvailableTiles))]
    public class AvailableTilesDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, ReorderableList> _lists = new Dictionary<string, ReorderableList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty tilesProp = property.FindPropertyRelative("_tiles");

            if (!_lists.TryGetValue(property.propertyPath, out ReorderableList list))
            {
                list = InitList(property, tilesProp, label);
                _lists[property.propertyPath] = list;
            }

            Rect socketRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            Rect listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);
            list.DoList(listRect);

            EditorGUI.EndProperty();
        }


        ReorderableList InitList(SerializedProperty property, SerializedProperty tilesProp, GUIContent label)
        {
            var list = new ReorderableList(property.serializedObject, tilesProp, true, true, true, true)
            {
                // 1. Draw the Header
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, label);
                },
                onAddCallback = (ReorderableList l) => {
                    property.serializedObject.ApplyModifiedProperties();

                    if (GetTargetObjectOfProperty(property) is AvailableTiles targetObj)
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "Add Tile");

                        targetObj.AddNew();
                        property.boxedValue = targetObj;
                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();
                    }
                },
                onRemoveCallback = (ReorderableList l) => {
                    property.serializedObject.ApplyModifiedProperties();

                    if (GetTargetObjectOfProperty(property) is AvailableTiles targetObj && 
                        l.index >= 0 && l.index < l.serializedProperty.arraySize)
                    {
                        targetObj.RemoveAt(l.index); 
                        property.boxedValue = targetObj;
                        property.serializedObject.Update();
                    }
                }
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2; // Slight padding
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, true);
            };
            list.elementHeightCallback = (int index) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4;
            };
            return list;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //Height of the _socketLength property
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Add height of the list
            if (_lists.TryGetValue(property.propertyPath, out ReorderableList list))
                height += list.GetHeight();
            else
                height += EditorGUIUtility.singleLineHeight * 3; // Fallback estimate

            return height;
        }

        private object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            return prop.boxedValue;
        }
    }
}
