using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core.Economy;

namespace Game.View.UI
{
    public class WeeklyLogin : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] amountTexts;
        [SerializeField] private Image[] typeImg;
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private Sprite gemSprite;
        private List<Reward> _rewardDataList;

        public void SetRewardData(List<Reward> rewardDataList)
        {
            _rewardDataList = rewardDataList;
        }

        private void Awake()
        {
            amountTexts = new TextMeshProUGUI[7];
            typeImg = new Image[7];
            UpdateAmountTexts();
        }

        private void UpdateAmountTexts()
        {
            for (int i = 0; i < amountTexts.Length; i++)
            {
                if (i < _rewardDataList.Count)
                {
                    if (_rewardDataList[i].type == ItemType.Gold)
                    {
                        typeImg[i].sprite = goldSprite;
                    }
                    else if (_rewardDataList[i].type == ItemType.Gem)
                    {
                        typeImg[i].sprite = gemSprite;
                    }

                    amountTexts[i].text = _rewardDataList[i].amount.ToString();
                }
            }
        }
    }
}