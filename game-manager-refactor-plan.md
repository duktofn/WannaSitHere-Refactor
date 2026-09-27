# GameManager orchestration migration

Date: 2026-09-25
Status: Implemented 2026-09-25. This document records the completed migration and its verification limits.

## Objective

GameBootstrapper creates objects, converts authoring configuration, injects dependencies, binds/unbinds endpoints, and forwards Unity lifecycle notifications. GameManager owns game commands, application state, operation ordering, success/failure decisions, and tutorial activation policy. Existing domain services and views retain their own mechanics and rendering responsibilities.

## Findings driving the migration

- GameBootstrapper currently chooses levels and rewards, resolves shop offers, coordinates booster follow-up, applies audio settings, and schedules daily refreshes.
- UIManager independently subscribes to Play/Next/Restart/Home commands. Its transitions run separately from the level load performed by GameBootstrapper; event subscription order cannot establish a reliable LevelReady boundary.
- AudioEventBinder also consumes raw Play/Home commands, so BGM can change before the corresponding operation succeeds.
- GridManager currently constructs LevelManager. GameManager retrieves it indirectly through Bootstrapper for booster operations.
- LevelBootstrapper.LoadLevel returns void, so callers cannot distinguish validation failure from successful loading. It already validates before clearing the old board; preserve that guarantee.
- CurrentLevel is advanced on win and stored as progression. Introduce a separate active-session level number so retry and tutorial checks can identify the board actually on screen.
- CheatToolWindow invokes Bootstrapper command methods. These callers need migration alongside runtime event bindings.
- The worktree has user edits in these classes and unfinished tutorial sources. Re-read their latest state before implementation; keep pre-existing compiler issues distinct from migration regressions.

## Target responsibilities

| Component | Responsibility after migration | Column 3 |
| --- | --- | --- |
| GameBootstrapper | Serialized references, construction, authoring conversion, dependency injection, lifecycle wiring, disposal |  |
| GameManager | Play/restart/next/home, active session, win/lose handling, reward and shop decisions, booster sequence, settings application, daily refresh, tutorial trigger timing |  |
| LevelManager | Move validation/history, condition evaluation, domain win/lose signals |  |
| EconomyManager / boosters / SaveLoadManager | Their existing focused rules and persistence operations |  |
| Unity level adapter | Validate/convert level assets, create environment/cell/person views, bind the supplied LevelManager and runtime data |  |
| UIManager / presentation adapter | Execute requested transitions and screen updates; report transition completion |  |
| TutorialService | Execute a selected mechanic's steps; report completion/cancellation |  |
| Audio/VFX players | Execute presentation feedback when the application reports a successful outcome or a local view owns the interaction |  |

GameManager remains a normal C# class in Game.App. It depends on App-owned interfaces implemented by Unity adapters. Game.App must not reference Game.View, Game.Data, or Game.Bootstrap. Bootstrapper must not retain use-case ordering inside event-binding lambdas.

## Dependency boundaries to introduce

Use interfaces only for actual layer crossings. Proposed names and signatures will be finalized while implementing callers:

- `ILevelLoader`: prepare/validate a level without clearing the active board, then activate the prepared level using a LevelManager supplied by GameManager. Prepared data contains runtime level data and the resolved catalog index, not ScriptableObjects or prefab references. The adapter retains asset lookup and environment instantiation.
- `IGamePresentation`: await close/open transition, show the requested screen, and apply HUD/settings/booster presentation results. Arguments use App/Core values such as MoveRecord and PersonRuntimeData. Resolving a PersonView or Transform stays in the adapter. Keep the interface limited to existing use cases; split only if implementation reveals unrelated consumers.
- Runtime configuration supplied to GameManager: copied economy prices/rewards/limits, booster amounts, converted CanSitAnywhere condition, and adjacency configuration. Level assets continue converting per load to preserve fresh person/runtime state.
- Move ShopPurchaseRequest to an App-owned contract, or translate it at the View boundary. GameManager must not import Game.View.UI to process purchases.

