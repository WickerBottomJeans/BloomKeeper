namespace DefaultNamespace
{
    public enum ObjectiveType
    {
        Match = 1,
        Butterfly = 2,
        ClearSpiderWeb = 3,
    }

    public enum ConstrainerType
    {
        MoveLimit = 1,
        TimeLimit = 2,
    }
    
    public enum PetalType
    {
        None = 0,
        Strawberry = 1,
        Mushroom = 2,
        Starfruit = 3,
        Clover = 4,
        Dewdrop = 5,
        BerryCluster = 6,
        Daisy = 7
    }

    public enum TileType
    {
        Normal = 1,
        Inactive = 2,
        Web = 3
    }
    
    public enum TileOverlay
    {
        None = 0,
        Web = 1
    }

    public enum SpecialSkillType
    {
        None = 0,
        StripedHorizontal = 1,
        StripedVertical = 2,
        Bomb = 3,
        Sunburst = 4,
        Butterfly = 5,
        StripeSunburst = 6,
        BouquetSunburst = 7,
        ButterflySunburst = 8,
    }
    
    public enum MatchShape
    {
        None = 0,
        Three = 1,
        Four = 2,
        Five = 3,
        TShape = 4,
        LShape = 5,
        Cross = 6,
        Square2x2 = 7
    }
}
