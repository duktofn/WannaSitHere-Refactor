# AGENTS.md

---

## 0. Agent Behavioral Rules

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

### Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it — don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

---

## 1. Build & Test Commands

```bash
# Convention lint (run before every PR)
bash scripts/ci/lint-conventions.sh

# Unity Test Runner — EditMode (Unit Tests)
# Open: Window > General > Test Runner > EditMode > Run All

# Unity Test Runner — PlayMode (Integration Tests)
# Open: Window > General > Test Runner > PlayMode > Run All
```

---

## 2. Project Structure

```
Assets/
└── _Project/                      # All custom code & assets
    ├── App/                       # Application layer (composition root, installers, scene mgmt)
    │   ├── Bootstrap/             # Game.App.Bootstrap — entry point
    │   ├── Config/Editor/         # Game.App.Editor — editor-only config
    │   ├── Installers/            # Game.App.Installers — Reflex installers
    │   └── SceneManagement/       # Game.App.SceneManagement
    ├── Core/
    │   └── Foundation/            # Core.Foundation — utilities, FSM, MVVM, Reactive base classes
    ├── Features/                  # Independent vertical slices — must NOT reference each other
    │   ├── Combat/                # Game.Features.Combat
    │   ├── Economy/               # Game.Features.Economy
    │   ├── GachaMerge/            # Game.Features.GachaMerge
    │   └── WaveSystem/            # Game.Features.WaveSystem
    ├── Shared/                    # Cross-feature contracts (channels, payloads, listeners)
    │   ├── Events/                # Event channel SOs (SpawnHeroChannelSO, EnergyRequestChannelSO, …)
    │   │   └── Assets/            # SO instances (SO_SpawnHero.asset, SO_EnergyResponse.asset)
    │   ├── Listener/              # Shared listener MonoBehaviours (AddCurrencyListener, …)
    │   ├── Payloads/              # Shared payload types (EnergyRequestPayload, UnitDiedPayload, …)
    │   └── Game.Shared.asmdef
    └── Game/                      # Game.App — top-level composition
```

### Assembly Dependency Rules

```
Game.App / Game.App.Installers
  └── references everything

Game.Features.*
  └── references Core.Foundation, R3.Unity, Reflex, Game.Shared
  └── must NOT reference other Game.Features.*

Game.Shared
  └── references Core.Foundation, R3.Unity

Core.Foundation
  └── no project references
```

> **Channel-ownership rule:** Any event channel consumed by more than one feature **must** live in `Game.Shared`, not inside a feature folder. Promote immediately when a second consumer appears.

---

## 3. Feature Folder Structure (standard)

```
Game.Features.GachaMerge/
├── Data/           # ScriptableObjects, config assets
├── Demo/           # Demo scenes & scripts (separate asmdef: *.Demo)
├── Domain/         # Pure C# services & models (no MonoBehaviour)
├── Tests/
│   └── Editor/     # EditMode unit tests (*.UnitTests asmdef)
├── View/           # MonoBehaviours, UI components
├── ViewModel/      # ViewModels (MVVM)
└── Game.Features.GachaMerge.asmdef
```

---

## 4. Coding Conventions (Quick Reference)

| Element | Convention | Example |
|---|---|---|
| Classes / Structs | PascalCase | `MergeService` |
| Interfaces | `I` + PascalCase | `IMergeAnimator` |
| Private fields | `_camelCase` | `_currentHealth` |
| Serialized fields | `[SerializeField] private` | `[SerializeField] private PullConfigSO config` |
| Async methods | Suffix `Async` | `ApplyMergePlanAsync()` |
| ScriptableObjects | Suffix `SO` | `PullConfigSO` |
| Event Channels | Suffix `ChannelSO` | `SpawnHeroChannelSO` |
| Services | Suffix `Service` | `MergeService` |
| ViewModels | Suffix `ViewModel` | `GachaViewModel` |
| SO asset files | Prefix `SO_` | `SO_SpawnHero.asset` |
| Dictionary fields | `[Key]2[Value]` | `_fruitKey2Items` |
| Namespace | Block-scoped, match folder | `namespace Game.Features.GachaMerge { }` |

**Critical rules:**
- No file-scoped namespaces (Unity 6000.3.x MonoScript registration bug).
- No `GameObject.Find`, `FindObjectOfType`, `Singleton`, `Resources.Load`.
- No LINQ in hot paths (Update, animation tick, R3 subscriptions).
- Subscribe events in `OnEnable`, unsubscribe in `OnDisable`.