using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Game.App;
using Game.App.SaveAndLoad;
using Game.Core.Booster;
using Game.Core.Board;
using Game.Core.Economy;
using Game.Core.Levels;
using Game.Core.People;
using Game.Events;

namespace Game.Tests.EditMode
{
    public sealed class GameManagerFlowTests
    {
        [Test]
        public async Task PlayLevel_PreparesBeforeTransitionAndPublishesReadyAfterOpen()
        {
            List<string> trace = new List<string>();
            FakeLevelLoader loader = new FakeLevelLoader(trace);
            FakePresentation presentation = new FakePresentation(trace);
            GameManager manager = CreateManager(loader, presentation, new FakeGameDataStore());
            manager.LevelReady += _ => trace.Add("level-ready");

            await manager.PlayLevelAsync();

            CollectionAssert.AreEqual(
                new[] { "prepare", "close", "activate", "show-game", "open", "game-audio", "level-ready" },
                trace);
            Assert.AreEqual(1, manager.ActiveLevelNumber);
            Assert.IsNotNull(manager.ActiveLevelManager);
        }

        [Test]
        public async Task FailedPreparation_LeavesCurrentSessionAndProgressUntouched()
        {
            List<string> trace = new List<string>();
            FakeLevelLoader loader = new FakeLevelLoader(trace);
            FakePresentation presentation = new FakePresentation(trace);
            GameManager manager = CreateManager(loader, presentation, new FakeGameDataStore());
            await manager.PlayLevelAsync();
            LevelManager activeLevel = manager.ActiveLevelManager;
            int closeCount = presentation.CloseCount;

            loader.FailPreparation = true;
            await manager.PlayLevelAsync(3);

            Assert.AreSame(activeLevel, manager.ActiveLevelManager);
            Assert.AreEqual(1, manager.ActiveLevelNumber);
            Assert.AreEqual(1, manager.CurrentLevel);
            Assert.AreEqual(closeCount, presentation.CloseCount);
        }

        [Test]
        public async Task RepeatedPlayCommand_IsRejectedWhileTransitionIsInFlight()
        {
            List<string> trace = new List<string>();
            FakeLevelLoader loader = new FakeLevelLoader(trace);
            FakePresentation presentation = new FakePresentation(trace) { BlockFirstClose = true };
            GameManager manager = CreateManager(loader, presentation, new FakeGameDataStore());

            Task firstPlay = manager.PlayLevelAsync();
            Task repeatedPlay = manager.PlayLevelAsync(2);
            await repeatedPlay;

            Assert.AreEqual(1, loader.PrepareCount);
            presentation.ReleaseClose();
            await firstPlay;

            Assert.AreEqual(1, loader.ActivateCount);
            Assert.AreEqual(1, manager.ActiveLevelNumber);
        }

        [Test]
        public async Task HomeCancelsPendingLoadAndReturnsToMenu()
        {
            FakeLevelLoader loader = new FakeLevelLoader(new List<string>());
            FakePresentation presentation = new FakePresentation { BlockFirstClose = true };
            GameManager manager = CreateManager(loader, presentation, new FakeGameDataStore());
            int levelReadyCount = 0;
            manager.LevelReady += _ => levelReadyCount++;

            Task play = manager.PlayLevelAsync();
            Task home = manager.BackToHomeAsync();
            presentation.ReleaseClose();
            await Task.WhenAll(play, home);

            Assert.AreEqual(0, levelReadyCount);
            Assert.AreEqual(0, manager.ActiveLevelNumber);
            Assert.IsNull(manager.ActiveLevelManager);
        }

        [Test]
        public async Task ReplacedLevelOutcomeCannotAdvanceProgression()
        {
            FakeGameDataStore store = new FakeGameDataStore();
            GameManager manager = CreateManager(new FakeLevelLoader(new List<string>()), new FakePresentation(), store);
            await manager.PlayLevelAsync(1);
            LevelManager replacedLevel = manager.ActiveLevelManager;
            await manager.PlayLevelAsync(2);
            int acceptedWins = 0;
            manager.WinAccepted += () => acceptedWins++;

            replacedLevel.CheckAllPersonConditions();

            Assert.AreEqual(0, acceptedWins);
            Assert.AreEqual(2, manager.CurrentLevel);
            Assert.AreEqual(2, manager.ActiveLevelNumber);
        }

        [Test]
        public async Task WinIsAcceptedOnceAndProgressesFromActiveLevel()
        {
            FakeGameDataStore store = new FakeGameDataStore();
            GameManager manager = CreateManager(new FakeLevelLoader(new List<string>()), new FakePresentation(), store);
            await manager.PlayLevelAsync(4);
            int acceptedWins = 0;
            manager.WinAccepted += () => acceptedWins++;

            manager.ForceWin();
            manager.ForceWin();

            Assert.AreEqual(1, acceptedWins);
            Assert.AreEqual(5, manager.CurrentLevel);
            Assert.AreEqual(4, manager.ActiveLevelNumber);
        }

