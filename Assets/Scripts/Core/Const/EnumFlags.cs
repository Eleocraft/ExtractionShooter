using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace ExoplanetStudios
{
    [Serializable]
    public class EnumFlags
    {
        [SerializeField]
        private int flags;
        public List<T> GetValues<T>() where T : Enum
        {
            List<T> list = new();
            foreach (T enumValue in Enum.GetValues(typeof(T)))
                if (Contains(enumValue))
                    list.Add(enumValue);
            return list;
        }
        public bool Contains<T>(T value) where T : Enum => flags == (flags | 1 << Convert.ToInt32(value));

        #if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(EnumFlags))]
        public class EnumFlagsPropertyDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty flags = property.FindPropertyRelative(nameof(flags));

                EnumTypeAttribute attribute = fieldInfo.GetCustomAttribute<EnumTypeAttribute>();

                if (attribute == null)
                    throw new Exception("no attribute");

                EditorGUI.BeginProperty(position, label, property);
                {
                    flags.intValue = EditorGUI.MaskField(position, label, flags.intValue, Enum.GetNames(attribute.enumType));
                }
                EditorGUI.EndProperty();
            }
        }
        #endif
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class EnumTypeAttribute : Attribute
    {
        public readonly Type enumType;
        public EnumTypeAttribute(Type enumType)
        {
            this.enumType = enumType;
        }
    }
}