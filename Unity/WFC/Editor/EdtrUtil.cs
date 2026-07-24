using System;
using System.Linq;
using UnityEditor;

namespace steph.Unity.Editor
{
    public class EdtrUtil
    {
        public static void DrawPolymorphicField<T>(SerializedProperty property) where T : class
        {
            var types = TypeCache.GetTypesDerivedFrom<T>()
                .Where(t => !t.IsAbstract)
                .ToArray();

            string[] typeNames = types.Select(t => t.Name)
                .Prepend("None")
                .ToArray();

            string currentTypeName = property.managedReferenceValue?.GetType().Name;
            int currentIndex = currentTypeName == null ? 0
                : Array.FindIndex(types, t => t.Name == currentTypeName) + 1;

            int selected = EditorGUILayout.Popup(property.displayName, currentIndex, typeNames);

            if (selected != currentIndex)
            {
                property.managedReferenceValue = selected == 0
                    ? null
                    : Activator.CreateInstance(types[selected - 1]);
            }

            EditorGUILayout.PropertyField(property, true);
        }
    }
}
