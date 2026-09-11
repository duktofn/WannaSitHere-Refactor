---
name: commit-convention
description: Use when creating, modifying, or preparing a Git commit.
---

# Commit Convention

Use this convention whenever creating, amending, or preparing a Git commit.

## Commit Message Format

The commit message must follow:

```text
prefix(scope): brief description
```

Example:

```text
feat(Game.Core.Player): add stamina regeneration
fix(Game.View.UI): prevent button from triggering twice
refactor(Game.Core.Combat): extract damage calculation
chore(project): configure git hooks
docs(readme): update installation guide
```

## Prefixes

Choose exactly one prefix based on the primary purpose of the commit.

| Prefix | Usage |
|---|---|
| `feat` | Add a new feature, behavior, or capability |
| `fix` | Fix a bug or incorrect behavior |
| `refactor` | Restructure existing code without changing its behavior |
| `chore` | Project maintenance, configuration, setup, tooling, or file management |
| `docs` | Add or update documentation |

### Prefix Rules

- Use `feat` when introducing new functionality.
- Use `fix` when correcting existing behavior.
- Use `refactor` when changing implementation while preserving behavior.
- Use `chore` for non-functional project work such as configuration, setup, dependencies, tooling, or project structure.
- Use `docs` only for documentation changes.

Do not use a prefix merely because it sounds appropriate. Choose the prefix based on the actual purpose of the change.

## Scope

The scope identifies the part of the codebase affected by the commit.

Prefer the **namespace** or the closest meaningful module/component as the scope.

Examples:

```text
feat(Game.Core.Player): add dash ability
fix(Game.Core.Inventory): prevent duplicated items
refactor(Game.Core.Combat): extract attack strategy
feat(Game.View.UI): add item tooltip in inventory
chore(project): update package configuration
```

### Scope Rules

- Keep the scope specific and meaningful.
- Prefer namespaces, modules, systems, or components over generic scopes.
- Avoid vague scopes such as `misc`, `stuff`, or `changes`.
- Do not use the repository name as the scope.
- If a change genuinely affects multiple unrelated systems, consider splitting it into multiple commits instead of using a broad scope.

## Commit Granularity

Each commit must represent **one logical change**.

A commit should be:

- focused on a single purpose;
- independently understandable;
- small enough to review easily;
- free of unrelated changes.

Avoid combining unrelated changes such as:

```text
feat(player): add dash + fix inventory + update README
```

Instead, split them into separate commits:

```text
feat(Game.Core.Player): add dash ability
fix(Game.Core.Inventory): prevent duplicated items
docs(readme): update usage instructions
```

## Description

The description should be:

- brief;
- specific;
- written in the imperative or concise action-oriented form;
- focused on what changed, not why the entire project exists.

Prefer:

```text
feat(Game.Core.Player): add air dash
fix(Game.Camera.View): prevent camera from clipping through walls
refactor(Game.Core.Dialogue): separate dialogue data from presentation
```

Avoid:

```text
feat(player): made some changes
fix(ui): fix stuff
refactor(game): improve code
```

## Final Checklist

Before creating a commit, verify:

1. The prefix accurately represents the change.
2. The scope identifies the affected system or namespace.
3. The description clearly states the change.
4. The commit contains only one logical change.
5. Unrelated changes are split into separate commits.
