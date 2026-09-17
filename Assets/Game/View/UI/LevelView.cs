using TMPro;
using UnityEngine;
using Game.Core.Economy;
using Game.Core.Levels;

namespace Game.View.UI
{
    public class LevelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moveText;
        
        [Header("Booster")]
        [SerializeField] private BoosterSlotView moreMoveSlot;
        [SerializeField] private BoosterSlotView undoSlot;
        [SerializeField] private BoosterSlotView removeSlot;

        private LevelRuntimeData data;
        private Inventory _inventory;

        public void BindData(LevelRuntimeData source)
        {
            if (data != null && isActiveAndEnabled)
                data.OnMoveChanged -= UpdateMove;

            data = source;

            if (data == null)
            {
                Debug.LogWarning("Level data cannot be bound because it is null");
                return;
            }

            if (isActiveAndEnabled)
                data.OnMoveChanged += UpdateMove;

            UpdateMove(data.CurrentMove);
        }

        /// <summary>
        /// Binds booster slot views to the inventory counts.
        /// Call once per play session or when inventory reference changes.
        /// Safe to call multiple times — properly unsubscribes from previous inventory.
        /// </summary>
        public void BindBoosters(Inventory inventory)
        {
            if (_inventory != null)
                _inventory.OnInventoryUpdate -= UpdateBoosterCounts;

            _inventory = inventory;

            if (_inventory == null) return;

            moreMoveSlot?.Bind(() => _inventory.GetAmount(ItemType.MoreMoves));
            undoSlot?.Bind(() => _inventory.GetAmount(ItemType.Undo));
            removeSlot?.Bind(() => _inventory.GetAmount(ItemType.Remove));

            _inventory.OnInventoryUpdate += UpdateBoosterCounts;
        }

        private void OnEnable()
        {
            if (data != null)
            {
                data.OnMoveChanged += UpdateMove;
                UpdateMove(data.CurrentMove);
            }

            if (_inventory != null)
                _inventory.OnInventoryUpdate += UpdateBoosterCounts;
        }

        private void OnDisable()
        {
            if (data != null)
                data.OnMoveChanged -= UpdateMove;

            if (_inventory != null)
                _inventory.OnInventoryUpdate -= UpdateBoosterCounts;
        }

        private void UpdateMove(int value)
        {
            if (moveText != null)
                moveText.text = value.ToString();
        }

        private void UpdateBoosterCounts()
        {
            moreMoveSlot?.UpdateCount();
            undoSlot?.UpdateCount();
            removeSlot?.UpdateCount();
        }
    }
}
