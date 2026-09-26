using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Booster;
using Game.Core.Economy;
using Game.Core.People;
using Game.Events;

namespace Game.App
{
    public interface IGamePresentation
    {
        Task CloseTransitionAsync(CancellationToken cancellationToken);
        Task OpenTransitionAsync(CancellationToken cancellationToken);
        bool IsGameplayInputEnabled { get; }
        void SetGameplayInputEnabled(bool enabled);
        void ShowGameScreen(int levelNumber);
        void ShowHomeScreen(int levelNumber);
        void EnterGameAudio();
        void EnterHomeAudio();
        void ApplyAudioSettings(int soundVolume, bool soundMuted, int musicVolume, bool musicMuted);
        void PlayAudioCue(AudioCueId cue);
        void RaiseItemReceived(Reward reward);
        void RaiseItemSpent(Reward reward);
        void RefreshShop();
        void RefreshLoginRewards(
            IReadOnlyList<Reward> weeklyRewards,
            int loginDay,
            bool weeklyRewardClaimed,
            Reward dailyReward,
            bool dailyRewardClaimed);
        void RevertMove(MoveRecord record);
        void PlayRemoveFeedback(PersonRuntimeData person);
        void ReportLoadFailure(string error);
    }
}
