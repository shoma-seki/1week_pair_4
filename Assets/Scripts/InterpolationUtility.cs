using UnityEngine;

public enum InterpolationType
{
    Linear,
    EaseIn,
    EaseOut,
    SmoothStep
}

public static class InterpolationUtility
{
    public static Vector3 Interpolate(
        Vector3 start,
        Vector3 end,
        float time,
        InterpolationType interpolationType)
    {
        float rate = Evaluate(Mathf.Clamp01(time), interpolationType);
        return Vector3.Lerp(start, end, rate);
    }

    public static Color Interpolate(
        Color start,
        Color end,
        float time,
        InterpolationType interpolationType)
    {
        float rate = Evaluate(Mathf.Clamp01(time), interpolationType);
        return Color.Lerp(start, end, rate);
    }

    private static float Evaluate(float time, InterpolationType interpolationType)
    {
        switch (interpolationType)
        {
            case InterpolationType.EaseIn:
                return time * time;

            case InterpolationType.EaseOut:
                return 1f - (1f - time) * (1f - time);

            case InterpolationType.SmoothStep:
                return time * time * (3f - 2f * time);

            default:
                return time;
        }
    }
}
