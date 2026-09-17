using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Events;

namespace Game.View.UI
{
    /// <summary>
    /// UI component for a single booster slot. Shows the remaining count and raises
    /// an event channel when clicked. Intended to be placed under <see cref="LevelView"/>
    /// with the matching <see cref="VoidEventChannelSO"/> assigned in the Inspector.
    /// </summary>
    public class BoosterSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private VoidEventChannelSO onUseBoosterEvent;

        private Func<int> _getCount;

        /// <summary>
        /// Binds a count source. Call once during level setup.
        /// The <paramref name="getCount"/> delegate should return the current inventory amount.
        /// </summary>
        public void Bind(Func<int> getCount)
        {
            _getCount = getCount;
            UpdateCount();
        }

        /// <summary>Refreshes the displayed count and button interactability.</summary>
        public void UpdateCount()
        {
            if (_getCount == null) return;

            int count = _getCount();

            if (countText != null)
                countText.text = count.ToString();

            if (button != null)
                button.interactable = count > 0;
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            onUseBoosterEvent?.Raise();
        }
    }
}

