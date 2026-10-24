public static class Level6Layout
{
    public const int Width = 22;
    public const int Height = 14;
    public const int RequiredReds = 3;
    public const int TotalReds = 4;
    public const string StageName = "THE MARKET";
    public const string TutorialHint = "Find Secret Chests!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 6,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel6,
        Rows = Rows,
    };

    public static readonly string[] Rows =
    {
        "################EE##",
        "#................r.#",
        "#.################.#",
        "#.#....T....t....#.#",
        "#.#.##########..d#.#",
        "#.#.#........#..d#.#",
        "#.#.#..g..r..#..r#.#",
        "#.#.#........#....#.#",
        "#.#.##########....#.#",
        "#.#......d.........#.#",
        "#.######..dd.......#.#",
        "#.P.....dd.........#.#",
        "#..................#.#",
        "######################",
    };
}
