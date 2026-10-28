public static class Level2Layout
{
    public const int Width = 34;
    public const int Height = 18;
    public const int RequiredReds = 2;
    public const int TotalReds = 4;
    public const int ExtraReds = TotalReds - RequiredReds;
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

    // Multi-room Courtyard from overview map (PRD L2: dig + push boulders, no snakes/traps).
    // Idol Hall (NW), Dig Garden (N), Vault (E), Lab pocket (NE), Boulder courtyard (SE), spawn SW.
    public static readonly string[] Rows =
    {
            "###############EE#################",
            "############...........###########",
            "############..ddd......####......#",
            "#..........#..drd.ddd..####..ddd.#",
            "#.....B.......ddd.drd..####..drd.#",
            "#...B.............ddd....##..ddd.#",
            "#.dd...B...#.............##......#",
            "#.dd.......#.............##......#",
            "#..........######..###..........##",
            "#dd..############..###.....t...###",
            "#dddd############..###..T......###",
            "#rddd#########..............g..###",
            "###..#########.......B.........###",
            "###..#########..B........#########",
            "#.................B......#########",
            "#.P...................B..#########",
            "#.......######...........#########",
            "##################################",
    };
}
