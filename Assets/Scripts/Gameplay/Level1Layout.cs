public static class Level1Layout
{
    public const int Width = 48;
    public const int Height = 20;
    public const int RequiredReds = 3;
    public const int TotalReds = 5;
    public const int ExtraReds = TotalReds - RequiredReds;
    public const string StageName = "THE GATES";
    public const string TutorialHint = "Move to Dig!";

    public static GameplayLevelSpec Spec => new GameplayLevelSpec
    {
        LevelIndex = 1,
        Width = Width,
        Height = Height,
        RequiredReds = RequiredReds,
        TotalReds = TotalReds,
        StageName = StageName,
        TutorialHint = TutorialHint,
        SceneName = GameScenes.GameplayLevel1,
        Rows = Rows,
    };

    // # stone, . floor, d soft dirt, r red gem, E exit (two cells wide), P spawn.
    // Generated and validated by ArtSource/Gameplay/level1_maze.mjs.
    public static readonly string[] Rows =
    {
        "############################################EE##",
        "#############################rd#############...#",
        "#############################dd########..###...#",
        "#########################..........####..####..#",
        "###.......###############..........####..####..#",
        "###.......###############..######..####..####..#",
        "###..###..###############..######..##.......#..#",
        "###..###..##...ddr....###..######..##..###..#..#",
        "#....###..##..######..###..######..##..###..#dd#",
        "#....###dd##..######..###..ddddd#..##..###..#dd#",
        "###..dr#dd##..######..###..dddrd#..##..###..#..#",
        "###..###..##..#r..dd..###..ddddd#..##..###dd#..#",
        "###..###..##..#...dd..###..######......###dd#..#",
        "###..###......######..###..######......###..#..#",
        "###dd###......######..###..######..#######..#..#",
        "###dd###############..###..######..#######..#..#",
        "#.....##############.........dd....#######.....#",
        "#.P...##############.........dd....#######.....#",
        "#.....##########################################",
        "################################################",
    };
}
