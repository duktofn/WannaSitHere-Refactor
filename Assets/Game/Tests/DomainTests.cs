using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Game.Core.Board;
using Game.Core.Booster;
using Game.Core.Conditions;
using Game.Core.Levels;
using Game.Core.People;
using Game.App;

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
        public void ConditionChecker_CanSitAnywhere_ReturnsTrueRegardlessOfNeighbors()
        {
            var checker = new ConditionChecker();
            var condition = CreateCanSitAnywhereCondition();

            Assert.IsTrue(checker.Check(new List<CellRuntimeData>(), condition));
            Assert.IsTrue(checker.Check(null, condition));
        }

        [Test]
        public void LevelConditionEvaluator_CanSitAnywhere_MarksPersonHappyWithoutFoodNeighbor()
        {
            var person = new PersonRuntimeData(
                "Anywhere",
                PersonTrait.Cool,
                new List<ConditionRuntimeData> { CreateCanSitAnywhereCondition() },
                null
            );
            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var cell = new CellRuntimeData(
                Vector2Int.zero,
                CellType.Seat,
                Vector2.one,
                person,
                Food.Any,
                null,
                GridId.MainGrid
            );
            mainGrid.Set(0, 0, cell);

            var evaluator = new LevelConditionEvaluator(new List<Vector2Int>
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            });

            evaluator.UpdateAllPersonStates(
                mainGrid,
                new Grid<CellRuntimeData>(new Vector2Int(1, 1))
            );

            Assert.AreEqual(PersonState.Happy, person.State);
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

        [Test]
        public void MoveHistory_RecordAndTryPop_ReturnsInLIFOOrder()
        {
            var history = new MoveHistory();
            var cellSize = new Vector2(1f, 1f);
            var c1 = new CellRuntimeData(Vector2Int.zero, CellType.Seat, cellSize, null, Food.Hamburger, null, GridId.MainGrid);
            var c2 = new CellRuntimeData(Vector2Int.right, CellType.Seat, cellSize, null, Food.Hamburger, null, GridId.MainGrid);
            var p1 = new PersonRuntimeData("P1", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);
            var p2 = new PersonRuntimeData("P2", PersonTrait.Sick, new List<ConditionRuntimeData>(), null);

            var r1 = new MoveRecord(c1, c2, p1, null);
            var r2 = new MoveRecord(c2, c1, p2, null);

            history.Record(r1);
            history.Record(r2);

            Assert.AreEqual(2, history.Count);
            Assert.IsTrue(history.HasHistory);

            Assert.IsTrue(history.TryPop(out MoveRecord popped1));
            Assert.AreSame(p2, popped1.MovedPerson);

            Assert.IsTrue(history.TryPop(out MoveRecord popped2));
            Assert.AreSame(p1, popped2.MovedPerson);

            Assert.IsFalse(history.TryPop(out _));
            Assert.IsFalse(history.HasHistory);
        }

        [Test]
        public void MoveHistory_Clear_RemovesAllRecords()
        {
            var history = new MoveHistory();
            var cell = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            var p = new PersonRuntimeData("P", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);

            history.Record(new MoveRecord(cell, cell, p, null));
            history.Clear();

            Assert.AreEqual(0, history.Count);
            Assert.IsFalse(history.HasHistory);
            Assert.IsFalse(history.TryPop(out _));
        }

        [Test]
        public void MoreMoveBooster_TryUse_AddsConfiguredMoves()
        {
            var level = new LevelRuntimeData(5, new Grid<CellRuntimeData>(Vector2Int.one), new Grid<CellRuntimeData>(Vector2Int.one));
            var booster = new MoreMoveBooster(level, 3);

            bool used = false;
            booster.OnBoosterUsed += () => used = true;

            Assert.IsTrue(booster.TryUse());
            Assert.IsTrue(used);
            Assert.AreEqual(8, level.CurrentMove);
        }

        [Test]
        public void UndoBooster_TryUse_RevertsCellsAndRefundsMove()
        {
            var cellSize = Vector2.one;
            var c1 = new CellRuntimeData(Vector2Int.zero, CellType.Seat, cellSize, null, Food.Hamburger, null, GridId.MainGrid);
            var c2 = new CellRuntimeData(Vector2Int.right, CellType.Seat, cellSize, null, Food.Hamburger, null, GridId.MainGrid);

            var p1 = new PersonRuntimeData("P1", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);
            var p2 = new PersonRuntimeData("P2", PersonTrait.Sick, new List<ConditionRuntimeData>(), null);

            c1.SetPerson(p2);
            c2.SetPerson(p1);

            var history = new MoveHistory();
            history.Record(new MoveRecord(c1, c2, p1, p2));

            var level = new LevelRuntimeData(5, new Grid<CellRuntimeData>(Vector2Int.one), new Grid<CellRuntimeData>(Vector2Int.one));
            var booster = new UndoBooster(history, level);

            Assert.IsTrue(booster.TryUse());
            Assert.AreEqual(6, level.CurrentMove); // refunded 1 move
            Assert.AreSame(p1, c1.CurrentPerson); // restored to original
            Assert.AreSame(p2, c2.CurrentPerson); // restored to original
            Assert.IsTrue(booster.LastUndoneRecord.HasValue);
            Assert.IsFalse(history.HasHistory);
        }

        [Test]
        public void UndoBooster_TryUse_WhenNoHistory_ReturnsFalse()
        {
            var history = new MoveHistory();
            var level = new LevelRuntimeData(5, new Grid<CellRuntimeData>(Vector2Int.one), new Grid<CellRuntimeData>(Vector2Int.one));
            var booster = new UndoBooster(history, level);

            Assert.IsFalse(booster.TryUse());
            Assert.AreEqual(5, level.CurrentMove);
        }

        [Test]
        public void PersonRuntimeData_ClearConditions_RemovesAllAndFiresEvent()
        {
            var cond = new ConditionRuntimeData(ConditionType.Like, ConditionTarget.Food, PersonTrait.Cool, Food.Hamburger, "desc", "angry");
            var person = new PersonRuntimeData("P", PersonTrait.Cool, new List<ConditionRuntimeData> { cond }, null);

            bool clearedFired = false;
            person.OnConditionsCleared += () => clearedFired = true;

            Assert.AreEqual(1, person.Conditions.Count);
            person.ClearConditions();

            Assert.AreEqual(0, person.Conditions.Count);
            Assert.IsTrue(clearedFired);
        }

        [Test]
        public void RemoveBooster_TryUse_PrioritizesAngryPersonOnMainGrid()
        {
            var cond = new ConditionRuntimeData(ConditionType.Like, ConditionTarget.Food, PersonTrait.Cool, Food.Hamburger, "desc", "angry");
            var angryPerson = new PersonRuntimeData("Angry", PersonTrait.Cool, new List<ConditionRuntimeData> { cond }, null);
            angryPerson.SetState(PersonState.Angry);

            var waitPerson = new PersonRuntimeData("Wait", PersonTrait.Sick, new List<ConditionRuntimeData> { cond }, null);

            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var mainCell = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            mainCell.SetPerson(angryPerson);
            mainGrid.Set(0, 0, mainCell);

            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var waitCell = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.WaitGrid);
            waitCell.SetPerson(waitPerson);
            waitGrid.Set(0, 0, waitCell);

            var level = new LevelRuntimeData(5, mainGrid, waitGrid);
            var booster = new RemoveBooster(level, CreateCanSitAnywhereCondition());

            Assert.IsTrue(booster.TryUse());
            Assert.AreSame(angryPerson, booster.TargetPerson);
            Assert.AreEqual(1, angryPerson.Conditions.Count);
            Assert.IsTrue(angryPerson.Conditions[0].IsCanSitAnywhere);
            Assert.AreEqual(1, waitPerson.Conditions.Count); // Wait person untouched
        }

        [Test]
        public void RemoveBooster_TryUse_FallsBackToWaitGridWhenNoAngry()
        {
            var cond = new ConditionRuntimeData(ConditionType.Like, ConditionTarget.Food, PersonTrait.Cool, Food.Hamburger, "desc", "angry");
            var happyPerson = new PersonRuntimeData("Happy", PersonTrait.Cool, new List<ConditionRuntimeData> { cond }, null);
            happyPerson.SetState(PersonState.Happy);

            var waitPerson = new PersonRuntimeData("Wait", PersonTrait.Sick, new List<ConditionRuntimeData> { cond }, null);

            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var mainCell = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            mainCell.SetPerson(happyPerson);
            mainGrid.Set(0, 0, mainCell);

            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var waitCell = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.WaitGrid);
            waitCell.SetPerson(waitPerson);
            waitGrid.Set(0, 0, waitCell);

            var level = new LevelRuntimeData(5, mainGrid, waitGrid);
            var booster = new RemoveBooster(level, CreateCanSitAnywhereCondition());

            Assert.IsTrue(booster.TryUse());
            Assert.AreSame(waitPerson, booster.TargetPerson);
            Assert.AreEqual(1, waitPerson.Conditions.Count);
            Assert.IsTrue(waitPerson.Conditions[0].IsCanSitAnywhere);
        }

        [Test]
        public void RemoveBooster_TryUse_SkipsPersonWhoCanSitAnywhere()
        {
            var canSitAnywhere = CreateCanSitAnywhereCondition();
            var anyPerson = new PersonRuntimeData(
                "Anywhere",
                PersonTrait.Cool,
                new List<ConditionRuntimeData> { canSitAnywhere },
                null
            );
            anyPerson.SetState(PersonState.Angry);

            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var mainCell = new CellRuntimeData(
                Vector2Int.zero,
                CellType.Seat,
                Vector2.one,
                null,
                Food.Hamburger,
                null,
                GridId.MainGrid
            );
            mainCell.SetPerson(anyPerson);
            mainGrid.Set(0, 0, mainCell);

            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var level = new LevelRuntimeData(5, mainGrid, waitGrid);
            var booster = new RemoveBooster(level, canSitAnywhere);

            Assert.IsFalse(booster.TryUse());
            Assert.IsNull(booster.TargetPerson);
            Assert.AreEqual(1, anyPerson.Conditions.Count);
        }

        [Test]
        public void RemoveBooster_TryUse_ReplacesAllConditionsWithCanSitAnywhere()
        {
            var original = new ConditionRuntimeData(
                ConditionType.Hate,
                ConditionTarget.Person,
                PersonTrait.Sick,
                Food.Any,
                "Hates Sick",
                "Angry at Sick"
            );
            var replacement = CreateCanSitAnywhereCondition();
            var person = new PersonRuntimeData(
                "Target",
                PersonTrait.Cool,
                new List<ConditionRuntimeData> { original, original },
                null
            );
            person.SetState(PersonState.Angry);

            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var mainCell = new CellRuntimeData(
                Vector2Int.zero,
                CellType.Seat,
                Vector2.one,
                null,
                Food.Hamburger,
                null,
                GridId.MainGrid
            );
            mainCell.SetPerson(person);
            mainGrid.Set(0, 0, mainCell);

            var level = new LevelRuntimeData(
                5,
                mainGrid,
                new Grid<CellRuntimeData>(new Vector2Int(1, 1))
            );
            var booster = new RemoveBooster(level, replacement);

            Assert.IsTrue(booster.TryUse());
            Assert.AreSame(person, booster.TargetPerson);
            Assert.AreEqual(1, person.Conditions.Count);
            Assert.AreSame(replacement, person.Conditions[0]);
            Assert.IsTrue(person.Conditions[0].IsCanSitAnywhere);
        }

        private static ConditionRuntimeData CreateCanSitAnywhereCondition()
        {
            return new ConditionRuntimeData(
                ConditionType.Like,
                ConditionTarget.Food,
                PersonTrait.Cool,
                Food.Any,
                "I can sit anywhere",
                string.Empty
            );
        }

        [Test]
        public void LevelManager_TryMovePerson_MainGridToMainGrid_RecordsInMoveHistory()
        {
            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(2, 1));
            var p1 = new PersonRuntimeData("P1", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);
            var c1 = new CellRuntimeData(new Vector2Int(0, 0), CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            var c2 = new CellRuntimeData(new Vector2Int(1, 0), CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            c1.SetPerson(p1);
            mainGrid.Set(0, 0, c1);
            mainGrid.Set(1, 0, c2);

            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var level = new LevelRuntimeData(10, mainGrid, waitGrid);
            var lm = new LevelManager(level, new List<Vector2Int> { Vector2Int.right, Vector2Int.left });

            bool moveSuccess = lm.TryMovePerson(c1, c2, p1);

            Assert.IsTrue(moveSuccess);
            Assert.AreEqual(1, lm.MoveHistory.Count);
            Assert.IsTrue(lm.MoveHistory.TryPop(out MoveRecord record));
            Assert.AreSame(c1, record.SourceCell);
            Assert.AreSame(c2, record.TargetCell);
            Assert.AreSame(p1, record.MovedPerson);
        }

        [Test]
        public void LevelManager_TryMovePerson_WaitGridToMainGrid_RecordsInMoveHistory()
        {
            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var cMain = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            mainGrid.Set(0, 0, cMain);

            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var pWait = new PersonRuntimeData("WaitP", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);
            var cWait = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.WaitGrid);
            cWait.SetPerson(pWait);
            waitGrid.Set(0, 0, cWait);

            var level = new LevelRuntimeData(10, mainGrid, waitGrid);
            var lm = new LevelManager(level, new List<Vector2Int> { Vector2Int.right, Vector2Int.left });

            bool moveSuccess = lm.TryMovePerson(cWait, cMain, pWait);

            Assert.IsTrue(moveSuccess);
            // Move from WaitLine to Board must be recorded for undo
            Assert.AreEqual(1, lm.MoveHistory.Count);
            Assert.IsTrue(lm.MoveHistory.TryPop(out MoveRecord record));
            Assert.AreSame(cWait, record.SourceCell);
            Assert.AreSame(cMain, record.TargetCell);
            Assert.AreSame(pWait, record.MovedPerson);
        }

        [Test]
        public void LevelManager_TryMovePerson_WaitGridToWaitGrid_DoesNotRecordInMoveHistory()
        {
            var mainGrid = new Grid<CellRuntimeData>(new Vector2Int(1, 1));
            var waitGrid = new Grid<CellRuntimeData>(new Vector2Int(2, 1));
            var pWait = new PersonRuntimeData("WaitP", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);
            var cWait1 = new CellRuntimeData(new Vector2Int(0, 0), CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.WaitGrid);
            var cWait2 = new CellRuntimeData(new Vector2Int(1, 0), CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.WaitGrid);
            cWait1.SetPerson(pWait);
            waitGrid.Set(0, 0, cWait1);
            waitGrid.Set(1, 0, cWait2);

            var level = new LevelRuntimeData(10, mainGrid, waitGrid);
            var lm = new LevelManager(level, new List<Vector2Int> { Vector2Int.right, Vector2Int.left });

            bool moveSuccess = lm.TryMovePerson(cWait1, cWait2, pWait);

            Assert.IsTrue(moveSuccess);
            // User requirement: internal WaitLine to WaitLine moves must NOT be recorded
            Assert.AreEqual(0, lm.MoveHistory.Count);
        }

        [Test]
        public void UndoBooster_TryUse_UndoesMoveFromWaitGridToMainGrid_RestoresPersonToWaitGrid()
        {
            var cWait = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.WaitGrid);
            var cMain = new CellRuntimeData(Vector2Int.zero, CellType.Seat, Vector2.one, null, Food.Hamburger, null, GridId.MainGrid);
            var p = new PersonRuntimeData("P", PersonTrait.Cool, new List<ConditionRuntimeData>(), null);

            // Simulating post-move state: p was moved from cWait to cMain
            cMain.SetPerson(p);
            cWait.SetPerson(null);

            var history = new MoveHistory();
            history.Record(new MoveRecord(cWait, cMain, p, null));

            var level = new LevelRuntimeData(5, new Grid<CellRuntimeData>(Vector2Int.one), new Grid<CellRuntimeData>(Vector2Int.one));
            var booster = new UndoBooster(history, level);

            Assert.IsTrue(booster.TryUse());
            Assert.AreEqual(6, level.CurrentMove);
            Assert.AreSame(p, cWait.CurrentPerson); // returned to WaitGrid
            Assert.IsNull(cMain.CurrentPerson);     // empty on MainGrid
        }
    }
}
