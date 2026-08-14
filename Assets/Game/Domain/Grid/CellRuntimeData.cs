using Game.Shared;
using Game.Data;
using UnityEngine;
using Game.Domain.Person;

namespace Game.Domain.Grid
{
    public class CellRuntimeData
    {
        public readonly Vector2Int Index;
        public readonly CellType Type;
        public readonly Vector2 Size;
        public readonly PersonRuntimeData DefaultPerson; 
        public readonly Food Food;                  
        public readonly Sprite Sprite;

        public PersonRuntimeData CurrentPerson { get; private set; }

        public CellRuntimeData(Vector2Int index,
                               CellType type,
                               Vector2 size,
                               PersonRuntimeData defaultPerson,
                               Food food,
                               Sprite sprite)
        {   
            Index = index;
            Type = type;
            Size = size;
            DefaultPerson = defaultPerson;
            Food = food;
            Sprite = sprite;
            CurrentPerson = defaultPerson;
        }

        public void SetPerson(PersonRuntimeData person)
        {
            if (CurrentPerson == person) return;
            CurrentPerson = person;
        }
    }
}
