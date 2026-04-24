using UnityEngine;

public enum BlockState
{
    Standing,
    Horizontal,
    Vertical,
}

public static class BlockSettings
{
    public const float StandingY = 1.5f;
    public const float LyingY = 1.0f;
    public const float RayLength = 3f;
    public const float WinDropSpeed = 5f;
    public const float WinDropDelay = 0.7f;
}
