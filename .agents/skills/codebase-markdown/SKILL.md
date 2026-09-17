---

name: codebase
description: Use when starting a new session, understanding project structure, investigating architecture, or after making changes that affect project structure, dependencies, classes, APIs, or code flow.
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Codebase Context Skill

## Purpose

Maintain a persistent architectural and structural understanding of the repository in `codebase.md`.

`codebase.md` acts as a semantic index of the current codebase so future agents can understand the project without rescanning the entire repository.

It should describe:

* what the project is,
* what technologies it uses,
* how the architecture is organized,
* what each architectural layer or module is responsible for,
* what important classes exist,
* what fields and methods those classes expose or depend on,
* why those fields and methods exist,
* how classes and systems communicate,
* and how important runtime code flows work.

The repository source code is always the ultimate source of truth.

If `codebase.md` conflicts with the source code:

1. Trust the source code.
2. Investigate the discrepancy.
3. Update `codebase.md`.

---

# File Location

Use:

```text
codebase.md
```

at the repository root unless the repository already defines another location for agent context.

---

# Session Startup

When starting a new working session:

1. Search for `codebase.md`.
2. If it does not exist:

   * inspect the repository,
   * determine the project structure,
   * create `codebase.md`.
3. If it exists:

   * read it before performing substantial work.
4. Read the current Git commit.
5. Compare it against `Last Reviewed Commit` in `codebase.md`.
6. If the repository has changed since that commit:

   * inspect the relevant Git diff,
   * determine whether architecture, dependencies, classes, APIs, or important code flows changed,
   * update `codebase.md` when necessary.

Do not blindly rescan the entire repository if the existing context is still reliable.

Prefer incremental inspection.

---

# Required Metadata

At the beginning of `codebase.md`, maintain:

```md
# Codebase Context

Last Updated: YYYY-MM-DD
Last Reviewed Commit: <commit hash>
```

`Last Reviewed Commit` represents the latest commit whose relevant codebase structure has been reflected in this document.

---

# Required Structure

Use the following general structure.

```md
# Codebase Context

Last Updated:
Last Reviewed Commit:

# Project Overview

# Tech Stack

# Project Structure

# Architecture

# Layers / Modules

# Class Index

# Code Flow

# Important Dependencies

# Notes
```

Sections may be expanded when the project requires it.

---

# Project Overview

Describe the project at a high level.

Include:

* project type,
* primary purpose,
* major gameplay or product systems,
* important architectural characteristics.

Keep this section concise.

Example:

```md
# Project Overview

A Unity-based casual puzzle game.

The project separates core gameplay logic from presentation and infrastructure concerns.

Primary systems include:

- level management,
- board gameplay,
- currency,
- UI navigation,
- persistence,
- audio.
```

---

# Tech Stack

Record technologies and versions that materially affect development.

Examples:

```md
# Tech Stack

- Unity 6.3 LTS
- C#
- UniTask 2.x
- PrimeTween 1.x
- Unity Input System
- Addressables
```

Include package or framework versions when known and relevant.

Do not guess versions.

---

# Project Structure

Describe important repository directories and their responsibilities.

Example:

```md
# Project Structure

Assets/
├── Game/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── View/
├── Editor/
└── Plugins/
```

Explain non-obvious directories.

Do not document generated folders unless they are architecturally relevant.

---

# Architecture

Describe the architecture used by the project.

Explain:

* architectural pattern,
* dependency direction,
* ownership of responsibilities,
* communication boundaries.

Example:

```text
View
  ↓
Application
  ↓
Domain

Infrastructure
  ↓
Domain interfaces
```

Document actual architecture, not intended architecture.

If the implementation deviates from the intended design, mention the current implementation.

---

# Layers / Modules

For each architectural layer or major module, document:

* responsibility,
* allowed dependencies,
* major systems,
* communication with other layers.

Example:

```md
## Domain

Responsibility:

Contains gameplay rules and core domain state.

Depends On:

- no presentation code,
- no concrete infrastructure implementations.

Used By:

- Application.

Communication:

Application invokes Domain behavior directly.
Domain exposes state, results, or events back to Application.
```

---

# Class Index

Document classes that contribute meaningful behavior, state, dependency relationships, APIs, or architectural structure.

Do not reproduce source code.

For each class, record:

* class name,
* source path,
* responsibility,
* base class or implemented interfaces when relevant,
* dependencies,
* fields,
* methods,
* events or delegates when present,
* important relationships with other classes.

Use the following format.

