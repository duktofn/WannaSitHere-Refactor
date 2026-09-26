# Technical Decisions

## DEC-001 — Presentation-owned VFX player with explicit calls

Date: 2026-09-16

Status: Accepted

### Problem

Gameplay actions and presentation views need to play particle effects at world and uGUI targets without coupling the domain layer to Unity prefabs, transforms, or particle-system lifecycle.

### Context

The project uses a layered architecture. `Game.Core` contains pure gameplay logic, `Game.Bootstrap` is the composition root, and `Game.View` owns Unity presentation. ScriptableObject event channels are already used for game-flow and UI notifications, while local view updates use C# events.

The initial VFX scope is small: Remove Booster at a `PersonView`, Happy feedback when a person state changes, and Win feedback at the `WinPanel`.

### Options

#### Option A — Global VFX event channel

Pros:

- Callers do not need a direct reference to the VFX player.
- Additional listeners could react to the same request.

Cons:

- A presentation request would need to carry Unity-specific targets or coordinates through the event layer.
- The flow becomes harder to trace for effects with a known owner.
- It encourages using the event bus for one-to-one calls.

#### Option B — Catalog plus scene-owned VfxPlayer

Pros:

- Prefab references and timing remain explicit and serializable.
- `VfxPlayer` centralizes spawning, UI/world placement, pooling, cancellation, and cleanup.
- `Game.Core` stays presentation-agnostic.
- Existing `GameManager`, `GridManager`, `PersonView`, and `UIManager` boundaries can call the player directly after the relevant state/result is known.

Cons:

- The scene needs explicit references to the player.
- Adding a new effect requires a catalog entry.

### Decision

Use `VfxCatalogSO` for prefab/timing configuration and a scene-owned `VfxPlayer` for all runtime particle spawning and lifecycle management.

Use direct calls for the current one-to-one presentation boundaries:

- `GameManager` plays Remove Booster VFX after `RemoveBooster.TryUse()` succeeds.
- `PersonView` plays Happy VFX after `PersonRuntimeData.OnPersonStateChanged` reports `Happy`.
- `UIManager` plays Win VFX after activating the win panel.

Do not add a global VFX event channel or make `Game.Core` depend on VFX.

### Reason

This keeps dependency direction consistent with the existing project. `Game.Bootstrap` and presentation components already hold explicit references to the systems they coordinate, and each current VFX trigger has a clear owner. A catalog avoids string-based loading while keeping art configuration editable. Centralizing lifecycle work is necessary because the imported particle prefabs use one-shot/looping systems without reliable automatic cleanup.

### Consequences

- New VFX assets should be registered in `Assets/Art/VFX/VFXCatalog.asset`.
- New runtime VFX behavior belongs under `Assets/Game/View/VFX`.
- `VfxPlayer` must stop/recycle instances when a level or UI flow is torn down.
- UI particle effects use the player’s UI coordinate path and a per-Canvas `VFXRoot`; board/person effects use world anchors.
- Overlay UI particle rendering uses the embedded `com.coffee.ui-particle` package instead of changing the project’s existing Overlay canvases to Camera Space.
- A presentation event channel can be introduced later only if multiple independent consumers genuinely need the same VFX notification.

### Related

- `Game.View.VFX.VfxCatalogSO`
- `Game.View.VFX.VfxPlayer`
- `Game.View.VFX.VfxInstance`
- `Game.Bootstrap.GameManager`
- `Game.View.People.PersonView`
- `Game.View.UI.UIManager`

## DEC-004 — Render Overlay UI particles through UI Particle

Date: 2026-09-16

Status: Accepted

### Problem

The Win feedback uses a regular Unity `ParticleSystem`, but `WinPanel` must remain in the project’s existing `ScreenSpaceOverlay` UI. A regular scene renderer can be hidden behind that overlay and cannot be ordered as a uGUI graphic.

### Context

The project has one scene-owned `VfxPlayer` for catalog lookup, pooling, and lifecycle management. Only UI effects need Canvas rendering; Remove Booster and Happy remain world-space effects. The Win effect should be placed under a `VFXRoot` belonging to the Canvas that owns the UI.

