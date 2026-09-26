using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.App.SaveAndLoad;
using Game.App.Tutorial;
using Game.Core.Booster;
using Game.Core.Economy;
using Game.Events;
using Game.Logging;

namespace Game.App
{
    /// <summary>
    /// Owns application commands, the active level session, progression, and operation ordering.
    /// Unity-specific level loading and presentation are supplied through App-owned contracts.
    /// </summary>
    public sealed class GameManager
    {
        private readonly IGameDataStore _saveLoad;
        private readonly Inventory _inventory;
        private readonly EconomyManager _economyManager;
        private readonly GameManagerConfig _config;
        private readonly ILevelLoader _levelLoader;
        private readonly IGamePresentation _presentation;
        private readonly TutorialService _tutorialService;
        private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);
        private GameData _gameData;
        private CancellationTokenSource _currentOperation;
        private LevelManager _activeLevelManager;
        private SessionState _sessionState;
        private bool _sessionOutcomeAccepted;
        private bool _hasHandledApplicationReady;
        private bool _firstTimeTutorialInProgress;
        private DateTime _lastDailyCheckDateUtc = DateTime.MinValue;

        public Inventory Inventory => _inventory;
        public EconomyManager EconomyManager => _economyManager;
        public int CurrentLevel => Math.Max(1, _gameData.currentLevel);
        public int ActiveLevelNumber { get; private set; }
        public int TotalLevels => _levelLoader?.TotalLevels ?? 0;
        public LevelManager ActiveLevelManager => _activeLevelManager;
        public int SoundVolume => _gameData.currentSoundVolume;
        public bool IsSoundMuted => _gameData.isSoundMuted;
        public int MusicVolume => _gameData.currentMusicVolume;
        public bool IsMusicMuted => _gameData.isMusicMuted;

        public event Action<int> LevelReady;
        public event Action<int> LevelNumberChanged;
        public event Action<TutorialTrigger> TutorialTriggerFired;
        public event Action WinAccepted;
        public event Action LoseAccepted;

        public GameManager(int[] goldShopLimits, SaveLoadManager saveLoad = null)
            : this(new GameManagerConfig { GoldShopLimits = goldShopLimits }, null, null, null, saveLoad)
        {
        }

        public GameManager(
            GameManagerConfig config,
            ILevelLoader levelLoader,
            IGamePresentation presentation,
            TutorialService tutorialService = null,
            IGameDataStore saveLoad = null)
        {
            _config = CopyConfig(config ?? new GameManagerConfig());
            _levelLoader = levelLoader;
            _presentation = presentation;
            _tutorialService = tutorialService ?? new TutorialService(Array.Empty<MechanicTutorial>());
            _saveLoad = saveLoad ?? new SaveLoadManager();
            _gameData = _saveLoad.GetGameData();

            _inventory = new Inventory(
                _gameData.currentGold,
                _gameData.currentGem,
                _gameData.currentRemove,
                _gameData.currentMoreMoves,
                _gameData.currentUndo);

            if (_gameData.goldShopPurchaseCountToday == null || _gameData.goldShopPurchaseCountToday.Length != 3)
                _gameData.goldShopPurchaseCountToday = new int[3];
            if (_gameData.completedTutorialIds == null)
                _gameData.completedTutorialIds = new List<string>();

            _economyManager = new EconomyManager(
                _inventory,
                _gameData.currentLoginDay,
                _gameData.isDailyRewardClaimed,
                _gameData.isWeeklyRewardClaimed,
                _config.GoldShopLimits,
                _gameData.goldShopPurchaseCountToday);

            List<MechanicTutorialID> completedTutorials = ReadCompletedTutorials();
            if (_gameData.IsTutorialCompleted && !completedTutorials.Contains(MechanicTutorialID.Drag))
                completedTutorials.Add(MechanicTutorialID.Drag);
            _tutorialService.RestoreCompleted(completedTutorials);
            _tutorialService.TriggerFired += HandleTutorialTriggerFired;
            _tutorialService.TutorialCompleted += HandleTutorialCompleted;
            _sessionState = SessionState.Home;
            ApplyAudioSettings();
        }