For awaited application flows, propose `Task`/`Task<T>` and CancellationToken at the App boundary. The View adapter awaits existing UniTask transitions internally. This avoids adding a UniTask reference to Game.App solely for orchestration. Unity rendering work stays on the Unity context; do not use Task.Run for it.

## Migration sequence and checks

### 1. Establish the behavior and caller baseline

Inventory public Bootstrapper commands, channel listeners, persistent UnityEvents, editor callers, and constructor users. Record current shop mappings, rewards, progress-on-win behavior, restart behavior, and level catalog wrapping. The legacy buy-button mappings differ from ShopPurchaseRequest mappings; preserve each existing path during this refactor rather than silently changing prices or items.

Check: each current entry point has one target GameManager command and an identified presentation output. Identify existing tutorial compilation errors before using compilation as a migration check.

### 2. Introduce App contracts and Unity adapters

Create the configuration/command DTOs and the two boundary interfaces. Adapt LevelBootstrapper into an explicitly named Unity level loader with separate preparation and activation. Retain its validation-before-clear behavior. Add a presentation adapter around existing UIManager, GridManager, AudioPlayer, and VfxPlayer methods as needed. Keep adapters mechanical: they execute a supplied operation and do not choose rewards, levels, or tutorial eligibility.

Check: assembly references remain acyclic; Game.App has no Data/View/Bootstrap references. Existing asset references and script GUIDs remain valid.

### 3. Move level session ownership and game flow into GameManager

Add PlayLevelAsync, RestartLevelAsync, NextLevelAsync, and BackToHomeAsync commands, plus accepted win/lose handlers. GameManager owns the active LevelManager and active level number. GridManager receives the LevelManager during binding instead of constructing it.

Replace LevelManager's direct outward win/lose channel publication with application result events consumed by GameManager. GameManager validates the active session and processes an outcome once, then publishes accepted notifications for views/audio. Detach the old session's events when replacing it. Update constructor call sites and relevant tests together.

Use a small session state enum plus an in-flight operation guard, rather than a new state-machine framework. A repeated Play/Next/Restart during transition is rejected; a Home/teardown request cancels pending work. Cancellation must be honored by presentation adapters and prevent late continuations from marking a replaced session ready.

Successful play sequence:

1. Accept command and determine requested level from application state.
2. Prepare/validate content while the old board is intact.
3. Cancel an active tutorial and block gameplay interaction during the transition.
4. Await the closed transition.
5. Create and attach the new LevelManager; activate prepared content and bind HUD/boosters.
6. Await the opened transition.
7. Commit the session to Playing, publish LevelReady, and evaluate tutorial eligibility.

A preparation failure preserves the old board, progress, and presentation mode, and emits no success/LevelReady notification. If activation fails after old objects have been cleared, use an explicit recoverable menu/error state; do not claim the old scene can be restored automatically. Ensure transition/input cleanup on failure or cancellation.

Keep persisted progression semantics compatible with existing saves. CurrentLevel continues representing saved progression; ActiveLevelNumber represents the loaded board. Restart targets the active level. Wins update progression at the existing point, once per accepted session result.

Check: successful, failed, repeated, canceled, and restarted flows have one owner and a deterministic readiness boundary. Old-session outcomes cannot advance the current session.

### 4. Move remaining runtime orchestration

Move reward selection, shop request validation/mapping, booster operation ordering, settings updates, daily refresh scheduling, and initial runtime synchronization into GameManager commands. Existing EconomyManager and booster classes continue executing their rules.

GameManager requests Undo view reversion and Remove feedback after successful domain operations, then evaluates conditions in the documented order. Preserve current animation timing semantics unless deliberately changed. Failure must not emit success audio, item-received events, or success VFX.