### Options

#### Option A — Change the existing UI to Screen Space - Camera

Pros:

- Native particle rendering and Canvas sorting remain available.
- No external package is required.

Cons:

- Changes the render mode of existing gameplay UI.
- Can affect UI positioning and other camera-dependent behavior.

#### Option B — Use a camera and RenderTexture bridge

Pros:

- Keeps the existing Overlay UI.
- Uses only built-in Unity rendering features.

Cons:

- Adds a camera, RenderTexture, RawImage, and resolution/placement coordination.
- Adds more runtime and mobile rendering overhead for one-shot UI feedback.

#### Option C — Use a uGUI particle renderer

Pros:

- Keeps `ScreenSpaceOverlay`.
- Renders particles through `CanvasRenderer` and supports uGUI sibling ordering.
- Does not require an additional camera or RenderTexture.

Cons:

- Adds a third-party package dependency.
- Particle materials must use a UI-compatible shader/material.

### Decision

Embed `com.coffee.ui-particle` version `4.13.3` and use `Coffee.UIExtensions.UIParticle` for UI VFX instances. `VfxPlayer` creates a UI wrapper under the configured per-Canvas `VFXRoot`, keeps the existing ParticleSystem prefab as its child, applies a UI-compatible material, and retains the existing shared catalog/player ownership.

### Reason

This satisfies the requirement that the existing UI remains Overlay while allowing the Win particle effect to participate in uGUI rendering and sibling ordering. The package supports the project’s Unity version, uGUI 2.0, and URP. Keeping the conversion inside `VfxPlayer` limits the dependency to UI effects and leaves world VFX unchanged.

### Consequences

- `Game.View` references the `Coffee.UIParticle` assembly.
- UI VFX requires a `RectTransform` `VFXRoot` under its owning Canvas and a UI-compatible material template.
- UI instances use a separate pool from world instances, while catalog and lifecycle ownership remain centralized.
- New UI particle materials must use a shader supported by `UIParticle`, such as the package’s `UI/Additive` shader.

### Related

- `Game.View.VFX.VfxPlayer`
- `Game.View.VFX.VfxInstance`
- `Assets/Scenes/SampleScene.unity`
- `Packages/com.coffee.ui-particle`

## DEC-002 — Inject the CanSitAnywhere runtime condition into RemoveBooster

Date: 2026-09-16

Status: Accepted

### Problem

Remove Booster must replace a person’s complete condition list with `CanSitAnywhere`, while refusing to target people who already carry that condition. The authoring condition exists as a `ConditionDataSO` asset, but the booster belongs to `Game.Core` and must not reference `Game.Data` or Unity assets.

### Context

`Food.Any` was added as the serialized sentinel for the `CanSitAnywhere` condition. The project already uses `Game.Bootstrap` as the boundary that converts authoring ScriptableObjects into runtime data before passing them to application/domain systems.

### Options

#### Option A — Hard-code CanSitAnywhere inside RemoveBooster

Pros:

- No additional scene reference is required.
- Existing callers need fewer constructor changes.

Cons:

- The domain duplicates authoring text and condition configuration.
- Localization or designer changes to `CanSitAnywhere.asset` would not reach the booster.
- The domain becomes responsible for a data-authoring default.

#### Option B — Inject the converted runtime condition

Pros:

- `Game.Core` receives only immutable runtime data.
- `Game.Bootstrap` remains the only layer that knows `ConditionDataSO`.
- The asset’s description and configuration are preserved.
- The booster’s replacement condition is explicit and testable.

Cons:

- `GameManager` and tests must provide the condition.
- The scene requires a serialized `CanSitAnywhere` reference.

### Decision

Use `ConditionDataSO.ToRuntimeData()` in `GameManager` and inject the resulting `ConditionRuntimeData` into `RemoveBooster`.

Define `Like + Food.Any` as `ConditionRuntimeData.IsCanSitAnywhere`. `ConditionChecker` treats it as unconditionally satisfied on the main grid. `RemoveBooster` excludes any person with that condition, and successful execution calls `PersonRuntimeData.ReplaceConditions()` with the injected condition.

### Reason

