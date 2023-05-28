using UnityEngine;

public static class ExtensionMethods
{
    public static float Lerp(float from, float to, float t)
    {
        t = Mathf.Clamp01(t);
        return from + (to - from) * t;
    }

    public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        // Calculate the percentage of the value within the original range
        var percentage = (value - fromMin) / (fromMax - fromMin);

        // Map the percentage to the new range
        var mappedValue = percentage * (toMax - toMin) + toMin;

        return mappedValue;
    }
}
