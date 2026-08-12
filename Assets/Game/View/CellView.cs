using Game.Domain.Grid;
using Game.Domain.Person;
using UnityEngine;

namespace Game.View
{
    public class CellView : MonoBehaviour
    {
        private CellRuntimeData _cell;

        public void AssignPersonToCell(PersonRuntimeData person)
        {
            _cell.SetPerson(person);
        }
    }
}