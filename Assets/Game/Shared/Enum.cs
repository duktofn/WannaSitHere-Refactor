namespace Game.Shared
{
    public enum PersonTrait
    {
        Cool = 0,
        Sick = 1,
        Dirty = 2,
        Loud = 3,
        Quiet = 4
    }

    public enum Food
    {
        Hamburger = 0, FrenchFries = 1,
    }

    public enum ConditionTarget
    {
        Food = 0, Person = 1
    }

    public enum ConditionType
    {
        Hate = 0, Like = 1
    }

    public enum CellType
    {
        Seat = 0, Food = 1, Block = 2
    }

    public enum PersonState
    {
        Normal = 0, Angry = 1, Happy = 2 
    }
}