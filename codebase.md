# Codebase Context

Last Updated: 2026-09-17
Last Reviewed Commit: 99888e1446c6a9e4f4d9e8c641ea63788226a3f8

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
- event-driven architecture via ScriptableObject channels.

The project separates core gameplay logic (pure C#, no MonoBehaviour) from presentation, configuration, and infrastructure concerns using a Clean/Layered Architecture with ScriptableObject Event Channels for decoupled communication.

---

# Tech Stack

- Unity 6000.3.18f1
- C#
- UniTask (via Git URL)
- PrimeTween 1.4.11
- Unity Input System 1.20.0
- Universal Render Pipeline 17.3.0
- TextMeshPro
- uGUI (com.unity.ugui 2.0.0)
- UI Particle (com.coffee.ui-particle 4.13.3, embedded)
- NUnit (via com.unity.test-framework 1.6.0)

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
│   ├── App/                 (Game.App)           Level coordination, persistence
│   ├── View/                (Game.View)          MonoBehaviour presentation, UI, input, effects, VFX
│   ├── Bootstrap/           (Game.Bootstrap)     Composition root, lifecycle orchestrator
│   ├── Editor/              (Game.Editor)        Custom inspectors, cheat tools
│   └── Tests/               (Game.Tests.*)       EditMode unit tests
├── Prefabs/                 UI and gameplay prefabs
├── Scenes/                  Unity scenes
├── Settings/                URP and quality settings
├── Shaders/                 Custom shaders (transition iris-wipe)
└── TextMesh Pro/            TMP default resources
```

`Assets/Art/VFX/` contains first-party gameplay VFX and `VFXCatalog.asset`. `Assets/Lana Studio/Hyper Casual FX/` contains imported third-party particle assets retained as source material for the configured effects.

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
- **`Game.Events`** — References `Game.Core` (for `Reward` type) and `UniTask`.
- **`Game.Data`** — References `Game.Core` only.
- **`Game.App`** — References `Game.Core`, `Game.Events`.
- **`Game.View`** — References `Game.Core`, `Game.Events`, `Game.App`, `PrimeTween`, `UniTask`, `TMP`, `InputSystem`, `uGUI`, `Coffee.UIParticle`.
- **`Game.Bootstrap`** — References all runtime assemblies: `Game.Core`, `Game.Data`, `Game.App`, `Game.View`, `Game.Events`.
- **`Game.Editor`** — Editor-only. References `Game.Data`, `Game.App`, `Game.Core`, `Game.Bootstrap`.
- **`Game.Tests.EditMode`** — Editor-only. References `Game.Core`, `Game.App`, `Game.Events`.

Key rule: `Game.View` does **not** reference `Game.Data`. Configuration-to-runtime conversion is `Game.Bootstrap`'s responsibility.

Presentation VFX is owned by `Game.View.VFX`. `VfxCatalogSO` stores prefab and lifetime configuration, while `VfxPlayer` owns spawning, UI/world placement, pooling, cancellation, and cleanup. UI particle instances use the embedded `Coffee.UIParticle` renderer under the per-Canvas `VFXRoot`, while world effects continue to use the regular ParticleSystem path. `Game.Core` remains unaware of VFX; `Game.Bootstrap` and presentation views provide explicit references to the player.

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

---

## Game.Data (Configuration Layer)

Responsibility:

ScriptableObject definitions for level layout, person, condition, and economy configuration. Provides `ToRuntimeData()` factory methods to convert static config into domain runtime objects.

Depends On:

- `Game.Core`.

Used By:

- `Game.Bootstrap`, `Game.Editor`.

Communication:

- `LevelBootstrapper` calls `LevelDataSO.ToRuntimeData()` to create `LevelRuntimeData`.
- `PersonDataSO.ToRuntimeData()` chains to `ConditionDataSO.ToRuntimeData()`.

---

## Game.App (Application Layer)

Responsibility:

Application state and game operations: level gameplay coordination (`LevelManager`), persisted progress (`GameManager`, `SaveLoadManager`, `GameData`), economy transactions, and core booster execution.

Depends On:

- `Game.Core`, `Game.Events`.

Used By:

- `Game.View` (via `LevelManager` reference in `GridManager`), `Game.Bootstrap`.

Communication:

- `LevelManager` validates moves, records history, evaluates conditions, and raises win/lose via `VoidEventChannelSO`.
- `GameManager` owns `GameData`, `Inventory`, and `EconomyManager`; it persists successful state changes through `SaveLoadManager` without referencing authoring data or presentation components.
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
- Subscribes to ScriptableObject event channels for game flow.
- Reads domain state but does not mutate domain models directly (mutations go through `GridManager` → `LevelManager`).

Modules: Board (CellView, GridManager, FoodTooltips), People (PersonView, PersonMover, PersonSpawner, PersonTooltip, PersonDragManager), UI (UIManager, LevelView, InventoryView, BoosterSlotView, ShopPanelView, ShopItemView, PanelController, TransitionController, LevelEndPanel, LevelEndText, WeeklyLogin, ButtonSpriteSwap, UIAlphaExtensions), Effect (ButtonPunchShake, AdsShaking, CurrencyFlyAnimation, CurrencyScatterAnimation), VFX (VfxId, VfxCatalogSO, VfxPlayer, VfxInstance).

---

## Game.Bootstrap (Composition Root)

Responsibility:

Sole layer that knows all other layers. Creates and wires runtime objects. Orchestrates game lifecycle.

Depends On:

- All runtime assemblies.

Used By:

- None (top of dependency tree).

Communication:

- `GameBootstrapper` holds all serialized Data/View/Event references, listens to 15+ ScriptableObject event channels, and delegates application state changes to `Game.App.GameManager`.
- `LevelBootstrapper` converts `LevelDataSO` → `LevelRuntimeData` and initializes `GridManager`.
- `LevelLayoutGizmosDrawer` is a normal `MonoBehaviour` that reads selected level SO data for Scene-view layout visualization only.

---

## Game.Editor (Editor Tooling)

Responsibility:

Custom inspectors and development utilities. Editor-only assembly.

Contains:

- `CheatToolWindow`: Dual-mode (Play/Edit) cheat window for manipulating save data and game state.
- `LevelDataSOEditor`: Visual 2D matrix editor for `LevelDataSO`.

---

## Game.Tests.EditMode (Unit Tests)

Responsibility:

NUnit EditMode tests covering pure domain logic: Grid, Conditions, Economy, MoveHistory, Boosters, LevelManager move recording.

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

Runtime state of a character: name, trait, conditions list, sprite, and emotional state. Fires events on state change and condition clearing.

### Fields

| Name | Type | Purpose |
|---|---|---|
| `PersonName` | `string` (readonly) | Display name |
| `Trait` | `PersonTrait` (readonly) | Personality trait (Cool, Sick, Dirty, Loud, Quiet) |
| `_conditions` | `List<ConditionRuntimeData>` | Satisfaction requirements |
| `BaseSprite` | `Sprite` (readonly) | Default visual sprite |
| `State` | `PersonState` (get; private set) | Normal, Angry, or Happy |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `ClearConditions` | — | `void` | Empties conditions list and fires `OnConditionsCleared` |
| `ReplaceConditions` | `ConditionRuntimeData condition` | `void` | Replaces the complete list with one condition and fires `OnConditionsCleared` |
| `SetState` | `PersonState state` | `void` | Updates State and fires `OnPersonStateChanged` if changed |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnPersonStateChanged` | `Action<PersonState>` | Fired when emotional state changes |
| `OnConditionsCleared` | `Action` | Fired after the condition list is cleared or replaced |

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

Primary gameplay coordinator for a level session. Validates moves, records history (skipping WaitGrid↔WaitGrid), evaluates conditions, and raises win/lose events.

Dependencies:

- `LevelRuntimeData`, `LevelConditionEvaluator`, `MoveHistory`, `VoidEventChannelSO`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_currentLevel` | `LevelRuntimeData` (readonly) | Active level state |
| `_conditionEvaluator` | `LevelConditionEvaluator` (readonly) | Condition validation engine |
| `_moveHistory` | `MoveHistory` (readonly) | Move recorder for undo |
| `_onWinEvent` | `VoidEventChannelSO` (readonly) | Win event channel |
| `_onLoseEvent` | `VoidEventChannelSO` (readonly) | Lose event channel |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `TryMovePerson` | `CellRuntimeData sourceCell, CellRuntimeData targetCell, PersonRuntimeData person` | `bool` | Validates, executes move, records history, decrements move, checks win/lose |
| `CheckAllPersonConditions` | — | `void` | Updates all states; raises win or lose |
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

---

## GameManager

Path:
`Assets/Game/App/GameManager.cs`

Responsibility:

Plain C# application service that owns loaded progress, inventory, economy state, persistence, reward/shop transactions, and core booster execution. It has no `Game.Data` or `Game.View` dependency.

Dependencies:

- `SaveLoadManager`, `GameData`, `Inventory`, `EconomyManager`
- `LevelManager`, `MoreMoveBooster`, `UndoBooster`, `RemoveBooster`
- Core `Reward`, `ItemType`, `ConditionRuntimeData`, and `PersonRuntimeData`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_saveLoad` | `SaveLoadManager` | Persistence controller |
| `_inventory` | `Inventory` | Runtime inventory |
| `_economyManager` | `EconomyManager` | Login/shop manager |
| `_gameData` | `GameData` | Runtime save data copy |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `InitializeLoginState` | `DateTime nowUtc` | `void` | Initializes daily login state through the daily refresh path |
| `RefreshDailyState` | `DateTime nowUtc` | `bool` | Detects a new UTC calendar day, resets Gold shop purchase counts through `EconomyManager`, updates the saved login timestamp, and persists the result |
| `SetLevel` | `int level` | `void` | Updates and persists the current level |
| `GrantReward` | `Reward reward` | `void` | Applies and persists a configured reward |
| `TryClaimDailyReward` / `TryClaimWeeklyReward` | `Reward reward` | `bool` | Claims a reward through `EconomyManager` and persists only on success |
| `TryPurchase` | `Reward cost, Reward item, int goldShopSlotIndex = -1` | `bool` | Shop transaction via EconomyManager |
| `TryUseMoreMoves` / `TryUseUndo` / `TryUseRemove` | `LevelManager, ...` | `bool` | Executes core booster behavior and persists the consumed inventory item |
| `SaveGame` | — | `void` | Serializes state to GameData and persists |

---

## GameBootstrapper

Path:
`Assets/Game/Bootstrap/GameBootstrapper.cs`

Responsibility:

Scene MonoBehaviour composition root. Owns serialized authoring, scene, VFX, and event-channel references; creates `GameManager`; wires lifecycle/event handlers; and performs level/UI/VFX orchestration around application operations.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `GameManager`, `LevelBootstrapper`, `EventListener`
- `EconomyConfigSO`, `ConditionDataSO`, `LevelDataSO`, `GameConfig`
- `UIManager`, `GridManager`, `LevelView`, `WeeklyLogin`, `ShopPanelView`, `VfxPlayer`
- Game-flow, reward, shop, booster, and economy-feedback channels

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_levelData` | `List<LevelDataSO>` | Ordered level authoring data passed to `LevelBootstrapper` |
| `_economyConfig` | `EconomyConfigSO` | Authoring source for rewards, shop prices, and Gold shop limits |
| `_canSitAnywhereCondition` | `ConditionDataSO` | Authoring condition converted for Remove Booster execution |
| `_uiManager` | `UIManager` | Main UI facade reference |
| `_gridManager` | `GridManager` | Board view and active `LevelManager` reference |
| `_levelView` | `LevelView` | In-game move and booster HUD reference |
| `_weeklyLogin` | `WeeklyLogin` | Daily/weekly reward UI reference |
| `_shopPanel` | `ShopPanelView` | Shop price, affordability, purchase, and limit UI reference |
| `_vfxPlayer` | `VfxPlayer` | Presentation VFX player reference |
| `_onPlayGameEvent` / `_onWinEvent` / `_onLoseEvent` | `VoidEventChannelSO` | Game flow event channels |
| `_onNextLevelEvent` / `_onRestartLevelEvent` | `VoidEventChannelSO` | Level navigation event channels |
| `_onLevelChangedEvent` | `IntEventChannelSO` | Publishes the current level number |
| `_onClaimWinRewardEvent` / `_onClaimAdsRewardEvent` | `VoidEventChannelSO` | Win and ad reward claim channels |
| `_onClaimDailyRewardEvent` / `_onClaimWeeklyRewardEvent` | `VoidEventChannelSO` | Login reward claim channels |
| `_onBuyRemoveEvent` / `_onBuyUndoEvent` / `_onBuyMoreMovesEvent` | `VoidEventChannelSO` | Legacy/item-specific shop purchase channels |
| `_onUseMoreMovesEvent` / `_onUseUndoEvent` / `_onUseRemoveEvent` | `VoidEventChannelSO` | In-game booster use channels |
| `_onItemReceive` / `_onItemSpend` | `OnItemReceiveSO` / `OnItemSpendSO` | Economy transaction feedback channels |
| `_listener` | `EventListener` | Tracks channel subscriptions for lifecycle-safe unbinding |
| `_gameManager` | `GameManager` | Application state and transaction service created at runtime |
| `_levelBootstrapper` | `LevelBootstrapper` | Level data conversion and view composition helper |
| `_lastDailyCheckDateUtc` | `DateTime` | Prevents repeated daily-state checks after the current UTC date has been evaluated |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Awake` | — | `void` | Converts authoring limits into application input, creates `GameManager`, initializes presentation, and creates `LevelBootstrapper` |
| `Update` | — | `void` | Checks for a UTC date change while the game remains open |
| `OnEnable` / `OnDisable` | — | `void` | Binds and unbinds all event-channel callbacks |
| `HandlePlayGame` | — | `void` | Loads the active level and binds the in-game booster HUD |
| `HandleUseUndo` | — | `void` | Delegates undo to `GameManager`, then performs the view revert and condition refresh |
| `HandleUseRemove` | — | `void` | Converts `CanSitAnywhere` data, delegates condition replacement, then plays presentation VFX and refreshes conditions |
| `HandleShopPurchase` | `ShopPurchaseRequest request` | `void` | Resolves the configured price and reward for a Gold/Gem slot and delegates the transaction |
| `UpdateWeeklyLoginUI` | — | `void` | Projects application economy state and authoring rewards onto `WeeklyLogin` |
| `SaveGame` | — | `void` | Delegates persistence to `GameManager` and refreshes the ShopPanel |
| `OnApplicationFocus` | `bool hasFocus` | `void` | Rechecks daily state when the application regains focus |
| `RefreshDailyStateIfNeeded` | — | `void` | Runs the once-per-day application refresh and updates login/shop presentation when state changes |
| `ResolveShopPanel` | — | `ShopPanelView` | Uses the serialized ShopPanel when available, otherwise resolves the existing scene object and adds the view component at runtime |

---

## ShopPurchaseRequest

Path:
`Assets/Game/View/UI/ShopPanelView.cs`

Responsibility:

Immutable presentation request identifying whether a shop slot uses Gold, its zero-based EconomyConfig slot index, and the booster item represented by that slot.

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

Presentation coordinator for the ShopPanel. Receives EconomyConfig-derived price and limit arrays from `GameBootstrapper`, binds the six slot views, refreshes prices/limits/affordability, and forwards purchase requests through a callback.

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
| `_onPurchaseRequested` | `Action<ShopPurchaseRequest>` | Callback to Bootstrap for the actual transaction |
| `_isSubscribed` | `bool` | Prevents duplicate Inventory event subscriptions across enable/disable cycles |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Bind` | `IReadOnlyList<Reward> goldShopPrices, IReadOnlyList<int> goldShopLimits, IReadOnlyList<Reward> gemShopPrices, EconomyManager economyManager, Action<ShopPurchaseRequest> onPurchaseRequested` | `void` | Supplies configured offers and transaction callback, then refreshes all slots |
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

POCO class that converts `LevelDataSO` to `LevelRuntimeData`, clears old grids, initializes `GridManager`, and binds `LevelView`.

Dependencies:

- `LevelDataSO`, `GridManager`, `LevelView`

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `LoadLevel` | `int levelNumber` | `void` | Wraps index, converts data, clears grids, creates grids, binds view |

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

Instantiates `CellView` prefabs from grid data, maintains `_cellViewMap` dictionary for domain↔view lookup, delegates move validation to `LevelManager`, and supports undo view reversion.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `LevelManager`, `PersonMover`, `VoidEventChannelSO`
- `VfxPlayer`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_cellViewMap` | `Dictionary<CellRuntimeData, CellView>` | Maps domain cell to view |
| `_levelManager` | `LevelManager` | Domain coordinator |
| `personMoveManager` | `PersonMover` | Movement handler |
| `cellPrefabs` | `GameObject` | Cell view prefab |
| `adjacent` | `List<Vector2Int>` | Cardinal direction offsets |
| `OnWinEvent` | `VoidEventChannelSO` | Win channel passed to LevelManager |
| `OnLoseEvent` | `VoidEventChannelSO` | Lose channel passed to LevelManager |
| `_vfxPlayer` | `VfxPlayer` | Player injected into spawned PersonViews and stopped when the level view is cleared |

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Initialize` | `LevelRuntimeData level` | `void` | Stores grids, constructs LevelManager |
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

---

## CellView

Path:
`Assets/Game/View/Board/CellView.cs`

Responsibility:

Visual representation of a single board cell. Hosts `PersonView` and `FoodTooltips`. Spawns default person on initialization.

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

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `BindVfxPlayer` | `VfxPlayer vfxPlayer` | `void` | Injects the presentation VFX player and handles an already-Happy state |

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

Shows character condition bubble on tap. Dynamically resizes bubble to fit condition text. Listens to `OnConditionsCleared` to refresh when conditions are removed.

Inherits / Implements:

- `MonoBehaviour`, `IPointerClickHandler`

Dependencies:

- `PersonRuntimeData`, `InputSystem`, `PrimeTween`, `UniTask`, `TMP`

---

## PersonSpawner

Path:
`Assets/Game/View/People/PersonSpawner.cs`

Responsibility:

Instantiates person prefab, binds `PersonRuntimeData`, initializes `PersonDragManager`, and links to `CellView`.

Inherits / Implements:

- `MonoBehaviour`

---

## UIManager

Path:
`Assets/Game/View/UI/UIManager.cs`

Responsibility:

Root UI coordinator. Manages canvas state switching (MainMenu ↔ InGame), win/lose panel visibility, settings panels, and screen transitions via `TransitionController`.

Inherits / Implements:

- `MonoBehaviour`

Dependencies:

- `EventListener`, `VoidEventChannelSO`, `IntEventChannelSO`, `TransitionController`, `InventoryView`, `UniTask`, `TMP`
- `VfxPlayer`

### Key Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `Initialize` | `Inventory inventory` | `void` | Binds InventoryView, refreshes level text |
| `UpdateLevelText` | `int level` | `void` | Updates level label |
| `PlayGame` | — | `void` | Transitions from menu to game |
| `NextLevel` | — | `void` | Transitions to next level |
| `RestartLevel` | — | `void` | Restarts current level |
| `BackToHome` | — | `void` | Returns to main menu |
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

Circle iris-wipe screen transition via custom shader material. Supports open, close, and full cycle transitions with configurable easing and focal points.

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

Responsibility: ScriptableObject configuring level layout (move count, main grid, wait grid). Provides `ToRuntimeData()` factory.

### PersonDataSO

Path: `Assets/Game/Data/People/PersonDataSO.cs`

Responsibility: ScriptableObject configuring a character (name, trait, conditions, sprite). Provides `ToRuntimeData()` factory.

### ConditionDataSO

Path: `Assets/Game/Data/Conditions/ConditionDataSO.cs`

Responsibility: ScriptableObject configuring a single condition rule. Provides `ToRuntimeData()` factory.

### CellDataSO

Path: `Assets/Game/Data/Board/CellDataSO.cs`

Responsibility: ScriptableObject configuring a board cell (type, food, default person, sprite). Provides `ToRuntimeData()` factory.

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

Responsibility: Dual-mode (Play/Edit) `EditorWindow` for manipulating save data: currencies, boosters, login state, shop purchases, level progression, and win/lose bypass.

### LevelDataSOEditor

Path: `Assets/Game/Editor/LevelDataSOEditor.cs`

Responsibility: Custom inspector providing a visual 2D matrix editor for `LevelDataSO` with resize, batch fill, and clear operations.

# Code Flow

## App Start & Initialization

`GameBootstrapper.Awake()`
→ creates `GameManager`
  → `SaveLoadManager.GetGameData()` loads `GameData` from disk
  → creates `Inventory` and `EconomyManager` from saved state
  → `GameManager.InitializeLoginState(DateTime.UtcNow)` updates daily state and persists when needed
→ creates `LevelBootstrapper`
→ `UIManager.Initialize(Inventory)` → `InventoryView.BindData()`
→ resolves/creates `ShopPanelView` and binds `EconomyConfigSO.goldShopPrice`, `goldShopLimit`, and `gemShopPrice`
→ updates `WeeklyLogin` UI

`GameBootstrapper.OnEnable()`
→ Registers 15+ event channel listeners via `_listener`

`GameBootstrapper.Start()`
→ Raises `_onLevelChangedEvent` with initial level

`GameBootstrapper.Update()` / `OnApplicationFocus(true)`
→ `GameManager.RefreshDailyState(DateTime.UtcNow)`
→ resets Gold shop purchase counts when the UTC calendar date changes
→ persists `GameData` and refreshes `WeeklyLogin`/`ShopPanel`

---

## Play Level

User taps Play button
→ `ButtonPunchShake.OnButtonPressed()` (visual only)
→ `ButtonEventRaiser.RaiseAll()` waits delay → raises `OnPlayGameEvent`
→ `UIManager.PlayGame()` transitions MainMenu → InGame
→ `GameBootstrapper.HandlePlayGame()`
  → `LevelBootstrapper.LoadLevel(currentLevel)`
    → `LevelDataSO.ToRuntimeData()` → `LevelRuntimeData`
    → `GridManager.ClearGrids()`
    → `GridManager.Initialize(levelRuntimeData)`
      → Creates `LevelManager` with grids, adjacency, and win/lose channels
    → `GridManager.CreateMainGrid()` and `CreateWaitGrid()`
      → Instantiates `CellView` prefabs, binds data, spawns persons
    → `LevelView.BindData(levelRuntimeData)`
  → `LevelView.BindBoosters(_inventory)`

---

## Drag & Drop Move

User drags `PersonView`
→ `PersonDragManager.OnBeginDrag()` → `PersonMover.BeginMove()`
→ `PersonDragManager.OnDrag()` → `PersonMover.DragTo()`
→ `PersonDragManager.OnEndDrag()`
  → `PersonMover.GetOverlappingCell()` (physics overlap)
  → `PersonMover.MoveToCell()`
    → `GridManager.TryMovePerson(sourceCell, targetCell, person)`
      → `LevelManager.TryMovePerson()`
        → Validates target is Seat, swaps cell occupants
        → Records `MoveRecord` in `MoveHistory` (skip WaitGrid↔WaitGrid)
        → `LevelRuntimeData.ModifyMove(-1)` → `OnMoveChanged`
        → `LevelManager.CheckAllPersonConditions()`
          → `LevelConditionEvaluator.UpdateAllPersonStates()` → sets Happy/Angry/Normal
            → `PersonView.UpdateState(Happy)` → `VfxPlayer.PlayAtWorld(Happy, person transform)`
          → If all satisfied → `_onWinEvent.Raise()`
          → If out of moves → `_onLoseEvent.Raise()`

---

## Use Booster (e.g. Undo)

User taps Undo slot
→ `BoosterSlotView.OnClick()` raises `_onUseUndoEvent`
→ `GameBootstrapper.HandleUseUndo()`
  → `GameManager.TryUseUndo(levelManager, out record)` checks active level, inventory, and executes core undo
  → `UndoBooster.TryUse()`
    → Pops `MoveRecord`, restores cells, refunds move
  → `GridManager.RevertMoveView(record)` → `PersonMover.RevertMove()` (tween animation)
  → `LevelManager.CheckAllPersonConditions()`
  → `GameManager.SaveGame()` persists the spent Undo item

## Remove Booster VFX

`BoosterSlotView.OnClick()` raises `_onUseRemoveEvent`
→ `GameBootstrapper.HandleUseRemove()` converts `_canSitAnywhereCondition`
→ `GameManager.TryUseRemove()` executes `RemoveBooster` and persists the spent item
→ `RemoveBooster` skips persons already carrying `CanSitAnywhere`
→ selected person receives a single `CanSitAnywhere` condition
→ `GridManager.FindPersonView(TargetPerson)` resolves the selected presentation object
→ `VfxPlayer.PlayAtWorld(RemoveBooster, targetPersonView.transform)`
→ `LevelManager.CheckAllPersonConditions()` updates the person to Happy
→ VFX instances are cleared when `GridManager.ClearGrids()` starts a new level

---

## Win & Claim Reward

`_onWinEvent.Raise()`
→ `UIManager.ShowWin()` activates `levelWinPanel`
→ `VfxPlayer.PlayAtUI(Win, levelWinPanel RectTransform)` places Win VFX at its center
→ `LevelEndPanel` reveals graphics sequentially
→ `GameBootstrapper.HandleWin()` → `GameManager.AdvanceLevel()` increments level and saves

User taps claim reward
→ `ButtonEventRaiser` raises `OnClaimWinRewardEvent`
→ `GameBootstrapper.HandleClaimWinReward()` gets reward from `EconomyConfigSO.levelWinReward`
  → `GameManager.GrantReward(reward)` → `Inventory.UpdateInventory(reward)` → `OnInventoryUpdate` → `InventoryView` animates
  → `GameBootstrapper` raises `_onItemReceive` with reward
  → `SaveGame()`

---

## Shop Purchase

`ShopPanelView.Bind()` copies `EconomyConfigSO` prices and Gold limits
→ discovers `GoldShopItem 1..3` and `GemShopItem 1..3`
→ `ShopItemView` displays the configured price; Gold slots display `Limit: purchased/limit`
→ Gold button interactability uses `EconomyManager.GetGoldShopPurchaseCount(i)` and inventory affordability

User taps a shop button
→ `ShopItemView` emits `ShopPurchaseRequest(UsesGold, SlotIndex, ItemType)` to `GameBootstrapper.HandleShopPurchase()`
→ `GameBootstrapper` selects `goldShopPrice[i]` or `gemShopPrice[i]` and the matching booster reward
→ `GameManager.TryPurchase(cost, item, goldSlotIndex)` delegates to `EconomyManager.TryPurchase()`
→ on success, inventory/purchase count are persisted and `_onItemReceive` is raised
→ `ShopPanelView.Refresh()` updates prices, affordability, and Gold limit text

---

# Important Dependencies

| Package | Version | Purpose |
|---|---|---|
| PrimeTween | 1.4.11 | All UI and gameplay tweening (scale, position, alpha, custom) |
| UniTask | Git (latest) | Async/await for delays, sequenced animations, fire-and-forget |
| Unity Input System | 1.20.0 | Pointer press detection for tooltips (FoodTooltips, PersonTooltip) |
| URP | 17.3.0 | Render pipeline |
| TextMeshPro | (bundled) | All text rendering |
| NUnit | (via test-framework 1.6.0) | EditMode unit tests |

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
- `ShopPanelView` receives copied EconomyConfig arrays from `GameBootstrapper`; `Game.View` remains independent of `Game.Data`.
- Shop slot mapping is `0 = MoreMoves`, `1 = Remove`, `2 = Undo`; Gold limits apply per slot while Gem slots have no daily cap.
- `CurrencyFlyAnimation` provides pooled coin burst-and-fly animations with `OnCoinArrived` / `OnAllCoinsArrived` events.
- `CurrencyScatterAnimation` provides pooled coin scatter-and-shrink animations with a configurable hold delay and `OnCoinDisappeared` / `OnAllCoinsDisappeared` events.
- `VFXCatalog.asset` maps `RemoveBooster`, `Happy`, and `Win` IDs to their configured prefabs; each entry owns its prewarm, fade, and scale settings.
- `VfxPlayer` is a scene-owned presentation service; gameplay code triggers it only after successful domain operations, while `PersonView` reacts to the domain state event for Happy.
- `UIAlphaExtensions` is a static extension class providing fluent alpha get/set and PrimeTween alpha tweening for `Graphic` and `CanvasGroup`.
