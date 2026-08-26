using UnityEngine;
using Game.Events;

namespace Game.View.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject levelEndPanel;
        [SerializeField] private LevelEndText levelEndText;
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
            levelEndPanel.SetActive(false);
        }

        public void ShowWin()
        {
            levelEndText.SetDisplayText(winText);
            levelEndPanel.SetActive(true);
        }

        public void ShowLose()
        {
            levelEndText.SetDisplayText(loseText);
            levelEndPanel.SetActive(true);
        }
    }
}
