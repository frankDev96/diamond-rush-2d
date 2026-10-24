public static class Level4Layout
{
    public const int Width = 22;
    public const int Height = 14;
    public const int RequiredReds = 2;
    public const int TotalReds = 4;
    public const string StageName = "THE RUINS";
    public const string TutorialHint = "Crush Enemies!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 4,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel4,
        Rows = Rows,
        Snakes = new[] { new SnakePatrolSpec { X = 12, Y = 5, Dx = 1, Dy = 0, Length = 3 } },
    };

    public static readonly string[] Rows =
    {
        "################EE##",
        "#................r.#",
        "#.################.#",
        "#.#......S.......#.#",
        "#.#.##########..d#.#",
        "#.#.#........#..d#.#",
        "#.#.#..B.....#..r#.#",
        "#.#.#........#....#.#",
        "#.#.##########....#.#",
        "#.#......d.........#.#",
        "#.######..dd.......#.#",
        "#.P.....dd.........#.#",
        "#..................#.#",
        "######################",
    };
}
