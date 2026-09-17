using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.Levels;
using Game.Core.People;

namespace Game.Tests.EditMode
{
    public class DomainTests
    {
        [Test]
        public void Grid_GetAndSet_WorksCorrectlyWithinBounds()
        {
            var grid = new Grid<int>(new Vector2Int(3, 3));
            grid.Set(1, 1, 42);

            Assert.AreEqual(42, grid.Get(1, 1));
            Assert.AreEqual(0, grid.Get(0, 0));
        }

        [Test]
        public void Grid_OutOfBounds_ReturnsDefaultAndDoesNotThrow()
        {
            var grid = new Grid<int>(new Vector2Int(2, 2));

            Assert.AreEqual(0, grid.Get(-1, 0));
            Assert.AreEqual(0, grid.Get(2, 2));

            Assert.DoesNotThrow(() => grid.Set(-1, 0, 10));
            Assert.DoesNotThrow(() => grid.Set(5, 5, 10));
        }

        [Test]
        public void PersonRuntimeData_SetState_FiresOnPersonStateChangedEvent()
        {
            var person = new PersonRuntimeData("TestPerson", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);
            PersonState receivedState = PersonState.Normal;
            bool eventFired = false;

            person.OnPersonStateChanged += (state) =>
            {
                eventFired = true;
                receivedState = state;
            };

            person.SetState(PersonState.Happy);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(PersonState.Happy, receivedState);
            Assert.AreEqual(PersonState.Happy, person.State);
        }

        [Test]
        public void ConditionChecker_HateFoodCondition_ReturnsFalseWhenAdjacentMatches()
        {
            var checker = new ConditionChecker();
            var condition = new ConditionRuntimeData(
                ConditionType.Hate,
                ConditionTarget.Food,
                PersonTrait.Cool,
                Food.Hamburger,
                "Hates Hamburger",
                "Angry at Hamburger"
            );

            var cellSize = new Vector2(0.5f, 0.5f);
            var cellWithFood = new CellRuntimeData(
                Vector2Int.zero,
                CellType.Food,
                cellSize,
                null,
                Food.Hamburger,
                null,
                GridId.MainGrid
            );
            var adjacentCells = new List<CellRuntimeData> { cellWithFood };

            bool result = checker.Check(adjacentCells, condition);

            Assert.IsFalse(result);
        }

        [Test]
        public void ConditionChecker_HatePersonCondition_IgnoresEmptySeat()
        {
            var checker = new ConditionChecker();
            var condition = new ConditionRuntimeData(
                ConditionType.Hate,
                ConditionTarget.Person,
                PersonTrait.Cool,
                Food.Hamburger,
                "Hates Cool person",
                "Angry at Cool person"
            );

            var emptySeat = new CellRuntimeData(
                Vector2Int.zero,
                CellType.Seat,
                new Vector2(0.5f, 0.5f),
                null,
                Food.Hamburger,
                null,
                GridId.MainGrid
            );

            Assert.IsTrue(checker.Check(new List<CellRuntimeData> { emptySeat }, condition));
        }

        [Test]
        public void ConditionChecker_LikeFoodCondition_RequiresMatchingFood()
        {
            var checker = new ConditionChecker();
            var condition = new ConditionRuntimeData(
                ConditionType.Like,
                ConditionTarget.Food,
                PersonTrait.Cool,
                Food.Hamburger,
                "Likes Hamburger",
                "Angry without Hamburger"
            );

            var matchingFood = new CellRuntimeData(
                Vector2Int.zero,
                CellType.Food,
                new Vector2(0.5f, 0.5f),
                null,
                Food.Hamburger,
                null,
                GridId.MainGrid
            );

            Assert.IsTrue(checker.Check(new List<CellRuntimeData> { matchingFood }, condition));
            Assert.IsFalse(checker.Check(new List<CellRuntimeData>(), condition));
        }

        [Test]
        public void LevelRuntimeData_ModifyMove_UpdatesLevelMoveCount()
        {
            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(2, 2));
            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(2, 1));
            var level = new LevelRuntimeData(10, mainGrid, waitGrid);

            level.ModifyMove(-1);
            Assert.AreEqual(9, level.CurrentMove);

            level.ModifyMove(3);
            Assert.AreEqual(12, level.CurrentMove);
        }

        [Test]
        public void Inventory_SetAmountAndAddAmount_ClampsToZeroAndUpdatesValues()
        {
            var inv = new Game.Core.Economy.Inventory(100, 50, 5, 5, 5);
            bool updated = false;
            inv.OnInventoryUpdate += () => updated = true;

            inv.SetAmount(Game.Core.Economy.ItemType.Gold, 500);
            Assert.IsTrue(updated);
            Assert.AreEqual(500, inv.Gold);

            inv.SetAmount(Game.Core.Economy.ItemType.Gem, -50);
            Assert.AreEqual(0, inv.Gem);

            inv.AddAmount(Game.Core.Economy.ItemType.Remove, 3);
            Assert.AreEqual(8, inv.Remove);
        }

        [Test]
        public void EconomyManager_SimulateNextDay_AdvancesDayAndResetsClaimsAndShop()
        {
            var inv = new Game.Core.Economy.Inventory(0, 0, 0, 0, 0);
            var eco = new Game.Core.Economy.EconomyManager(inv, 2, true, true, new int[] { 5, 5, 5 }, new int[] { 3, 2, 1 });

            eco.SimulateNextDay();

            Assert.AreEqual(3, eco.CurrentLoginDay);
            Assert.IsFalse(eco.IsDailyRewardClaimed);
            Assert.IsFalse(eco.IsWeeklyRewardClaimed);
            Assert.AreEqual(0, eco.GetGoldShopPurchaseCount(0));
            Assert.AreEqual(0, eco.GetGoldShopPurchaseCount(1));
            Assert.AreEqual(0, eco.GetGoldShopPurchaseCount(2));
        }

        [Test]
        public void EconomyManager_ResetLoginStreak_SetsDayZeroAndResetsAll()
        {
            var inv = new Game.Core.Economy.Inventory(0, 0, 0, 0, 0);
            var eco = new Game.Core.Economy.EconomyManager(inv, 5, true, true, new int[] { 5, 5, 5 }, new int[] { 5, 5, 5 });

            eco.ResetLoginStreak();

            Assert.AreEqual(0, eco.CurrentLoginDay);
            Assert.IsFalse(eco.IsDailyRewardClaimed);
            Assert.IsFalse(eco.IsWeeklyRewardClaimed);
            Assert.AreEqual(0, eco.GetGoldShopPurchaseCount(0));
        }
    }
}
