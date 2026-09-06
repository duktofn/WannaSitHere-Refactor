namespace Game.App.SaveAndLoad
{
    [System.Serializable]
    public struct GameData
    {
        public int currentLevel;
        public int currentGold;
        public int currentGem; 
        public int currentRemove;
        public int currentMoreMoves;
        public int currentUndo;
    }
}