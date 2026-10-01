namespace Game.Core.Board
{
    public enum Food
    {
        Any = 0,
        Fries = 1,
        Hamburger = 2,
        ChungCake = 3,
        Watermelon = 4,
        Cheese = 5,
        Donut = 6,
        Cake = 7,
        Corn = 8,
        Soda = 9, 
        Milk = 10,
        Pudding = 11,
        Sandwich = 12
    }

    public enum CellType
    {
        Seat = 0,
        Food = 1,
        Block = 2
    }

    public enum GridId
    {
        MainGrid = 0,
        WaitGrid = 1
    }
}
