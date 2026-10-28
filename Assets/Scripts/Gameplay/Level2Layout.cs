public static class Level2Layout
{
    public const int Width = 40;
    public const int Height = 26;
    public const int RequiredReds = 3;
    public const int TotalReds = 5;
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

    // Pixel-mockup rooms (1408×896 / 36px): SW dig spawn, Idol Hall + EE, Sacred Spring dig,
    // Central Hub, Vault chests, Serpent corridors as open floor (no S until L3).
    public static readonly string[] Rows =
    {
            "###########EE###########################",
            "###########..###########################",
            "###########..####dddddd#################",
            "#######..B..B....drdddd.........########",
            "#######........##dddddd#........########",
            "#######........##dddddd#........########",
            "#.....#.B....B.####.####........#.....##",
            "#.....#........####.#######.#####.....##",
            "#.....#..B..B..##......####.#####.....##",
            "#..g...........#ddd....#T.....T.......##",
            "#.....#........#drddd..#........#.....##",
            "#.....####B#####dddrd..#.Tt.....#.....##",
            "##########.######.ddd..#..g.....#.....##",
            "#####....ddTddd..........T............##",
            "#####....dddddd..#######........#.....##",
            "#####.g..dddddd..##.####################",
            "#####....ddddddB.##.####################",
            "#####....ddddrd..##.....ddd........#####",
            "#####....ddBddd.........drd........#####",
            "#####....dddddd..##.....ddd........#####",
            "#####......P.....##................#####",
            "#####............##................#####",
            "###################................#####",
            "########################################",
            "########################################",
            "########################################",
    };
}
