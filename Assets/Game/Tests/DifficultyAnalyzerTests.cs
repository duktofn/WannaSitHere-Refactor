using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.People;
using Game.Data.Board;
using Game.Data.Conditions;
using Game.Data.Levels;
using Game.Data.People;
using Game.Editor.Difficulty;

namespace Game.Tests.EditMode
{
    public sealed class DifficultyAnalyzerTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                    Object.DestroyImmediate(_createdObjects[i]);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void FoodCondition_UsesNewModelGolden()
        {
            var r = Analyze(CreateFoodGoalLevel(1, true));
            Assert.That(r.Valid && r.HasExactMoves && r.HasScore);
            Assert.AreEqual(1, r.MinMoves);
            Assert.AreEqual(2.0 / 3, r.Tightness, 1e-12);
            Assert.AreEqual(1.0 / 3, r.Occupancy, 1e-12);
            Assert.AreEqual(0.6, r.ConditionComplexity, 1e-12);
            Assert.AreEqual(0, r.DependencyComplexity);
            Assert.AreEqual(1, r.MovePressure, 1e-12);
            Assert.AreEqual(50, r.DifficultyScore, 1e-9);
        }

        [Test]
        public void DisabledAndBudgetedSolver_DoNotInventMinimumMovesOrScore()
        {
            var level = CreateFoodGoalLevel(1, true);
            foreach (var settings in new[] {
                new DifficultyAnalysisSettings { UseSolver = false },
                new DifficultyAnalysisSettings { MaxNodes = 1 } })
            {
                var r = Analyze(level, settings);
                Assert.IsFalse(r.HasExactMoves);
                Assert.IsFalse(r.HasScore);
                Assert.AreEqual(-1, r.MinMoves);
                Assert.AreEqual(0.6, r.ConditionComplexity, 1e-12);
            }
            Assert.AreEqual(DifficultyAnalysisStatus.LimitExceeded,
                Analyze(level, new DifficultyAnalysisSettings { MaxNodes = 1 }).Status);
        }

        [Test]
        public void ReachingWaitLowerBound_CertifiesMinimumWithoutExhaustiveEnumeration()
        {
            var level = CreateAnywhereLevel();
            level.personConfigs.Add(CreatePerson("Other", PersonTrait.Dirty, Vector2Int.right));
            var r = Analyze(level, new DifficultyAnalysisSettings { MaxNodes = 11 });
            Assert.IsFalse(r.SolverExact);
            Assert.IsTrue(r.HasExactMoves);
            Assert.IsTrue(r.HasScore);
            Assert.AreEqual(0, r.MinMoves);
        }

        [Test]
        public void PersonProbability_UsesPerSeatHypergeometricAndExcludesSelf()
        {
            var level = CreateFoodGoalLevel(5, true);
            var c = level.personConfigs[0].conditions[0];
            c.target = ConditionTarget.Person;
            c.targetTrait = PersonTrait.Dirty;
            level.personConfigs.Add(CreatePerson("Other", PersonTrait.Dirty, Vector2Int.right));
            var r = Analyze(level);
            Assert.AreEqual(2.0 / 3, r.Conditions[0].Probability, 1e-12);
            Assert.AreEqual(0.5, r.SeatProbabilities[0].JointProbability, 1e-12);
            Assert.AreEqual(1, r.SeatProbabilities[1].JointProbability, 1e-12);
            Assert.AreEqual(1, r.Conditions[0].MatchingOthers);
            Assert.AreEqual(0.25, r.LikeDensity, 1e-12);
            Assert.AreEqual(1, r.MaxDependencyDepth);
            Assert.AreEqual(0.5, r.DependencyDepth, 1e-12);
            Assert.AreEqual(19.708333333333336, r.DifficultyScore, 1e-9);
        }

        [Test]
        public void Hypergeometric_HandlesNoNeighborsNoTargetsAndLargeCounts()
        {
            Assert.AreEqual(1, DifficultyAnalysisJob.NoAdjacentProbability(1, 0, 0));
            Assert.AreEqual(1, DifficultyAnalysisJob.NoAdjacentProbability(5, 0, 2));
            Assert.AreEqual(0, DifficultyAnalysisJob.NoAdjacentProbability(3, 2, 1));
            Assert.AreEqual(1.0 / 6, DifficultyAnalysisJob.NoAdjacentProbability(5, 2, 2), 1e-12);
            Assert.That(DifficultyAnalysisJob.NoAdjacentProbability(10000, 4, 5000), Is.InRange(0.0, 1.0));
        }

