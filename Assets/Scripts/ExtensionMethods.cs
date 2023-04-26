using UnityEngine;

public static class ExtensionMethods
{
    public static float Lerp(float from, float to, float t)
    {
        t = Mathf.Clamp01(t);
        return from + (to - from) * t;
    }
}
