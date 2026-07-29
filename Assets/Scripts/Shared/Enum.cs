namespace DefaultNamespace
{
    public enum ObjectiveType
    {
        Match = 1,
        Butterfly = 2,
        ClearSpiderWeb = 3,
    }

    public enum ObjectiveUpdateState
    {
        NoChange,
        Progressed,
        Completed
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
        Bubble = 3,
        Sunburst = 4,
        Butterfly = 5,
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

    public enum ScoreRuleType
    {
        PetalCleared = 1,
        SpiderWebCleared = 2,
        CascadeDepthPetalBonus = 3,
        MatchShapeBonus = 4,
        SkillActivation = 5
    }

    public enum UIJawCurtainTipCategory
    {
        General = 0,
        LevelStart = 1,
        Retry = 2,
        ReturnHome = 3
    }

    /// <summary>
    /// Convenient IDs for common dialog buttons, with no attached logic.
    /// </summary>
    public enum DialogButtonType
    {
        Ok = 1,
        Cancel = 2,
        Yes = 3,
        No = 4,
        Close = 5,
        Retry = 6
    }

    public enum UIButtonVariant
    {
        Green = 1,
        Blue = 2,
        Orange = 3,
        Purple = 4
    }

    public enum AudioBus
    {
        Music,
        GameplaySfx,
        UiSfx
    }
}
