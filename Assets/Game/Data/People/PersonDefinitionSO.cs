using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.Conditions;
using Game.Core.People;

namespace Game.Data.People
{
    /// <summary>
    /// Shared definition data for a character. Conditions are owned by the
    /// level occurrence, not by this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "New Person Definition", menuName = "Game/New Person Definition")]
    public class PersonDefinitionSO : ScriptableObject
    {
        public string personName;
        public PersonTrait trait;
        public Sprite baseSprite;

        public PersonRuntimeData ToRuntimeData(IReadOnlyList<ConditionRuntimeData> conditions)
        {
            return new PersonRuntimeData(
                personName,
                trait,
                conditions ?? Array.Empty<ConditionRuntimeData>(),
                baseSprite);
        }
    }
}