        public void InitializeLoginState(DateTime nowUtc)
        {
            _lastDailyCheckDateUtc = nowUtc.Date;
            RefreshDailyState(nowUtc);
            RefreshLoginRewardsPresentation();
        }

        public void OnApplicationReady()
        {
            if (_hasHandledApplicationReady)
                return;

            _hasHandledApplicationReady = true;
            if (!_gameData.IsFirstTimePlaying)
            {
                Logger.Log("GameManager: Application ready, not first time playing.");
                return;
            }

            Logger.Log("GameManager: First-time tutorial will start when Level 1 is ready.");
            TryStartFirstTimeTutorial(ActiveLevelNumber);
        }

        private void TryStartFirstTimeTutorial(int levelNumber)
        {
            if (!_gameData.IsFirstTimePlaying || _firstTimeTutorialInProgress ||
                _tutorialService.HasActiveTutorial || levelNumber != 1 || _sessionState != SessionState.Playing)
                return;

            if (_tutorialService.IsCompleted(MechanicTutorialID.Drag))
            {
                _gameData.IsFirstTimePlaying = false;
                SaveGame();
                return;
            }

            _firstTimeTutorialInProgress = true;
            bool tutorialStarted = _tutorialService.TryStart(TutorialTrigger.FirstTimePlaying);
            if (!tutorialStarted)
                _firstTimeTutorialInProgress = false;

            Logger.Log(tutorialStarted
                ? "GameManager: FirstTimePlaying trigger fired and tutorial started."
                : "GameManager: FirstTimePlaying trigger fired; no matching tutorial is configured.");
        }

        private void HandleTutorialTriggerFired(TutorialTrigger trigger)
        {
            TutorialTriggerFired?.Invoke(trigger);
        }

        public void Tick(DateTime nowUtc)
        {
            _tutorialService.Tick();
            if (_lastDailyCheckDateUtc.Date == nowUtc.Date)
                return;

            _lastDailyCheckDateUtc = nowUtc.Date;
            if (RefreshDailyState(nowUtc))
                RefreshLoginRewardsPresentation();
        }

        public bool RefreshDailyState(DateTime nowUtc)
        {
            DateTime parsedDate = DateTime.MinValue;
            bool hasValidLastLogin = !string.IsNullOrEmpty(_gameData.lastLoginDateUtc) &&
                DateTime.TryParse(
                    _gameData.lastLoginDateUtc,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out parsedDate);

            bool isNewDay = hasValidLastLogin && _economyManager.EvaluateLoginState(parsedDate, nowUtc);
            if (!isNewDay && hasValidLastLogin)
                return false;

            if (!hasValidLastLogin)
                _economyManager.ResetShopPurchases();

            _gameData.lastLoginDateUtc = nowUtc.ToString("o");
            SaveGame();
            return true;
        }

        public void PublishCurrentLevel()
        {
            LevelNumberChanged?.Invoke(CurrentLevel);
        }

        public void RefreshEconomyPresentation()
        {
            RefreshLoginRewardsPresentation();
        }

        public void SetLevel(int level)
        {
            _gameData.currentLevel = Math.Max(1, level);
            SaveGame();
            LevelNumberChanged?.Invoke(CurrentLevel);
        }

        public void AdvanceLevel()
        {
            SetLevel(CurrentLevel + 1);
        }

