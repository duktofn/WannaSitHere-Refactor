using System.Collections.Generic;
using UnityEngine;
using Game.Core.Conditions;
using Game.Core.Economy;

namespace Game.App
{
    public sealed class GameManagerConfig
    {
        public int[] GoldShopLimits { get; set; } = new[] { 5, 5, 5 };
        public Reward[] GoldShopPrices { get; set; } = new Reward[3];
        public Reward[] GemShopPrices { get; set; } = new Reward[3];
        public Reward LevelWinReward { get; set; }
        public Reward LevelAdsWinReward { get; set; }
        public Reward DailyReward { get; set; }
        public Reward[] WeeklyRewards { get; set; } = new Reward[7];
        public int MoreMovesAmount { get; set; } = 3;
        public ConditionRuntimeData CanSitAnywhereCondition { get; set; }
        public List<Vector2Int> AdjacentOffsets { get; set; } = new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };
    }
}
