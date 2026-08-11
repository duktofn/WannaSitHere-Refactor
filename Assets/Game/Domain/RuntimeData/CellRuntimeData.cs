using Game.Shared;
using Game.Data;
using UnityEngine;
using System;

namespace Game.Domain.RuntimeData
{
    public class CellRuntimeData
    {
        public readonly CellType Type;
        public readonly PersonDataSO DefaultPerson; 
        public readonly Food Food;                  
        public readonly Sprite Sprite;
        public PersonRuntimeData Person { get; private set; }

        public CellRuntimeData(CellType type,
                               PersonDataSO defaultPerson,
                               Food food,
                               Sprite sprite)
        {
            Type = type;
            DefaultPerson = defaultPerson;
            Food = food;
            Sprite = sprite;
        }

        public void SetPerson(PersonRuntimeData person)
        {
            Person = person;
        }
    }
}