        [Test]
        public void MissingLikeProvider_IsUnsolvableWithoutScore()
        {
            var level = CreateAnywhereLevel();
            var c = level.personConfigs[0].conditions[0];
            c.target = ConditionTarget.Person;
            c.targetTrait = PersonTrait.Cool;
            var r = Analyze(level);
            Assert.AreEqual(0, r.Conditions[0].MatchingOthers);
            Assert.AreEqual(DifficultyAnalysisStatus.Unsolvable, r.Status);
            Assert.IsFalse(r.HasScore);
        }

        [Test]
        public void HateFoodAny_MatchesGameplayRatherThanCanSitAnywhere()
        {
            var level = CreateFoodGoalLevel(5, true);
            var c = level.personConfigs[0].conditions[0];
            c.type = ConditionType.Hate;
            c.foodTarget = Food.Any;
            var r = Analyze(level);
            Assert.AreEqual(2, r.SolutionsFound);
            Assert.AreEqual(0, r.IgnoredConditions);
            Assert.AreEqual(2.0 / 3, r.Conditions[0].Probability, 1e-12);
        }

        [Test]
        public void DuplicateConditions_DoNotSquareTheSameProbability()
        {
            var level = CreateFoodGoalLevel(5, true);
            var c = level.personConfigs[0].conditions[0];
            c.target = ConditionTarget.Person;
            c.targetTrait = PersonTrait.Dirty;
            level.personConfigs.Add(CreatePerson("Other", PersonTrait.Dirty, Vector2Int.right));
            double before = Analyze(level).People[0].ExpectedSeatRatio;
            level.personConfigs[0].conditions.Add(c);
            Assert.AreEqual(before, Analyze(level).People[0].ExpectedSeatRatio, 1e-12);
        }

        [Test]
        public void OpposingSameTarget_IsUnsolvableEvenWithSolverDisabled()
        {
            var level = CreateFoodGoalLevel(5, true);
            var hate = NewPersonCondition(ConditionType.Hate, PersonTrait.Cool);
            hate.target = ConditionTarget.Food;
            hate.foodTarget = Food.Hamburger;
            level.personConfigs[0].conditions.Add(hate);
            var r = Analyze(level, new DifficultyAnalysisSettings { UseSolver = false });
            Assert.IsFalse(r.Valid);
            Assert.IsFalse(r.HasScore);
        }

