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
        public int currentSoundVolume;
        public bool isSoundMuted;
        public int currentMusicVolume;
        public bool isMusicMuted;
    }
}