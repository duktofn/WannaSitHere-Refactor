using UnityEngine;
using Game.Core.Board;
using Game.Core.People;
using Game.View.Input;
using Game.View.People;

namespace Game.View.Board
{
    public class CellView : MonoBehaviour
    {
        private CellRuntimeData _cell;
        [SerializeField] private GameObject personViewPrefabs;
        [SerializeField] private PersonView personView;

        public CellRuntimeData RuntimeData => _cell;
        public PersonView CurrentPersonView => personView;

        public CellType GetCellType() => _cell.Type;

        public void BindData(CellRuntimeData cell, PersonMoveManager personMoveManager)
        {
            if (cell == null) return;
            _cell = cell;
            personView = null;
            if (_cell.DefaultPerson != null)
            {
                if (personViewPrefabs == null)
                {
                    Debug.LogError($"[CellView] personViewPrefabs chưa được gán trên {gameObject.name}! Vui lòng kéo Prefab vào Inspector.", this);
                    return;
                }

                GameObject tmp = Instantiate(personViewPrefabs, transform.position, Quaternion.identity, transform.root);
                personView = tmp.GetComponent<PersonView>();
                personView.BindData(_cell.DefaultPerson);
                tmp.GetComponent<PersonDragManager>()
                    .Initialize(personMoveManager, _cell.DefaultPerson, this);
            }
        }

        public void SetPersonView(PersonView view)
        {
            personView = view;
        }

        public Vector2Int GetCellIndex()
        {
            if (_cell != null) 
                Debug.LogWarning("No Cell valid to get index");
            return _cell.Index;
        }
        
        public void AssignPersonToCell(PersonRuntimeData person)
        {
            _cell.SetPerson(person);
        }
    }
}
