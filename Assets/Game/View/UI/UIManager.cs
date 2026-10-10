using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Game.Events;
using Game.Core.Economy;
using Game.View.VFX;
using Game.View.Effect;
using TMPro;
using Cysharp.Threading.Tasks;

namespace Game.View.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private GameObject InGameUICanvas;
        [SerializeField] private GameObject MainMenuCanvas;
        [SerializeField] private GameObject TransitionCanvas;
        [SerializeField] private GameObject CurrencyCanvas;

        [Header("Transition")]
        [SerializeField] private TransitionController transitionController;

        [Header("Win/Lose Panel")]
        [SerializeField] private GameObject levelWinPanel;
        [SerializeField] private GameObject levelLosePanel;
        [SerializeField] private GameObject mainSettingPanel;
        [SerializeField] private GameObject gameSettingPanel;

        [Header("Paid Continue Feedback")]
        [SerializeField] private CurrencyScatterAnimation paidContinueScatter;
        [SerializeField, Min(0f)] private float paidContinueReturnDelay = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private VfxPlayer _vfxPlayer;

        [Header("Game Events")]
        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;
        [SerializeField] private VoidEventChannelSO OnSettingShow;
        [SerializeField] private VoidEventChannelSO OnSettingHide;
        [SerializeField] private IntEventChannelSO onLevelChangedEvent;

        [Header("UI Components")]
        [SerializeField] private InventoryView inventoryView;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private RectTransform winVFXAnchor;

        private readonly EventListener _listener = new();
        private int _currentLevel = 1;

        public Transform InGameCanvasTransform => InGameUICanvas != null ? InGameUICanvas.transform : null;

        private void OnEnable()
        {
            _listener.Listen(OnWinEvent, ShowWin);
            _listener.Listen(OnLoseEvent, ShowLose);
            _listener.Listen(OnSettingShow, ShowSetting);
            _listener.Listen(OnSettingHide, HideSetting);
            _listener.Listen<int>(onLevelChangedEvent, UpdateLevelText);
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
        }

        private void Awake()
        {
            if (levelWinPanel != null) levelWinPanel.SetActive(false);
            if (levelLosePanel != null) levelLosePanel.SetActive(false);

            HideSetting();

            if (transitionController == null)
            {
                transitionController = GetComponent<TransitionController>();
            }
        }

        public void Initialize(Inventory inventory)
        {
            if (inventoryView != null)
                inventoryView.BindData(inventory);

            UpdateLevelText(_currentLevel);
        }

        public void UpdateLevelText(int level)
        {
            _currentLevel = level;
            if (levelText != null)
                levelText.text = $"Level {level}";
        }

        public async Task CloseTransitionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (transitionController != null)
                await transitionController.CloseAsync().AttachExternalCancellation(cancellationToken);
        }

        public async Task OpenTransitionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (transitionController != null)
                await transitionController.OpenAsync().AttachExternalCancellation(cancellationToken);
        }

        public void ShowHomeScreen(int levelNumber)
        {
            MainMenuCanvas?.SetActive(true);
            InGameUICanvas?.SetActive(false);
            CurrencyCanvas?.SetActive(true);

            HideWin();
            HideLose();
            HideSetting();
            UpdateLevelText(levelNumber);
        }

        public void ShowGameScreen(int levelNumber)
        {
            MainMenuCanvas?.SetActive(false);
            InGameUICanvas?.SetActive(true);
            CurrencyCanvas?.SetActive(false);

            HideWin();
            HideLose();
            HideSetting();
            UpdateLevelText(levelNumber);
        }

        public void ShowWin()
        {
            Debug.Log("[UIManager] Win Event raised");
            levelWinPanel?.SetActive(true);
            CurrencyCanvas?.SetActive(true);

            if (levelWinPanel != null)
                _vfxPlayer?.PlayAtUI(VfxId.Win, winVFXAnchor);
        }

        public void ShowSetting()
        {
            if (MainMenuCanvas != null && MainMenuCanvas.activeInHierarchy) {
                mainSettingPanel?.SetActive(true);
                return;
            }

            if (InGameUICanvas != null && InGameUICanvas.activeInHierarchy)
            {
                gameSettingPanel?.SetActive(true);
            }
        }

        public void HideSetting()
        {
            if (mainSettingPanel != null && mainSettingPanel.activeInHierarchy) {
                mainSettingPanel.SetActive(false);
                return;
            }

            if (gameSettingPanel != null && gameSettingPanel.activeInHierarchy)
            {
                gameSettingPanel.SetActive(false);
            }
        }

        public void ShowLose()
        {
            Debug.Log("[UIManager] Lose Event raised");
            levelLosePanel?.SetActive(true);
            CurrencyCanvas?.SetActive(true);
        }

        public async Task PlayPaidContinueFeedbackAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (paidContinueScatter != null)
                paidContinueScatter.PlayFromTransform(paidContinueScatter.transform);

            await UniTask.Delay(
                Mathf.CeilToInt(Mathf.Max(0f, paidContinueReturnDelay) * 1000f),
                ignoreTimeScale: true,
                cancellationToken: cancellationToken);
        }

        private void HideWin()
        {
            _vfxPlayer?.Stop(VfxId.Win);
            levelWinPanel?.SetActive(false);
        }

        private void HideLose()
        {
            levelLosePanel?.SetActive(false);
        }
    }
}

// LL2Dumper
// IESprite
// EDRA
// APK Tool
