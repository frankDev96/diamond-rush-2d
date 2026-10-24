public static class Level9Layout
{
    public const int Width = 22;
    public const int Height = 14;
    public const int RequiredReds = 3;
    public const int TotalReds = 4;
    public const string StageName = "THE GROTTO";
    public const string TutorialHint = "Use Checkpoints!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 9,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel9,
        Rows = Rows,
        Snakes = new[] { new SnakePatrolSpec { X = 11, Y = 7, Dx = 1, Dy = 0, Length = 2 } },
    };

    public static readonly string[] Rows =
    {
        "################EE##",
        "#................r.#",
        "#.################.#",
        "#.#....K.........#.#",
        "#.#.##########..d#.#",
        "#.#.#..F.....#..d#.#",
        "#.#.#..S.....#..r#.#",
        "#.#.#....!...#....#.#",
        "#.#.##########....#.#",
        "#.#......d.........#.#",
        "#.######..dd.......#.#",
        "#.P.....dd.........#.#",
        "#..................#.#",
        "######################",
    };
}
