public static class Level1Layout
{
    public const int RequiredReds = 3;
    public const string StageName = "THE GATES";

    // # stone, . floor, d soft dirt, r red gem, E exit (two cells wide), P spawn
    public static readonly string[] Rows =
    {
        "#######EE#######",
        "#.....#..#.....#",
        "#.ddd.#..#.ddd.#",
        "#.drd......drd.#",
        "#.ddd.dddd.ddd.#",
        "#.....drdd.....#",
        "#..#..dddd..#..#",
        "#.......P......#",
        "################"
    };
}
