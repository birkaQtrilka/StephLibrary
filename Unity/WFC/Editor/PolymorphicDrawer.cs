using System;
using System.Collections.Generic;
using System.Linq;
using steph.Unity.WFC.Runtime;
using UnityEditor;
using UnityEngine;

namespace steph.Unity.WFC.Editor
{
    [CustomPropertyDrawer(typeof(PolymorphicAttribute))]
    public class PolymorphicDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.PropertyField(position, property, label, true);

            Type baseType = GetBaseType();

            List<Type> types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .ToList();

            if (!baseType.IsAbstract && !baseType.IsInterface)
                types.Insert(0, baseType);

            string[] typeNames = types.Select(t => t.Name).Prepend("None").ToArray();

            string currentTypeName = property.managedReferenceValue?.GetType().Name;
            int currentIndex = currentTypeName == null ? 0 : types.FindIndex(t => t.Name == currentTypeName) + 1;

            Rect dropdownRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight
            );

            int selected = EditorGUI.Popup(dropdownRect, currentIndex, typeNames);

            if (selected != currentIndex)
            {
                property.managedReferenceValue = selected == 0 ? null : Activator.CreateInstance(types[selected - 1]);
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private Type GetBaseType()
        {
            Type type = fieldInfo.FieldType;

            if (type.IsArray) return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];

            return type;
        }
    }
}