public static class GameplayLevelCatalog
{
    public static GameplayLevelSpec Get(int levelIndex)
    {
        switch (levelIndex)
        {
            case 2: return Level2Layout.Spec;
            case 3: return Level3Layout.Spec;
            case 4: return Level4Layout.Spec;
            case 5: return Level5Layout.Spec;
            case 6: return Level6Layout.Spec;
            case 7: return Level7Layout.Spec;
            case 8: return Level8Layout.Spec;
            case 9: return Level9Layout.Spec;
            default: return Level1Layout.Spec;
        }
    }
}
