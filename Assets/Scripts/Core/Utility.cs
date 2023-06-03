using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;

public static class Utility
{
    // Weighted Random
    public static int WeightedSystemRandom(float[] Weights, System.Random prng)
    {
        float randomValue = prng.Next(0, (int)Weights.Sum());
        float currentValue = 0;
        for (int i = 0; i < Weights.Length; i++)
        {
            if (currentValue + Weights[i] < randomValue)
                currentValue += Weights[i];
            else
                return i;
        }
        return 0;
    }
    public static int WeightedRandom(float[] Weights)
    {
        float randomValue = UnityEngine.Random.Range(0, Weights.Sum());
        float currentValue = 0;
        for (int i = 0; i < Weights.Length; i++)
        {
            if (currentValue + Weights[i] < randomValue)
                currentValue += Weights[i];
            else
                return i;
        }
        return 0;
    }
    // Lerp
    public static float LerpBetween4Values(Vector2 vec, float TL, float TR, float BL, float BR)
    {
        float v1 = Mathf.Lerp(BL, BR, vec.x);
        float v2 = Mathf.Lerp(TL, TR, vec.x);
        float O = Mathf.Lerp(v1, v2, vec.y);
        return O;
    }

    // Rotation
    public static float RotateTowards(float CurrentAngle, float TargetAngle, float Speed)
    {
        CurrentAngle = CurrentAngle.ClampToAngle();
        TargetAngle = TargetAngle.ClampToAngle();
        // Calculate the two rotation posibilities
        float Angle1 = Mathf.Abs(TargetAngle - CurrentAngle);
        float Angle2 = 360f - Angle1;
        // Test if One of the Angles is in Range
        if (Mathf.Min(Angle1, Angle2) < Speed)
            return TargetAngle;
        // Test for the better Angle
        if (Angle1 < Angle2) // Rotate towards target directly
        {
            if (TargetAngle < CurrentAngle)
                CurrentAngle -= Speed;
            else
                CurrentAngle += Speed;
        }
        else // Rotate towards target by overturning
        {
            if (TargetAngle < CurrentAngle)
                CurrentAngle += Speed;
            else
                CurrentAngle -= Speed;
        }
        CurrentAngle = CurrentAngle.ClampToAngle();
        return CurrentAngle;
    }
    public static float RotateLerp(float CurrentAngle, float TargetAngle, float t)
    {
        CurrentAngle = CurrentAngle.ClampToAngle();
        TargetAngle = TargetAngle.ClampToAngle();
        t = Mathf.Clamp01(t);
        // Calculate the two rotation posibilities
        float Angle1 = Mathf.Abs(TargetAngle - CurrentAngle);
        float Angle2 = 360f - Angle1;
        // Test for the better Angle
        if (Angle1 < Angle2) // Rotate towards target directly
            return Mathf.Lerp(CurrentAngle, TargetAngle, t);
        else // Rotate towards target by overturning
        {
            float tInDegrees = t * Angle2;
            if (CurrentAngle > TargetAngle)
            {
                if (tInDegrees <= 360f - CurrentAngle)
                    return Mathf.Lerp(CurrentAngle, 360f + TargetAngle, t);
                else
                    return Mathf.Lerp(CurrentAngle - 360f, TargetAngle, t);
            }
            else
            {
                if (tInDegrees < CurrentAngle)
                    return Mathf.Lerp(CurrentAngle, TargetAngle - 360f, t);
                else
                    return Mathf.Lerp(360f + CurrentAngle, TargetAngle, t);
            }
        }
    }
    public static float RotateLerpUnclamped(float CurrentAngle, float TargetAngle, float t)
    {
        CurrentAngle = CurrentAngle.ClampToAngle();
        TargetAngle = TargetAngle.ClampToAngle();
        // Calculate the two rotation posibilities
        float Angle1 = Mathf.Abs(TargetAngle - CurrentAngle);
        float Angle2 = 360f - Angle1;
        // Test for the better Angle
        if (Angle1 < Angle2) // Rotate towards target directly
            return Mathf.LerpUnclamped(CurrentAngle, TargetAngle, t).ClampToAngle();
        else // Rotate towards target by overturning
        {
            float tInDegrees = t * Angle2;
            if (CurrentAngle > TargetAngle)
            {
                if (tInDegrees < 360f - CurrentAngle)
                    return Mathf.LerpUnclamped(CurrentAngle, 360f + TargetAngle, t).ClampToAngle();
                else
                    return Mathf.LerpUnclamped(CurrentAngle - 360f, TargetAngle, t).ClampToAngle();
            }
            else
            {
                if (tInDegrees < CurrentAngle)
                    return Mathf.LerpUnclamped(CurrentAngle, TargetAngle - 360f, t).ClampToAngle();
                else
                    return Mathf.LerpUnclamped(360f + CurrentAngle, TargetAngle, t).ClampToAngle();
            }
        }
    }
    public static Vector2 Vector2RotateLerp(Vector2 CurrentAngle, Vector2 TargetAngle, float t)
    {
        return new Vector2(RotateLerp(CurrentAngle.x, TargetAngle.x, t), RotateLerp(CurrentAngle.y, TargetAngle.y, t));
    }
    public static Vector2 Vector2RotateLerpUnclamped(Vector2 CurrentAngle, Vector2 TargetAngle, float t)
    {
        return new Vector2(RotateLerpUnclamped(CurrentAngle.x, TargetAngle.x, t), RotateLerpUnclamped(CurrentAngle.y, TargetAngle.y, t));
    }

    // NormalCalculation
    public static float CalcualteSteepnessRadientFromNormal(Vector3 normal)
	{
		return Mathf.Acos(Vector3.Dot(normal.normalized, Vector3.up));
	}

    // Enum stuff
    public static T[] GetEnumValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToArray();
    }

#if UNITY_EDITOR
    // Find assets
    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        List<T> assets = new();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        for( int i = 0; i < guids.Length; i++ )
        {
            string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
            T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );
            if( asset != null )
                assets.Add(asset);
        }
        return assets;
    }
#endif
    // Math stuff (Frac)
    public static float Frac(float x)
    {
        return x - Mathf.FloorToInt(x);
    }
    public static Vector2 Frac(Vector2 vec)
    {
        return new Vector2(vec.x - Mathf.FloorToInt(vec.x), vec.y - Mathf.FloorToInt(vec.y));
    }
    public static Vector2 FloorVector(Vector2 vec)
    {
        return new Vector2(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
    }
    public static Vector2 Modulo(Vector2 vec, Vector2 vec2)
    {
        return new Vector2(vec.x % vec2.x, vec.y % vec2.y);
    }
    // Rounding
    public static float Round(float value, float step) => Mathf.Round(value / step) * step;
    public static float Floor(float value, float step) => Mathf.FloorToInt(value / step) * step;
    //Autocreating ID from name
    public static string CreateID(string Name)
    {
        return string.Concat(Name.Trim()
                    .Select(c => char.IsWhiteSpace(c) ? '_' : c)
                    .Select(c => char.ToLower(c)));
    }
    // Wait For Frames
    public static IEnumerator WaitForFrames(int frameCount, Action callback)
    {
        while (frameCount > 0)
        {
            frameCount--;
            yield return null;
        }
        callback?.Invoke();
    }
    public static IEnumerator WaitForPhysicsUpdate(Action callback)
    {
        yield return new WaitForSeconds(Time.fixedDeltaTime);
        callback?.Invoke();
    }
}
