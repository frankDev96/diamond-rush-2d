public static class Level2Layout
{
    public const int Width = 20;
    public const int Height = 14;
    public const int RequiredReds = 2;
    public const int TotalReds = 3;
    public const string StageName = "THE COURTYARD";
    public const string TutorialHint = "Push Boulders!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 2,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel2,
        Rows = Rows,
    };

    // B boulder, d dirt, r red, EE door top-center.
    public static readonly string[] Rows =
    {
        "##############EE####",
        "#..............r..#",
        "#.##############..#",
        "#.#............d..#",
        "#.#.##########.d..#",
        "#.#.#........#....#",
        "#.#.#..B....d#..r.#",
        "#.#.#..B....##....#",
        "#.#.#........#....#",
        "#.#.##########....#",
        "#.#........d......#",
        "#.######..dd......#",
        "#.P.....dd........#",
        "####################",
    };
}
