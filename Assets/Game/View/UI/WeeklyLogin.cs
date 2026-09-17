using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using Game.Core.Economy;
using Game.View.Effect;

namespace Game.View.UI
{
    public class WeeklyLogin : MonoBehaviour
    {
        [Header("UI Slots - Daily")]
        [SerializeField] private TextMeshProUGUI dailyAmountText;
        [SerializeField] private Image dailyTypeImg;
        [SerializeField] private Button dailyClaimButton;
        [SerializeField] private ButtonSpriteSwap dailyClaimButtonSwap;

        [Header("UI Slots - Weekly")]
        [FormerlySerializedAs("amountTexts")]
        [SerializeField] private TextMeshProUGUI[] weeklyAmountTexts;
        [FormerlySerializedAs("typeImg")]
        [SerializeField] private Image[] weeklyTypeImg;
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private Sprite gemSprite;

        [Header("Weekly State Indicator (Optional)")]
        [SerializeField] private GameObject[] claimedMarks;
        [SerializeField] private Button claimButton;
        [SerializeField] private ButtonSpriteSwap claimButtonSwap;
        [SerializeField] private CurrencyFlyAnimation claimGoldCurrencyFly;
        [SerializeField] private CurrencyFlyAnimation claimGemCurrencyFly;

        private Reward _dailyReward;
        private bool _hasDailyReward;
        private bool _isDailyRewardClaimed;
        private List<Reward> _rewardDataList;
        private int _currentLoginDay;
        private bool _isWeeklyRewardClaimed;

        public void SetDailyReward(Reward dailyReward, bool isDailyRewardClaimed = false)
        {
            _dailyReward = dailyReward;
            _hasDailyReward = true;
            _isDailyRewardClaimed = isDailyRewardClaimed;
            UpdateAmountTexts();
        }

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

        public void SetRewardData(
            List<Reward> rewardDataList,
            int currentLoginDay,
            bool isWeeklyRewardClaimed,
            Reward dailyReward,
            bool isDailyRewardClaimed = false)
        {
            _dailyReward = dailyReward;
            _hasDailyReward = true;
            _isDailyRewardClaimed = isDailyRewardClaimed;
            _rewardDataList = rewardDataList;
            _currentLoginDay = currentLoginDay;
            _isWeeklyRewardClaimed = isWeeklyRewardClaimed;
            UpdateAmountTexts();
        }

        public void UpdateAmountTexts()
        {
            // Update Daily Reward UI
            if (_hasDailyReward)
            {
                if (dailyTypeImg != null)
                {
                    if (_dailyReward.type == ItemType.Gold && goldSprite != null)
                    {
                        dailyTypeImg.sprite = goldSprite;
                    }
                    else if (_dailyReward.type == ItemType.Gem && gemSprite != null)
                    {
                        dailyTypeImg.sprite = gemSprite;
                    }
                }

                if (dailyAmountText != null)
                {
                    dailyAmountText.text = _dailyReward.amount.ToString();
                }
            }

            // Update Daily Claim Button
            if (dailyClaimButton != null)
            {
                bool canClaimDaily = !_isDailyRewardClaimed;
                dailyClaimButton.interactable = canClaimDaily;

                if (dailyClaimButtonSwap != null)
                {
                    dailyClaimButtonSwap.SetState(canClaimDaily);
                }
                else if (dailyClaimButton.TryGetComponent<ButtonSpriteSwap>(out var swap))
                {
                    swap.SetState(canClaimDaily);
                }
            }

            // Update Weekly Reward UI
            if (_rewardDataList != null && weeklyAmountTexts != null && weeklyTypeImg != null)
            {
                int count = Mathf.Min(weeklyAmountTexts.Length, weeklyTypeImg.Length, _rewardDataList.Count);
                for (int i = 0; i < count; i++)
                {
                    if (weeklyTypeImg[i] != null)
                    {
                        if (_rewardDataList[i].type == ItemType.Gold && goldSprite != null)
                        {
                            weeklyTypeImg[i].sprite = goldSprite;
                        }
                        else if (_rewardDataList[i].type == ItemType.Gem && gemSprite != null)
                        {
                            weeklyTypeImg[i].sprite = gemSprite;
                        }
                    }

                    if (weeklyAmountTexts[i] != null)
                    {
                        weeklyAmountTexts[i].text = _rewardDataList[i].amount.ToString();
                    }

                    if (claimedMarks != null && i < claimedMarks.Length && claimedMarks[i] != null)
                    {
                        bool isClaimed = i < _currentLoginDay || (i == _currentLoginDay && _isWeeklyRewardClaimed);
                        claimedMarks[i].SetActive(isClaimed);
                    }
                }
            }

            // Update Weekly Claim Button
            if (claimButton != null)
            {
                bool canClaimWeekly = !_isWeeklyRewardClaimed;
                bool isDaySeven = _currentLoginDay == 6;
                claimButton.interactable = canClaimWeekly;

                if (claimGoldCurrencyFly != null)
                {
                    claimGoldCurrencyFly.enabled = !isDaySeven;
                }

                if (claimGemCurrencyFly != null)
                {
                    claimGemCurrencyFly.enabled = isDaySeven;
                }

                if (claimButtonSwap != null)
                {
                    claimButtonSwap.SetState(canClaimWeekly);
                }
                else if (claimButton.TryGetComponent<ButtonSpriteSwap>(out var swap))
                {
                    swap.SetState(canClaimWeekly);
                }
            }
        }
    }
}
