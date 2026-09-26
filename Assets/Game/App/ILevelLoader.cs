using Game.Core.Levels;

namespace Game.App
{
    public interface IPreparedLevel
    {
        int CatalogIndex { get; }
        LevelRuntimeData RuntimeData { get; }
    }

    public interface ILevelLoader
    {
        int TotalLevels { get; }
        bool TryPrepare(int levelNumber, out IPreparedLevel preparedLevel, out string error);
        void Activate(IPreparedLevel preparedLevel, LevelManager levelManager);
    }
}