This preserves the project’s dependency direction and keeps designer-authored condition data authoritative. It also prevents Remove Booster from silently diverging from the condition shown in the Inspector or tooltip.

### Consequences

- `GameManager` must keep `_canSitAnywhereCondition` assigned to `Assets/Data/Condition/CanSitAnywhere.asset`.
- A malformed replacement condition causes Remove Booster to fail safely rather than applying the wrong rule.
- Existing presentation listeners continue to refresh through `OnConditionsCleared` after replacement.
- Any future special condition using `Food.Any` will share the same semantic unless the condition model is extended with a dedicated type.

### Related

- `Game.Core.Board.Food`
- `Game.Core.Conditions.ConditionRuntimeData`
- `Game.Core.Conditions.ConditionChecker`
- `Game.Core.Booster.RemoveBooster`
- `Game.Data.Conditions.ConditionDataSO`
- `Game.Bootstrap.GameManager`

## DEC-003 — Append Food.Any to preserve serialized food data

Date: 2026-09-16

Status: Accepted

### Problem

Adding the `Any` food type must not reinterpret existing serialized food values in cells and condition assets.

### Context

Existing assets store `Hamburger` as enum value `0` and `FrenchFries` as enum value `1`. Unity serializes enum fields by their numeric value, so inserting `Any` before those values would silently change existing content.

### Options

#### Option A — Insert Any at enum value 0

Pros:

- `Any` becomes the default enum value.
- New `CanSitAnywhere` assets can use `foodTarget: 0`.

Cons:

- Existing Hamburger/FrenchFries serialized values are reinterpreted.
- Every existing food cell and food condition would require a migration.

#### Option B — Append Any after existing values

Pros:

- Existing serialized assets remain valid without bulk migration.
- `CanSitAnywhere` remains explicit through `foodTarget: 2`.

Cons:

- `Any` is not the default enum value.
- New authoring data must use the appended value.

### Decision

Keep `Hamburger = 0` and `FrenchFries = 1`, and append `Any = 2`. Update `CanSitAnywhere.asset` to use `foodTarget: 2`.

### Reason

Preserving the serialized meaning of existing level and condition assets is safer than forcing a repository-wide data migration for a sentinel value whose numeric position is not part of the gameplay contract.

### Consequences

- Existing food assets retain their current behavior.
- Code should use `Food.Any` symbolically rather than relying on its numeric value.
- Future enum additions should normally be appended unless an explicit migration is planned.

### Related

- `Game.Core.Board.Food`
- `Game.Data.Conditions.ConditionDataSO`
- `Assets/Data/Condition/CanSitAnywhere.asset`

## DEC-005 — Keep application state in Game.App and Unity composition in Game.Bootstrap

Date: 2026-09-17

Status: Superseded by DEC-008

The former ownership split is retained here as historical context. DEC-008 kept the App/Unity dependency boundary and completed the move of runtime orchestration out of Bootstrapper.

### Problem

The previous `Game.Bootstrap.GameManager` combined persistent application state, economy and booster execution, Unity lifecycle, serialized authoring data, scene references, event-channel subscriptions, UI updates, and VFX calls. Keeping that class in Bootstrap made the application owner inaccessible as a clean `Game.App` service and coupled its responsibilities to Data and View.

### Context

`Game.App` already depends only on `Game.Core` and `Game.Events`, while `Game.Bootstrap` is the sole layer allowed to reference all runtime layers. `Game.View` depends on `Game.App`, so moving the existing MonoBehaviour unchanged into `Game.App` would introduce an invalid App-to-View dependency cycle.

### Options

#### Option A — Keep the combined GameManager in Game.Bootstrap

Pros:

- No class split or scene migration is needed.
- Existing serialized references remain on the same component.

Cons:

- Application state and Unity composition remain coupled.
- `Game.App` has no owner for persisted progress, economy transactions, and booster execution.

#### Option B — Move the existing MonoBehaviour unchanged into Game.App

Pros:

- `GameManager` would be physically located in the application folder.

Cons:

- It would require `Game.App` to reference `Game.Data` and `Game.View`.
- `Game.View` already references `Game.App`, creating a circular assembly dependency.

