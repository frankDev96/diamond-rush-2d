public static class Level7Layout
{
    public const int Width = 22;
    public const int Height = 14;
    public const int RequiredReds = 2;
    public const int TotalReds = 3;
    public const string StageName = "THE TOMB";
    public const string TutorialHint = "Block the Flames!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 7,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel7,
        Rows = Rows,
    };

    public static readonly string[] Rows =
    {
        "################EE##",
        "#................r.#",
        "#.################.#",
        "#.#....K.........#.#",
        "#.#.##########..d#.#",
        "#.#.#..F.....#..d#.#",
        "#.#.#..B.....#..r#.#",
        "#.#.#....!...#....#.#",
        "#.#.##########....#.#",
        "#.#......d.........#.#",
        "#.######..dd.......#.#",
        "#.P.....dd.........#.#",
        "#..................#.#",
        "######################",
    };
}
