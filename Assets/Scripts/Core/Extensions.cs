using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public static class Extensions
{
    public static bool AsBool(this float value) {
        return Mathf.Approximately(Mathf.Min(value, 1), 1);
    }

    public static float ClampToAngle(this float value) {
        while (value < 0)
            value += 360;
        while (value > 360)
            value -= 360;
        return value;
    }

    public static void ClearChilds(this GameObject GO)
    {
        foreach (Transform child in GO.transform) {
            GameObject.Destroy(child.gameObject);
        }
    }
    
    public static void Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;

    }
    
    // Invoke for lambdas
    public static void Invoke(this MonoBehaviour mb, Action f, float delay)
    {
        mb.StartCoroutine(InvokeRoutine(f, delay));
    }
    private static IEnumerator InvokeRoutine(Action f, float delay)
    {
        yield return new WaitForSeconds(delay);
        f();
    }

    
    // Arrays
    public static void InsertArray2D<T>(this T[,] array, T[,] secondArray, Vector2Int pos)
    {
        for (int x = 0; x < secondArray.GetLength(0); x++)
            for (int y = 0; y < secondArray.GetLength(1); y++)
                array[x + pos.x, y + pos.y] = secondArray[x,y];
    }

    public static T[,] GetArrayPart2D<T>(this T[,] array, Vector2Int pos, Vector2Int lengths)
    {
        T[,] newArray = new T[lengths.x,lengths.y];
        for (int x = 0; x < lengths.x; x++)
            for (int y = 0; y < lengths.y; y++)
                    newArray[x,y] = array[x+pos.x,y+pos.y];
        return newArray;
    }

    public static void AddArray2D<T>(this T[,] array, T[,] secondArray)
    {
        for (int x = 0; x < secondArray.GetLength(0); x++)
            for (int y = 0; y < secondArray.GetLength(1); y++)
                if (secondArray[x,y] != null)
                    array[x,y] = secondArray[x,y];
    }
    // More efficient way to rotate Vector2s (without quaternions)
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
         
        float tx = v.x;
        float ty = v.y;
 
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    // Vector2Int cast
    public static Vector2Int CastToInt(this Vector2 vec) => new((int)vec.x, (int)vec.y);
    public static Vector2Int RoundToInt(this Vector2 vec) => new(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));

    // Vector2 topdown
    public static Vector2 XZ(this Vector3 vec) => new(vec.x, vec.z);

    // Vector2 to Vector3 with height
    public static Vector3 AddHeight(this Vector2 vec, float height) => new(vec.x, height, vec.y);

    // Change y of Vector3
    public static Vector3 WithHeight(this Vector3 vec, float height) => new(vec.x, height, vec.z);

    // AddComponent in smart
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (PropertyInfo pinfo in pinfos) {
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }
    public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
    {
        return go.AddComponent<T>().GetCopyOf(toAdd) as T;
    }
    // Set all layers in children
    public static void SetLayerAllChildren(this GameObject root, int layer)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform child in children)
            child.gameObject.layer = layer;
    }
    // Layer mask
    public static bool Contains(this LayerMask mask, int layer) => mask == (mask | (1 << layer));
}
