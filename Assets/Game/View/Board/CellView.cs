using UnityEngine;
using Game.Core.Board;
using Game.Core.People;
using Game.View.Input;
using Game.View.People;
using UnityEngine.EventSystems;

namespace Game.View.Board
{
    public class CellView : MonoBehaviour, IPointerClickHandler
    {
        private CellRuntimeData _cell;
        [SerializeField] private GameObject personViewPrefabs;
        [SerializeField] private PersonView personView;
        [SerializeField] private SpriteRenderer renderer;

        public CellRuntimeData RuntimeData => _cell;
        public PersonView CurrentPersonView => personView;

        public CellType GetCellType() => _cell.Type;

        private void InitCell(PersonMoveManager personMoveManager)
        {
            if (_cell.Type == CellType.Food)
            {
                renderer.sprite = _cell.Sprite;
                return;
            }

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

        public void BindData(CellRuntimeData cell, PersonMoveManager personMoveManager)
        {
            if (cell == null) return;
            _cell = cell;
            personView = null;
            InitCell(personMoveManager);
        }

        public void SetPersonView(PersonView view)
        {
            personView = view;
        }

        public Vector2Int GetCellIndex()
        {
            if (_cell == null)
            {
                Debug.LogWarning("No Cell valid to get index");
                return Vector2Int.zero;
            }
            return _cell.Index;
        }
        
        public void AssignPersonToCell(PersonRuntimeData person)
        {
            _cell.SetPerson(person);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // TODO: Implement click handling logic
        }
    }
}
