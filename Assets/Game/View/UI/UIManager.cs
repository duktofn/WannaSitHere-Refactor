using UnityEngine;
using Game.Events;
using Game.Core.Economy;
using System;
using TMPro;

namespace Game.View.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private GameObject InGameUICanvas;
        [SerializeField] private GameObject MainMenuCanvas;
        [SerializeField] private GameObject TransitionCanvas;
        [SerializeField] private GameObject CurrencyCanvas;

        [Header("Win/Lose Panel")]
        [SerializeField] private GameObject levelWinPanel;
        [SerializeField] private GameObject levelLosePanel;
        [SerializeField] private GameObject mainSettingPanel;

        [Header("Game Events")]
        [SerializeField] private VoidEventChannelSO OnPlayGameEvent;
        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;
        [SerializeField] private VoidEventChannelSO OnNextLevelEvent;
        [SerializeField] private VoidEventChannelSO OnRestartLevelEvent;
        [SerializeField] private VoidEventChannelSO OnSettingShow;
        [SerializeField] private VoidEventChannelSO OnSettingHide;

        [Header("UI Components")]
        [SerializeField] private InventoryView inventoryView;

        private void OnEnable()
        {
            if (OnPlayGameEvent != null) OnPlayGameEvent.OnRaised += PlayGame;
            if (OnWinEvent != null) OnWinEvent.OnRaised += ShowWin;
            if (OnLoseEvent != null) OnLoseEvent.OnRaised += ShowLose;
            if (OnNextLevelEvent != null) OnNextLevelEvent.OnRaised += HideWin;
            if (OnRestartLevelEvent != null) OnRestartLevelEvent.OnRaised += HideLose;
            if (OnSettingShow != null) OnSettingShow.OnRaised += ShowSetting;
            if (OnSettingHide != null) OnSettingHide.OnRaised += HideSetting;
        }

        private void OnDisable()
        {
            if (OnPlayGameEvent != null) OnPlayGameEvent.OnRaised -= PlayGame;
            if (OnWinEvent != null) OnWinEvent.OnRaised -= ShowWin;
            if (OnLoseEvent != null) OnLoseEvent.OnRaised -= ShowLose;
            if (OnNextLevelEvent != null) OnNextLevelEvent.OnRaised -= HideWin;
            if (OnRestartLevelEvent != null) OnRestartLevelEvent.OnRaised -= HideLose;
            if (OnSettingShow != null) OnSettingShow.OnRaised -= ShowSetting;
            if (OnSettingHide != null) OnSettingHide.OnRaised -= HideSetting;
        }

        private void Awake()
        {
            if (levelWinPanel != null) levelWinPanel.SetActive(false);
            if (levelLosePanel != null) levelLosePanel.SetActive(false);
            mainSettingPanel?.SetActive(false);
        }

        public void Initialize(Inventory inventory)
        {
            if (inventoryView != null)
                inventoryView.BindData(inventory);
        }

        private void PlayGame()
        {
            MainMenuCanvas?.SetActive(false);
            InGameUICanvas?.SetActive(true);
            CurrencyCanvas?.SetActive(false);

            HideWin();
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
            mainSettingPanel.SetActive(true);
        }

        public void HideSetting()
        {
            mainSettingPanel.SetActive(false);
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
