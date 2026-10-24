public struct GameplayLevelSpec
{
    public int LevelIndex;
    public int Width;
    public int Height;
    public int RequiredReds;
    public int TotalReds;
    public string StageName;
    public string TutorialHint;
    public string SceneName;
    public string[] Rows;
    public SnakePatrolSpec[] Snakes;
    public int ExtraReds => TotalReds - RequiredReds;
}

public struct SnakePatrolSpec
{
    public int X;
    public int Y;
    public int Dx;
    public int Dy;
    public int Length;
}
