using UnityEngine;

public static class BlockMath
{
    public static BlockState GetNextState(BlockState current, Vector3 dir)
    {
        bool isLateral = Mathf.Abs(dir.x) > 0.1f;
        return current switch
        {
            BlockState.Standing => isLateral ? BlockState.Horizontal : BlockState.Vertical,
            BlockState.Horizontal => isLateral ? BlockState.Standing : BlockState.Horizontal,
            _ => isLateral ? BlockState.Vertical : BlockState.Standing,
        };
    }

    public static Vector3 GetTargetPosition(
        Vector3 currentPos,
        Vector3 dir,
        BlockState current,
        BlockState next
    )
    {
        bool involvesStanding = (current == BlockState.Standing || next == BlockState.Standing);
        Vector3 target = currentPos + dir * (involvesStanding ? 1.5f : 1.0f);
        target.y = (next == BlockState.Standing) ? BlockSettings.StandingY : BlockSettings.LyingY;

        return next switch
        {
            BlockState.Standing => new Vector3(
                Mathf.Round(target.x),
                target.y,
                Mathf.Round(target.z)
            ),
            BlockState.Horizontal => new Vector3(
                Mathf.Round(target.x - 0.5f) + 0.5f,
                target.y,
                Mathf.Round(target.z)
            ),
            _ => new Vector3(Mathf.Round(target.x), target.y, Mathf.Round(target.z - 0.5f) + 0.5f),
        };
    }

    public static Vector3 GetPivot(
        Vector3 currentPos,
        Vector3 dir,
        BlockState current,
        float floorOffset
    )
    {
        bool isLateral = Mathf.Abs(dir.x) > 0.1f;
        float offset = current switch
        {
            BlockState.Standing => 0.5f,
            BlockState.Horizontal => isLateral ? 1.0f : 0.5f,
            _ => !isLateral ? 1.0f : 0.5f,
        };
        return new Vector3(currentPos.x, floorOffset, currentPos.z) + (dir * offset);
    }
}