GameManager raises typed application outputs, wired to existing channels/presentation endpoints. Local ButtonClick, Transition, and PersonHappy audio remain owned by their views as in DEC-006. Initial settings and daily UI synchronization are application startup work, distinct from Bootstrapper's one-time binding.

Check: rewards, limits, inventory spending, settings persistence, daily rollover, and booster feedback retain their successful/failed behavior; no duplicated notification path remains.

### 5. Rewire Unity lifecycle, commands, and tooling

Bootstrapper's Awake constructs and binds dependencies. Start forwards application startup; Update/focus/quit forward lifecycle signals or time values, with date checks and save policy inside GameManager. OnEnable/OnDisable manage symmetrical subscriptions; teardown cancels outstanding flows and releases bindings.

Bind incoming channels directly to GameManager commands through thin wiring. Add Home as an application command. UIManager stops independently running game transitions from those same incoming channels. AudioEventBinder consumes accepted flow notifications for menu/game BGM rather than raw requests. Maintain one-way routing: a command must not be re-raised as its own result.

Expose the constructed GameManager through a read-only Bootstrapper property for editor composition access. Update CheatToolWindow to invoke GameManager commands. Audit scene/prefab UnityEvent method references before removing Bootstrapper wrappers; any temporary compatibility wrapper must only delegate and must have a removal step.

Retain serialized Bootstrapper fields/GUID where practical. Replace the existing ShopPanel runtime Find/AddComponent fallback with an explicit assigned dependency as part of scene composition wiring. Preserve user-authored scene changes during that targeted update.

Check: enable/disable cycles do not multiply subscriptions; editor cheats still work; scene references resolve; no gameplay Handle* method or decision logic remains in Bootstrapper.

### 6. Connect tutorial lifecycle and validate the migration

GameManager invokes TutorialService at confirmed application moments: LevelReady, successful move/booster outcomes, and unlock changes. TutorialService remains responsible for step execution. Home/restart/replacement cancels the active tutorial. Only a completed mechanic is persisted as complete. Full tutorial authoring, visuals, and content are a separate implementation task; reconcile the user's current scaffold before integrating these hooks.

Verification performed:

- Unity EditMode suite passed, including the GameManager fake-loader/presentation cases and existing domain/level-data tests.
- `dotnet build --no-restore` passed for Game.App, Game.Bootstrap, Game.View, Game.Editor, and Game.Tests.EditMode, with zero warnings/errors.
- Unity Play Mode/manual scene-flow smoke checks were not run; build and EditMode results do not verify runtime animation/input behavior.
- Scene request/result wiring and App assembly references were reviewed during migration; `git diff --check` passed.
- `scripts/ci/lint-conventions.sh` is absent in this checkout, so convention lint was not run.
- `codebase.md` and DEC-008 now describe the implemented ownership and known tutorial-content limitation.

## Completion criteria

- GameBootstrapper has only composition, binding, lifecycle forwarding, and cleanup responsibilities.
- GameManager is the single owner of application command ordering and tutorial activation decisions.
- App depends only on permitted assemblies and App-owned contracts; views continue owning Unity rendering details.
- Active level ownership, failure results, transition completion, and teardown cancellation are explicit.
- Existing saved progression and serialized Unity assignments remain compatible, and runtime/cheat entry points are migrated.
- DEC-008 and codebase.md accurately describe the implemented result and its verification limits.

## Implementation record

`GameManager` now owns level commands/session state, accepted outcomes and progression, economy/booster/settings/daily commands, and tutorial trigger policy through App-owned interfaces. `GameBootstrapper` constructs and wires the manager and Unity adapters, binds request/result endpoints, and forwards Unity lifecycle calls. `LevelBootstrapper` prepares levels before clearing the current board and activates using the manager-created `LevelManager`.

The tutorial lifecycle and per-mechanic completion persistence are wired. Authored mechanic tutorial definitions, concrete tutorial steps, and tutorial presentation content remain future content work. The scene flow has not received a manual Play Mode smoke pass; see the verification results above.
