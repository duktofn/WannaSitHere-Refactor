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
        public Reward[] goldShopPrice = new Reward[3];
        public int[] goldShopLimit = new int[3];
        public Reward[] gemShopPrice = new Reward[3];
    }
}