#### Option C — Split application ownership from the composition root

Pros:

- `Game.App.GameManager` can own progress, inventory, economy, persistence, and core booster operations without View/Data dependencies.
- `Game.Bootstrap.GameBootstrapper` can keep Unity lifecycle, serialized references, event wiring, level loading, UI binding, and VFX orchestration.
- The existing Bootstrap assembly remains the only composition root.

Cons:

- Event handlers must delegate across the new application/bootstrap boundary.
- The scene component class changes from `GameManager` to `GameBootstrapper` and needs Unity import validation.

### Decision

Use `Game.App.GameManager` as a plain C# application service. It owns `GameData`, `Inventory`, `EconomyManager`, save/load synchronization, reward/shop transactions, and core booster execution.

Use `Game.Bootstrap.GameBootstrapper` as the scene MonoBehaviour. It keeps the existing serialized field names and script GUID, constructs `GameManager`, subscribes to event channels, converts ScriptableObject configuration into application input, loads levels, binds UI, and performs view/VFX-only follow-up work.

### Reason

This preserves the current assembly dependency direction: App stays independent of authoring and presentation, while Bootstrap remains the explicit place where all layers are wired together. It also keeps Unity-only state and lifecycle callbacks out of the application service without introducing a new framework or event abstraction.

### Consequences

- Editor and scene tooling must find `GameBootstrapper` rather than a MonoBehaviour `GameManager`.
- `GameBootstrapper` retains the serialized field names from the previous component so existing scene assignments remain compatible through the preserved script GUID.
- VFX and `ConditionDataSO` conversion formerly attributed to `GameManager` are now Bootstrap responsibilities; DEC-001 and DEC-002 remain valid about their dependency boundaries.
- New application behavior should be added to `Game.App.GameManager` when it does not require Unity authoring or presentation references.

### Related

- `Game.App.GameManager`
- `Game.Bootstrap.GameBootstrapper`
- `Game.Bootstrap.LevelBootstrapper`
- `Game.Editor.CheatToolWindow`
- DEC-001
- DEC-002

## DEC-006 — Use hybrid event/direct-call communication for audio

Date: 2026-09-18

Status: Accepted

Ownership amendment (2026-09-25): DEC-008 moves successful application-outcome decisions from GameBootstrapper to GameManager. The hybrid event/direct-call strategy remains accepted; Bootstrapper binds accepted outcome outputs to existing result/audio endpoints. This migration is implemented.

### Problem

Audio feedback needs to react to cross-system gameplay outcomes without coupling `Game.Core` or `Game.App` to Unity audio objects, while view-owned interactions such as buttons, transitions, and person feedback need precise local timing.

### Context

The project already uses ScriptableObject event channels for game flow and a scene-owned presentation service for VFX. Outcome success is known in `GameBootstrapper`, while `PersonView`, `TransitionController`, and individual `Button` components own their local presentation moments. A single global audio event for every sound would make local one-to-one timing less explicit; direct calls from gameplay would violate the application/presentation boundary.

### Options

#### Option A — Route every sound through one audio event channel

Pros:

- One uniform publication mechanism.
- Additional listeners can observe every cue.

Cons:

- Local UI timing becomes indirect.
- Button/transition/person views need extra event assets or payload conventions.
- It is easier to raise a cue before an outcome has actually succeeded.

#### Option B — Directly call `AudioPlayer` from every producer

Pros:

- Precise local timing and simple call flow.

Cons:

- Gameplay/application code would know the presentation player.
- Cross-system win/lose and outcome notifications become tightly coupled.

#### Option C — Hybrid event/direct-call strategy

Pros:

- Successful gameplay outcomes use one typed `OnAudioCue` channel and remain decoupled from `AudioClip`/`AudioSource`.
- Win/lose and BGM reuse existing game-flow channels.
- View-owned button, transition, and Happy feedback call the player at the exact presentation boundary.

Cons:

- Two trigger paths must be documented and validated.
- The scene must wire both the binder and local player references.

### Decision

