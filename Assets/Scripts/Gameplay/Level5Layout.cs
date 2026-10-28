public static class Level5Layout
{
    public const int Width = 22;
    public const int Height = 14;
    public const int RequiredReds = 2;
    public const int TotalReds = 3;
    public const string StageName = "THE RIVER";
    public const string TutorialHint = "Open Treasure Chests!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 5,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel5,
        Rows = Rows,
    };

    public static readonly string[] Rows =
    {
        "#################EE###",
        "#................r.#.#",
        "#.################.#.#",
        "#.#....T.........#.#.#",
        "#.#.##########..d#.#.#",
        "#.#.#........#..d#.#.#",
        "#.#.#..g.....#..r#.#.#",
        "#.#.#........#....#.##",
        "#.#.##########....#.##",
        "#.#......d.........#.#",
        "#.######..dd.......#.#",
        "#.P.....dd.........#.#",
        "#..................#.#",
        "######################",
    };
}
