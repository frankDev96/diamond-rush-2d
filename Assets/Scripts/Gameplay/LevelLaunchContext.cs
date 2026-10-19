public static class LevelLaunchContext
{
    public static int WorldIndex { get; private set; }
    public static int LevelIndex { get; private set; } = 1;

    public static void Set(int worldIndex, int levelIndex)
    {
        WorldIndex = worldIndex;
        LevelIndex = levelIndex;
    }
}
