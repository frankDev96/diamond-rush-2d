public static class Level3Layout
{
    public const int Width = 20;
    public const int Height = 14;
    public const int RequiredReds = 2;
    public const int TotalReds = 3;
    public const string StageName = "THE JUNGLE";
    public const string TutorialHint = "Avoid Snakes!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 3,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel3,
        Rows = Rows,
        Snakes = new[]
        {
            new SnakePatrolSpec { X = 10, Y = 6, Dx = 1, Dy = 0, Length = 4 },
            new SnakePatrolSpec { X = 6, Y = 9, Dx = 0, Dy = -1, Length = 3 },
        },
    };

    public static readonly string[] Rows =
    {
        "##############EE####",
        "#..............r..#",
        "#.##############..#",
        "#.#............d..#",
        "#.#.##########.d..#",
        "#.#.#........#....#",
        "#.#.#....S...#..r.#",
        "#.#.#........#....#",
        "#.#.##########....#",
        "#.#....S...d......#",
        "#.######..dd......#",
        "#.P.....dd........#",
        "#.................#",
        "####################",
    };
}
