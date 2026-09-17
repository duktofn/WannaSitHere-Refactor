using System;
using TMPro;
using UnityEngine;
using Game.Core.Economy;

namespace Game.View.UI
{
    /// <summary>
    /// Presentation and interaction for one shop offer. The panel supplies the configured
    /// price, limit state, inventory, and purchase callback during binding.
    /// </summary>
    public sealed class ShopItemView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI limitText;
        [SerializeField] private UnityEngine.UI.Button purchaseButton;

        private ShopPurchaseRequest _request;
        private Reward _price;
        private EconomyManager _economyManager;
        private Action<ShopPurchaseRequest> _onPurchaseRequested;
        private int _purchaseCount;
        private int _limit;
        private bool _hasOffer;

        private void Awake()
        {
            CacheReferences();
        }

        public void Bind(
            ShopPurchaseRequest request,
            Reward price,
            EconomyManager economyManager,
            int purchaseCount,
            int limit,
            Action<ShopPurchaseRequest> onPurchaseRequested)
        {
            CacheReferences();
            EnsureClickBinding();

            _request = request;
            _price = price;
            _economyManager = economyManager;
            _purchaseCount = Mathf.Max(0, purchaseCount);
            _limit = Mathf.Max(0, limit);
            _onPurchaseRequested = onPurchaseRequested;
            _hasOffer = true;

            Refresh();
        }

        public void SetUnavailable()
        {
            _hasOffer = false;
            _onPurchaseRequested = null;

            if (priceText != null)
                priceText.text = "-";

            if (limitText != null)
            {
                limitText.text = string.Empty;
                limitText.gameObject.SetActive(false);
            }

            if (purchaseButton != null)
                purchaseButton.interactable = false;
        }

        public void Refresh()
        {
            if (!_hasOffer)
                return;

            if (priceText != null)
                priceText.text = Mathf.Max(0, _price.amount).ToString();

            if (limitText != null)
            {
                bool showLimit = _request.UsesGold;
                limitText.gameObject.SetActive(showLimit);
                if (showLimit)
                    limitText.text = $"Limit: {_purchaseCount}/{_limit}";
            }

            bool hasInventory = _economyManager != null && _economyManager.Inventory != null;
            bool canAfford = hasInventory && _economyManager.Inventory.HasEnough(_price.type, _price.amount);
            bool hasRemainingLimit = !_request.UsesGold || _purchaseCount < _limit;

            if (purchaseButton != null)
                purchaseButton.interactable = canAfford && hasRemainingLimit;
        }

        public void OnPurchaseButtonClicked()
        {
            HandlePurchaseClicked();
        }

        private void OnEnable()
        {
            CacheReferences();
            EnsureClickBinding();
        }

        private void OnDisable()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
        }

        private void EnsureClickBinding()
        {
            if (purchaseButton == null || purchaseButton.onClick.GetPersistentEventCount() > 0)
                return;

            purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);
        }

        private void HandlePurchaseClicked()
        {
            if (!_hasOffer)
                return;

            Refresh();
            if (purchaseButton != null && !purchaseButton.interactable)
                return;

            _onPurchaseRequested?.Invoke(_request);
        }

        private void CacheReferences()
        {
            priceText ??= FindChild<TextMeshProUGUI>("Price");
            limitText ??= FindChild<TextMeshProUGUI>("LimitText");
            purchaseButton ??= FindChild<UnityEngine.UI.Button>("Button");
        }

        private T FindChild<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
