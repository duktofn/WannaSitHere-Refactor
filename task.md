# Task: Lose panel continuation

Status: Complete

## Goal

Bind PayButton to spend 500 Gold and resume the current board with +3 moves. Bind AdsButton to request rewarded ads and resume with +5 moves only after a reward callback. The user explicitly chose event/callback wiring; SDK integration is deferred.

## Steps

1. Extend GameManager's Lost session commands → complete: payment validation/persistence, resume without board reset, input restoration and outcome re-arming; ad request/cancel/reward handling.
2. Create and bind event assets and button listeners in MainScene → complete: Unity editor authoring succeeded; both buttons use the existing ButtonEventRaiser and keep their sound/punch effects.
3. Compile and inspect the focused diff → complete: Game.App and Game.Bootstrap compiled in Unity 6000.3.25f1; the final small payment/pending-ad adjustment also passed the same Unity Roslyn toolchain using isolated outputs in Temp/lose-continue-compile. No new tests added or run, following user preference. Runtime continuation and SDK ads are Not verified.

## SDK connection points

- OnRequestContinueAd: raised by AdsButton and handled by GameManager.
- OnShowContinueAd: request output for the future SDK adapter.
- OnContinueAdRewarded: SDK reward callback; adds five moves and resumes.
- OnContinueAdCancelled: SDK close/failure/cancel callback; grants nothing and permits another request.
- OnContinueWithGold: PayButton request; only spends 500 Gold in a Lost session with sufficient funds.

Ad requests remain pending until a callback or a session change. Payment can still resume while an ad request is pending, and clears that pending request so a later reward does not grant more moves. Losing again after continuation is supported.

## Handoff

### Follow-up: Pay scatter and configurable return delay

User added CurrencyScatterAnimation to PayButton and requested immediate scatter on successful payment, configurable time before returning to gameplay, and continuation of the scatter after LosePanel hides.

1. Route successful payment through asynchronous presentation feedback → implemented: one pending payment is permitted; Home/restart/teardown cancel the pending return.
2. Bind UIManager to the existing PayButton scatter and configure a 0.5-second return delay → complete: Unity editor authoring assigned the existing component and saved MainScene.
3. Let the Pay scatter finish while its owner is disabled, using the existing overlay canvas → implemented with an opt-in finishWhenDisabled field; destruction still cleans up. Other scatter instances retain their existing disable behavior.
4. Compile and review diff → complete: Game.App/Game.View/Game.Bootstrap compiled in Unity; the existing EditMode test double also compiled with the expanded interface. No tests added/run per user preference. Visual runtime behavior is Not verified.

The return delay is configured on UIManager under Paid Continue Feedback; the PayButton scatter has Finish When Disabled enabled and already uses the shared overlay. The temporary scene authoring script/metadata were removed. User-added PayButton scatter settings were preserved. No commit made.

- Branch: main; starting HEAD: f13a528. Completed and committed across commits 9ba33e8..73b4829 following repository conventions.
- Existing font, renamed audio files, audio catalog/mixer and recovery-scene changes were preserved.
- Changed runtime files: Assets/Game/App/GameManager.cs and Assets/Game/Bootstrap/GameBootstrapper.cs.
- MainScene has five channel references and one new event raiser/listener per Lose button. Five SO assets and Unity-generated metadata were created under Assets/Data/Events/.
- A temporary Editor authoring script wired and saved the scene, then was removed with its metadata. An unrelated automatic Light2D version serialization change was reverted; remaining scene additions are only continuation wiring.
- Previous work is archived in tasks/archive/2026-10-10-person-game-feel.md.
