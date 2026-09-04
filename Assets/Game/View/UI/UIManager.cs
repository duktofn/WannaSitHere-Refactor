using UnityEngine;
using Game.Events;

namespace Game.View.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject levelWinPanel;
        [SerializeField] private GameObject levelLosePanel;

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
            Debug.Log("Win Event raised");
            levelWinPanel.SetActive(true);
        }

        public void ShowLose()
        {
            Debug.Log("Lose Event raised");
            levelLosePanel.SetActive(true);
        }
    }
}
