# Codebase Context

Last Updated: 2026-09-26
Last Reviewed Commit: working tree (includes uncommitted changes)

---

# Project Overview

A Unity-based casual puzzle game — **Wanna Sit Here** (Puzzle Seat Sorting).

Players drag and swap characters between seats on a board. Each character has Like/Hate conditions about adjacent people or food. A level is won when all conditions are satisfied before running out of moves.

Primary systems include:

- board and grid management,
- person drag-and-drop movement,
- condition evaluation (Like/Hate rules),
- booster system (More Moves, Undo, Remove),
- economy (Gold, Gem, shop, daily/weekly login rewards),
- persistence (JSON file save/load),
- UI navigation and transitions,
- catalogued UI/SFX/BGM audio playback with persisted settings,
- event-driven architecture via ScriptableObject channels.

The project separates core gameplay logic (pure C#, no MonoBehaviour) from presentation, configuration, and infrastructure concerns using a Clean/Layered Architecture with ScriptableObject Event Channels for decoupled communication.

---

# Tech Stack

- Unity 6000.3.18f1 (revision `5ebeb53e4c07`)
- C#
- UniTask (via Git URL)
- PrimeTween 1.4.11
- Unity Input System 1.20.0
- Universal Render Pipeline 17.3.0
- Unity LevelPlay Ads Mediation UPM 9.5.1
- TextMeshPro
- uGUI (com.unity.ugui 2.0.0)
- UI Particle (com.coffee.ui-particle 4.13.3, embedded)
- Unity Test Framework 1.6.0 (NUnit)

---

# Project Structure

```text
Assets/
├── Art/                     Sprite and visual assets
├── Audio/                   Audio clips and mixer assets
├── Data/                    ScriptableObject asset instances
├── Game/
│   ├── Core/                (Game.Core)         Pure C# domain: rules, state, economy, boosters
│   ├── Events/              (Game.Events)       ScriptableObject event channels
│   ├── Data/                (Game.Data)          SO configuration authoring + conversion
│   ├── App/                 (Game.App)           Game orchestration, level coordination, persistence
│   ├── View/                (Game.View)          MonoBehaviour presentation, UI, input, effects, VFX
│   ├── Bootstrap/           (Game.Bootstrap)     Composition root and Unity adapters
│   ├── Editor/              (Game.Editor)        Custom inspectors, cheat tools
│   └── Tests/               (Game.Tests.*)       EditMode unit tests
├── Prefabs/                 UI and gameplay prefabs
├── Scenes/                  Unity scenes
├── Settings/                URP and quality settings
├── Shaders/                 Custom shaders (transition iris-wipe)
└── TextMesh Pro/            TMP default resources
```

`Assets/Art/VFX/` contains first-party gameplay VFX and `VFXCatalog.asset`. `Assets/Lana Studio/Hyper Casual FX/` contains imported third-party particle assets retained as source material for the configured effects.

Mobile mediation support includes the `com.unity.services.levelplay` package, editor dependency metadata under `Assets/LevelPlay/Editor/`, Google dependency resolver files under `Assets/MobileDependencyResolver/`, and Android Gradle templates under `Assets/Plugins/Android/`.

---

# Verified Project Configuration

- **Editor:** Unity `6000.3.18f1`, revision `5ebeb53e4c07`, from `ProjectSettings/ProjectVersion.txt`.
- **Render pipeline:** URP `17.3.0`; the quality profiles reference `Assets/Settings/UniversalRP.asset` in `ProjectSettings/QualitySettings.asset`.
- **Input:** Input System package `1.20.0`; `ProjectSettings/ProjectSettings.asset` sets `activeInputHandler: 1` (Input System only).
- **Build scenes:** `Assets/Scenes/MainScene.unity` is the only enabled Build Settings scene in `ProjectSettings/EditorBuildSettings.asset`. `Assets/Scenes/LV Scene.unity` is not listed there.
- **Tests:** `Game.Tests.EditMode` in `Assets/Game/Tests/Game.Tests.EditMode.asmdef` is the only test assembly; sources include `DomainTests.cs`, `LevelDataRefactorTests.cs`, and `GameManagerFlowTests.cs`. No PlayMode test assembly was found.
- **C# conventions:** Block-scoped namespaces, `_camelCase` private fields, and `[SerializeField] private` for Unity-serialized fields; see `AGENTS.md`.
- **Ads and networking:** LevelPlay `9.5.1` is installed, but no first-party LevelPlay API usage was found under `Assets/Game`. `com.unity.multiplayer.center` is installed, but no runtime networking package or first-party networking API usage was found under `Assets/Game`.
- **Unity tooling:** Unity MCP tools are available. The Editor state query succeeded and reported the Editor idle and outside Play Mode during this inspection.
- **Review scope:** The working tree already contained uncommitted changes. They were left untouched and are not treated as stable project behavior in this document.

---

# Architecture

Layered architecture with strict dependency direction. `Game.Core` sits at the center with zero assembly references. Communication between View and logic layers uses ScriptableObject Event Channels.

```text
Game.Core  (no dependencies)
   ▲           ▲                     ▲                   ▲
   │           │                     │                   │
Game.Events   Game.Data           Game.App               │
   ▲                                 ▲                   │
   │                                 │                   │
   └──────────────┬──────────────────┴──────────── Game.View
                  │                                      ▲
                  │                                      │
                  └───────────────────────────── Game.Bootstrap
```

- **`Game.Core`** — Domain center. No MonoBehaviours. No assembly references.
- **`Game.Events`** — References `Game.Core` (for `Reward` type) and `UniTask`; also owns the presentation-only `AudioCueId` contract and typed cue channel.
- **`Game.Data`** — References `Game.Core` only.
- **`Game.App`** — References `Game.Core`, `Game.Events`, and `Game.Debug`; stores audio volume/mute primitives in `GameData` but never references Unity audio objects.
- **`Game.View`** — References `Game.Core`, `Game.Events`, `Game.App`, `Game.Debug`, `PrimeTween`, `UniTask`, `TMP`, `InputSystem`, `uGUI`, `Coffee.UIParticle`, and Unity audio types only inside `Game.View.Audio`.
- **`Game.Bootstrap`** — References all runtime assemblies: `Game.Core`, `Game.Data`, `Game.App`, `Game.View`, `Game.Events`.
- **`Game.Editor`** — Editor-only. References `Game.Data`, `Game.App`, `Game.Core`, `Game.Bootstrap`.
- **`Game.Tests.EditMode`** — Editor-only. References `Game.Core`, `Game.App`, `Game.Events`, `Game.Data`.

Key rules: `Game.View` does **not** reference `Game.Data`. `GameManager` owns application command ordering and game-flow decisions. `GameBootstrapper` only creates and wires dependencies, converts scene authoring configuration, binds endpoints, and forwards Unity lifecycle notifications. `LevelDataSO` owns validation and configuration-to-runtime conversion through the Unity level-loader adapter.

Presentation VFX is owned by `Game.View.VFX`. `VfxCatalogSO` stores prefab and lifetime configuration, while `VfxPlayer` owns spawning, UI/world placement, pooling, cancellation, and cleanup. UI particle instances use the embedded `Coffee.UIParticle` renderer under the per-Canvas `VFXRoot`, while world effects continue to use the regular ParticleSystem path. `Game.Core` remains unaware of VFX; `Game.Bootstrap` and presentation views provide explicit references to the player.

Presentation audio is owned by `Game.View.Audio`. `AudioCueId` and `AudioCueEventChannelSO` provide the cross-layer cue contract without audio objects. `AudioCatalogSO` stores cue/music configuration, while the scene-owned `AudioPlayer` owns AudioSources, Mixer routing, variation/cooldown rules, and BGM cross-fade. `GameManager` reports accepted application results through presentation outputs; view components call the player directly for button, transition, and Happy feedback.

---

# Layers / Modules

## Game.Core (Domain Layer)

Responsibility:

Contains gameplay rules, runtime state, condition evaluation, economy, and booster logic as pure C# classes.

Depends On:

- No other project assembly.
- Unity engine primitives (`Vector2`, `Vector2Int`, `Sprite`, `Mathf`, `Random`).

Used By:

- Every other project assembly.

Communication:

- Exposes C# events (`Action`, `Action<T>`) for state changes (`OnMoveChanged`, `OnPersonStateChanged`, `OnConditionsCleared`, `OnInventoryUpdate`, `OnBoosterUsed`).
- Upper layers invoke domain methods directly and subscribe to domain events.

Modules: Board, People, Conditions, Booster, Economy, Levels.

---

## Game.Events (Event-Driven Messaging)

Responsibility:

Provides ScriptableObject-based event channels that decouple publishers from subscribers across assemblies.

Depends On:

- `Game.Core` (for `Reward` struct in payload channels).
- `UniTask` (for async delay in `ButtonEventRaiser`).

Used By:

- `Game.App`, `Game.View`, `Game.Bootstrap`.

Communication:

- Channels are ScriptableObject assets assigned via serialized references in the Inspector.
- Publishers call `channel.Raise()`. Subscribers register via `channel.OnRaised += handler` or through `EventListener`.
- `AudioCueEventChannelSO` is the single typed `AudioCueId` channel at `Assets/Data/Events/OnAudioCue.asset`; it carries no `AudioClip` or `AudioSource`.

---

## Game.Data (Configuration Layer)

Responsibility:

ScriptableObject definitions for level layout, person, condition, and economy configuration. Provides `ToRuntimeData()` factory methods to convert static config into domain runtime objects.

Depends On:

- `Game.Core`.

Used By:

- `Game.Bootstrap`, `Game.Editor`.

Communication:

- `LevelBootstrapper` calls `LevelDataSO.TryToRuntimeData()` to validate and create `LevelRuntimeData` before the active grids are cleared.
- `LevelDataSO` owns the conversion boundary: it validates `LevelPersonConfig` entries, creates one `PersonRuntimeData` per occurrence, and passes that instance to `CellDataSO.ToRuntimeData()`.
- `PersonDefinitionSO` contains shared name/trait/sprite data. Conditions and placement belong to `LevelPersonConfig` owned by `LevelDataSO`.

---

## Game.App (Application Layer)

Responsibility:

Application state and game operations: active-level coordination and game-flow orchestration (`GameManager`), gameplay rules/history (`LevelManager`), persisted progress (`SaveLoadManager`, `GameData`), economy transactions, core booster execution, and tutorial trigger policy (`TutorialService`).

Depends On:

- `Game.Core`, `Game.Events`.

Used By:

- `Game.View` (via the active `LevelManager` supplied to `GridManager`), `Game.Bootstrap`.

Communication:

- `LevelManager` validates moves, records history, evaluates conditions, and raises typed C# outcome/move notifications to its owning `GameManager`.
- `GameManager` owns `GameData`, `Inventory`, `EconomyManager`, active-session state, level loading/transition order, outcome acceptance, rewards, shop/booster decisions, daily refresh, settings, and tutorial activation. Unity work is accessed only through `ILevelLoader` and `IGamePresentation`; the App assembly does not reference `Game.Data`, `Game.View`, or `Game.Bootstrap`.
- `GameManager` persists volume/mute settings and sends primitive settings values through `IGamePresentation`.
- `TutorialService` raises `TriggerFired` whenever it receives a trigger request, before looking for a configured tutorial; `GameManager` forwards it as `TutorialTriggerFired`. On a fresh save, the first-time tutorial trigger waits until Level 1 is ready. `IsFirstTimePlaying` remains true when there is no matching tutorial, or while a started tutorial is still incomplete; it is persisted as false after completion.
- `SaveLoadManager` serializes `GameData` to `data.json` at `Application.persistentDataPath`.

---

## Game.View (Presentation & UI Layer)

Responsibility:

All MonoBehaviour presentation: board rendering, person sprites, drag-and-drop input, UI panels, tweens, transitions, and effects.

Depends On:

- `Game.Core`, `Game.Events`, `Game.App`, `PrimeTween`, `UniTask`, `TMP`, `InputSystem`.

Used By:

- `Game.Bootstrap`.

Communication:

- Subscribes to domain C# events (`OnPersonStateChanged`, `OnMoveChanged`, `OnInventoryUpdate`, `OnConditionsCleared`).
- Owns rendering/input and subscribes to result/feedback channels; requested game-flow commands are bound by `GameBootstrapper` to `GameManager`.
- Reads domain state but does not mutate domain models directly (mutations go through `GridManager` → `LevelManager`).

Modules: Board (CellView, GridManager, FoodTooltips), People (PersonView, PersonMover, PersonSpawner, PersonTooltip, PersonDragManager), UI (UIManager, LevelView, InventoryView, BoosterSlotView, ShopPanelView, ShopItemView, PanelController, TransitionController, LevelEndPanel, LevelEndText, WeeklyLogin, ButtonSpriteSwap, UIAlphaExtensions), Effect (ButtonPunchShake, AdsShaking, CurrencyFlyAnimation, CurrencyScatterAnimation), VFX (VfxId, VfxCatalogSO, VfxPlayer, VfxInstance), Audio (AudioCatalogSO, AudioPlayer, AudioEventBinder, UIButtonSound, AudioSettingsView).

---

## Game.Bootstrap (Composition Root)

Responsibility:

Sole layer that knows all runtime layers. Creates and wires runtime objects, translates scene configuration into App-owned inputs, binds command/result channels, and forwards Unity lifecycle notifications. It does not decide or sequence game use cases.

Depends On:

- All runtime assemblies.

Used By:

- None (top of dependency tree).

Communication:

- `GameBootstrapper` creates `GameManager`, the Unity implementations of `ILevelLoader` and `IGamePresentation`, and `TutorialService`; when no `FirstTimePlaying` tutorial is authored, it composes the default Level 1 tutorial at runtime. It binds serialized request channels directly to manager commands and accepted result events to existing result channels.
- `LevelBootstrapper` implements `ILevelLoader`: it validates/converts a level before clearing the board, then activates it using the `LevelManager` supplied by `GameManager`.
- `UnityGamePresentation` adapts application presentation requests to `UIManager`, `GridManager`, audio/VFX services, and existing feedback channels.
- `LevelLayoutGizmosDrawer` is a normal `MonoBehaviour` that reads selected level SO data for Scene-view layout visualization only.

---

## Game.Editor (Editor Tooling)

Responsibility:

Custom inspectors and development utilities. Editor-only assembly.

Contains:

- `CheatToolWindow`: Dual-mode (Play/Edit) cheat window for manipulating save data and game state.
- `LevelDataSOEditor`: Visual 2D matrix editor for `LevelDataSO`, including level-owned person placements, conditions, person-name markers that can be clicked to select a configuration, and shared validation feedback.

---

## Game.Tests.EditMode (Unit Tests)

Responsibility:

NUnit EditMode tests covering domain logic, level-data conversion, and GameManager orchestration through fake loader/presentation/store dependencies. GameManager flow cases cover preparation failure preserving the active session, transition ordering, duplicate-command rejection, home cancellation, stale-session outcomes, one-time win progression, and restart progression semantics.

---

# Class Index

## EventChannelSO

Path:
`Assets/Game/Events/EventChannelSO.cs`

Responsibility:

Abstract base for all ScriptableObject event channels. Provides parameterless raise/subscribe pattern.

Inherits / Implements:

- `ScriptableObject`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `entries` | `List<Entry>` | Serialized mapping from VFX identifiers to prefabs, timing settings, and runtime scale multipliers |

Each `Entry` contains a `VfxId`, prefab reference, prewarm count, fade delay, fade duration, and scale multiplier. The Happy entry uses `0.01` while the `VFX_Happy` prefab retains its original root scale.

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Raise` | — | `void` | Invokes `OnRaised` |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnRaised` | `Action` | Fired when the channel is raised |

---

## VoidEventChannelSO

Path:
`Assets/Game/Events/VoidChannelSO.cs`

Responsibility:

Concrete parameterless event channel. CreateAssetMenu: `Game/Event Channel/No Payload`.

Inherits / Implements:

- `EventChannelSO`

## AudioCueId

Path:
`Assets/Game/Events/AudioCueId.cs`

Responsibility:

Presentation cue contract containing `ButtonClick`, `Transition`, `BoosterUsed`, `PersonHappy`, `Win`, `Lose`, `Claim`, and `Spend`. It has no Unity audio references and is not used by `Game.Core`.

## AudioCueEventChannelSO

Path:
`Assets/Game/Events/AudioCueEventChannelSO.cs`

Responsibility:

Typed ScriptableObject event channel carrying one `AudioCueId`. The project uses one serialized asset, `Assets/Data/Events/OnAudioCue.asset`, for all gameplay outcome cues.

Inherits / Implements:

- `EventChannelSO<AudioCueId>`

---

## EventChannelSO\<T\>

Path:
`Assets/Game/Events/SinglePayloadChannelSO.cs`

Responsibility:

Abstract generic single-payload event channel. Hides base `OnRaised` with `Action<T>`.

Inherits / Implements:

- `EventChannelSO`

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Raise` | `T value` | `void` | Invokes `OnRaised` with payload |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnRaised` | `Action<T>` | Fired with typed payload |

---

## EventChannelSO\<T1, T2\>

Path:
`Assets/Game/Events/DoublePayloadChannelSO.cs`

Responsibility:

Abstract generic dual-payload event channel.

Inherits / Implements:

- `EventChannelSO`

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Raise` | `T1 val1, T2 val2` | `void` | Invokes `OnRaised` with two payloads |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnRaised` | `Action<T1, T2>` | Fired with dual payload |

---

## IntEventChannelSO

Path:
`Assets/Game/Events/IntEventChannelSO.cs`

Responsibility:

Concrete `EventChannelSO<int>`. Used for level number changes.

Inherits / Implements:

- `EventChannelSO<int>`

---

## OnItemReceiveSO

Path:
`Assets/Game/Events/OnItemReceiveSO.cs`

Responsibility:

Concrete `EventChannelSO<Reward>`. Broadcasts when player receives an item.

Inherits / Implements:

- `EventChannelSO<Reward>`

---

## OnItemSpendSO

Path:
`Assets/Game/Events/OnItemSpendSO.cs`

Responsibility:

Concrete `EventChannelSO<Reward>`. Broadcasts when player spends or fails to spend.

Inherits / Implements:

- `EventChannelSO<Reward>`

---

## EventListener

Path:
`Assets/Game/Events/EventListener.cs`

Responsibility:

Utility class that tracks ScriptableObject event channel subscriptions and enables batch unsubscription via `UnbindAll()`.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_unbindActions` | `List<Action>` | Stores closures that unsubscribe handlers |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Listen` | `EventChannelSO channel, Action handler` | `void` | Subscribes handler and records unbind action |
| `Listen<T>` | `EventChannelSO<T> channel, Action<T> handler` | `void` | Subscribes typed handler and records unbind action |
| `Listen<T1, T2>` | `EventChannelSO<T1, T2> channel, Action<T1, T2> handler` | `void` | Subscribes dual-typed handler and records unbind action |
| `UnbindAll` | — | `void` | Executes all unbind actions and clears the list |

---

## ButtonEventRaiser

Path:
`Assets/Game/Events/ButtonEventRaiser.cs`

Responsibility:

MonoBehaviour that raises a list of `EventChannelSO` channels when clicked, with optional delay via UniTask and one-shot disable.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `EventChannelSO`
- `UnityEngine.UI.Button`
- `UniTask`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `eventChannels` | `List<EventChannelSO>` | Channels to raise on click |
| `delay` | `float` | Seconds to wait before raising |
| `isPressedOnce` | `bool` | If true, disables button after first press |
| `button` | `Button` | UI Button reference |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `RaiseAll` | — | `void` | Entry point: optionally disables button, raises with or without delay |
| `RaiseImmediately` | — | `void` | Iterates `eventChannels` and calls `Raise()` on each |
| `RaiseAllWithDelay` | — | `UniTaskVoid` | Awaits delay then calls `RaiseImmediately` |

---

## Grid\<T\>

Path:
`Assets/Game/Core/Board/Grid.cs`

Responsibility:

Generic 2D grid container with configurable size, cell dimensions, spacing, and viewport position. Backed by a flattened 1D array.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_gridSize` | `Vector2Int` | Width and height in cell count |
| `_cellSize` | `Vector2` | World dimensions of a cell |
| `_cellDistance` | `Vector2` | Gap between cells |
| `_posX` | `float` | Normalized horizontal viewport anchor [0, 1] |
| `_posY` | `float` | Normalized vertical viewport anchor [0, 1] |
| `_gridContent` | `T[]` | Flattened content array |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Get` | `int x, int y` | `T` | Returns cell at (x, y) with bounds check; default if out of bounds |
| `Set` | `int x, int y, T value` | `void` | Sets cell at (x, y) with bounds check |

---

## CellRuntimeData

Path:
`Assets/Game/Core/Board/CellRuntimeData.cs`

Responsibility:

Runtime state of a single board cell. Tracks type, position, food, sprite, grid ownership, and current person occupant.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `Index` | `Vector2Int` (readonly) | Grid coordinate |
| `Type` | `CellType` (readonly) | Seat, Food, or Block |
| `Size` | `Vector2` (readonly) | Layout size |
| `DefaultPerson` | `PersonRuntimeData` (readonly) | Person assigned at level start |
| `Food` | `Food` (readonly) | Food type if cell is food |
| `Sprite` | `Sprite` (readonly) | Visual sprite |
| `OwnGrid` | `GridId` (readonly) | MainGrid or WaitGrid |
| `CurrentPerson` | `PersonRuntimeData` (get; private set) | Currently occupying person |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `SetPerson` | `PersonRuntimeData person` | `void` | Updates CurrentPerson |

---

## PersonRuntimeData

Path:
`Assets/Game/Core/People/PersonRuntimeData.cs`

Responsibility:

Runtime state of a character: name, trait, conditions, per-condition satisfaction, sprite, and emotional state. Fires events when state, condition list, or condition satisfaction changes.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `PersonName` | `string` (readonly) | Display name |
| `Trait` | `PersonTrait` (readonly) | Personality trait (Cool, Sick, Dirty, Loud, Quiet) |
| `_conditions` | `List<ConditionRuntimeData>` | Satisfaction requirements |
| `_conditionSatisfied` | `List<bool>` | Satisfaction state aligned by index with `_conditions` |
| `Conditions` | `IReadOnlyList<ConditionRuntimeData>` | Read-only condition list |
| `ConditionSatisfied` | `IReadOnlyList<bool>` | Read-only per-condition satisfaction list |
| `BaseSprite` | `Sprite` (readonly) | Default visual sprite |
| `State` | `PersonState` (get; private set) | Normal, Angry, or Happy |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `ClearConditions` | — | `void` | Empties conditions list and fires `OnConditionsCleared` |
| `ReplaceConditions` | `ConditionRuntimeData condition` | `void` | Replaces the complete list with one condition and fires `OnConditionsCleared` |
| `SetConditionSatisfied` | `int index, bool satisfied` | `void` | Updates one condition's status and fires `OnConditionStatusChanged` when the value changes |
| `SetState` | `PersonState state` | `void` | Updates State and fires `OnPersonStateChanged` if changed |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnPersonStateChanged` | `Action<PersonState>` | Fired when emotional state changes |
| `OnConditionsCleared` | `Action` | Fired after the condition list is cleared or replaced |
| `OnConditionStatusChanged` | `Action` | Fired when a condition's satisfied state changes |

---

## ConditionRuntimeData

Path:
`Assets/Game/Core/Conditions/ConditionRuntimeData.cs`

Responsibility:

Immutable data object representing a single Like/Hate condition targeting a Food type or PersonTrait. `Like + Food.Any` is the `CanSitAnywhere` semantic condition.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `Type` | `ConditionType` (readonly) | Hate or Like |
| `Target` | `ConditionTarget` (readonly) | Food or Person |
| `TargetTrait` | `PersonTrait` (readonly) | Target trait when target is Person |
| `FoodTarget` | `Food` (readonly) | Target food when target is Food |
| `Description` | `string` (readonly) | Satisfied condition text |
| `AngryDescription` | `string` (readonly) | Unsatisfied condition text |
| `IsCanSitAnywhere` | `bool` (computed) | Identifies the `Like + Food.Any` condition that is satisfied at any main-grid seat |

---

## ConditionChecker

Path:
`Assets/Game/Core/Conditions/ConditionChecker.cs`

Responsibility:

Stateless evaluator that checks whether a single condition is satisfied given a list of adjacent cells.

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Check` | `List<CellRuntimeData> adjacent, ConditionRuntimeData condition` | `bool` | Hate: no adjacent match. Like: at least one adjacent match; `CanSitAnywhere` always returns true |
| `MatchTarget` | `CellRuntimeData cell, ConditionRuntimeData condition` | `bool` | Compares cell food or occupant trait against condition target |

---

## LevelConditionEvaluator

Path:
`Assets/Game/Core/Conditions/LevelConditionEvaluator.cs`

Responsibility:

Core referee evaluating all person conditions across both grids. Sets person emotional states (Normal/Angry/Happy) and determines win condition.

Dependencies:

- `ConditionChecker`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_conditionChecker` | `ConditionChecker` | Evaluator for individual conditions |
| `_adjacentOffsets` | `List<Vector2Int>` | Directional offsets for neighbor lookup |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `UpdateAllPersonStates` | `Grid<CellRuntimeData> mainGrid, Grid<CellRuntimeData> waitGrid` | `void` | Evaluates conditions for all persons in both grids |
| `AreAllPersonConditionsSatisfied` | `Grid<CellRuntimeData> mainGrid, Grid<CellRuntimeData> waitGrid` | `bool` | Returns true if every person is Happy |
| `GetAdjacentCells` | `Vector2Int index, Grid<CellRuntimeData> grid` | `List<CellRuntimeData>` | Returns neighbor cells |
| `IsConditionSatisfied` | `CellRuntimeData containCell, ConditionRuntimeData condition, Grid<CellRuntimeData> mainGrid` | `bool` | Checks single condition on mainGrid |
| `CheckPersonCondition` | `CellRuntimeData containCell, PersonRuntimeData person, GridId cellGrid, Grid<CellRuntimeData> mainGrid` | `void` | Sets person to Normal (WaitGrid) or Happy/Angry (MainGrid) |

---

## LevelRuntimeData

Path:
`Assets/Game/Core/Levels/LevelRuntimeData.cs`

Responsibility:

Runtime model for an active level: remaining moves and two grids (MainGrid, WaitGrid).

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_levelMove` | `int` | Current remaining moves |
| `MainGrid` | `Grid<CellRuntimeData>` (get; private set) | Main seating board |
| `WaitGrid` | `Grid<CellRuntimeData>` (get; private set) | Waiting line buffer |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `ModifyMove` | `int amount` | `void` | Adjusts move count and fires `OnMoveChanged` |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnMoveChanged` | `Action<int>` | Fired with new move count |

---

## Booster (abstract)

Path:
`Assets/Game/Core/Booster/Booster.cs`

Responsibility:

Template Method base class. `TryUse()` calls `CanUse()` → `Execute()` → fires `OnBoosterUsed`.

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `TryUse` | — | `bool` | Template method: validates then executes |
| `CanUse` | — | `bool` | Abstract precondition check |
| `Execute` | — | `void` | Abstract execution payload |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnBoosterUsed` | `Action` | Fired after successful execution |

---

## MoreMoveBooster

Path:
`Assets/Game/Core/Booster/MoreMoveBooster.cs`

Responsibility:

Adds configured number of moves to the current level.

Inherits / Implements:

- `Booster`

Dependencies:

- `LevelRuntimeData`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_levelData` | `LevelRuntimeData` (readonly) | Target level |
| `_amount` | `int` (readonly) | Moves to add |

---

## UndoBooster

Path:
`Assets/Game/Core/Booster/UndoBooster.cs`

Responsibility:

Pops the last move from `MoveHistory`, restores cell occupants to previous positions, and refunds 1 move.

Inherits / Implements:

- `Booster`

Dependencies:

- `MoveHistory`, `LevelRuntimeData`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_moveHistory` | `MoveHistory` (readonly) | Source of undo records |
| `_levelData` | `LevelRuntimeData` (readonly) | Level to refund move to |
| `LastUndoneRecord` | `MoveRecord?` (get; private set) | Last undone record, read by View for animation |

---

## RemoveBooster

Path:
`Assets/Game/Core/Booster/RemoveBooster.cs`

Responsibility:

Finds a target person and replaces all their conditions with the injected `CanSitAnywhere` condition. Priority 1: random eligible Angry person on MainGrid. Priority 2: random eligible person on WaitGrid with conditions. Persons already carrying `CanSitAnywhere` are excluded.

Inherits / Implements:

- `Booster`

Dependencies:

- `LevelRuntimeData`
- `ConditionRuntimeData` for the `CanSitAnywhere` replacement

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_levelData` | `LevelRuntimeData` (readonly) | Level state |
| `_canSitAnywhereCondition` | `ConditionRuntimeData` (readonly) | Replacement condition and validity contract |
| `TargetPerson` | `PersonRuntimeData` (get) | Person selected for condition replacement |

---

## MoveRecord

Path:
`Assets/Game/Core/Booster/MoveRecord.cs`

Responsibility:

Immutable snapshot of a single move: source cell, target cell, moved person, displaced person.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `SourceCell` | `CellRuntimeData` (readonly) | Starting cell |
| `TargetCell` | `CellRuntimeData` (readonly) | Destination cell |
| `MovedPerson` | `PersonRuntimeData` (readonly) | Person that was dragged |
| `DisplacedPerson` | `PersonRuntimeData` (readonly) | Person originally at target (null if empty) |

---

## MoveHistory

Path:
`Assets/Game/Core/Booster/MoveHistory.cs`

Responsibility:

Stack-based LIFO storage of `MoveRecord` entries for undo support.

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Record` | `MoveRecord record` | `void` | Pushes record onto stack |
| `TryPop` | `out MoveRecord record` | `bool` | Pops top record; false if empty |
| `Clear` | — | `void` | Empties history |

---

## Inventory

Path:
`Assets/Game/Core/Economy/Inventory.cs`

Responsibility:

Manages player currency and booster balances (Gold, Gem, Remove, Undo, MoreMoves). Clamps values ≥ 0.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_currentGold` | `int` | Gold balance |
| `_currentGem` | `int` | Gem balance |
| `_currentRemove` | `int` | Remove booster count |
| `_currentMoreMoves` | `int` | MoreMoves booster count |
| `_currentUndo` | `int` | Undo booster count |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `GetAmount` | `ItemType type` | `int` | Returns balance for item type |
| `HasEnough` | `ItemType type, int amount` | `bool` | Checks sufficient balance |
| `UpdateInventory` | `Reward reward` | `void` | Adds reward amount and fires `OnInventoryUpdate` |
| `TrySpendItem` | `ItemType type, int amount = 1` | `bool` | Deducts if sufficient; fires `OnInventoryUpdate` |
| `SetAmount` | `ItemType type, int amount` | `void` | Overwrites balance (clamped ≥ 0) |
| `AddAmount` | `ItemType type, int delta` | `void` | Adjusts balance by delta |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnInventoryUpdate` | `Action` | Fired when any balance changes |

---

## EconomyManager

Path:
`Assets/Game/Core/Economy/EconomyManager.cs`

Responsibility:

Manages login streak progression, daily/weekly reward claims, gold shop purchase limits, and purchase transactions.

Dependencies:

- `Inventory`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_inventory` | `Inventory` (readonly) | Inventory instance |
| `_goldShopLimits` | `int[]` (readonly) | Daily purchase caps per slot |
| `_goldShopPurchaseCounts` | `int[]` | Purchases made today per slot |
| `CurrentLoginDay` | `int` (get; private set) | 0-indexed day in 7-day streak |
| `IsDailyRewardClaimed` | `bool` (get; private set) | Daily claim flag |
| `IsWeeklyRewardClaimed` | `bool` (get; private set) | Weekly claim flag |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `EvaluateLoginState` | `DateTime lastLoginUtc, DateTime nowUtc` | `bool` | Rolls forward day: resets claims, shop, advances streak |
| `ClaimDailyReward` | `Reward dailyReward` | `bool` | Claims daily reward into inventory |
| `ClaimWeeklyReward` | `Reward weeklyReward` | `bool` | Claims weekly reward into inventory |
| `TryPurchase` | `Reward cost, Reward item, int goldShopSlotIndex = -1` | `bool` | Validates cap, spends cost, grants item |
| `SimulateNextDay` | — | `void` | Advances day, resets claims and shop |
| `ResetLoginStreak` | — | `void` | Resets to day 0 |

---

## LevelManager

Path:
`Assets/Game/App/LevelManager.cs`

Responsibility:

Domain rules for one level session. Validates moves, records history (skipping WaitGrid↔WaitGrid), evaluates conditions, and raises C# outcome/move notifications to the owning `GameManager`.

Dependencies:

- `LevelRuntimeData`, `LevelConditionEvaluator`, `MoveHistory`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_currentLevel` | `LevelRuntimeData` (readonly) | Active level state |
| `_conditionEvaluator` | `LevelConditionEvaluator` (readonly) | Condition validation engine |
| `_moveHistory` | `MoveHistory` (readonly) | Move recorder for undo |
| `OutcomeRaised` | `Action<LevelManager, LevelOutcome>` | Reports a domain win/lose result to the owning application session |
| `MoveSucceeded` | `Action` | Reports a committed move for application tutorial triggers |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `TryMovePerson` | `CellRuntimeData sourceCell, CellRuntimeData targetCell, PersonRuntimeData person` | `bool` | Validates, executes move, records history, decrements move, checks win/lose |
| `CheckAllPersonConditions` | — | `void` | Updates all states; reports win or lose through `OutcomeRaised` |
| `CheckPersonCondition` | `CellRuntimeData containCell, PersonRuntimeData person, GridId cellGrid` | `void` | Evaluates single person |
| `IsConditionSatisfied` | `CellRuntimeData cell, ConditionRuntimeData condition` | `bool` | Checks single condition |
| `GetAdjacentCells` | `Vector2Int index, Grid<CellRuntimeData> grid` | `List<CellRuntimeData>` | Returns adjacent cells |

---

## SaveLoadManager

Path:
`Assets/Game/App/SaveLoadManager.cs`

Responsibility:

Serializes/deserializes `GameData` to/from `data.json` at `Application.persistentDataPath` using `JsonUtility`.

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `SaveGameData` | `GameData data` | `void` | Writes formatted JSON to disk |
| `GetGameData` | — | `GameData` | Reads JSON from disk; returns default (level=1) if missing |

---

## GameData

Path:
`Assets/Game/App/GameData.cs`

Responsibility:

Serializable DTO for player progression, currencies, boosters, login state, shop purchases, and audio settings.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `currentLevel` | `int` | Level progression index |
| `currentGold` | `int` | Gold currency |
| `currentGem` | `int` | Gem currency |
| `currentRemove` | `int` | Remove booster count |
| `currentMoreMoves` | `int` | MoreMoves booster count |
| `currentUndo` | `int` | Undo booster count |
| `currentLoginDay` | `int` | Login streak day |
| `isDailyRewardClaimed` | `bool` | Daily claim flag |
| `isWeeklyRewardClaimed` | `bool` | Weekly claim flag |
| `lastLoginDateUtc` | `string` | UTC timestamp of last login |
| `goldShopPurchaseCountToday` | `int[]` | Daily shop purchase counts |
| `currentSoundVolume` | `int` | SFX volume |
| `isSoundMuted` | `bool` | SFX mute toggle |
| `currentMusicVolume` | `int` | Music volume |
| `isMusicMuted` | `bool` | Music mute toggle |
| `isTutorialCompleted` | `bool` | Legacy completion flag for the Drag mechanic tutorial |
| `completedTutorialIds` | `List<string>` | Persisted per-mechanic tutorial completions |

---

## GameManager

Path:
`Assets/Game/App/GameManager.cs`

Responsibility:

Plain C# application orchestrator that owns game commands, active level/session state, operation ordering, accepted outcomes, progression, reward/shop/booster decisions, daily refresh, settings, and tutorial trigger policy. It has no `Game.Data`, `Game.View`, or `Game.Bootstrap` dependency; Unity work crosses App-owned interfaces.

Dependencies:

- `IGameDataStore`, `GameData`, `Inventory`, `EconomyManager`
- `ILevelLoader`, `IGamePresentation`, `LevelManager`, `TutorialService`
- `Game.Debug.Logger` for application diagnostics
- `MoreMoveBooster`, `UndoBooster`, `RemoveBooster`
- Core `Reward`, `ItemType`, `ConditionRuntimeData`, and `PersonRuntimeData`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_saveLoad` | `IGameDataStore` | Persistence boundary |
| `_inventory` | `Inventory` | Runtime inventory |
| `_economyManager` | `EconomyManager` | Login/shop manager |
| `_gameData` | `GameData` | Runtime save data copy |
| `_levelLoader` | `ILevelLoader` | Prepares and activates authored levels |
| `_presentation` | `IGamePresentation` | Executes Unity transitions, UI, audio, VFX, and feedback |
| `_activeLevelManager` | `LevelManager` | Domain manager for the current session |
| `_sessionState` | `SessionState` | Home/transition/playing/win/lose application state |

### Properties

| Name | Type | Purpose |
|---|---|---|
| `CurrentLevel` | `int` | Saved progression level |
| `ActiveLevelNumber` | `int` | Level loaded into the active session, distinct from progression |
| `ActiveLevelManager` | `LevelManager` | Domain manager for the active board |
| `TotalLevels` | `int` | Catalog size supplied by `ILevelLoader` |
| `SoundVolume` / `MusicVolume` | `int` | Persisted 0–100 volume percentages |
| `IsSoundMuted` / `IsMusicMuted` | `bool` | Persisted mute flags |

### Events

| Name | Type | Purpose |
|---|---|---|
| `TutorialTriggerFired` | `Action<TutorialTrigger>` | Raised for each tutorial trigger request; the first-time request occurs when Level 1 is ready |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `PlayLevelAsync` / `NextLevelAsync` | `CancellationToken` | `Task` | Prepares, transitions to, activates, and commits a level session |
| `RestartLevelAsync` | `CancellationToken` | `Task` | Reloads the active level without advancing progression |
| `BackToHomeAsync` | `CancellationToken` | `Task` | Cancels active tutorial/load work and transitions to Home |
| `OnApplicationReady` | — | `void` | Marks application startup and defers the first-time tutorial until Level 1 is ready |
| `Tick` | `DateTime nowUtc` | `void` | Advances an active tutorial and performs once-per-day refresh |
| `TryUseMoreMoves` / `TryUseUndo` / `TryUseRemove` | — | `bool` | Validates session/inventory, executes booster, persists it, then requests success feedback |
| `TryPurchaseShopRequest` | `ShopPurchaseRequest request` | `bool` | Resolves configured offer and purchases it through `EconomyManager` |
| `TryClaimDailyReward` / `TryClaimWeeklyReward` | — | `bool` | Claims configured reward and persists only on success |
| `SetSoundSettings` / `SetMusicSettings` | `int volume, bool muted` | `void` | Clamps, persists, and applies settings through presentation boundary |
| `CancelPendingOperations` | — | `void` | Cancels in-flight operations and tutorials during teardown |
| `SaveGame` | — | `void` | Copies runtime economy/progression/tutorial state into `GameData` and persists |

---

## TutorialService

Path:
`Assets/Game/App/Tutorial/TutorialService.cs`

Responsibility:

Selects a configured `MechanicTutorial` for a `TutorialTrigger`, raises `TriggerFired` before tutorial matching, ticks the active tutorial, cancels it when a session is replaced, restores completed mechanic IDs from save data, and reports completed IDs to `GameManager` for persistence. `GameManager` forwards `TriggerFired` through its public `TutorialTriggerFired` event. `TutorialTrigger` and `MechanicTutorialID` are declared in this file.

Related types:

- `MechanicTutorial` (`Assets/Game/App/Tutorial/MechanicTutorial.cs`) owns an ordered set of steps and their begin/update/end lifecycle.
- `TutorialStep` (`Assets/Game/App/Tutorial/TutorialStep.cs`) is the abstract lifecycle for one authored tutorial action.
- Trigger points are FirstTimePlaying, LevelReady, SuccessfulMove, and successful MoreMoves/Undo/Remove booster use. A trigger request is observable even when no configured tutorial matches; in that case `TryStart` returns `false` and no tutorial step begins. If `MainScene` has no authored first-time tutorial, `GameBootstrapper` supplies one runtime tutorial using a single sequence step.

## FirstTimeTutorialStep

Path:
`Assets/Game/View/Tutorial/FirstTimeTutorialStep.cs`

Responsibility:

Runs the Level 1 first-time sequence in one reusable step component: disables boosters without a separate intro screen, asks the player to inspect the person tooltip, locks input for 2.5 seconds, guides food selection to hamburger, then lets the player either drag the person or tap the person and an eligible seat. It completes when the person becomes happy. Instruction text uses the VAG Rounded font and Figma's 600x1213 artboard positions; the animated pointer uses the `hand_0` sprite from `Assets/Art/UI/Common/hand.png`. Missing tutorial targets are logged and skipped. The overlay is created under the existing in-game Canvas at runtime, with its font and sprite assigned on `MainScene`.

---

## GameBootstrapper

Path:
`Assets/Game/Bootstrap/GameBootstrapper.cs`

Responsibility:

Scene MonoBehaviour composition root. Owns serialized authoring, scene, VFX, and event-channel references; constructs and wires `GameManager`, the Unity level/presentation adapters, and `TutorialService`; binds request/result endpoints; and forwards Unity lifecycle notifications. It contains no game-flow use-case decisions.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `GameManager`, `LevelBootstrapper`, `UnityGamePresentation`, `EventListener`
- `EconomyConfigSO`, `ConditionDataSO`, `LevelDataSO`, `GameConfig`
- `UIManager`, `GridManager`, `LevelView`, `WeeklyLogin`, `ShopPanelView`, `VfxPlayer`
- `TMP_FontAsset` for tutorial text and `Sprite` for its hand pointer
- `AudioPlayer`, `AudioSettingsView`
- Game-flow, reward, shop, booster, and economy-feedback channels
- `AudioCueEventChannelSO`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_levelData` | `List<LevelDataSO>` | Ordered level authoring data passed to `LevelBootstrapper` |
| `_economyConfig` | `EconomyConfigSO` | Authoring source for rewards, shop prices, and Gold shop limits |
| `_canSitAnywhereCondition` | `ConditionDataSO` | Authoring condition converted for Remove Booster execution |
| `_tutorialFont` | `TMP_FontAsset` | VAG Rounded font assigned to the default first-time tutorial |
| `_tutorialHandSprite` | `Sprite` | Hand pointer sprite assigned to the default first-time tutorial |
| `_uiManager` | `UIManager` | Main UI facade reference |
| `_gridManager` | `GridManager` | Board view and active `LevelManager` reference |
| `_levelView` | `LevelView` | In-game move and booster HUD reference |
| `_weeklyLogin` | `WeeklyLogin` | Daily/weekly reward UI reference |
| `_shopPanel` | `ShopPanelView` | Shop price, affordability, purchase, and limit UI reference |
| `_vfxPlayer` | `VfxPlayer` | Presentation VFX player reference |
| `_audioPlayer` | `AudioPlayer` | Scene-owned presentation audio service |
| `_audioSettingsViews` | `AudioSettingsView[]` | Existing Main and InGame settings panels bound to persisted audio callbacks |
| Request channels | `VoidEventChannelSO` | Bound directly to manager commands for play, navigation, claims, purchases, and boosters |
| Result channels | `VoidEventChannelSO` / `IntEventChannelSO` | Publish accepted win/lose and progression-level notifications from manager events |
| `_listener` | `EventListener` | Tracks channel subscriptions for lifecycle-safe unbinding |
| `_gameManager` | `GameManager` | Application orchestrator created at runtime |
| `_mechanicTutorials` | `MechanicTutorial[]` | Tutorial definitions passed to `TutorialService` |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Awake` | — | `void` | Converts authoring input, constructs application services/adapters, and initializes presentation |
| `OnEnable` / `OnDisable` | — | `void` | Binds/unbinds command and accepted-result endpoints; cancels work when disabled |
| `Start` | — | `void` | Forwards initial level-number publication |
| `Update` / `OnApplicationFocus` | — | `void` | Forwards current time to `GameManager.Tick` |
| `OnApplicationQuit` | — | `void` | Cancels pending operations and forwards save |

---

## ShopPurchaseRequest

Path:
`Assets/Game/App/ShopPurchaseRequest.cs`

Responsibility:

Immutable App-owned command data identifying whether a shop slot uses Gold, its zero-based EconomyConfig slot index, and the booster item represented by that slot. View code can reference this contract without an App-to-View dependency.

### Fields / Properties

| Name | Type | Purpose |
|---|---|---|
| `UsesGold` | `bool` (get) | Identifies whether the request came from the Gold shop row and therefore uses a daily limit |
| `SlotIndex` | `int` (get) | Maps the clicked slot to the corresponding price and limit array entry |
| `ItemType` | `ItemType` (get) | Identifies the reward item to purchase |

---

## ShopPanelView

Path:
`Assets/Game/View/UI/ShopPanelView.cs`

Responsibility:

Presentation coordinator for the ShopPanel. Receives copied price and limit arrays through `UnityGamePresentation`, binds the six slot views, refreshes prices/limits/affordability, and forwards purchase requests through an application callback.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `ShopItemView`, `EconomyManager`, `Reward`, `ItemType`
- `Inventory.OnInventoryUpdate`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `goldShopItems` | `ShopItemView[]` | Gold-row slots; discovered from `GoldShopItem 1..3` when not assigned in the Inspector |
| `gemShopItems` | `ShopItemView[]` | Gem-row slots; discovered from `GemShopItem 1..3` when not assigned in the Inspector |
| `_goldShopPrices` | `Reward[]` | Snapshot of `EconomyConfigSO.goldShopPrice` |
| `_goldShopLimits` | `int[]` | Snapshot of `EconomyConfigSO.goldShopLimit` |
| `_gemShopPrices` | `Reward[]` | Snapshot of `EconomyConfigSO.gemShopPrice` |
| `_economyManager` | `EconomyManager` | Provides inventory affordability and Gold purchase counts |
| `_onPurchaseRequested` | `Action<ShopPurchaseRequest>` | Callback to `GameManager` for the actual transaction |
| `_isSubscribed` | `bool` | Prevents duplicate Inventory event subscriptions across enable/disable cycles |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Bind` | `IReadOnlyList<Reward> goldShopPrices, IReadOnlyList<int> goldShopLimits, IReadOnlyList<Reward> gemShopPrices, EconomyManager economyManager, Action<ShopPurchaseRequest> onPurchaseRequested` | `void` | Supplies configured offers and application transaction callback, then refreshes all slots |
| `Refresh` | — | `void` | Reprojects current prices, Gold purchase counts, limits, and affordability to the slot views |
| `Awake` | — | `void` | Ensures slot components exist and discovers slots from the existing ShopPanel hierarchy |
| `OnEnable` / `OnDisable` | — | `void` | Subscribes/unsubscribes from inventory updates and refreshes presentation |

### Relations

- Maps slot indices `0..2` to `MoreMoves`, `Remove`, and `Undo`.
- Uses `goldShopLimit[i]` and `EconomyManager.GetGoldShopPurchaseCount(i)` for Gold slots.
- Uses `gemShopPrice[i]` without a purchase limit for Gem slots.
- Calls `ShopItemView.Bind()` and receives purchase callbacks from each item.

---

## ShopItemView

Path:
`Assets/Game/View/UI/ShopItemView.cs`

Responsibility:

Displays one shop offer's configured price and optional Gold limit, controls button interactability from inventory and limit state, and forwards valid clicks as a `ShopPurchaseRequest`.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `TextMeshProUGUI` price/limit labels
- `UnityEngine.UI.Button`
- `EconomyManager`, `ShopPurchaseRequest`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `priceText` | `TextMeshProUGUI` | Displays the current configured price amount |
| `limitText` | `TextMeshProUGUI` | Displays `Limit: purchased/limit` for Gold offers when present |
| `purchaseButton` | `UnityEngine.UI.Button` | Starts a purchase request and is disabled when the offer cannot be bought |
| `_request` | `ShopPurchaseRequest` | Identifies the bound currency row, slot, and item |
| `_price` | `Reward` | Current configured cost |
| `_economyManager` | `EconomyManager` | Checks whether the player can afford the cost |
| `_purchaseCount` / `_limit` | `int` | Current Gold purchase count and configured cap |
| `_hasOffer` | `bool` | Distinguishes a configured slot from an unavailable slot |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Bind` | `ShopPurchaseRequest request, Reward price, EconomyManager economyManager, int purchaseCount, int limit, Action<ShopPurchaseRequest> onPurchaseRequested` | `void` | Binds runtime offer state, updates labels, and evaluates button interactability |
| `SetUnavailable` | — | `void` | Clears/hides labels and disables a slot when EconomyConfig lacks its entry |
| `Refresh` | — | `void` | Updates price, limit text, and affordability/limit gating |
| `OnPurchaseButtonClicked` | — | `void` | Entry point wired to each scene Button `OnClick`; forwards the validated purchase request |
| `OnEnable` / `OnDisable` | — | `void` | Balances the Button click listener subscription |

### Relations

- Resolves existing child objects named `Price`, `LimitText`, and `Button` when serialized references are not assigned.
- Each scene purchase Button has a persistent `OnClick` listener targeting its parent `ShopItemView.OnPurchaseButtonClicked`; runtime listener fallback remains available when no persistent listener exists.
- Invokes the panel callback only when the current offer remains affordable and within its Gold limit.

---

## LevelBootstrapper

Path:
`Assets/Game/Bootstrap/LevelBootstrapper.cs`

Responsibility:

Unity adapter implementing `ILevelLoader`. It validates and converts a requested `LevelDataSO` into `LevelRuntimeData` before modifying the current board, then activates the prepared board with the `LevelManager` supplied by `GameManager`.

Dependencies:

- `ILevelLoader`, `LevelDataSO`, `GridManager`, `LevelView`

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `TryPrepare` | `int levelNumber, out IPreparedLevel, out string error` | `bool` | Wraps catalog index and validates/converts level data without clearing the active board |
| `Activate` | `IPreparedLevel prepared, LevelManager levelManager` | `void` | Clears the prior view, spawns environment, binds the supplied domain manager, builds grids, and binds HUD |

---

## LevelLayoutGizmosDrawer

Path:
`Assets/Game/Bootstrap/LevelLayoutGizmosDrawer.cs`

Responsibility:

Normal `MonoBehaviour` that draws the selected `LevelDataSO` MainGrid and WaitGrid as Scene-view wireframe Gizmos. Designers drag level SOs into `levels` and choose the zero-based `levelIndex`; MainGrid is cyan and WaitGrid is yellow.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `levels` | `List<LevelDataSO>` | Level assets available for Gizmos preview |
| `levelIndex` | `int` | Zero-based index of the level to draw |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `OnDrawGizmos` | — | `void` | Draws MainGrid and WaitGrid cell rectangles using the selected level's layout data |

---

## GridManager

Path:
`Assets/Game/View/Board/GridManager.cs`

Responsibility:

Instantiates `CellView` prefabs from non-null grid cells, maintains `_cellViewMap` for domain↔view lookup, delegates move validation to the `LevelManager` supplied by `GameManager`, supports undo view reversion, and gates gameplay input during transitions. `WorldRoot` exposes the configured `gridRoot` or falls back to the component transform for level environment objects.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `LevelManager`, `PersonMover`
- `VfxPlayer`
- `AudioPlayer`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_cellViewMap` | `Dictionary<CellRuntimeData, CellView>` | Maps domain cell to view |
| `_levelManager` | `LevelManager` | Domain coordinator |
| `personMoveManager` | `PersonMover` | Movement handler |
| `cellPrefabs` | `GameObject` | Cell view prefab |
| `gridRoot` | `Transform` | Parent for generated cell and level environment objects |
| `adjacent` | `List<Vector2Int>` | Cardinal direction offsets |
| `_vfxPlayer` | `VfxPlayer` | Player injected into spawned PersonViews and stopped when the level view is cleared |
| `_audioPlayer` | `AudioPlayer` | Player injected into spawned PersonViews so state-transition audio stays in the View layer |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Initialize` | `LevelRuntimeData level, LevelManager levelManager` | `void` | Binds the runtime data and application-owned domain manager |
| `ClearGrids` | — | `void` | Stops active VFX and destroys all cell views |
| `CreateMainGrid` | — | `void` | Instantiates main grid cells |
| `CreateWaitGrid` | — | `void` | Instantiates wait grid cells |
| `TryMovePerson` | `CellView sourceCell, CellView targetCell, PersonRuntimeData person` | `bool` | Delegates to LevelManager |
| `RevertMoveView` | `MoveRecord record` | `void` | Finds cells and instructs PersonMover to revert |
| `FindCellView` | `CellRuntimeData cellData` | `CellView` | Lookup from _cellViewMap |
| `FindPersonView` | `PersonRuntimeData person` | `PersonView` | Finds the current presentation object for a person across both grids |

---

## VfxId

Path:
`Assets/Game/View/VFX/VfxId.cs`

Responsibility:

Presentation identifier for catalogued effects: `RemoveBooster`, `Happy`, and `Win`.

---

## VfxCatalogSO

Path:
`Assets/Game/View/VFX/VfxCatalogSO.cs`

Responsibility:

ScriptableObject registry mapping `VfxId` values to particle prefabs, prewarm counts, optional fade timing, and runtime scale multipliers. The configured asset is `Assets/Art/VFX/VFXCatalog.asset`.

Inherits / Implements:

- `ScriptableObject`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `TryGet` | `VfxId id, out Entry entry` | `bool` | Resolves a VFX configuration by identifier |

---

## VfxInstance

Path:
`Assets/Game/View/VFX/VfxInstance.cs`

Responsibility:

Runtime component attached to spawned particle prefabs. Plays all child particle systems, applies per-renderer opacity through `MaterialPropertyBlock`, supports delayed fade, observes particle lifetime, and reports completion for pooling.

Inherits / Implements:

- `MonoBehaviour`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Play` | `float fadeDelay, float fadeDuration, Action<VfxInstance> onCompleted` | `void` | Resets and starts the particle systems with optional delayed fade |
| `StopImmediately` | — | `void` | Cancels lifecycle work and clears particle state |
| `ResetForPool` | — | `void` | Restores an instance to a reusable state |

---

## VfxPlayer

Path:
`Assets/Game/View/VFX/VfxPlayer.cs`

Responsibility:

Scene-level presentation service that resolves catalog entries, prewarms and reuses `VfxInstance` objects, places effects in world space or at a uGUI `RectTransform`, and stops active effects during level/UI teardown.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `VfxCatalogSO`, `VfxInstance`, `Coffee.UIExtensions.UIParticle`, Unity `ParticleSystem`, `Canvas`, `CanvasScaler`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `catalog` | `VfxCatalogSO` | Prefab and timing registry |
| `worldRoot` | `Transform` | Parent for pooled/world VFX instances; defaults to the player transform |
| `uiVfxRoot` | `RectTransform` | Explicit per-Canvas root that receives UI particle wrappers |
| `uiParticleMaterial` | `Material` | UI-compatible material template for particles rendered through `CanvasRenderer` |
| `uiSortingOrder` | `int` | Sorting order for the runtime UI VFX fallback canvas |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `PlayAtWorld` | `VfxId id, Transform anchor = null, bool followAnchor = true` | `void` | Plays an effect at a world anchor, optionally following it |
| `PlayAtUI` | `VfxId id, RectTransform target` | `void` | Converts a uGUI target position and plays a pooled `UIParticle` wrapper under the configured VFX root |
| `Stop` | `VfxId id` | `void` | Stops and pools active instances of one type |
| `StopAll` | — | `void` | Stops and pools all active instances |

---

## CurrencyScatterAnimation

Path:
`Assets/Game/View/Effect/CurrencyScatterAnimation.cs`

Responsibility:

Presentation component that prewarms and reuses currency icon UI objects, scatters them from a screen or transform position, holds them for a configurable delay, then shrinks and releases them back to the pool.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `CurrencyFlyAnimation` for the shared top-level currency overlay container
- `PrimeTween`, `UniTask`, `UnityEngine.UI`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `PlayFromScreenCenter` | — | `void` | Starts the scatter animation from the screen center |
| `PlayFromTransform` | `Transform source` | `void` | Starts the scatter animation from a world or UI transform; falls back to the screen center when the source is missing |
| `Play` | `Vector3 startPos, bool isScreenPos = false` | `void` | Starts the animation from an explicit world or screen position |
| `PlayAsync` | `Vector3 startPos, bool isScreenPos = false, Transform sourceTransform = null` | `UniTask` | Runs the scatter, delay, shrink, and release sequence |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnCoinDisappeared` | `Action` | Signals that one coin has completed its shrink animation |
| `OnAllCoinsDisappeared` | `Action` | Signals that the current scatter animation has completed |

## AudioCatalogSO

Path:
`Assets/Game/View/Audio/AudioCatalogSO.cs`

Responsibility:

ScriptableObject catalog for all cue and music configuration. Cue entries contain variation clips, UI/SFX bus, volume, pitch range, cooldown, and overlap policy; music entries contain `MusicId`, clip, volume, and fade duration. The configured asset is `Assets/Data/Audio/AudioCatalog.asset`.

Dependencies:

- `AudioCueId` from `Game.Events`
- Unity `AudioClip`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `TryGetCue` | `AudioCueId cue, out CueEntry entry` | `bool` | Resolves one cue configuration |
| `TryGetMusic` | `MusicId musicId, out MusicEntry entry` | `bool` | Resolves one music configuration |
| `EnsureRequiredEntries` | — | `void` | Ensures the catalog has one editable entry per supported cue/music identifier |

## AudioPlayer

Path:
`Assets/Game/View/Audio/AudioPlayer.cs`

Responsibility:

Scene-owned runtime player. Uses one UI source, a four-source SFX pool, and two music sources; selects variations, applies cooldown/overlap rules, routes sources to Mixer groups, cross-fades Menu/In-game music, and applies exposed volume parameters.

Dependencies:

- `AudioCatalogSO`
- `AudioMixer`, `AudioMixerGroup`, `AudioSource`

### Public API

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Play` | `AudioCueId cue` | `void` | Plays a catalogued UI/SFX cue after lookup and anti-spam checks |
| `PlayMusic` | `MusicId music` | `void` | Starts or cross-fades the selected music entry |
| `StopMusic` | — | `void` | Stops both music sources and clears active music |
| `ApplySettings` | `int soundVolume, bool soundMuted, int musicVolume, bool musicMuted` | `void` | Sets `SoundVolume` and `MusicVolume` Mixer parameters |

## AudioEventBinder

Path:
`Assets/Game/View/Audio/AudioEventBinder.cs`

Responsibility:

Lifecycle-safe `EventListener` bridge from `OnAudioCue` and accepted win/lose result channels to `AudioPlayer`. Game music changes are requested through `IGamePresentation` only after a successful GameManager flow operation; raw Play/Home requests do not change BGM.

## UIButtonSound

Path:
`Assets/Game/View/Audio/UIButtonSound.cs`

Responsibility:

Button-local direct-call component that plays `ButtonClick` from the existing Button callback without replacing `ButtonEventRaiser` or gameplay event listeners.

## AudioSettingsView

Path:
`Assets/Game/View/Audio/AudioSettingsView.cs`

Responsibility:

Presentation adapter for the existing Sound/Music sliders and mute buttons. It emits primitive volume/mute callbacks supplied by `GameBootstrapper`; it does not own `GameData` or Unity audio objects. Main Menu and In-Game `MainScene` controls are persistently wired to its public `SetSoundVolume`, `SetMusicVolume`, `ToggleSoundMute`, and `ToggleMusicMute` methods; runtime listeners are used only as a fallback for controls without a persistent UnityEvent.

---

## CellView

Path:
`Assets/Game/View/Board/CellView.cs`

Responsibility:

Visual representation of a board cell. Applies the configured cell sprite, initializes `FoodTooltips` for food cells, and spawns the configured default person for occupied cells.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `PersonSpawner`, `PersonView`, `PersonMover`, `FoodTooltips`

---

## PersonView

Path:
`Assets/Game/View/People/PersonView.cs`

Responsibility:

Visual representation of a character. Swaps facial expression sprite based on `PersonState` by subscribing to `PersonRuntimeData.OnPersonStateChanged`; plays the catalogued Happy VFX when the state becomes Happy.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `PersonRuntimeData`, `PersonTooltip`
- `VfxPlayer`
- `AudioPlayer`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `BindVfxPlayer` | `VfxPlayer vfxPlayer` | `void` | Injects the presentation VFX player and handles an already-Happy state |
| `BindAudioPlayer` | `AudioPlayer audioPlayer` | `void` | Injects the presentation audio player without replaying the current state |
| `HandleStateChanged` | `PersonState state` | `void` | Updates the face and plays `PersonHappy` only for a domain state-change notification |

---

## PersonMover

Path:
`Assets/Game/View/People/PersonMover.cs`

Responsibility:

Handles character movement tweens during drag-and-drop. Validates moves via `GridManager.TryMovePerson`. Supports `RevertMove` for undo animation.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `GridManager`, `PrimeTween`

---

## PersonDragManager

Path:
`Assets/Game/View/Input/PersonDragManager.cs`

Responsibility:

Input handler implementing `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`. Translates pointer events into `PersonMover` calls.

Inherits / Implements:

- `MonoBehaviour`, `IDragHandler`, `IBeginDragHandler`, `IEndDragHandler`

Dependencies:

- `PersonMover`, `PersonRuntimeData`, `CellView`, `PersonTooltip`

---

## PersonTooltip

Path:
`Assets/Game/View/People/PersonTooltip.cs`

Responsibility:

Shows the character's condition bubble on tap, including checked/unchecked status for up to two conditions. Dynamically sizes the bubble and refreshes when conditions or their satisfaction states change.

Inherits / Implements:

- `MonoBehaviour`, `IPointerClickHandler`

Dependencies:

- `PersonRuntimeData`, `InputSystem`, `PrimeTween`, `UniTask`, `TMP`

---

## PersonSpawner

Path:
`Assets/Game/View/People/PersonSpawner.cs`

Responsibility:

Instantiates person prefab, binds `PersonRuntimeData`, injects the scene `AudioPlayer` into the spawned `PersonView`, initializes `PersonDragManager`, and links to `CellView`.

Inherits / Implements:

- `MonoBehaviour`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `BindAudioPlayer` | `AudioPlayer audioPlayer` | `void` | Stores the scene-owned player for subsequently spawned views |
| `SpawnPerson` | `PersonRuntimeData, CellView, PersonMover` | `PersonView` | Instantiates and initializes one character presentation |

---

## UIManager

Path:
`Assets/Game/View/UI/UIManager.cs`

Responsibility:

Presentation facade for canvas state, win/lose panels, settings panels, and transitions via `TransitionController`. `GameManager` owns game-flow timing and awaits its transition operations through `UnityGamePresentation`; `UIManager` does not subscribe to Play/Next/Restart/Home request channels.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `VoidEventChannelSO`, `IntEventChannelSO`, `TransitionController`, `InventoryView`, `UniTask`, `TMP`
- `VfxPlayer`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Initialize` | `Inventory inventory` | `void` | Binds InventoryView, refreshes level text |
| `UpdateLevelText` | `int level` | `void` | Updates level label |
| `CloseTransitionAsync` / `OpenTransitionAsync` | `CancellationToken` | `Task` | Executes the requested transition and reports completion to GameManager |
| `ShowGameScreen` | `int levelNumber` | `void` | Selects the in-game screen and updates displayed level |
| `ShowHomeScreen` | `int levelNumber` | `void` | Selects the home screen and updates displayed level |
| `ShowWin` | — | `void` | Shows win panel and plays Win VFX at the panel center |
| `ShowLose` | — | `void` | Shows lose panel |
| `ShowSetting` | — | `void` | Opens settings panel |
| `HideSetting` | — | `void` | Closes settings panel |

---

## LevelView

Path:
`Assets/Game/View/UI/LevelView.cs`

Responsibility:

In-game HUD showing remaining moves and three booster slots. Subscribes to `LevelRuntimeData.OnMoveChanged` and `Inventory.OnInventoryUpdate`.

Inherits / Implements:

- `MonoBehaviour`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `moveText` | `TextMeshProUGUI` | Moves counter display |
| `moreMoveSlot` | `BoosterSlotView` | More Moves booster UI |
| `undoSlot` | `BoosterSlotView` | Undo booster UI |
| `removeSlot` | `BoosterSlotView` | Remove booster UI |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `BindData` | `LevelRuntimeData source` | `void` | Subscribes to move changes |
| `BindBoosters` | `Inventory inventory` | `void` | Binds booster counts to inventory |

---

## BoosterSlotView

Path:
`Assets/Game/View/UI/BoosterSlotView.cs`

Responsibility:

Single booster button in gameplay HUD. Displays count from `Inventory`, disables when 0, raises `VoidEventChannelSO` on click.

Inherits / Implements:

- `MonoBehaviour`

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Bind` | `Func<int> getCount` | `void` | Binds count getter delegate |
| `UpdateCount` | — | `void` | Refreshes count text and button interactability |

---

## InventoryView

Path:
`Assets/Game/View/UI/InventoryView.cs`

Responsibility:

Displays gold and gem balances with smooth number-rolling animation via PrimeTween. Subscribes to `Inventory.OnInventoryUpdate`.

Inherits / Implements:

- `MonoBehaviour`

---

## TransitionController

Path:
`Assets/Game/View/UI/TransitionController.cs`

Responsibility:

Circle iris-wipe screen transition via custom shader material. Supports open, close, and full cycle transitions with configurable easing and focal points, and plays the View-owned `Transition` cue before the transition tween.

Inherits / Implements:

- `MonoBehaviour`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `OpenAsync` | `Transform target = null, ...` | `UniTask` | Opens circle centered on target |
| `CloseAsync` | `Transform target = null, ...` | `UniTask` | Closes circle centered on target |
| `DoTransitionAsync` | `Action onCovered, ...` | `UniTask` | Full cycle: close → delay → callback → open |

---

## PanelController

Path:
`Assets/Game/View/UI/PanelController.cs`

Responsibility:

Horizontal sliding carousel between Shop, Main, and Login panels. Responds to `VoidEventChannelSO` navigation events.

Inherits / Implements:

- `MonoBehaviour`

---

## LevelEndPanel

Path:
`Assets/Game/View/UI/LevelEndPanel.cs`

Responsibility:

Level end popup with sequential graphic reveals and reward→navigation phase transition via CanvasGroup fades.

Inherits / Implements:

- `MonoBehaviour`

---

## WeeklyLogin

Path:
`Assets/Game/View/UI/WeeklyLogin.cs`

Responsibility:

Weekly login attendance UI. Displays 7-day reward grid with claimed marks, daily reward section, claim buttons with sprite states, and day-specific currency fly feedback.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `Reward`, `ItemType`, `ButtonSpriteSwap`, `CurrencyFlyAnimation`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `claimGoldCurrencyFly` | `CurrencyFlyAnimation` | CurrencyFly effect for Weekly ClaimButton on Days 1–6, configured with GoldIcon and GoldView |
| `claimGemCurrencyFly` | `CurrencyFlyAnimation` | CurrencyFly effect for Weekly ClaimButton on Day 7, configured with GemIcon and GemView |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `UpdateAmountTexts` | — | `void` | Refreshes reward labels and claim states, and switches the Weekly ClaimButton between Gold and Gem CurrencyFly effects based on the current login day |

### Relations

- Uses the 0-based `_currentLoginDay` supplied by `GameBootstrapper`; index `6` represents Day 7.
- The SampleScene assigns `claimGoldCurrencyFly` to component fileID `668022246` and `claimGemCurrencyFly` to component fileID `1819088850`; `ButtonSpriteSwap` remains independent of day-specific CurrencyFly selection.

---

## Data Layer SOs

### LevelDataSO

Path: `Assets/Game/Data/Levels/LevelDataSO.cs`

Responsibility: ScriptableObject configuring level layout (move count, main grid, wait grid), serialized `LevelPersonConfig` occurrences, and `levelEnvironmentPrefabs`. `TryToRuntimeData()` validates the complete configuration and creates the runtime grids without mutating the authoring data before gameplay grids are cleared.

### PersonDefinitionSO

Path:
`Assets/Game/Data/People/PersonDefinitionSO.cs`

Responsibility: Shared character definition containing only `personName`, `trait`, and `baseSprite`. It never owns occurrence-specific conditions.

### LevelPersonConfig

Path:
`Assets/Game/Data/Levels/LevelPersonConfig.cs`

Responsibility: Serializable level-owned occurrence containing a definition, condition references, grid id, and position. Each conversion creates a fresh `PersonRuntimeData` and fresh condition runtime list.

### ConditionDataSO

Path: `Assets/Game/Data/Conditions/ConditionDataSO.cs`

Responsibility: ScriptableObject configuring a single condition rule. Provides `ToRuntimeData()` factory.

### CellDataSO

Path: `Assets/Game/Data/Board/CellDataSO.cs`

Responsibility: ScriptableObject configuring a board cell (type, food, sprite). Runtime conversion receives the initial person from `LevelDataSO`.

### EconomyConfigSO

Path: `Assets/Game/Data/Economy/EconomyConfigSO.cs`

Responsibility: ScriptableObject holding economy parameters: win rewards, daily/weekly rewards, shop prices and limits.

### GameConfig

Path: `Assets/Game/Data/People/GameConfig.cs`

Responsibility: Static class with gameplay constants: `MAX_CONDITION_PER_PERSON = 2`, `MORE_MOVE_AMOUNT = 3`.

---

## Editor Tools

### CheatToolWindow

Path: `Assets/Game/Editor/CheatToolWindow.cs`

Responsibility: Dual-mode (Play/Edit) `EditorWindow` for manipulating save data and application state. In Play Mode it issues level, win/lose, save, and economy commands through `GameBootstrapper.GameManager`.

### LevelDataSOEditor

Path: `Assets/Game/Editor/LevelDataSOEditor.cs`

Responsibility: Custom inspector providing a visual 2D matrix editor for `LevelDataSO` with resize, batch fill, and clear operations. It restricts MainGrid cell references to `Seat`/`Food`/`Block` and WaitGrid references to `Seat`; invalid existing references are shown as warnings instead of being silently deleted.

## Level-owned person data flow

```text
LevelDataSO
  -> Validate personConfigs (shared by Inspector and runtime)
  -> LevelPersonConfig.ToRuntimeData()
  -> fresh PersonRuntimeData per (gridId, position)
  -> CellDataSO.ToRuntimeData(..., initialPerson)
  -> CellRuntimeData.DefaultPerson == CurrentPerson
  -> LevelRuntimeData
```

`LevelBootstrapper.TryPrepare` performs this conversion before `GameManager` starts the transition or modifies the active board. An invalid level logs the level name and configuration location and leaves the current session and grids untouched. During activation it clears the previous view, instantiates every `LevelDataSO.levelEnvironmentPrefabs` entry as a child of `GridManager.WorldRoot` at local position `(0, 0, 0)`, binds the manager-created `LevelManager`, then creates cells and binds the HUD. The Inspector keeps invalid configurations visible after grid resize or cell-type changes so the author can correct or remove them explicitly.

# Code Flow

## App Start and lifecycle forwarding

`GameBootstrapper.Awake()`
→ converts serialized economy/condition/grid settings into `GameManagerConfig`
→ constructs `LevelBootstrapper` (`ILevelLoader`), `UnityGamePresentation` (`IGamePresentation`), `TutorialService`, and `GameManager`
→ initializes UI, inventory, shop, audio settings, and daily login state

`GameBootstrapper.OnEnable()`
→ binds request channels directly to `GameManager` command methods
→ forwards accepted win/lose and level-number events to existing result channels

`GameBootstrapper.Start()` publishes the current level and calls `GameManager.OnApplicationReady()`. For a fresh save, the manager requests `FirstTimePlaying`; `TutorialService.TriggerFired` and `GameManager.TutorialTriggerFired` are raised before tutorial matching. The manager then clears and saves `IsFirstTimePlaying`, even if no tutorial definition is configured. `Update`, focus, disable, and quit callbacks forward tick/cancellation/save lifecycle work. Daily date checks and application policy remain inside `GameManager`.

---

## Play, restart, next, and home

Play/Next/Restart/Home request channel
→ thin `GameBootstrapper` binding invokes the corresponding `GameManager` command
→ manager serializes level operations with its operation gate and owns the session state

For Play/Next/Restart:

1. `ILevelLoader.TryPrepare()` validates and converts the requested level while the current board remains intact.
2. On preparation failure, the manager reports the error and retains the current board/session without publishing LevelReady.
3. The manager cancels the active tutorial, disables gameplay input, and awaits the closing transition.
4. `GameManager` creates the session's `LevelManager` and passes it into `ILevelLoader.Activate()`.
5. `LevelBootstrapper` clears the old view, creates environment and cells, and binds `LevelView`; the presentation selects the game screen.
6. After the opening transition completes, the manager commits `ActiveLevelNumber` and Playing state, persists requested progression, enables input, updates music/level display, and evaluates LevelReady tutorial eligibility.

Restart targets `ActiveLevelNumber` and does not advance saved progression. A win advances saved progression once for that accepted active session; Next then loads the new progression level. Home and teardown cancel pending work and active tutorials. If activation fails after clearing the board, the manager recovers to Home because the previous board can no longer be restored automatically.

---

## Move, outcomes, and tutorials

`PersonDragManager` → `PersonMover` → `GridManager.TryMovePerson()` → active `LevelManager.TryMovePerson()`
→ validates and applies the move, records history/decrements moves, and evaluates conditions
→ updates person state/tooltip presentation
→ emits an outcome for a win/loss, or `MoveSucceeded` after the move check

`GameManager` accepts an outcome only from the current active manager while Playing and only once. It persists the win progression before emitting `WinAccepted`; accepted result channels then drive the existing win/lose UI and audio listeners. Stale managers and duplicate outcomes have no effect.

`TutorialService` raises `TriggerFired` for each requested application trigger, then matches configured mechanic tutorials (FirstTimePlaying, LevelReady, successful move, or successful booster use), runs the current step from `GameManager.Tick()`, and reports completion. `GameManager` forwards trigger requests through `TutorialTriggerFired` and persists completed mechanic IDs; restart, home, and level replacement cancel the active tutorial. With the current empty `_mechanicTutorials` list, `FirstTimePlaying` is still emitted once but no tutorial step starts.

---

## Booster, economy, and settings commands

Booster request channel
→ `GameManager` validates active session and inventory
→ core booster changes domain state
→ manager persists the result and requests successful audio/VFX/view feedback through `IGamePresentation`

Undo additionally asks the presentation adapter to revert the move view before re-evaluating conditions. Remove resolves its `PersonView` and world VFX inside `UnityGamePresentation`; application code sees only the domain `PersonRuntimeData`.

Shop/reward request channels
→ direct manager command binding
→ `GameManager` resolves configured prices/rewards, validates EconomyManager rules, mutates inventory, persists success, and requests item/audio/UI refresh feedback

`AudioSettingsView` callbacks
→ `GameManager.SetSoundSettings` / `SetMusicSettings`
→ saved `GameData` and `IGamePresentation.ApplyAudioSettings()`

---

## Audio and UI feedback

`GameManager` calls `IGamePresentation` after successful application flow operations. `UnityGamePresentation` adapts these calls to `UIManager`, `GridManager`, `AudioPlayer`, `VfxPlayer`, and existing item/audio event channels. Raw Play/Home requests do not independently transition the UI or change music.

Local ButtonClick, Transition, and PersonHappy feedback remains owned by the corresponding view components. Accepted win/lose result channels continue driving `UIManager` panels and `AudioEventBinder` outcome cues.
# Important Dependencies

| Package | Version | Purpose |
|---|---|---|
| PrimeTween | 1.4.11 | All UI and gameplay tweening (scale, position, alpha, custom) |
| UniTask | Git (latest) | Async/await for delays, sequenced animations, fire-and-forget |
| Unity Input System | 1.20.0 | Pointer press detection for tooltips (FoodTooltips, PersonTooltip) |
| URP | 17.3.0 | Render pipeline |
| TextMeshPro | (bundled) | All text rendering |
| NUnit | (via test-framework 1.6.0) | EditMode unit tests |
| `com.unity.services.levelplay` | 9.5.1 | Unity LevelPlay Ads Mediation package; no first-party runtime calls found yet |

---

# Notes

- Enums defined in `Game.Core`:
  - `Food`: Any, Hamburger, FrenchFries
  - `CellType`: Seat, Food, Block
  - `GridId`: MainGrid, WaitGrid
  - `PersonTrait`: Cool, Sick, Dirty, Loud, Quiet
  - `PersonState`: Normal, Angry, Happy
  - `ConditionTarget`: Food, Person
  - `ConditionType`: Hate, Like
- `CanSitAnywhere` is represented by `ConditionType.Like` + `ConditionTarget.Food` + `Food.Any` and is always satisfied on the main seating grid.
  - `ItemType`: Gold, Gem, Remove, Undo, MoreMoves
- `Reward` is a `[Serializable] struct` with `ItemType type` and `int amount`.
- UI button convention: visual effects (`ButtonPunchShake`) and logic (`ButtonEventRaiser`) are separate components on the same button.
- `Game.View` does **not** reference `Game.Data`. Config-to-runtime conversion is done exclusively by `Game.Bootstrap`.
- Move recording skips WaitGrid→WaitGrid moves to prevent cluttering undo history.
- `ShopPanelView` receives copied EconomyConfig arrays from `UnityGamePresentation`; `Game.View` remains independent of `Game.Data`.
- Shop slot mapping is `0 = MoreMoves`, `1 = Remove`, `2 = Undo`; Gold limits apply per slot while Gem slots have no daily cap.
- `CurrencyFlyAnimation` provides pooled coin burst-and-fly animations with `OnCoinArrived` / `OnAllCoinsArrived` events.
- `CurrencyScatterAnimation` provides pooled coin scatter-and-shrink animations with a configurable hold delay and `OnCoinDisappeared` / `OnAllCoinsDisappeared` events.
- `VFXCatalog.asset` maps `RemoveBooster`, `Happy`, and `Win` IDs to their configured prefabs; each entry owns its prewarm, fade, and scale settings.
- `VfxPlayer` is a scene-owned presentation service; gameplay code triggers it only after successful domain operations, while `PersonView` reacts to the domain state event for Happy.
- `PersonTooltip` displays each character condition's live satisfied state with a checkbox and refreshes from `OnConditionsCleared` / `OnConditionStatusChanged`.
- `UIAlphaExtensions` is a static extension class providing fluent alpha get/set and PrimeTween alpha tweening for `Graphic` and `CanvasGroup`.
- `AudioCueId` is a presentation contract only; `Game.Core` and `Game.App` do not reference `AudioClip` or `AudioSource`.
- `AudioCatalog.asset` is the single cue/music catalog and `OnAudioCue.asset` is the single typed gameplay cue channel.
- `MainScene/[Audio]` is wired with one UI source, four SFX sources, two music sources, `AudioEventBinder`, and `UIButtonSound` on 34 scene buttons. `GameAudioMixer.mixer` routes these sources through `Master/Sound(UI,SFX)/Music` and exposes `SoundVolume` plus `MusicVolume` for `AudioPlayer` settings.
- Existing Main and InGame setting panels now have `AudioSettingsView`; all four Sound/Music controls in each panel are persistently wired to the view methods, while `AudioSettingsView` retains a no-duplicate runtime fallback for unwired controls. `GameData` uses 0–100 volume percentages with 100% defaults for new/legacy saves missing the audio fields.
- The `GameAudioMixer.mixer` asset uses a `Master/Sound(UI,SFX)/Music` group tree with exposed `MusicVolume`/`SoundVolume` parameters; the MainScene AudioPlayer and seven audio sources are routed to it. The catalog still has intentionally unassigned cue clips for content not yet provided.
