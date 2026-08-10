using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Model
{
    public class Person
    {
        [SerializeField] private PersonDataSO personData;
        [SerializeField] private List<ConditionDataSO> condition;
    }
}