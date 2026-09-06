using UnityEngine;
using Game.Core.Economy;

namespace Game.Data.Economy {
    [CreateAssetMenu(fileName = "Economy Config", menuName = "Game/Economy")]
    public class EconomyConfigSO : ScriptableObject
    {
        public Reward levelWinReward;
        public Reward levelAdsWinReward;
        public Reward dailyReward;
        public Reward[] weeklyReward = new Reward[7];
    }
}
