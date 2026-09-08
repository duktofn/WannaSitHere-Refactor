using Cysharp.Threading.Tasks;
using PrimeTween;
using Game.Core.Economy;
using TMPro;
using UnityEngine;
using System;

namespace Game.View.UI
{
    public class InventoryView : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private TextMeshProUGUI goldView;
        [SerializeField] private TextMeshProUGUI gemView;

        [Header("Increase Tween")]
        [SerializeField] private float duration;
        [SerializeField] private Ease ease;

        private Inventory _inventory;
        
        public void BindData(Inventory inventory)
        {
            if (inventory != null)
            {
                _inventory = inventory;
            }
            else Debug.LogWarning("[InventoryView] Data is null");
        }

        private void OnEnable()
        {
            _inventory.OnInventoryUpdate += UpdateView;
        }

        private void OnDisable()
        {
            _inventory.OnInventoryUpdate -= UpdateView;
        }

        private void UpdateView()
        {
            int startGold = int.TryParse(goldView != null ? goldView.text : null, out int g) ? g : 0;
            int startGem = int.TryParse(gemView != null ? gemView.text : null, out int m) ? m : 0;

            Increase(goldView, startGold, _inventory != null ? _inventory.Gold : 0).Forget();
            Increase(gemView, startGem, _inventory != null ? _inventory.Gem : 0).Forget();
        }

        private async UniTask Increase(TextMeshProUGUI view, int start, int end)
        {
            if (view == null) return;

            await Tween.Custom(
                target: view,
                startValue: start,
                endValue: end,
                duration: duration,
                onValueChange: (target, val) =>
                {
                    target.text = Mathf.RoundToInt(val).ToString();
                },
                ease: ease
            );
        }
    }
}