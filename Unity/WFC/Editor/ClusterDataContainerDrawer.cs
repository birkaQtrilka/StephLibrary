using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace steph.Unity.WFC.Editor
{

    [CustomPropertyDrawer(typeof(ClusterDataContainer))]
    public class ClusterDataContainerDrawer : PropertyDrawer
    {
        private static Dictionary<int, bool> foldouts = new ();
        string searchInput;
    
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //ClusterDataContainer target = GetInstance<ClusterDataContainer>(fieldInfo, property);

            SerializedProperty array = property.FindPropertyRelative("_clusters");
            if (array == null || array.arraySize == 0)
            {
                EditorGUILayout.LabelField("No cluster formed yet");
                return;
            }
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("DeleteSearch"))
            {
                searchInput = string.Empty;
            }
            searchInput = GUILayout.TextField(searchInput);
            GUILayout.EndHorizontal();
            if (searchInput == string.Empty)
                ShowFullList(array);
            else
                FindCluster(array);
        }

        void FindCluster(SerializedProperty array)
        {
            if (!(int.TryParse(searchInput, out int result) && BuildSpace.TryGetCluster(result, out Cluster cluster))) return;

            if (GUILayout.Button("Select"))
            {
                GizmosDrawer.Instance.PersistentCall = () => cluster.DrawCells(0);
                SceneView.lastActiveSceneView.LookAt(cluster.GetDrawnCenter() + Vector3.up * 5);
            }
            SerializedProperty data = null;
            int i = 0;
            for (; i < array.arraySize; i++)
            {
                SerializedProperty arrayElement = array.GetArrayElementAtIndex(i);
                if(arrayElement.FindPropertyRelative("id").intValue == result)
                    data = arrayElement.FindPropertyRelative("Data");
            }
            if (data == null) return;

            EditorGUILayout.PropertyField(data);

            var obj = data.objectReferenceValue;
            if (obj == null) return;
            int foldoutKey = i;

            if (!foldouts.ContainsKey(foldoutKey))
                foldouts.Add(foldoutKey, false);

            bool foldout = EditorGUILayout.Foldout(foldouts[foldoutKey], "Show Data Contents", true);
            foldouts[foldoutKey] = foldout;
            if (!foldout) return;
            EditorGUI.indentLevel++;

            // Create a serialized object from the ScriptableObject
            SerializedObject dataSO = new(data.objectReferenceValue);
            SerializedProperty prop = dataSO.GetIterator();

            // Needed to start iterating
            prop.NextVisible(true);

            while (prop.NextVisible(false))
            {
                EditorGUILayout.PropertyField(prop, true);
            }

            dataSO.ApplyModifiedProperties();

            EditorGUI.indentLevel--;
        }

        void ShowFullList(SerializedProperty array)
        {
            int testCount = Mathf.Min(5, array.arraySize);
            for (int i = 0; i < testCount; ++i)
            {
                SerializedProperty arrayElement = array.GetArrayElementAtIndex(i);
                if (GUILayout.Button("Select"))
                {
                    Cluster cluster = BuildSpace.GetCluster(arrayElement.FindPropertyRelative("id").intValue);
                    GizmosDrawer.Instance.PersistentCall = () => cluster.DrawCells(0);
                    SceneView.lastActiveSceneView.LookAt(cluster.GetDrawnCenter() + Vector3.up * 5);
                }
                var data = arrayElement.FindPropertyRelative("Data");
                EditorGUILayout.PropertyField(data);
                // Store foldout state per-object

                var obj = data.objectReferenceValue;
                if (obj == null) continue;
                int foldoutKey = i;

                if (!foldouts.ContainsKey(foldoutKey))
                    foldouts.Add(foldoutKey, false);

                bool foldout = EditorGUILayout.Foldout(foldouts[foldoutKey], "Show Data Contents", true);
                foldouts[foldoutKey] = foldout;
                if (!foldout) continue;
                EditorGUI.indentLevel++;

                // Create a serialized object from the ScriptableObject
                SerializedObject dataSO = new(data.objectReferenceValue);
                SerializedProperty prop = dataSO.GetIterator();

                // Needed to start iterating
                prop.NextVisible(true);

                while (prop.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }

                dataSO.ApplyModifiedProperties();

                EditorGUI.indentLevel--;

            }
        }

        public static T GetInstance<T>(FieldInfo fieldInfo, SerializedProperty property) where T : class
        {
            var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj == null) { return null; }

            T actualObject = null;
            if (obj.GetType().IsArray)
            {
                var index = Convert.ToInt32(new string(property.propertyPath.Where(c => char.IsDigit(c)).ToArray()));
                actualObject = ((T[])obj)[index];
            }
            else
            {
                actualObject = obj as T;
            }
            return actualObject;
        }
    }
}
