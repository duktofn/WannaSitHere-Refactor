using UnityEngine;
using Game.Events;

namespace Game.View.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private GameObject InGameUI;
        [SerializeField] private GameObject MainMenu;
        [SerializeField] private GameObject Transition;

        [Header("Win/Lose Panel")]
        [SerializeField] private GameObject levelWinPanel;
        [SerializeField] private GameObject levelLosePanel;

        [Header("Game Events")]
        [SerializeField] private VoidEventChannelSO OnPlayGameEvent;
        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;
        [SerializeField] private VoidEventChannelSO OnNextLevelEvent;
        [SerializeField] private VoidEventChannelSO OnRestartLevelEvent;

        private void OnEnable()
        {
            if (OnPlayGameEvent != null) OnPlayGameEvent.OnRaised += PlayGame;
            if (OnWinEvent != null) OnWinEvent.OnRaised += ShowWin;
            if (OnLoseEvent != null) OnLoseEvent.OnRaised += ShowLose;
            if (OnNextLevelEvent != null) OnNextLevelEvent.OnRaised += HideWin;
            if (OnRestartLevelEvent != null) OnRestartLevelEvent.OnRaised += HideLose;
        }

        private void OnDisable()
        {
            if (OnPlayGameEvent != null) OnPlayGameEvent.OnRaised -= PlayGame;
            if (OnWinEvent != null) OnWinEvent.OnRaised -= ShowWin;
            if (OnLoseEvent != null) OnLoseEvent.OnRaised -= ShowLose;
            if (OnNextLevelEvent != null) OnNextLevelEvent.OnRaised -= HideWin;
            if (OnRestartLevelEvent != null) OnRestartLevelEvent.OnRaised -= HideLose;
        }

        private void Awake()
        {
            if (levelWinPanel != null) levelWinPanel.SetActive(false);
            if (levelLosePanel != null) levelLosePanel.SetActive(false);
        }

        private void PlayGame()
        {
            if (MainMenu != null) MainMenu.SetActive(false);
            if (InGameUI != null) InGameUI.SetActive(true);
            HideWin();
            HideLose();
        }

        public void ShowWin()
        {
            Debug.Log("[UIManager] Win Event raised");
            if (levelWinPanel != null) levelWinPanel.SetActive(true);
        }

        public void ShowLose()
        {
            Debug.Log("[UIManager] Lose Event raised");
            if (levelLosePanel != null) levelLosePanel.SetActive(true);
        }

        private void HideWin()
        {
            if (levelWinPanel != null) levelWinPanel.SetActive(false);
        }

        private void HideLose()
        {
            if (levelLosePanel != null) levelLosePanel.SetActive(false);
        }
    }
}
