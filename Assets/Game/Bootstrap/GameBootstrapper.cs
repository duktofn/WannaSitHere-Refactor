using System;
using System.Collections.Generic;
using UnityEngine;
using Game.App;
using Game.App.Tutorial;
using Game.Core.Economy;
using Game.Data.Conditions;
using Game.Data.Economy;
using Game.Data.Levels;
using Game.Events;
using Game.View.Audio;
using Game.View.Board;
using Game.View.UI;
using Game.View.Tutorial;
using Game.View.VFX;
using TMPro;

namespace Game.Bootstrap
{
    /// <summary>
    /// Unity composition root. Creates application services and connects serialized scene endpoints.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField] private List<LevelDataSO> _levelData;

        [Header("Economy")]
        [SerializeField] private EconomyConfigSO _economyConfig;

        [Header("Conditions")]
        [SerializeField] private ConditionDataSO _canSitAnywhereCondition;

        [Header("Tutorials")]
        [SerializeField] private MechanicTutorial[] _mechanicTutorials;
        [SerializeField] private TMP_FontAsset _tutorialFont;
        [SerializeField] private Sprite _tutorialHandSprite;

        [Header("Scene References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private WeeklyLogin _weeklyLogin;
        [SerializeField] private ShopPanelView _shopPanel;

        [Header("Visual Effects")]
        [SerializeField] private VfxPlayer _vfxPlayer;

        [Header("Audio")]
        [SerializeField] private AudioPlayer _audioPlayer;
        [SerializeField] private AudioSettingsView[] _audioSettingsViews;

        [Header("Events - Requests")]
        [SerializeField] private VoidEventChannelSO _onPlayGameEvent;
        [SerializeField] private VoidEventChannelSO _onNextLevelEvent;
        [SerializeField] private VoidEventChannelSO _onRestartLevelEvent;
        [SerializeField] private VoidEventChannelSO _onBackToHomeEvent;

        [Header("Events - Results")]
        [SerializeField] private VoidEventChannelSO _onWinEvent;
        [SerializeField] private VoidEventChannelSO _onLoseEvent;
        [SerializeField] private IntEventChannelSO _onLevelChangedEvent;

        [Header("Events - Rewards")]
        [SerializeField] private VoidEventChannelSO _onClaimWinRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimAdsRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimDailyRewardEvent;
        [SerializeField] private VoidEventChannelSO _onClaimWeeklyRewardEvent;

        [Header("Events - Shop")]
        [SerializeField] private VoidEventChannelSO _onBuyRemoveEvent;
        [SerializeField] private VoidEventChannelSO _onBuyUndoEvent;
        [SerializeField] private VoidEventChannelSO _onBuyMoreMovesEvent;

        [Header("Events - Booster Use")]
        [SerializeField] private VoidEventChannelSO _onUseMoreMovesEvent;
        [SerializeField] private VoidEventChannelSO _onUseUndoEvent;
        [SerializeField] private VoidEventChannelSO _onUseRemoveEvent;

        [Header("Events - Feedback")]
        [SerializeField] private OnItemReceiveSO _onItemReceive;
        [SerializeField] private OnItemSpendSO _onItemSpend;
        [SerializeField] private AudioCueEventChannelSO _onAudioCue;

        private readonly EventListener _listener = new EventListener();
        private GameManager _gameManager;

        public GameManager GameManager => _gameManager;

        private void Awake()
        {
            GameManagerConfig config = CreateConfig();
            ILevelLoader levelLoader = new LevelBootstrapper(_levelData, _gridManager, _levelView);
            IGamePresentation presentation = new UnityGamePresentation(
                _uiManager,
                _gridManager,
                _levelView,
                _weeklyLogin,
                _shopPanel,
                _vfxPlayer,
                _audioPlayer,
                _onItemReceive,
                _onItemSpend,
                _onAudioCue);
            MechanicTutorial[] tutorials = GetTutorialsWithFirstTimeDefault(out FirstTimeTutorialStep firstTimeTutorialStep);
            TutorialService tutorialService = new TutorialService(tutorials, presentation);

            _gameManager = new GameManager(config, levelLoader, presentation, tutorialService);
            firstTimeTutorialStep?.Configure(
                _gameManager,
                _gridManager,
                _levelView,
                _uiManager != null ? _uiManager.InGameCanvasTransform : null,
                _tutorialFont,
                _tutorialHandSprite);
            ((UnityGamePresentation)presentation).Initialize(
                _gameManager.Inventory,
                _gameManager.EconomyManager,
                config,
                request => _gameManager.TryPurchaseShopRequest(request));

            BindAudioSettingsViews();
            _gameManager.InitializeLoginState(DateTime.UtcNow);
        }

        private void OnEnable()
        {
            _listener.Listen(_onPlayGameEvent, _gameManager.RequestPlayCurrentLevel);
            _listener.Listen(_onNextLevelEvent, _gameManager.RequestNextLevel);
            _listener.Listen(_onRestartLevelEvent, _gameManager.RequestRestartLevel);
            _listener.Listen(_onBackToHomeEvent, _gameManager.RequestBackToHome);

            _listener.Listen(_onClaimWinRewardEvent, _gameManager.ClaimWinReward);
            _listener.Listen(_onClaimAdsRewardEvent, _gameManager.ClaimAdsReward);
            _listener.Listen(_onClaimDailyRewardEvent, _gameManager.ClaimDailyReward);
            _listener.Listen(_onClaimWeeklyRewardEvent, _gameManager.ClaimWeeklyReward);

            _listener.Listen(_onBuyRemoveEvent, _gameManager.BuyRemove);
            _listener.Listen(_onBuyUndoEvent, _gameManager.BuyUndo);
            _listener.Listen(_onBuyMoreMovesEvent, _gameManager.BuyMoreMoves);

            _listener.Listen(_onUseMoreMovesEvent, _gameManager.UseMoreMoves);
            _listener.Listen(_onUseUndoEvent, _gameManager.UseUndo);
            _listener.Listen(_onUseRemoveEvent, _gameManager.UseRemove);

            if (_gameManager != null)
            {
                _gameManager.WinAccepted += RaiseWinResult;
                _gameManager.LoseAccepted += RaiseLoseResult;
                _gameManager.LevelNumberChanged += RaiseLevelNumberChanged;
            }
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
            if (_gameManager == null)
                return;

            _gameManager.WinAccepted -= RaiseWinResult;
            _gameManager.LoseAccepted -= RaiseLoseResult;
            _gameManager.LevelNumberChanged -= RaiseLevelNumberChanged;
            _gameManager.CancelPendingOperations();
        }

        private void Start()
        {
            _gameManager?.PublishCurrentLevel();
            _gameManager?.OnApplicationReady();
        }

        private void Update()
        {
            _gameManager?.Tick(DateTime.UtcNow);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                _gameManager?.Tick(DateTime.UtcNow);
        }

        private void OnApplicationQuit()
        {
            _gameManager?.CancelPendingOperations();
            _gameManager?.SaveGame();
        }

        private GameManagerConfig CreateConfig()
        {
            return new GameManagerConfig
            {
                GoldShopLimits = _economyConfig?.goldShopLimit ?? new[] { 5, 5, 5 },
                GoldShopPrices = _economyConfig?.goldShopPrice ?? Array.Empty<Reward>(),
                GemShopPrices = _economyConfig?.gemShopPrice ?? Array.Empty<Reward>(),
                LevelWinReward = _economyConfig != null ? _economyConfig.levelWinReward : default(Reward),
                LevelAdsWinReward = _economyConfig != null ? _economyConfig.levelAdsWinReward : default(Reward),
                DailyReward = _economyConfig != null ? _economyConfig.dailyReward : default(Reward),
                WeeklyRewards = _economyConfig?.weeklyReward ?? Array.Empty<Reward>(),
                CanSitAnywhereCondition = _canSitAnywhereCondition?.ToRuntimeData(),
                AdjacentOffsets = _gridManager != null ? _gridManager.AdjacentOffsets : null
            };
        }

        private MechanicTutorial[] GetTutorialsWithFirstTimeDefault(out FirstTimeTutorialStep firstTimeTutorialStep)
        {
            firstTimeTutorialStep = null;
            List<MechanicTutorial> tutorials = _mechanicTutorials == null
                ? new List<MechanicTutorial>()
                : new List<MechanicTutorial>(_mechanicTutorials);

            for (int i = 0; i < tutorials.Count; i++)
            {
                if (tutorials[i] != null && tutorials[i].Trigger == TutorialTrigger.FirstTimePlaying)
                    return tutorials.ToArray();
            }

            MechanicTutorial tutorial = gameObject.AddComponent<MechanicTutorial>();
            firstTimeTutorialStep = gameObject.AddComponent<FirstTimeTutorialStep>();
            tutorial.Configure(
                MechanicTutorialID.Drag,
                TutorialTrigger.FirstTimePlaying,
                new TutorialStep[] { firstTimeTutorialStep });
            tutorials.Add(tutorial);
            return tutorials.ToArray();
        }

        private void BindAudioSettingsViews()
        {
            if (_audioSettingsViews == null)
                return;

            for (int i = 0; i < _audioSettingsViews.Length; i++)
            {
                _audioSettingsViews[i]?.Bind(
                    _gameManager.SoundVolume,
                    _gameManager.IsSoundMuted,
                    _gameManager.MusicVolume,
                    _gameManager.IsMusicMuted,
                    _gameManager.SetSoundSettings,
                    _gameManager.SetMusicSettings);
            }
        }

        private void RaiseWinResult() => _onWinEvent?.Raise();
        private void RaiseLoseResult() => _onLoseEvent?.Raise();
        private void RaiseLevelNumberChanged(int levelNumber) => _onLevelChangedEvent?.Raise(levelNumber);
    }
}
