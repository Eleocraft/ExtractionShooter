using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace ExoplanetStudios
{
    [Serializable]
    public class RangeSlider
    {
        [SerializeField] private float startValue = 0;
        [SerializeField] private float endValue = 1;

    #pragma warning disable 0414
        [SerializeField] private float sliderMin = 0;
        [SerializeField] private float sliderMax = 1;
    #pragma warning restore 0414

        // Returns the value between the start- and endvalue
        // at t (between 0 and 1 --> 0 being the startvalue and 1 being the endvalue)
        public float Evaluate(float t)
        {
            return (endValue - startValue) * Mathf.Clamp01(t) + startValue;
        }

        // Returns true if value is between start and end
        // else returns false
        public bool InBounds(float value)
        {
            if (value < startValue || value > endValue)
                return true;
            return false;
        }

        // Returns a random value between the start- and endvalue
        public float RandomValue()
        {
            return UnityEngine.Random.Range(startValue, endValue);
        }

    #if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(RangeSlider))]
        public class RangeSliderDrawer : PropertyDrawer
        {
            const float fieldWidth = 50f;
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // Find the SerializedProperties by name
                SerializedProperty startValue = property.FindPropertyRelative(nameof(startValue));
                SerializedProperty endValue = property.FindPropertyRelative(nameof(endValue));

                SerializedProperty sliderMin = property.FindPropertyRelative(nameof(sliderMin));
                SerializedProperty sliderMax = property.FindPropertyRelative(nameof(sliderMax));

                MinMaxRangeAttribute attribute = fieldInfo.GetCustomAttribute<MinMaxRangeAttribute>();

                // Using BeginProperty / EndProperty on the parent property means that
                // prefab override logic works on the entire property.
                EditorGUI.BeginProperty(position, label, property);
                {
                    if (attribute != null)
                    {
                        sliderMin.floatValue = attribute.min;
                        sliderMax.floatValue = attribute.max;
                    }
                    float start = startValue.floatValue;
                    float end = endValue.floatValue;
                    // MinMaxSlider
                    Rect sliderPos = position;
                    sliderPos.width -= 2 * fieldWidth;
                    EditorGUI.MinMaxSlider(sliderPos, label, ref start, ref end, sliderMin.floatValue, sliderMax.floatValue);
                    // MinPos Field
                    Rect MinFieldPos = position;
                    MinFieldPos.width = fieldWidth;
                    MinFieldPos.x += position.width - 2 * fieldWidth;
                    start = EditorGUI.FloatField(MinFieldPos, Utility.Round(start, 0.01f));
                    // MaxPos Field
                    Rect MaxFieldPos = position;
                    MaxFieldPos.width = fieldWidth;
                    MaxFieldPos.x += position.width - fieldWidth;
                    end = EditorGUI.FloatField(MaxFieldPos, Utility.Round(end, 0.01f));

                    startValue.floatValue = start;
                    endValue.floatValue = end;
                }
                EditorGUI.EndProperty();
            }
        }
    #endif
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class MinMaxRangeAttribute : Attribute
    {
        public readonly float min;
        public readonly float max;
        public MinMaxRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}