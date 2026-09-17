using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core.Economy;

namespace Game.View.UI
{
    public class WeeklyLogin : MonoBehaviour
    {
        [Header("UI Slots")]
        [SerializeField] private TextMeshProUGUI[] amountTexts;
        [SerializeField] private Image[] typeImg;
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private Sprite gemSprite;

        [Header("State Indicator (Optional)")]
        [SerializeField] private GameObject[] claimedMarks;
        [SerializeField] private Button claimButton;
        [SerializeField] private ButtonSpriteSwap claimButtonSwap;

        private List<Reward> _rewardDataList;
        private int _currentLoginDay;
        private bool _isWeeklyRewardClaimed;

        public void SetRewardData(List<Reward> rewardDataList)
        {
            _rewardDataList = rewardDataList;
            UpdateAmountTexts();
        }

        public void SetRewardData(List<Reward> rewardDataList, int currentLoginDay, bool isWeeklyRewardClaimed)
        {
            _rewardDataList = rewardDataList;
            _currentLoginDay = currentLoginDay;
            _isWeeklyRewardClaimed = isWeeklyRewardClaimed;
            UpdateAmountTexts();
        }

        public void UpdateAmountTexts()
        {
            if (_rewardDataList == null || amountTexts == null || typeImg == null)
                return;

            int count = Mathf.Min(amountTexts.Length, typeImg.Length, _rewardDataList.Count);
            for (int i = 0; i < count; i++)
            {
                if (typeImg[i] != null)
                {
                    if (_rewardDataList[i].type == ItemType.Gold && goldSprite != null)
                    {
                        typeImg[i].sprite = goldSprite;
                    }
                    else if (_rewardDataList[i].type == ItemType.Gem && gemSprite != null)
                    {
                        typeImg[i].sprite = gemSprite;
                    }
                }

                if (amountTexts[i] != null)
                {
                    amountTexts[i].text = _rewardDataList[i].amount.ToString();
                }

                if (claimedMarks != null && i < claimedMarks.Length && claimedMarks[i] != null)
                {
                    bool isClaimed = i < _currentLoginDay || (i == _currentLoginDay && _isWeeklyRewardClaimed);
                    claimedMarks[i].SetActive(isClaimed);
                }
            }

            if (claimButton != null)
            {
                bool canClaim = !_isWeeklyRewardClaimed;
                claimButton.interactable = canClaim;

                if (claimButtonSwap != null)
                {
                    claimButtonSwap.SetState(canClaim);
                }
                else if (claimButton.TryGetComponent<ButtonSpriteSwap>(out var swap))
                {
                    swap.SetState(canClaim);
                }
            }
        }
    }
}