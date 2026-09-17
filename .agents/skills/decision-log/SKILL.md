---

name: decision-log
description: Use when making or discovering an important technical, architectural, workflow, dependency, API, or design decision that may affect future development or require future agents to understand why the current solution was chosen.
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Decision Log Skill

## Purpose

Maintain important project decisions in `decisions.md`.

The purpose of the decision log is to preserve reasoning that cannot be reliably reconstructed from the source code alone.

`decisions.md` should answer:

> Why was this solution chosen instead of the other reasonable alternatives?

It records the reasoning behind meaningful technical and architectural decisions so future developers or agents do not repeat already-resolved discussions or accidentally undo intentional design choices.

---

# File Location

Use:

```text
decisions.md
```

at the repository root unless the project defines another location for agent context.

If the file does not exist when the first qualifying decision is made, create it.

---

# When to Log a Decision

Create a decision entry when:

1. there are two or more reasonable approaches,
2. a meaningful choice is made,
3. that choice may influence future implementation, architecture, APIs, dependencies, workflow, or maintainability.

Examples that normally qualify:

* architectural pattern selection,
* dependency direction,
* event-driven vs direct communication,
* interfaces vs concrete dependencies,
* dependency injection strategy,
* singleton vs instance ownership,
* UniTask vs Coroutine,
* state machine architecture,
* save-system architecture,
* object pooling strategy,
* scene-loading strategy,
* package or framework adoption,
* replacing an existing major dependency,
* public API design,
* serialization strategy,
* networking architecture,
* folder/module boundaries,
* data ownership,
* error-handling strategy,
* asynchronous cancellation strategy.

---

# When Not to Log

Do not create decision entries for routine implementation work.

Examples:

* renaming a variable,
* renaming a method,
* fixing a typo,
* formatting code,
* adding a null check,
* moving a file without architectural consequences,
* small implementation refactors,
* obvious bug fixes,
* local changes with no future architectural significance.

A useful test is:

> Would a future developer reasonably ask "Why did we choose this approach instead of another valid approach?"

If yes, the decision probably belongs in the log.

---

# Decision ID

Each decision must have a stable sequential identifier.

Format:

```text
DEC-001
DEC-002
DEC-003
```

Never reuse an existing ID.

---

# Decision Format

Use the following format.

```md
## DEC-XXX — Decision Title

Date: YYYY-MM-DD

Status: Accepted

### Problem

Describe the problem that required a decision.

### Context

Describe relevant technical or project constraints.

### Options

#### Option A — Name

Pros:

- ...

Cons:

- ...

#### Option B — Name

Pros:

- ...

Cons:

- ...

### Decision

State which option was selected.

### Reason

Explain why this option was chosen.

### Consequences

Describe the expected consequences of this decision.

### Related

- related classes
- related systems
- related decisions
```

---

# Required Fields

Every decision must contain:

* ID,
* title,
* date,
* status,
* problem,
* considered options,
* selected option,
* reasoning,
* advantages and disadvantages of relevant options.

Where useful, also include:

* context,
* constraints,
* consequences,
* affected systems,
* related decisions.

---

# Status

Use one of the following statuses:

```text
Proposed
Accepted
Rejected
Deprecated
Superseded
```

Meaning:

### Proposed

A decision is being considered but is not final.

### Accepted

The decision is currently active.

### Rejected

The option or proposal was explicitly considered and not adopted.

### Deprecated

The decision is still historically relevant but should no longer be used for new work.

### Superseded

A newer decision replaced this decision.

When superseded, reference the replacement.

Example:

```md
Status: Superseded by DEC-014
```

Do not delete old decisions simply because they are no longer active.

The decision log preserves history.

---

# Problem

Describe the actual problem, not the implementation.

Bad:

```md
We need to add an EventBus.
```

Good:

```md
Several gameplay systems need to react to domain events without introducing direct references between unrelated modules.
```

---

# Options

Record realistic options that were actually considered.

Do not create fake alternatives merely to fill the template.

Each meaningful option should contain its primary advantages and disadvantages.

Example:

```md
### Options

#### Option A — Direct References

Pros:

- simple,
- explicit call flow,
- easy to debug.

Cons:

- increases coupling,
- requires consumers to know each other.

#### Option B — C# Events

Pros:

- publishers do not depend directly on subscribers,
- lightweight,
- built into the language.

Cons:

- subscription lifecycle must be managed,
- call flow becomes less explicit.

#### Option C — Global Event Bus

Pros:

- systems can communicate without references,
- convenient for cross-cutting events.

Cons:

- global dependency,
- harder event tracing,
- ownership becomes unclear.
```

