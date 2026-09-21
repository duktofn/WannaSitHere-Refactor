using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Core.Board;
using Game.Core.Levels;
using Game.Core.People;
using Game.Data.Board;
using Game.Data.Conditions;
using Game.Data.Levels;
using Game.Data.People;

namespace Game.Tests.EditMode
{
    public sealed class LevelDataRefactorTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in _createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void RuntimeConversion_CreatesIndependentPersonsPerOccurrence()
        {
            LevelDataSO level = CreateLevel(new Vector2Int(2, 1));
            CellDataSO firstSeat = CreateCell(CellType.Seat);
            CellDataSO secondSeat = CreateCell(CellType.Seat);
            level.mainGrid.Set(0, 0, firstSeat);
            level.mainGrid.Set(1, 0, secondSeat);

            PersonDefinitionSO definition = CreateDefinition("Shared Person", PersonTrait.Cool);
            ConditionDataSO condition = CreateCondition("Likes food");
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                conditions = new List<ConditionDataSO> { condition },
                gridId = GridId.MainGrid,
                position = new Vector2Int(0, 0)
            });
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                conditions = new List<ConditionDataSO> { condition },
                gridId = GridId.MainGrid,
                position = new Vector2Int(1, 0)
            });

            Assert.IsTrue(level.TryToRuntimeData(out var runtime, out var errors), string.Join("\n", errors));

            CellRuntimeData firstRuntimeCell = runtime.MainGrid.Get(0, 0);
            CellRuntimeData secondRuntimeCell = runtime.MainGrid.Get(1, 0);
            PersonRuntimeData firstPerson = firstRuntimeCell.DefaultPerson;
            PersonRuntimeData secondPerson = secondRuntimeCell.DefaultPerson;

            Assert.IsNotNull(firstPerson);
            Assert.IsNotNull(secondPerson);
            Assert.AreSame(firstPerson, firstRuntimeCell.CurrentPerson);
            Assert.AreSame(secondPerson, secondRuntimeCell.CurrentPerson);
            Assert.AreNotSame(firstPerson, secondPerson);
            Assert.AreNotSame(firstPerson.Conditions, secondPerson.Conditions);
            Assert.AreNotSame(firstPerson.Conditions[0], secondPerson.Conditions[0]);

            firstPerson.ClearConditions();
            Assert.AreEqual(0, firstPerson.Conditions.Count);
            Assert.AreEqual(1, secondPerson.Conditions.Count);
        }

        [Test]
        public void Validate_ReportsMissingDefinitionAndNullConditionWithLocation()
        {
            LevelDataSO level = CreateLevel(new Vector2Int(1, 1));
            level.mainGrid.Set(0, 0, CreateCell(CellType.Seat));
            level.personConfigs.Add(new LevelPersonConfig
            {
                conditions = new List<ConditionDataSO> { null },
                gridId = GridId.MainGrid,
                position = Vector2Int.zero
            });

            List<string> errors = new();
            Assert.IsFalse(level.Validate(errors));
            AssertHasError(errors, "ValidationLevel");
            AssertHasError(errors, "MainGrid (0, 0)");
            AssertHasError(errors, "missing its PersonDefinitionSO");
            AssertHasError(errors, "null condition reference");
        }

        [Test]
        public void Validate_ReportsConditionLimitDuplicateOutOfBoundsEmptyAndNonSeat()
        {
            LevelDataSO level = CreateLevel(new Vector2Int(2, 1));
            level.mainGrid.Set(0, 0, CreateCell(CellType.Seat));
            level.mainGrid.Set(1, 0, CreateCell(CellType.Block));
            level.waitGrid.Set(0, 0, null);

            PersonDefinitionSO definition = CreateDefinition("Configured Person", PersonTrait.Sick);
            List<ConditionDataSO> tooManyConditions = new()
            {
                CreateCondition("One"),
                CreateCondition("Two"),
                CreateCondition("Three")
            };

            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                conditions = tooManyConditions,
                gridId = GridId.MainGrid,
                position = new Vector2Int(0, 0)
            });
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                gridId = GridId.MainGrid,
                position = new Vector2Int(0, 0)
            });
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                gridId = GridId.MainGrid,
                position = new Vector2Int(5, 5)
            });
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                gridId = GridId.MainGrid,
                position = new Vector2Int(1, 0)
            });
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = definition,
                gridId = GridId.WaitGrid,
                position = Vector2Int.zero
            });

            List<string> errors = new();
            Assert.IsFalse(level.Validate(errors));
            AssertHasError(errors, "maximum is");
            AssertHasError(errors, "overlaps another person configuration");
            AssertHasError(errors, "outside the 2x1 grid");
            AssertHasError(errors, "targets a Block cell");
            AssertHasError(errors, "targets an empty cell");
        }

        [Test]
        public void TryToRuntimeData_InvalidLevelReturnsBeforeCreatingRuntime()
        {
            LevelDataSO level = CreateLevel(new Vector2Int(1, 1));
            level.personConfigs.Add(new LevelPersonConfig
            {
                definition = CreateDefinition("Invalid Person", PersonTrait.Dirty),
                gridId = GridId.MainGrid,
                position = new Vector2Int(3, 3)
            });

            LogAssert.Expect(
                LogType.Error,
                "Level 'ValidationLevel': person configuration #0 at MainGrid (3, 3) is outside the 1x1 grid.");
            bool converted = level.TryToRuntimeData(out LevelRuntimeData runtime, out List<string> errors);

            Assert.IsFalse(converted);
            Assert.IsNull(runtime);
            Assert.IsNotEmpty(errors);
        }

        private LevelDataSO CreateLevel(Vector2Int mainSize)
        {
            LevelDataSO level = ScriptableObject.CreateInstance<LevelDataSO>();
            level.name = "ValidationLevel";
            level.levelMove = 10;
            level.mainGrid = CreateGrid(mainSize);
            level.waitGrid = CreateGrid(new Vector2Int(1, 1));
            level.waitGrid.Set(0, 0, CreateCell(CellType.Seat));
            _createdObjects.Add(level);
            return level;
        }

        private Grid<CellDataSO> CreateGrid(Vector2Int size)
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

        private PersonDefinitionSO CreateDefinition(string personName, PersonTrait trait)
        {
            PersonDefinitionSO definition = ScriptableObject.CreateInstance<PersonDefinitionSO>();
            definition.personName = personName;
            definition.trait = trait;
            _createdObjects.Add(definition);
            return definition;
        }

        private ConditionDataSO CreateCondition(string description)
        {
            ConditionDataSO condition = ScriptableObject.CreateInstance<ConditionDataSO>();
            condition.description = description;
            _createdObjects.Add(condition);
            return condition;
        }

        private static void AssertHasError(List<string> errors, string expectedText)
        {
            Assert.IsTrue(
                errors.Exists(error => error.Contains(expectedText)),
                $"Expected validation error containing '{expectedText}'. Actual errors:\n{string.Join("\n", errors)}");
        }
    }
}