Use a hybrid strategy. `GameBootstrapper` raises `AudioCueId.BoosterUsed`, `Claim`, and `Spend` only after successful operations; `AudioEventBinder` maps those cues plus existing win/lose/menu/game-flow channels to `AudioPlayer`. `UIButtonSound`, `TransitionController`, and `PersonView` call `AudioPlayer` directly because they own the exact local interaction/state-transition timing.

`Game.Core` and `Game.App` may store primitive audio settings and raise the cue contract through the composition root, but they must not reference `AudioClip`, `AudioSource`, or `AudioMixer`.

### Reason

This preserves the existing event-channel architecture for one-to-many outcome and flow notifications while following the established presentation-owned VFX pattern for one-to-one view feedback. It also places the success boundary in `GameBootstrapper`, preventing failed booster/purchase attempts from playing success audio.

### Consequences

- `Assets/Data/Events/OnAudioCue.asset` is the single typed gameplay cue channel.
- `AudioPlayer` and `AudioCatalogSO` remain in `Game.View.Audio`.
- Local view components need serialized or runtime-injected `AudioPlayer` references and must respect Unity lifecycle unbinding.
- New audio triggers should first classify as an outcome/flow event or a view-owned direct interaction before adding code.

### Related

- `Game.Events.AudioCueId`
- `Game.Events.AudioCueEventChannelSO`
- `Game.View.Audio.AudioPlayer`
- `Game.View.Audio.AudioEventBinder`
- `Game.Bootstrap.GameBootstrapper`
- `DEC-001 — Presentation-owned VFX player with explicit calls`

## DEC-007 — Keep shared person definitions separate from level occurrences

Date: 2026-09-21

Status: Accepted

### Problem

The former model stores conditions and a default person on reusable cell assets. That makes two occurrences of the same character share authoring data and makes level-specific placement difficult to inspect or validate.

### Context

Character name, trait, and base sprite are stable definition data. Conditions and placement vary by level and may vary between occurrences of the same character. `CellView` also relies on `CellRuntimeData.DefaultPerson` during initial spawn, so conversion must provide the initial person before the cell is presented.

### Decision

Use `PersonDefinitionSO` for shared `personName`, `trait`, and `baseSprite`. Store each occurrence in a serializable `LevelPersonConfig` owned by `LevelDataSO`, including its `ConditionDataSO` references, `GridId`, and `Vector2Int` position.

`LevelDataSO` is the single authoring-to-runtime conversion boundary. It shares validation with the `LevelDataSO` Inspector, creates a new `PersonRuntimeData` and condition list for every occurrence, passes that person into `CellDataSO.ToRuntimeData()`, and preserves the invariant `CellRuntimeData.DefaultPerson == CurrentPerson` at level load.

Migration has been fully completed: `PersonDataSO`, `CellDataSO.defaultPerson`, `LevelPersonMigrationUtility`, `LevelPersonMigrationWindow`, and legacy migration validation/tests have been permanently removed. All character configurations are authoring-owned by `LevelDataSO.personConfigs` referencing shared `PersonDefinitionSO` assets.

### Consequences

- Adding a level requires configuring cells, selecting an existing definition, and assigning conditions in `LevelDataSO`.
- Runtime boosters and condition changes affect only the relevant `PersonRuntimeData` instance.
- Invalid definitions, conditions, duplicate placements, grid coordinates, and target cells are reported before the active grids are cleared.
- `CellDataSO` contains only cell configuration (type, food, sprite) without any person references.
- All legacy `PersonDataSO` assets and temporary migration tooling have been cleanly retired.

## DEC-008 — GameManager owns runtime orchestration; Bootstrapper only composes dependencies

Date: 2026-09-25

Status: Accepted and implemented

The ownership direction is explicitly requested by the user and implemented. `game-manager-refactor-plan.md` records the migration and verification limits.

### Problem

GameBootstrapper currently owns runtime decisions for level flow, reward selection, shop mapping, booster follow-up, settings, and daily refresh. Adding mechanic tutorials would put more application policy in the composition root. Independently subscribed UI transitions and level loading also provide no single authoritative moment at which a level is ready for interaction.

### Context