---

# Decision

State the selected approach precisely.

Bad:

```md
Use events.
```

Good:

```md
Use local C# events for communication between the Board domain object and its presenter.

Do not introduce a global EventBus.
```

---

# Reason

Explain why the selected option fits this project better than the alternatives.

Reasoning should reference concrete constraints such as:

* coupling,
* testability,
* complexity,
* performance,
* maintainability,
* package dependency,
* implementation cost,
* team familiarity,
* Unity lifecycle,
* runtime behavior,
* scalability.

Avoid vague explanations such as:

```text
This is cleaner.
This is better.
This is more professional.
```

Explain what makes it better in the current context.

---

# Consequences

Document important implications.

Example:

```md
### Consequences

- `Board` exposes domain events.
- Presentation classes subscribe during initialization.
- Subscribers must unsubscribe when their lifecycle ends.
- Gameplay modules should not introduce a global EventBus for local interactions.
```

Consequences may be positive or negative.

---

# Updating Decisions

Existing decisions should not normally be rewritten to hide historical reasoning.

If circumstances change significantly:

1. create a new decision,
2. reference the previous decision,
3. mark the old decision as `Superseded`,
4. identify the new decision that replaces it.

Example:

```md
## DEC-004 — Use Coroutine for Scene Loading

Status: Superseded by DEC-019
```

Then:

```md
## DEC-019 — Replace Coroutine Scene Loading with UniTask

Related:

- Supersedes DEC-004
```

Minor clarifications may be added to an existing entry if they do not change the actual historical decision.

---

# Discovered Historical Decisions

Sometimes an important decision already exists implicitly in the repository but was never documented.

If source code, documentation, commits, or project context clearly demonstrate the reasoning, it may be added to `decisions.md`.

Do not invent historical reasoning.

If the selected architecture is visible but the reason is unknown, record only what can be established.

Example:

```md
Reason:

The original reasoning is not documented.

The current architecture uses this approach consistently across the project.
```

---

# Relationship with Codebase Context

`codebase.md` describes:

> What the repository currently looks like.

`decisions.md` describes:

> Why important parts of the repository were designed that way.

Do not copy large architecture descriptions into the decision log.

Instead reference relevant systems.

Example:

```md
### Related

- `GameStateMachine`
- `IState`
- `PlayingState`
- Codebase: `Architecture > State Management`
```

---

# Agent Behavior

Before proposing a significant architectural or technical change:

1. inspect relevant entries in `decisions.md`,
2. determine whether the issue has already been decided,
3. respect active accepted decisions unless new requirements justify reconsideration.

If a new implementation conflicts with an active decision:

1. identify the conflict,
2. reconsider the decision explicitly,
3. create a new decision if the old one is replaced,
4. mark the previous decision as superseded when appropriate.

Do not silently violate an existing accepted decision.

---

# Decision Logging Trigger

After making a meaningful technical choice, ask internally:

```text
Were multiple reasonable approaches available?

Would this choice affect future development?

Would understanding the reasoning help a future developer or agent?
```

If the answer is yes, update `decisions.md`.

---

# Example

```md
## DEC-007 — Use UniTask for Asynchronous Gameplay Flow

Date: 2026-09-16

Status: Accepted

### Problem

The project requires asynchronous gameplay, UI animation, and scene-loading sequences that need cancellation and composition.

### Context

Unity Coroutine is already available, but several systems require cancellation and values returned from asynchronous operations.

### Options

#### Option A — Unity Coroutine

Pros:

- built into Unity,
- simple for basic sequences,
- no external dependency.

Cons:

- weak composition,
- cancellation must be handled manually,
- cannot naturally return values,
- more difficult to compose across systems.

#### Option B — UniTask

Pros:

- supports async/await,
- supports `CancellationToken`,
- composable asynchronous APIs,
- optimized for Unity.

Cons:

- external dependency,
- requires familiarity with async/await lifecycle issues.

### Decision

Use UniTask for new asynchronous gameplay and application-level flows.

### Reason

The project frequently requires asynchronous operations that need cancellation and composition.

UniTask provides these capabilities while integrating with Unity's runtime model.

### Consequences

- new asynchronous APIs should normally return `UniTask` or `UniTask<T>`,
- cancellation should be propagated where lifecycle cancellation is relevant,
- Coroutine should only be introduced when there is a specific reason to prefer it.

### Related

- `SceneLoader`
- `GameFlowController`
- `CurrencyFlyAnimation`
```

---

# Core Principle

A decision log is not a changelog.

Do not record everything that changed.

Record decisions whose reasoning should survive the current session.