        [Test]
        public async Task RestartAfterWinReloadsActiveLevelWithoutRewindingProgression()
        {
            FakeLevelLoader loader = new FakeLevelLoader(new List<string>());
            GameManager manager = CreateManager(loader, new FakePresentation(), new FakeGameDataStore());
            await manager.PlayLevelAsync(4);
            manager.ForceWin();

            await manager.RestartLevelAsync();

            Assert.AreEqual(4, loader.LastRequestedLevel);
            Assert.AreEqual(4, manager.ActiveLevelNumber);
            Assert.AreEqual(5, manager.CurrentLevel);
        }

        private static GameManager CreateManager(
            FakeLevelLoader loader,
            FakePresentation presentation,
            IGameDataStore store)
        {
            return new GameManager(new GameManagerConfig(), loader, presentation, saveLoad: store);
        }

        private sealed class FakeGameDataStore : IGameDataStore
        {
            private GameData _data = new GameData { currentLevel = 1, currentSoundVolume = 100, currentMusicVolume = 100 };

            public GameData GetGameData() => _data;
            public void SaveGameData(GameData data) => _data = data;
        }

        private sealed class FakeLevelLoader : ILevelLoader
        {
            private readonly List<string> _trace;
            public bool FailPreparation;
            public int PrepareCount;
            public int ActivateCount;
            public int LastRequestedLevel;
            public int TotalLevels => 5;

            public FakeLevelLoader(List<string> trace)
            {
                _trace = trace;
            }

            public bool TryPrepare(int levelNumber, out IPreparedLevel preparedLevel, out string error)
            {
                PrepareCount++;
                LastRequestedLevel = levelNumber;
                _trace?.Add("prepare");
                preparedLevel = null;
                error = null;
                if (FailPreparation)
                {
                    error = "Test preparation failure.";
                    return false;
                }

                preparedLevel = new PreparedLevel();
                return true;
            }

            public void Activate(IPreparedLevel preparedLevel, LevelManager levelManager)
            {
                ActivateCount++;
                _trace?.Add("activate");
            }

            private sealed class PreparedLevel : IPreparedLevel
            {
                public int CatalogIndex => 0;
                public LevelRuntimeData RuntimeData => new LevelRuntimeData(
                    5,
                    new Grid<Game.Core.Board.CellRuntimeData>(new UnityEngine.Vector2Int(1, 1)),
                    new Grid<Game.Core.Board.CellRuntimeData>(new UnityEngine.Vector2Int(1, 1)));
            }
        }

        private sealed class FakePresentation : IGamePresentation
        {
            private readonly List<string> _trace;
            private readonly TaskCompletionSource<bool> _closeRelease = new TaskCompletionSource<bool>();

            public bool BlockFirstClose;
            public int CloseCount;
            public bool IsGameplayInputEnabled { get; private set; } = true;

            public FakePresentation(List<string> trace = null)
            {
                _trace = trace;
            }

            public async Task CloseTransitionAsync(CancellationToken cancellationToken)
            {
                CloseCount++;
                _trace?.Add("close");
                if (BlockFirstClose && CloseCount == 1)
                    await _closeRelease.Task;
                cancellationToken.ThrowIfCancellationRequested();
            }

            public Task OpenTransitionAsync(CancellationToken cancellationToken)
            {
                _trace?.Add("open");
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }

            public void SetGameplayInputEnabled(bool enabled) => IsGameplayInputEnabled = enabled;
            public void ShowGameScreen(int levelNumber) => _trace?.Add("show-game");
            public void ShowHomeScreen(int levelNumber) { }
            public void EnterGameAudio() => _trace?.Add("game-audio");
            public void EnterHomeAudio() { }
            public void ApplyAudioSettings(int soundVolume, bool soundMuted, int musicVolume, bool musicMuted) { }
            public void PlayAudioCue(AudioCueId cue) { }
            public Task PlayPaidContinueFeedbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public void RaiseItemReceived(Reward reward) { }
            public void RaiseItemSpent(Reward reward) { }
            public void RefreshShop() { }
            public void RefreshLoginRewards(
                IReadOnlyList<Reward> weeklyRewards,
                int loginDay,
                bool weeklyRewardClaimed,
                Reward dailyReward,
                bool dailyRewardClaimed) { }
            public void RevertMove(MoveRecord record) { }
            public void PlayRemoveFeedback(PersonRuntimeData person) { }
            public void ReportLoadFailure(string error) { }

            public void ReleaseClose()
            {
                _closeRelease.TrySetResult(true);
            }
        }
    }
}
