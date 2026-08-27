using UnityEngine;
using Game.Events;

namespace Game.View.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject levelWinPanel;
        [SerializeField] private GameObject levelLosePanel;
        [SerializeField] private string winText;
        [SerializeField] private string loseText;

        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;

        private void OnEnable()
        {
            OnWinEvent.OnRaised += ShowWin;
            OnLoseEvent.OnRaised += ShowLose;
        }

        private void OnDisable()
        {
            OnWinEvent.OnRaised -= ShowWin;
            OnLoseEvent.OnRaised -= ShowLose;
        }

        private void Awake()
        {
            levelWinPanel.SetActive(false);
            levelLosePanel.SetActive(false);
        }

        public void ShowWin()
        {
            levelWinPanel.SetActive(true);
        }

        public void ShowLose()
        {
            levelLosePanel.SetActive(true);
        }
    }
}