        [Test]
        public void DependencyCycle_ExcludesTailPeople()
        {
            var level = CreateAnywhereLevel();
            level.personConfigs[0].conditions.Clear();
            level.personConfigs[0].conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Dirty));
            var other = CreatePerson("Cycle B", PersonTrait.Dirty, Vector2Int.right);
            other.conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Cool));
            level.personConfigs.Add(other);
            var tail = CreatePerson("Tail", PersonTrait.Sick, new Vector2Int(2, 0));
            tail.conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Cool));
            level.personConfigs.Add(tail);
            var r = Analyze(level, new DifficultyAnalysisSettings { UseSolver = false });
            Assert.AreEqual(3, r.BlockedPeople);
            Assert.AreEqual(2, r.CyclePeople);
            Assert.IsTrue(r.People[0].InDependencyCycle);
            Assert.IsTrue(r.People[1].InDependencyCycle);
            Assert.IsFalse(r.People[2].InDependencyCycle);
            Assert.AreEqual(2.0 / 3, r.CycleRatio, 1e-12);
        }

        [Test]
        public void AlternativeGroundedProvider_BreaksCycleAndUsesPreviousLayersOnly()
        {
            var level = CreateAnywhereLevel();
            level.personConfigs[0].conditions.Clear();
            level.personConfigs[0].conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Dirty));
            var other = CreatePerson("B", PersonTrait.Dirty, Vector2Int.right);
            other.conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Cool));
            level.personConfigs.Add(other);
            level.personConfigs.Add(CreatePerson("Grounded", PersonTrait.Cool, new Vector2Int(2, 0)));
            var r = Analyze(level, new DifficultyAnalysisSettings { UseSolver = false });
            Assert.AreEqual(0, r.CyclePeople);
            Assert.AreEqual(2, r.People[0].DependencyLayer);
            Assert.AreEqual(1, r.People[1].DependencyLayer);
            Assert.AreEqual(0, r.People[2].DependencyLayer);
        }

        [Test]
        public void MultipleTargetTraits_RequireAllProvidersAndHateDoesNotCreateDependency()
        {
            var level = CreateAnywhereLevel();
            level.personConfigs[0].conditions.Clear();
            level.personConfigs[0].conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Dirty));
            level.personConfigs[0].conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Sick));
            var middle = CreatePerson("Middle", PersonTrait.Dirty, Vector2Int.right);
            middle.conditions.Add(NewPersonCondition(ConditionType.Like, PersonTrait.Sick));
            level.personConfigs.Add(middle);
            var root = CreatePerson("Root", PersonTrait.Sick, new Vector2Int(2, 0));
            root.conditions.Add(NewPersonCondition(ConditionType.Hate, PersonTrait.Cool));
            level.personConfigs.Add(root);
            var r = Analyze(level, new DifficultyAnalysisSettings { UseSolver = false });
            Assert.AreEqual(2, r.People[0].DependencyLayer);
            Assert.AreEqual(1, r.People[1].DependencyLayer);
            Assert.AreEqual(0, r.People[2].DependencyLayer);
            Assert.AreEqual(3, r.LikePersonConditions);
        }

        [Test]
        public void WaitPeopleAndRearrangement_UsePersonCountRatherThanWaitSeatCapacity()
        {
            var level = CreateAnywhereLevel();
            level.waitGrid = CreateGrid(new Vector2Int(3, 1));
            for (int i = 0; i < 3; i++) level.waitGrid.Set(i, 0, CreateCell(CellType.Seat));
            level.personConfigs[0].gridId = GridId.WaitGrid;
            var r = Analyze(level);
            Assert.AreEqual(3, r.W);
            Assert.AreEqual(1, r.WaitPeople);
            Assert.AreEqual(1, r.MinMoves);
            Assert.AreEqual(0, r.Rearrangement);
            Assert.AreEqual(0.2, r.BudgetTightness, 1e-12);
        }

        [Test]
        public void ImpossibleAndInsufficientMoves_HaveNoScore()
        {
            foreach (var level in new[] { CreateFoodGoalLevel(5, false), CreateFoodGoalLevel(0, true) })
            {
                var r = Analyze(level);
                Assert.IsFalse(r.Valid);
                Assert.IsFalse(r.HasScore);
                Assert.AreEqual(DifficultyAnalysisStatus.Unsolvable, r.Status);
            }
            var overfull = CreateAnywhereLevel();
            overfull.mainGrid = CreateGrid(Vector2Int.one);
            overfull.mainGrid.Set(0, 0, CreateCell(CellType.Seat));
            overfull.waitGrid.Set(0, 0, CreateCell(CellType.Seat));
            var waiting = CreatePerson("Waiting", PersonTrait.Dirty, Vector2Int.zero);
            waiting.gridId = GridId.WaitGrid;
            overfull.personConfigs.Add(waiting);
            Assert.IsFalse(Analyze(overfull).HasScore);
        }

        [Test]
        public void CompetingForSingleFoodSeat_SolverProvesNoWinEvenWhenEachPersonHasOptions()
        {
            var level = CreateFoodGoalLevel(5, true);
            var other = CreatePerson("Other food lover", PersonTrait.Dirty, Vector2Int.right);
            other.conditions.Add(level.personConfigs[0].conditions[0]);
            level.personConfigs.Add(other);
            var r = Analyze(level);
            Assert.That(r.People[0].ExpectedSeatRatio, Is.GreaterThan(0));
            Assert.That(r.People[1].ExpectedSeatRatio, Is.GreaterThan(0));
            Assert.IsTrue(r.SolverExact);
            Assert.AreEqual(0, r.SolutionsFound);
            Assert.AreEqual(DifficultyAnalysisStatus.Unsolvable, r.Status);
            Assert.IsFalse(r.HasScore);
        }

        [Test]
        public void ZeroBudgetAlreadyWon_HasFiniteZeroMovePressure()
        {
            var level = CreateAnywhereLevel();
            level.levelMove = 0;
            var r = Analyze(level);
            Assert.IsTrue(r.HasScore);
            Assert.AreEqual(0, r.MovePressure);
        }

        [Test]
        public void EmptyMalformedAndInvalidSettings_HaveNoScore()
        {
            Assert.IsFalse(Analyze(CreateLevel(5, Vector2Int.one)).HasScore);
            var malformed = CreateAnywhereLevel();
            malformed.personConfigs.Add(CreatePerson("Overlap", PersonTrait.Dirty, Vector2Int.zero));
            Assert.IsFalse(Analyze(malformed).HasScore);
            Assert.IsFalse(Analyze(CreateAnywhereLevel(), new DifficultyAnalysisSettings { MaxNodes = 0 }).HasScore);
        }

        [Test]
        public void Fingerprint_TracksNestedAssetsAndSettings()
        {
            var level = CreateAnywhereLevel();
            var settings = new DifficultyAnalysisSettings();
            string initial = DifficultyAnalysisJob.ComputeInputFingerprint(level, settings);
            level.personConfigs[0].conditions[0].foodTarget = Food.Hamburger;
            Assert.AreNotEqual(initial, DifficultyAnalysisJob.ComputeInputFingerprint(level, settings));
            initial = DifficultyAnalysisJob.ComputeInputFingerprint(level, settings);
            settings.MaxNodes++;
            Assert.AreNotEqual(initial, DifficultyAnalysisJob.ComputeInputFingerprint(level, settings));
        }

        [Test]
        public void Cancellation_DoesNotPublishScore()
        {
            var job = new DifficultyAnalysisJob(CreateAnywhereLevel());
            job.Cancel();
            Assert.AreEqual(DifficultyAnalysisStatus.Cancelled, job.Result.Status);
            Assert.IsFalse(job.Result.HasScore);
        }

        [Test]
        public void Reports_ContainNewModelAndPerSeatProbabilitiesWithFiniteJson()
        {
            var r = Analyze(CreateFoodGoalLevel(5, true));
            string json = JsonUtility.ToJson(r);
            Assert.IsFalse(json.Contains(":Infinity"));
            Assert.IsFalse(json.Contains(":NaN"));
            var copy = JsonUtility.FromJson<DifficultyAnalysisResult>(json);
            Assert.AreEqual(r.SeatProbabilities.Count, copy.SeatProbabilities.Count);
            string csv = DifficultyAnalysisReport.Csv(r);
            Assert.IsTrue(csv.Contains("SeatProbabilities[0].JointProbability"));
            Assert.IsTrue(csv.Contains("Settings.MaxNodes"));
            string text = DifficultyAnalysisReport.Text(r);
            Assert.IsTrue(text.Contains("100*(0.50C+0.30Dp+0.20M)"));
            Assert.IsFalse(text.Contains("A_ref"));
        }

        private ConditionDataSO NewPersonCondition(ConditionType type, PersonTrait trait)
        {
            var condition = ScriptableObject.CreateInstance<ConditionDataSO>();
            condition.type = type;
            condition.target = ConditionTarget.Person;
            condition.targetTrait = trait;
            _createdObjects.Add(condition);
            return condition;
        }

        [Test]
        public void MinimumMoves_MatchesIndependentBreadthFirstSearchForEverySmallFinalPlacement()
        {
            // Three main seats and two waiting seats. Includes cycles, empty destinations,
            // chains and swaps involving one/two people initially in WaitGrid.
            foreach (var sources in new[] { new[] { 0, 1, 2 }, new[] { 0, 1, -1 }, new[] { 0, -1, -1 }, new[] { 0, 1 }, new[] { 0, -1 } })
            {
                int[] start = { -1, -1, -1, -1, -1 };
                int wait = 3;
                for (int p = 0; p < sources.Length; p++) start[sources[p] < 0 ? wait++ : sources[p]] = p;
                var queue = new Queue<int[]>();
                var distance = new Dictionary<string, int>();
                queue.Enqueue(start);
                distance.Add(string.Join(",", start), 0);
                while (queue.Count > 0)
                {
                    var state = queue.Dequeue();
                    int d = distance[string.Join(",", state)];
                    if (state[3] < 0 && state[4] < 0)
                    {
                        int[] destinations = new int[sources.Length];
                        for (int s = 0; s < 3; s++) if (state[s] >= 0) destinations[state[s]] = s;
                        Assert.AreEqual(d, DifficultyAnalysisJob.MinimumMoveCost(sources, destinations));
                    }
                    for (int i = 0; i < 5; i++)
                        for (int j = i + 1; j < 5; j++)
                        {
                            if (state[i] < 0 && state[j] < 0) continue;
                            var next = (int[])state.Clone();
                            (next[i], next[j]) = (next[j], next[i]);
                            string key = string.Join(",", next);
                            if (distance.ContainsKey(key)) continue;
                            distance.Add(key, d + 1);
                            queue.Enqueue(next);
                        }
                }
            }
        }

        private DifficultyAnalysisResult Analyze(LevelDataSO level, DifficultyAnalysisSettings settings = null)
        {
            var job = new DifficultyAnalysisJob(level, settings);
            int ticks = 0;
            while (job.IsRunning && ticks++ < 10000) job.Tick();
            Assert.IsFalse(job.IsRunning, "Analyzer exceeded test tick limit.");
            return job.Result;
        }

        private LevelDataSO CreateFoodGoalLevel(int moveLimit, bool createFood)
        {
            LevelDataSO level = CreateLevel(moveLimit, new Vector2Int(4, 1));
            level.mainGrid.Set(0, 0, CreateCell(CellType.Seat));
            level.mainGrid.Set(1, 0, CreateCell(CellType.Seat));
            level.mainGrid.Set(2, 0, CreateCell(CellType.Seat));
            if (createFood)
            {
                CellDataSO food = CreateCell(CellType.Food);
                food.food = Food.Hamburger;
                level.mainGrid.Set(3, 0, food);
            }

            ConditionDataSO condition = ScriptableObject.CreateInstance<ConditionDataSO>();
            condition.type = ConditionType.Like;
            condition.target = ConditionTarget.Food;
            condition.foodTarget = Food.Hamburger;
            _createdObjects.Add(condition);

            LevelPersonConfig person = CreatePerson("Food lover", PersonTrait.Cool, Vector2Int.zero);
            person.conditions.Add(condition);
            level.personConfigs.Add(person);
            return level;
        }

        private LevelDataSO CreateAnywhereLevel()
        {
            LevelDataSO level = CreateLevel(5, new Vector2Int(3, 1));
            level.mainGrid.Set(0, 0, CreateCell(CellType.Seat));
            level.mainGrid.Set(1, 0, CreateCell(CellType.Seat));
            level.mainGrid.Set(2, 0, CreateCell(CellType.Seat));

            ConditionDataSO condition = ScriptableObject.CreateInstance<ConditionDataSO>();
            condition.type = ConditionType.Like;
            condition.target = ConditionTarget.Food;
            condition.foodTarget = Food.Any;
            _createdObjects.Add(condition);

            LevelPersonConfig person = CreatePerson("Any seat", PersonTrait.Cool, Vector2Int.zero);
            person.conditions.Add(condition);
            level.personConfigs.Add(person);
            return level;
        }

        private LevelDataSO CreateLevel(int moveLimit, Vector2Int mainSize)
        {
            LevelDataSO level = ScriptableObject.CreateInstance<LevelDataSO>();
            level.name = "Difficulty Test Level";
            level.levelMove = moveLimit;
            level.mainGrid = CreateGrid(mainSize);
            level.waitGrid = CreateGrid(new Vector2Int(1, 1));
            _createdObjects.Add(level);
            return level;
        }

        private LevelPersonConfig CreatePerson(string name, PersonTrait trait, Vector2Int position)
        {
            PersonDefinitionSO definition = ScriptableObject.CreateInstance<PersonDefinitionSO>();
            definition.personName = name;
            definition.trait = trait;
            _createdObjects.Add(definition);
            return new LevelPersonConfig
            {
                definition = definition,
                gridId = GridId.MainGrid,
                position = position
            };
        }

        private static Grid<CellDataSO> CreateGrid(Vector2Int size)
        {
            return new Grid<CellDataSO>(size, Vector2.one, Vector2.zero, 0.5f, 0.5f);
        }

        private CellDataSO CreateCell(CellType type)
        {
            CellDataSO cell = ScriptableObject.CreateInstance<CellDataSO>();
            cell.type = type;
            _createdObjects.Add(cell);
            return cell;
        }
    }
}
