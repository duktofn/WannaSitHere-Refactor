using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Game.App;
using Game.Core.Booster;
using Game.Core.Economy;
using Game.Core.People;
using Game.Data.Economy;
using Game.Events;
using Game.View.Audio;
using Game.View.Board;
using Game.View.People;
using Game.View.UI;
using Game.View.VFX;

namespace Game.Bootstrap
{
    public sealed class UnityGamePresentation : IGamePresentation
    {
        private readonly UIManager _uiManager;
        private readonly GridManager _gridManager;
        private readonly LevelView _levelView;
        private readonly WeeklyLogin _weeklyLogin;
        private readonly ShopPanelView _shopPanel;
        private readonly VfxPlayer _vfxPlayer;
        private readonly AudioPlayer _audioPlayer;
        private readonly OnItemReceiveSO _onItemReceive;
        private readonly OnItemSpendSO _onItemSpend;
        private readonly AudioCueEventChannelSO _onAudioCue;

        public UnityGamePresentation(
            UIManager uiManager,
            GridManager gridManager,
            LevelView levelView,
            WeeklyLogin weeklyLogin,
            ShopPanelView shopPanel,
            VfxPlayer vfxPlayer,
            AudioPlayer audioPlayer,
            OnItemReceiveSO onItemReceive,
            OnItemSpendSO onItemSpend,
            AudioCueEventChannelSO onAudioCue)
        {
            _uiManager = uiManager;
            _gridManager = gridManager;
            _levelView = levelView;
            _weeklyLogin = weeklyLogin;
            _shopPanel = shopPanel;
            _vfxPlayer = vfxPlayer;
            _audioPlayer = audioPlayer;
            _onItemReceive = onItemReceive;
            _onItemSpend = onItemSpend;
            _onAudioCue = onAudioCue;
        }

        public void Initialize(
            Inventory inventory,
            EconomyManager economyManager,
            GameManagerConfig config,
            System.Action<ShopPurchaseRequest> onPurchaseRequested)
        {
            _uiManager?.Initialize(inventory);
            _levelView?.BindBoosters(inventory);
            _shopPanel?.Bind(
                config?.GoldShopPrices,
                config?.GoldShopLimits,
                config?.GemShopPrices,
                economyManager,
                onPurchaseRequested);
        }

        public async Task CloseTransitionAsync(CancellationToken cancellationToken)
        {
            if (_uiManager != null)
                await _uiManager.CloseTransitionAsync(cancellationToken);
            else
                cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task OpenTransitionAsync(CancellationToken cancellationToken)
        {
            if (_uiManager != null)
                await _uiManager.OpenTransitionAsync(cancellationToken);
            else
                cancellationToken.ThrowIfCancellationRequested();
        }

        public bool IsGameplayInputEnabled => _gridManager != null && _gridManager.IsGameplayInputEnabled;

        public void SetGameplayInputEnabled(bool enabled)
        {
            _gridManager?.SetGameplayInputEnabled(enabled);
        }

        public void ShowGameScreen(int levelNumber)
        {
            _uiManager?.ShowGameScreen(levelNumber);
        }

        public void ShowHomeScreen(int levelNumber)
        {
            _uiManager?.ShowHomeScreen(levelNumber);
        }

        public void EnterGameAudio()
        {
            _audioPlayer?.PlayMusic(MusicId.InGame);
        }

        public void EnterHomeAudio()
        {
            _audioPlayer?.PlayMusic(MusicId.MainMenu);
        }

        public void ApplyAudioSettings(int soundVolume, bool soundMuted, int musicVolume, bool musicMuted)
        {
            _audioPlayer?.ApplySettings(soundVolume, soundMuted, musicVolume, musicMuted);
        }

        public void PlayAudioCue(AudioCueId cue)
        {
            _onAudioCue?.Raise(cue);
        }

        public Task PlayPaidContinueFeedbackAsync(CancellationToken cancellationToken)
        {
            return _uiManager != null
                ? _uiManager.PlayPaidContinueFeedbackAsync(cancellationToken)
                : Task.CompletedTask;
        }

        public void RaiseItemReceived(Reward reward)
        {
            _onItemReceive?.Raise(reward);
        }

        public void RaiseItemSpent(Reward reward)
        {
            _onItemSpend?.Raise(reward);
        }

        public void RefreshShop()
        {
            _shopPanel?.Refresh();
        }

        public void RefreshLoginRewards(
            IReadOnlyList<Reward> weeklyRewards,
            int loginDay,
            bool weeklyRewardClaimed,
            Reward dailyReward,
            bool dailyRewardClaimed)
        {
            if (_weeklyLogin != null && weeklyRewards != null)
            {
                _weeklyLogin.SetRewardData(
                    new List<Reward>(weeklyRewards),
                    loginDay,
                    weeklyRewardClaimed,
                    dailyReward,
                    dailyRewardClaimed);
            }

            _shopPanel?.Refresh();
        }

        public void RevertMove(MoveRecord record)
        {
            _gridManager?.RevertMoveView(record);
        }

        public void PlayRemoveFeedback(PersonRuntimeData person)
        {
            PersonView personView = _gridManager?.FindPersonView(person);
            _vfxPlayer?.PlayAtWorld(VfxId.RemoveBooster, personView?.transform);
        }

        public void ReportLoadFailure(string error)
        {
            Debug.LogError($"[GameManager] Level operation failed: {error}");
        }
    }
}
