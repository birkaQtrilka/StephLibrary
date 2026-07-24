    using System.Collections.Generic;
    using System.Linq;
    using System;
    using UnityEngine;
    using UnityEditor;
    using System.Reflection;
using System.IO;

namespace steph.Unity.WFC.Editor
{

    public static class SerializedPropertyExtensions
    {

        static ScriptableObject GetRootScriptableObject(this SerializedProperty property)
        {
            return (ScriptableObject)property.serializedObject.targetObject;
        }

        /// <summary>
        /// You can change its values without troubles
        /// </summary>
        public static T GetValueFromScriptableObject<T>(this SerializedProperty property)
        {
            return GetNestedObject<T>(property.propertyPath.Split('.'), property.GetRootScriptableObject());
        }

        /// <summary>
        /// Use this to change the property itself. Not the values of the property
        /// </summary>
        public static bool SetValueOnScriptableObject<T>(this SerializedProperty property, T value)
        {
            //the property only gives you the path, but you need the path relative to the root object, so I'm getting it here
            //right now it's hard coded to expect an array at the end because I only wanted to see if it even works
            //is there a way to save custom classes? 
            string[] fieldStructure = property.propertyPath.Split('.');
            object obj = property.GetRootScriptableObject();
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            Array array = null;
        
            int index = -1;

            for (int i = 0; i < fieldStructure.Length; i++)
            {
                string fieldName = fieldStructure[i];

                FieldInfo field = obj.GetType().GetField(fieldName, bindings);
                if (!field.FieldType.IsArray)
                {
                    obj = field.GetValue(obj);
                    continue;
                }
                i += 2;
                string stringIndex = fieldStructure[i].Split('[', ']')[1];
                index = int.Parse(stringIndex);

                array = (Array)field.GetValue(obj);
                obj = array.GetValue(index);
            }
            array.SetValue(value, index);
            return true;
        }

        /// <summary>
        /// Iterates through objects to handle objects that are nested in the root object
        /// </summary>
        static T GetNestedObject<T>(string[] path, object obj)
        {
            BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            for (int i = 0; i < path.Length; i++)
            {
                string fieldName = path[i];

                FieldInfo field = obj.GetType().GetField(fieldName, bindings);
                if (!field.FieldType.IsArray)
                {
                    obj = field.GetValue(obj);
                    continue;
                }
                i += 2;
                Array array = (Array)field.GetValue(obj);
                string stringIndex = path[i].Split('[', ']')[1];
                int index = int.Parse(stringIndex);
                obj = array.GetValue(index);
            }
            return (T)obj;
        }

        static bool SetFieldOrPropertyValue(string fieldName, object obj, object value, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                field.SetValue(obj, value);
                return true;
            }

            PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
            if (property != null)
            {
                property.SetValue(obj, value, null);
                return true;
            }

            return false;
        }

    
    }
}
