using System;
using UnityEngine;
using Game.Events;
using Game.Core.Economy;
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

        [Header("Game Events")]
        [SerializeField] private VoidEventChannelSO OnPlayGameEvent;
        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;
        [SerializeField] private VoidEventChannelSO OnNextLevelEvent;
        [SerializeField] private VoidEventChannelSO OnRestartLevelEvent;
        [SerializeField] private VoidEventChannelSO OnSettingShow;
        [SerializeField] private VoidEventChannelSO OnSettingHide;
        [SerializeField] private VoidEventChannelSO OnBackToHome;

        [Header("UI Components")]
        [SerializeField] private InventoryView inventoryView;

        private readonly EventListener _listener = new();

        private void OnEnable()
        {
            _listener.Listen(OnPlayGameEvent, PlayGame);
            _listener.Listen(OnWinEvent, ShowWin);
            _listener.Listen(OnLoseEvent, ShowLose);
            _listener.Listen(OnNextLevelEvent, NextLevel);
            _listener.Listen(OnRestartLevelEvent, RestartLevel);
            _listener.Listen(OnSettingShow, ShowSetting);
            _listener.Listen(OnSettingHide, HideSetting);
            _listener.Listen(OnBackToHome, BackToHome);
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
        }

        private void BackToHome()
        {
            if (transitionController != null)
            {
                BackToHomeWithTransition().Forget();
            }
            else
            {
                ApplyBackToHome();
            }
        }

        private async UniTaskVoid BackToHomeWithTransition()
        {
            await transitionController.DoTransitionAsync(ApplyBackToHome);
        }

        private void ApplyBackToHome()
        {
            MainMenuCanvas?.SetActive(true);
            InGameUICanvas?.SetActive(false);
            CurrencyCanvas?.SetActive(true);

            HideWin();
            HideLose();
        }

        private void PlayGame()
        {
            if (transitionController != null)
            {
                PlayGameWithTransition().Forget();
            }
            else
            {
                ApplyPlayGame();
            }

            gameSettingPanel.SetActive(false);
        }

        private async UniTaskVoid PlayGameWithTransition()
        {
            await transitionController.DoTransitionAsync(ApplyPlayGame);
        }

        private void ApplyPlayGame()
        {
            MainMenuCanvas?.SetActive(false);
            InGameUICanvas?.SetActive(true);
            CurrencyCanvas?.SetActive(false);

            HideWin();
            HideLose();
        }

        private void NextLevel()
        {
            if (transitionController != null)
            {
                NextLevelWithTransition().Forget();
            }
            else
            {
                ApplyNextLevel();
            }
        }

        private async UniTaskVoid NextLevelWithTransition()
        {
            await transitionController.DoTransitionAsync(ApplyNextLevel);
        }

        private void ApplyNextLevel()
        {
            CurrencyCanvas?.SetActive(false);
            HideWin();
        }

        private void RestartLevel()
        {
            if (transitionController != null)
            {
                RestartLevelWithTransition().Forget();
            }
            else
            {
                ApplyRestartLevel();
            }

            gameSettingPanel.SetActive(false);
        }

        private async UniTaskVoid RestartLevelWithTransition()
        {
            await transitionController.DoTransitionAsync(ApplyRestartLevel);
        }

        private void ApplyRestartLevel()
        {
            CurrencyCanvas?.SetActive(false);
            HideLose();
        }

        public void ShowWin()
        {
            Debug.Log("[UIManager] Win Event raised");
            levelWinPanel?.SetActive(true);
            CurrencyCanvas?.SetActive(true);
        }

        public void ShowSetting()
        {
            if (MainMenuCanvas.activeInHierarchy) {
                mainSettingPanel.SetActive(true);
                return;
            }

            if (InGameUICanvas.activeInHierarchy)
            {
                gameSettingPanel.SetActive(true);
            }
        }

        public void HideSetting()
        {
            if (mainSettingPanel.activeInHierarchy) {
                mainSettingPanel.SetActive(false);
                return;
            }

            if (gameSettingPanel.activeInHierarchy)
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

        private void HideWin()
        {
            levelWinPanel?.SetActive(false);
        }

        private void HideLose()
        {
            levelLosePanel.SetActive(false);
        }
    }
}
