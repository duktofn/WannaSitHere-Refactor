using Game.Shared;
using Game.Data;
using UnityEngine;
using Game.Domain.Person;

namespace Game.Domain.Grid
{
    public class CellRuntimeData
    {
        public readonly int X;
        public readonly int Y;
        public readonly CellType Type;
        public readonly PersonDataSO DefaultPerson; 
        public readonly Food Food;                  
        public readonly Sprite Sprite;
        public PersonRuntimeData CurrentPerson { get; private set; }

        public CellRuntimeData(int X, int Y,
                               CellType type,
                               PersonDataSO defaultPerson,
                               Food food,
                               Sprite sprite)
        {
            this.X = X;
            this.Y = Y;
            Type = type;
            DefaultPerson = defaultPerson;
            Food = food;
            Sprite = sprite;
        }

        public void SetPerson(PersonRuntimeData person)
        {
            if (CurrentPerson == person) return;
            CurrentPerson = person;
        }
    }
}