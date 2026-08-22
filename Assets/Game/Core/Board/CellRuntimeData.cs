using UnityEngine;
using Game.Core.People;

namespace Game.Core.Board
{
    public class CellRuntimeData
    {
        public readonly Vector2Int Index;
        public readonly CellType Type;
        public readonly Vector2 Size;
        public readonly PersonRuntimeData DefaultPerson; 
        public readonly Food Food;                  
        public readonly Sprite Sprite;
        public readonly GridId OwnGrid;

        public PersonRuntimeData CurrentPerson { get; private set; }

        public CellRuntimeData(Vector2Int index,
                               CellType type,
                               Vector2 size,
                               PersonRuntimeData defaultPerson,
                               Food food,
                               Sprite sprite,
                               GridId ownGrid)
        {   
            Index = index;
            Type = type;
            Size = size;
            DefaultPerson = defaultPerson;
            Food = food;
            Sprite = sprite;
            OwnGrid = ownGrid;
            CurrentPerson = defaultPerson;
        }

        public void SetPerson(PersonRuntimeData person)
        {
            if (CurrentPerson == person) return;
            CurrentPerson = person;
        }
    }
}
