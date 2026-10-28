public static class Level8Layout
{
    public const int Width = 22;
    public const int Height = 14;
    public const int RequiredReds = 2;
    public const int TotalReds = 3;
    public const string StageName = "THE SPIRE";
    public const string TutorialHint = "Watch the Spears!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 8,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel8,
        Rows = Rows,
    };

    public static readonly string[] Rows =
    {
        "#################EE###",
        "#................r.#.#",
        "#.################.#.#",
        "#.#....K.........#.#.#",
        "#.#.##########..d#.#.#",
        "#.#.#..!.....#..d#.#.#",
        "#.#.#........#..r#.#.#",
        "#.#.#....!...#....#.##",
        "#.#.##########....#.##",
        "#.#......d.........#.#",
        "#.######..dd.......#.#",
        "#.P.....dd.........#.#",
        "#..................#.#",
        "######################",
    };
}