```md
## ClassName

Path:
`path/to/ClassName.cs`

Responsibility:

Short explanation of what problem this class solves and what it owns.

Inherits / Implements:

- `BaseClass`
- `IInterface`

Dependencies:

- `DependencyA`
- `DependencyB`

### Fields

| Name | Type | Purpose |
|---|---|---|
| `_field` | `FieldType` | Explain why this field exists and what responsibility/state/dependency it represents |

### Methods

| Method | Parameters | Return Type | Purpose |
|---|---|---|---|
| `MethodName` | `Type parameter` | `ReturnType` | Explain what problem this method solves or what operation it performs |

### Events

| Name | Type | Purpose |
|---|---|---|
| `OnChanged` | `Action<Value>` | Explain what change it communicates |

### Relations

- Calls `OtherClass.Method()`
- Subscribes to `OtherClass.OnEvent`
- Publishes `OnSomethingChanged`
- Creates `SomeObject`
- Owns `SomeState`
```

---

# Field Documentation Rules

Document fields by semantic purpose.

For each relevant field include:

* field name,
* exact type,
* reason it exists,
* what dependency, configuration, runtime state, or reference it represents.

Example:

```md
| `_board` | `Board` | Domain object containing the current board state |
| `_inputReader` | `IInputReader` | Provides player input without coupling gameplay logic to Unity input APIs |
| `_isDragging` | `bool` | Tracks whether a drag interaction is currently active |
```

Private fields may be documented.

Do not document compiler-generated or trivial implementation details that provide no useful understanding of the class.

---

# Method Documentation Rules

For each relevant method include:

* method name,
* parameter names and types,
* return type,
* semantic purpose.

Example:

```md
| `TryMove` | `Piece piece, Cell target` | `bool` | Validates and performs a piece movement and reports whether it succeeded |
```

For asynchronous methods preserve the real return type.

Example:

```md
| `LoadLevelAsync` | `int levelId, CancellationToken token` | `UniTask<Level>` | Loads level data while supporting cancellation |
```

Do not explain line-by-line implementation here.

Implementation details belong in source code.

---

# Code Flow

Use this section to describe behavior that crosses multiple classes or systems.

Document important flows such as:

* initialization,
* user input,
* gameplay actions,
* event propagation,
* state transitions,
* scene loading,
* saving,
* dependency construction,
* UI updates.

Prefer concise call-chain notation.

Example:

```md
## Player Move

`InputController`
→ `GameController.TryMove()`
→ `Board.TryMove()`
→ `Board.OnPieceMoved`
→ `BoardPresenter.HandlePieceMoved()`
→ `BoardView.MovePiece()`
```

Add short explanations when the flow is not obvious.

---

# References and Events

When classes communicate indirectly, explicitly document the mechanism.

Examples:

```text
Direct reference
Interface
Constructor injection
Serialized reference
C# event
UnityEvent
Callback
State machine transition
Message bus
ScriptableObject channel
Service locator
Singleton
```

Do not reduce all communication to generic phrases such as "communicates with".

Record how the communication actually occurs.

---

# Update Conditions

Update `codebase.md` when changes affect any of the following:

* project architecture,
* dependency direction,
* project folder structure,
* layer responsibilities,
* new or removed major systems,
* important classes,
* class responsibilities,
* fields that represent important state or dependencies,
* public or architecturally relevant methods,
* events,
* interfaces,
* inheritance relationships,
* object ownership,
* important code flows,
* initialization flow,
* major third-party dependencies,
* framework or package versions.

Do not update `codebase.md` for changes such as:

* formatting,
* comments,
* local variable renaming,
* trivial implementation changes,
* typo fixes,
* changes that do not affect semantic understanding.

---

# Updating Existing Entries

When updating a class:

1. Inspect the current source.
2. Compare it with its existing documentation.
3. Modify only affected parts.
4. Remove fields, methods, dependencies, or relations that no longer exist.
5. Add newly introduced ones.
6. Update relevant code flows if behavior changed.

Do not append duplicate descriptions.

`codebase.md` should describe the current repository state, not its history.

Historical reasoning belongs in `decisions.md`.

---

# Accuracy Rules

Never invent:

* classes,
* methods,
* fields,
* dependencies,
* package versions,
* architectural rules,
* event relationships,
* code flows.

When uncertain, inspect the source.

Prefer exact types and exact class/member names.

---

# Documentation Principle

Document:

> what exists, what it represents, what problem it solves, and how it relates to the rest of the system.
> don't infer or guess the purpose of a class, field, or method, just describe what it actually does.

Do not document:

> step-by-step implementation details that can be obtained directly from the source code.

Class documentation explains local responsibility.

Code Flow documentation explains cross-class behavior.

---

# Completion

After updating `codebase.md`:

1. update `Last Updated`,
2. update `Last Reviewed Commit` when appropriate,
3. ensure removed code is no longer documented,
4. ensure all documented class names and member signatures match the current source.