DEC-005 intentionally left Unity presentation orchestration in Bootstrapper. The user now requires Bootstrapper to initialize, inject, and connect dependencies only, with GameManager owning game orchestration. Game.View already references Game.App, so moving concrete View/Data dependencies into GameManager would introduce invalid dependencies. Existing scenes, channels, save data, and editor tooling need a staged migration.

### Options

#### Option A — Retain runtime orchestration in GameBootstrapper

Pros:

- Preserves the current implementation and direct access to scene references.
- Requires few new contracts.

Cons:

- Conflicts with the requested responsibility boundary.
- Tutorial and game-flow policy would continue accumulating in the composition root.
- Level loading and transition completion remain coordinated across independent listeners.

#### Option B — Move concrete Unity operations into GameManager

Pros:

- GameManager would visibly contain all command execution in one class.
- Direct calls are easy to follow locally.

Cons:

- References to LevelDataSO, GridManager, and UIManager would violate the assembly direction and create an App/View cycle.
- Application behavior would become coupled to scene objects and asset lifetimes.

#### Option C — GameManager orchestrates through App-owned contracts

Pros:

- Matches the user's requested ownership while preserving assembly direction.
- Gives level readiness, outcome acceptance, and tutorial activation one application owner.
- Allows command ordering and failure behavior to be exercised without scene objects.

Cons:

- Requires a small set of Unity adapters and explicit completion/failure contracts.
- Existing UI/audio listeners and editor callers must be rewired to avoid duplicate command execution.

### Decision

Select Option C. GameManager owns runtime game commands and their ordering, active-session ownership, success/failure policy, and tutorial activation. Domain services retain their mechanics; TutorialService retains step execution; views retain rendering and animation implementation.

GameBootstrapper constructs dependencies, converts authoring settings into application input, binds endpoints, forwards Unity lifecycle notifications, and cancels/unbinds owned work during teardown. It must not hide game-flow decisions in handlers or binding callbacks.

Unity adapters implement interfaces defined in Game.App. GameManager must not reference Game.Data, Game.View, or Game.Bootstrap. One-way command/result wiring keeps requested actions separate from accepted application outcomes. Existing view-local audio/VFX ownership remains valid.

### Reason

This fulfills the explicit responsibility requirement without replacing the repository's layering model. The application can determine when a load actually succeeds and when a transition completes before activating tutorial behavior. Scene references and authoring data remain accessible to adapters composed by Bootstrapper, while orchestration no longer depends on event subscription order.

### Consequences

- Supersedes DEC-005's responsibility split; its historical reasoning is retained above.
- Amends DEC-006's successful-outcome owner while preserving its hybrid communication strategy.
- LevelManager ownership, level load results, transition completion, cancellation, and scene teardown are explicit in the implementation.
- UIManager and AudioEventBinder consume presentation requests and accepted outputs without independently duplicating application flow.
- CheatToolWindow issues application commands through `GameManager`; scene request/result references were rewired while preserving the component script identity.
- `GameManager` now owns level-session sequencing, accepted outcomes, progression, economy/booster/settings/daily commands, and tutorial trigger policy through `ILevelLoader` and `IGamePresentation`.
- `GameBootstrapper` is limited to composition, endpoint wiring, Unity lifecycle forwarding, and cancellation/save forwarding.
- The tutorial lifecycle and completion persistence are wired, but no authored mechanic tutorial definitions or concrete tutorial-step assets exist yet.
- Verification: Unity EditMode tests passed; App, Bootstrap, View, Editor, and EditMode test assemblies built with zero warnings/errors. Manual Play Mode flow checks were not run. `scripts/ci/lint-conventions.sh` is absent from this checkout.

### Related

- `game-manager-refactor-plan.md`
- `Game.App.GameManager`
- `Game.App.LevelManager`
- `Game.Bootstrap.GameBootstrapper`
- `Game.Bootstrap.LevelBootstrapper`
- `Game.App.ILevelLoader`
- `Game.App.IGamePresentation`
- `Game.View.Board.GridManager`
- `Game.View.UI.UIManager`
- `Game.View.Audio.AudioEventBinder`
- `Game.Editor.CheatToolWindow`
- DEC-005
- DEC-006
