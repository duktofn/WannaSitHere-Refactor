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

Status: Accepted

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