        public Task PlayLevelAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadLevelAsync(CurrentLevel, true, cancellationToken);
        }

        public Task PlayLevelAsync(int levelNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadLevelAsync(Math.Max(1, levelNumber), true, cancellationToken);
        }

        public Task RestartLevelAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            int levelNumber = ActiveLevelNumber > 0 ? ActiveLevelNumber : CurrentLevel;
            return LoadLevelAsync(levelNumber, false, cancellationToken);
        }

        public Task NextLevelAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadLevelAsync(CurrentLevel, true, cancellationToken);
        }

        public void RequestPlayCurrentLevel() => ObserveCommand(PlayLevelAsync());
        public void RequestNextLevel() => ObserveCommand(NextLevelAsync());
        public void RequestRestartLevel() => ObserveCommand(RestartLevelAsync());
        public void RequestBackToHome() => ObserveCommand(BackToHomeAsync());

        public async Task BackToHomeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _currentOperation?.Cancel();
            await _operationGate.WaitAsync(cancellationToken);
            SessionState previousState = _sessionState;
            bool transitionStarted = false;
            bool homeApplied = false;
            try
            {
                _tutorialService.Cancel();
                _sessionState = SessionState.Transitioning;
                _presentation?.SetGameplayInputEnabled(false);
                transitionStarted = true;
                await CloseTransitionAsync(cancellationToken);
                _presentation?.ShowHomeScreen(CurrentLevel);
                homeApplied = true;
                DetachLevelManager(_activeLevelManager);
                _activeLevelManager = null;
                ActiveLevelNumber = 0;
                _sessionOutcomeAccepted = false;
                _sessionState = SessionState.Home;
                await OpenTransitionAsync(cancellationToken);
                transitionStarted = false;

                _presentation?.EnterHomeAudio();
                LevelNumberChanged?.Invoke(CurrentLevel);
            }
            catch (OperationCanceledException)
            {
                _sessionState = homeApplied ? SessionState.Home : previousState;
                throw;
            }
            catch (Exception exception)
            {
                _presentation?.ReportLoadFailure(exception.Message);
                _sessionState = homeApplied ? SessionState.Home : previousState;
            }
            finally
            {
                if (transitionStarted)
                    await OpenTransitionSafelyAsync();

                _presentation?.SetGameplayInputEnabled(_sessionState == SessionState.Playing);
                _operationGate.Release();
            }
        }

        public void ForceWin()
        {
            AcceptOutcome(_activeLevelManager, LevelOutcome.Won);
        }

        public void ForceLose()
        {
            AcceptOutcome(_activeLevelManager, LevelOutcome.Lost);
        }

        public void GrantReward(Reward reward)
        {
            _inventory.UpdateInventory(reward);
            SaveGame();
        }

        public void ClaimWinReward()
        {
            if (_config.LevelWinReward.amount <= 0)
                return;

            GrantReward(_config.LevelWinReward);
            PresentRewardGranted(_config.LevelWinReward);
        }

        public void ClaimAdsReward()
        {
            if (_config.LevelAdsWinReward.amount <= 0)
                return;

            GrantReward(_config.LevelAdsWinReward);
            PresentRewardGranted(_config.LevelAdsWinReward);
        }

        public bool TryClaimDailyReward()
        {
            if (_config.DailyReward.amount <= 0)
                return false;

            if (!_economyManager.ClaimDailyReward(_config.DailyReward))
                return false;

            SaveGame();
            PresentRewardGranted(_config.DailyReward);
            RefreshLoginRewardsPresentation();
            return true;
        }

        public bool TryClaimWeeklyReward()
        {
            if (_config.WeeklyRewards == null || _config.WeeklyRewards.Length == 0)
                return false;

            Reward reward = GetCurrentWeeklyReward();
            if (!_economyManager.ClaimWeeklyReward(reward))
                return false;

            SaveGame();
            PresentRewardGranted(reward);
            RefreshLoginRewardsPresentation();
            return true;
        }

        public void ClaimDailyReward() => TryClaimDailyReward();
        public void ClaimWeeklyReward() => TryClaimWeeklyReward();

        public bool TryPurchase(Reward cost, Reward item, int goldShopSlotIndex = -1)
        {
            if (!_economyManager.TryPurchase(cost, item, goldShopSlotIndex))
                return false;

            SaveGame();
            PresentPurchaseSucceeded(item);
            return true;
        }

        public bool TryPurchaseShopRequest(ShopPurchaseRequest request)
        {
            Reward[] prices = request.UsesGold ? _config.GoldShopPrices : _config.GemShopPrices;
            if (request.SlotIndex < 0 || prices == null || request.SlotIndex >= prices.Length)
                return false;

            Reward item = CreateShopItem(request.ItemType);
            if (item.amount <= 0)
                return false;

            return TryPurchaseOffer(prices[request.SlotIndex], item, request.UsesGold ? request.SlotIndex : -1);
        }

        public bool TryBuyRemove()
        {
            return TryPurchaseOffer(GetPrice(_config.GemShopPrices, 0, ItemType.Gem, 50),
                new Reward { type = ItemType.Remove, amount = 1 });
        }

        public bool TryBuyUndo()
        {
            return TryPurchaseOffer(GetPrice(_config.GemShopPrices, 1, ItemType.Gem, 50),
                new Reward { type = ItemType.Undo, amount = 1 });
        }

        public bool TryBuyMoreMoves()
        {
            return TryPurchaseOffer(GetPrice(_config.GoldShopPrices, 2, ItemType.Gold, 100),
                new Reward { type = ItemType.MoreMoves, amount = _config.MoreMovesAmount }, 2);
        }

        public void BuyRemove() => TryBuyRemove();
        public void BuyUndo() => TryBuyUndo();
        public void BuyMoreMoves() => TryBuyMoreMoves();

        public void SetSoundSettings(int volume, bool muted)
        {
            _gameData.currentSoundVolume = ClampVolume(volume);
            _gameData.isSoundMuted = muted;
            SaveGame();
            ApplyAudioSettings();
        }

        public void SetMusicSettings(int volume, bool muted)
        {
            _gameData.currentMusicVolume = ClampVolume(volume);
            _gameData.isMusicMuted = muted;
            SaveGame();
            ApplyAudioSettings();
        }

        public bool TryUseMoreMoves()
        {
            if (!CanUseBooster(ItemType.MoreMoves))
                return false;

            MoreMoveBooster booster = new MoreMoveBooster(_activeLevelManager.CurrentLevel, _config.MoreMovesAmount);
            if (!booster.TryUse())
                return false;

            _inventory.TrySpendItem(ItemType.MoreMoves);
            SaveGame();
            _presentation?.PlayAudioCue(AudioCueId.BoosterUsed);
            _tutorialService.TryStart(TutorialTrigger.MoreMovesBoosterUsed);
            return true;
        }

        public bool TryUseUndo()
        {
            if (!CanUseBooster(ItemType.Undo))
                return false;

            UndoBooster booster = new UndoBooster(_activeLevelManager.MoveHistory, _activeLevelManager.CurrentLevel);
            if (!booster.TryUse() || !booster.LastUndoneRecord.HasValue)
                return false;

            _inventory.TrySpendItem(ItemType.Undo);
            SaveGame();
            _presentation?.PlayAudioCue(AudioCueId.BoosterUsed);
            _presentation?.RevertMove(booster.LastUndoneRecord.Value);
            _activeLevelManager.CheckAllPersonConditions();
            _tutorialService.TryStart(TutorialTrigger.UndoBoosterUsed);
            return true;
        }

        public bool TryUseRemove()
        {
            if (!CanUseBooster(ItemType.Remove) || _config.CanSitAnywhereCondition == null)
                return false;

            RemoveBooster booster = new RemoveBooster(
                _activeLevelManager.CurrentLevel,
                _config.CanSitAnywhereCondition);
            if (!booster.TryUse())
                return false;

            _inventory.TrySpendItem(ItemType.Remove);
            SaveGame();
            _presentation?.PlayAudioCue(AudioCueId.BoosterUsed);
            _presentation?.PlayRemoveFeedback(booster.TargetPerson);
            _activeLevelManager.CheckAllPersonConditions();
            _tutorialService.TryStart(TutorialTrigger.RemoveBoosterUsed);
            return true;
        }

        public void UseMoreMoves() => TryUseMoreMoves();
        public void UseUndo() => TryUseUndo();
        public void UseRemove() => TryUseRemove();

        public void SaveGame()
        {
            _gameData.currentLevel = CurrentLevel;
            _gameData.currentGold = _inventory.Gold;
            _gameData.currentGem = _inventory.Gem;
            _gameData.currentRemove = _inventory.Remove;
            _gameData.currentMoreMoves = _inventory.MoreMoves;
            _gameData.currentUndo = _inventory.Undo;
            _gameData.currentLoginDay = _economyManager.CurrentLoginDay;
            _gameData.isDailyRewardClaimed = _economyManager.IsDailyRewardClaimed;
            _gameData.isWeeklyRewardClaimed = _economyManager.IsWeeklyRewardClaimed;
            _gameData.goldShopPurchaseCountToday = _economyManager.GoldShopPurchaseCounts.ToArray();
            if (_gameData.completedTutorialIds == null)
                _gameData.completedTutorialIds = new List<string>();

            _saveLoad.SaveGameData(_gameData);
        }

        public void CancelPendingOperations()
        {
            _currentOperation?.Cancel();
            CancelActiveTutorial();
        }

        private void CancelActiveTutorial()
        {
            _tutorialService.Cancel();
            _firstTimeTutorialInProgress = false;
        }

        private async Task LoadLevelAsync(int levelNumber, bool updateProgression, CancellationToken cancellationToken)
        {
            if (_levelLoader == null)
            {
                _presentation?.ReportLoadFailure("No level loader is configured.");
                return;
            }

            if (!await _operationGate.WaitAsync(0, cancellationToken))
                return;

            using (CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                _currentOperation = operation;
                SessionState previousState = _sessionState;
                _sessionState = SessionState.Transitioning;
                bool transitionStarted = false;
                bool inputDisabled = false;
                bool activated = false;
                bool committed = false;
                LevelManager newLevelManager = null;

                try
                {
                    if (!_levelLoader.TryPrepare(levelNumber, out IPreparedLevel prepared, out string error))
                    {
                        _presentation?.ReportLoadFailure(error);
                        return;
                    }

                    operation.Token.ThrowIfCancellationRequested();
                    CancelActiveTutorial();
                    _presentation?.SetGameplayInputEnabled(false);
                    inputDisabled = true;
                    transitionStarted = true;
                    await CloseTransitionAsync(operation.Token);

                    newLevelManager = new LevelManager(prepared.RuntimeData, _config.AdjacentOffsets);
                    newLevelManager.OutcomeRaised += HandleLevelOutcome;
                    newLevelManager.MoveSucceeded += HandleSuccessfulMove;
                    _levelLoader.Activate(prepared, newLevelManager);
                    activated = true;

                    DetachLevelManager(_activeLevelManager);
                    _activeLevelManager = null;
                    _presentation?.ShowGameScreen(levelNumber);
                    await OpenTransitionAsync(operation.Token);
                    transitionStarted = false;
                    operation.Token.ThrowIfCancellationRequested();

                    _activeLevelManager = newLevelManager;
                    ActiveLevelNumber = levelNumber;
                    _sessionOutcomeAccepted = false;
                    _sessionState = SessionState.Playing;
                    if (updateProgression && CurrentLevel != levelNumber)
                    {
                        _gameData.currentLevel = levelNumber;
                        SaveGame();
                    }

                    committed = true;
                    _presentation?.SetGameplayInputEnabled(true);
                    _presentation?.EnterGameAudio();
                    LevelNumberChanged?.Invoke(levelNumber);
                    LevelReady?.Invoke(levelNumber);
                    TryStartFirstTimeTutorial(levelNumber);
                    _tutorialService.TryStart(TutorialTrigger.LevelReady);
                }
                catch (OperationCanceledException)
                {
                    if (activated)
                        RecoverToHome(newLevelManager);
                    else
                        _sessionState = previousState;
                }
                catch (Exception exception)
                {
                    _presentation?.ReportLoadFailure(exception.Message);
                    if (activated)
                        RecoverToHome(newLevelManager);
                    else
                        _sessionState = previousState;
                }
                finally
                {
                    if (transitionStarted)
                        await OpenTransitionSafelyAsync();

                    if (!activated && _sessionState == SessionState.Transitioning)
                        _sessionState = previousState;

                    if (!committed && activated && _activeLevelManager == null)
                        DetachLevelManager(newLevelManager);

                    if (inputDisabled)
                        _presentation?.SetGameplayInputEnabled(_sessionState == SessionState.Playing);

                    _currentOperation = null;
                    _operationGate.Release();
                }
            }
        }

        private void RecoverToHome(LevelManager failedLevelManager)
        {
            DetachLevelManager(failedLevelManager);
            DetachLevelManager(_activeLevelManager);
            _activeLevelManager = null;
            ActiveLevelNumber = 0;
            _sessionOutcomeAccepted = false;
            _sessionState = SessionState.Home;
            _presentation?.ShowHomeScreen(CurrentLevel);
            _presentation?.EnterHomeAudio();
        }

        private void HandleLevelOutcome(LevelManager levelManager, LevelOutcome outcome)
        {
            AcceptOutcome(levelManager, outcome);
        }

        private void AcceptOutcome(LevelManager source, LevelOutcome outcome)
        {
            if (source == null || source != _activeLevelManager ||
                _sessionState != SessionState.Playing || _sessionOutcomeAccepted)
            {
                return;
            }

            _sessionOutcomeAccepted = true;
            if (outcome == LevelOutcome.Won)
            {
                _sessionState = SessionState.Won;
                _gameData.currentLevel = Math.Max(CurrentLevel, ActiveLevelNumber + 1);
                SaveGame();
                LevelNumberChanged?.Invoke(CurrentLevel);
                WinAccepted?.Invoke();
            }
            else
            {
                _sessionState = SessionState.Lost;
                LoseAccepted?.Invoke();
            }
        }

        private void HandleSuccessfulMove()
        {
            if (_sessionState == SessionState.Playing)
                _tutorialService.TryStart(TutorialTrigger.SuccessfulMove);
        }

        private void HandleTutorialCompleted(MechanicTutorialID tutorialId)
        {
            string serializedId = tutorialId.ToString();
            if (!_gameData.completedTutorialIds.Contains(serializedId))
                _gameData.completedTutorialIds.Add(serializedId);

            if (tutorialId == MechanicTutorialID.Drag)
                _gameData.IsTutorialCompleted = true;

            if (_firstTimeTutorialInProgress)
            {
                _gameData.IsFirstTimePlaying = false;
                _firstTimeTutorialInProgress = false;
            }

            SaveGame();
        }

        private List<MechanicTutorialID> ReadCompletedTutorials()
        {
            List<MechanicTutorialID> completed = new List<MechanicTutorialID>();
            for (int i = 0; i < _gameData.completedTutorialIds.Count; i++)
            {
                if (Enum.TryParse(_gameData.completedTutorialIds[i], out MechanicTutorialID tutorialId))
                    completed.Add(tutorialId);
            }

            return completed;
        }

        private void DetachLevelManager(LevelManager levelManager)
        {
            if (levelManager == null)
                return;

            levelManager.OutcomeRaised -= HandleLevelOutcome;
            levelManager.MoveSucceeded -= HandleSuccessfulMove;
        }

        private bool CanUseBooster(ItemType type)
        {
            return _sessionState == SessionState.Playing &&
                   _activeLevelManager?.CurrentLevel != null &&
                   !_activeLevelManager.CurrentLevel.IsOutOfMove &&
                   _inventory.HasEnough(type, 1);
        }

        private bool TryPurchaseOffer(Reward cost, Reward item, int goldShopSlotIndex = -1)
        {
            if (goldShopSlotIndex >= 0 && !_economyManager.CanPurchaseGoldShopItem(goldShopSlotIndex))
                return false;

            if (!TryPurchase(cost, item, goldShopSlotIndex))
            {
                _presentation?.RaiseItemSpent(cost);
                return false;
            }

            return true;
        }

        private void PresentRewardGranted(Reward reward)
        {
            _presentation?.RaiseItemReceived(reward);
            _presentation?.PlayAudioCue(AudioCueId.Claim);
        }

        private void PresentPurchaseSucceeded(Reward item)
        {
            _presentation?.PlayAudioCue(AudioCueId.Spend);
            _presentation?.RaiseItemReceived(item);
            _presentation?.RefreshShop();
        }

        private void ApplyAudioSettings()
        {
            _presentation?.ApplyAudioSettings(SoundVolume, IsSoundMuted, MusicVolume, IsMusicMuted);
        }

        private void RefreshLoginRewardsPresentation()
        {
            _presentation?.RefreshLoginRewards(
                _config.WeeklyRewards,
                _economyManager.CurrentLoginDay,
                _economyManager.IsWeeklyRewardClaimed,
                _config.DailyReward,
                _economyManager.IsDailyRewardClaimed);
        }

        private static GameManagerConfig CopyConfig(GameManagerConfig source)
        {
            return new GameManagerConfig
            {
                GoldShopLimits = source.GoldShopLimits != null ? (int[])source.GoldShopLimits.Clone() : null,
                GoldShopPrices = source.GoldShopPrices != null ? (Reward[])source.GoldShopPrices.Clone() : null,
                GemShopPrices = source.GemShopPrices != null ? (Reward[])source.GemShopPrices.Clone() : null,
                LevelWinReward = source.LevelWinReward,
                LevelAdsWinReward = source.LevelAdsWinReward,
                DailyReward = source.DailyReward,
                WeeklyRewards = source.WeeklyRewards != null ? (Reward[])source.WeeklyRewards.Clone() : null,
                MoreMovesAmount = source.MoreMovesAmount,
                CanSitAnywhereCondition = source.CanSitAnywhereCondition,
                AdjacentOffsets = source.AdjacentOffsets != null
                    ? new List<UnityEngine.Vector2Int>(source.AdjacentOffsets)
                    : null
            };
        }

        private async void ObserveCommand(Task command)
        {
            try
            {
                await command;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _presentation?.ReportLoadFailure(exception.Message);
            }
        }

        private Reward GetCurrentWeeklyReward()
        {
            if (_config.WeeklyRewards == null || _config.WeeklyRewards.Length == 0)
                return default(Reward);

            int day = _economyManager.CurrentLoginDay;
            if (day < 0 || day >= _config.WeeklyRewards.Length)
                day = 0;

            return _config.WeeklyRewards[day];
        }

        private Reward CreateShopItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.MoreMoves:
                    return new Reward { type = ItemType.MoreMoves, amount = _config.MoreMovesAmount };
                case ItemType.Remove:
                case ItemType.Undo:
                    return new Reward { type = itemType, amount = 1 };
                default:
                    return default(Reward);
            }
        }

        private static Reward GetPrice(Reward[] prices, int index, ItemType fallbackType, int fallbackAmount)
        {
            if (prices != null && index >= 0 && index < prices.Length)
                return prices[index];

            return new Reward { type = fallbackType, amount = fallbackAmount };
        }

        private async Task CloseTransitionAsync(CancellationToken cancellationToken)
        {
            if (_presentation != null)
                await _presentation.CloseTransitionAsync(cancellationToken);
            else
                cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task OpenTransitionAsync(CancellationToken cancellationToken)
        {
            if (_presentation != null)
                await _presentation.OpenTransitionAsync(cancellationToken);
            else
                cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task OpenTransitionSafelyAsync()
        {
            try
            {
                await OpenTransitionAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                _presentation?.ReportLoadFailure(exception.Message);
            }
        }

        private static int ClampVolume(int volume)
        {
            return Math.Max(0, Math.Min(100, volume));
        }

        private enum SessionState
        {
            Home,
            Transitioning,
            Playing,
            Won,
            Lost
        }
    }
}
