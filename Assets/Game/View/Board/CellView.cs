using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Core.Board;
using Game.View.Audio;
using Game.View.People;

namespace Game.View.Board
{
    public class CellView : MonoBehaviour, IPointerClickHandler
    {
        private CellRuntimeData _cell;
        private bool _inputEnabled = true;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private FoodTooltips foodTooltips;
        [SerializeField] private PersonSpawner personSpawner;
        [SerializeField] private PersonView personView;

        public CellRuntimeData RuntimeData => _cell;
        public PersonView CurrentPersonView => personView;
        public FoodTooltips FoodTooltips => foodTooltips;
        public event Action<CellView> Tapped;

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
            if (spriteRenderer != null)
                spriteRenderer.sprite = _cell.Sprite;

            if (_cell.Type == CellType.Food)
            {
                if (foodTooltips != null)
                    foodTooltips.Initialize(_cell.Food.ToString(), GetComponent<Collider2D>());

                return;
            }

            if (_cell.CurrentPerson != null && personSpawner != null)
            {
                personView = personSpawner.SpawnPerson(_cell.CurrentPerson, this, personMoveManager);
            }
        }

        public void SetPersonView(PersonView view)
        {
            personView = view;
        }

        public void BindAudioPlayer(AudioPlayer audioPlayer)
        {
            personSpawner?.BindAudioPlayer(audioPlayer);
            personView?.BindAudioPlayer(audioPlayer);
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            foodTooltips?.SetInputEnabled(enabled);
            personView?.SetInputEnabled(enabled);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_inputEnabled || eventData.dragging ||
                Vector2.Distance(eventData.pressPosition, eventData.position) > 5f)
            {
                return;
            }

            Tapped?.Invoke(this);
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
