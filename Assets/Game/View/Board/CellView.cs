using UnityEngine;
using Game.Core.Board;
using Game.View.People;

namespace Game.View.Board
{
    public class CellView : MonoBehaviour
    {
        private CellRuntimeData _cell;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private FoodTooltips foodTooltips;
        [SerializeField] private PersonSpawner personSpawner;
        [SerializeField] private PersonView personView;

        public CellRuntimeData RuntimeData => _cell;
        public PersonView CurrentPersonView => personView;

        public CellType GetCellType() => _cell != null ? _cell.Type : CellType.Block;

        private void Awake()
        {
            if (foodTooltips == null)
                foodTooltips = GetComponentInChildren<FoodTooltips>(true);

            if (personSpawner == null)
                personSpawner = GetComponent<PersonSpawner>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void BindData(CellRuntimeData cell, PersonMover personMoveManager)
        {
            if (cell == null) return;
            _cell = cell;
            personView = null;
            InitCell(personMoveManager);
        }

        private void InitCell(PersonMover personMoveManager)
        {
            if (_cell.Type == CellType.Food)
            {
                if (spriteRenderer != null)
                    spriteRenderer.sprite = _cell.Sprite;

                if (foodTooltips != null)
                    foodTooltips.Initialize(_cell.Food.ToString(), GetComponent<Collider2D>());

                return;
            }

            if (_cell.DefaultPerson != null && personSpawner != null)
            {
                personView = personSpawner.SpawnPerson(_cell.DefaultPerson, this, personMoveManager);
            }
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
    }
}
