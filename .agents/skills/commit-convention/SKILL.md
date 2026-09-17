---

name: commit-convention
description: Use when creating, modifying, or preparing a Git commit.
---------------------------------------------------------------------

# Commit Convention

Use this convention whenever creating, amending, or preparing a Git commit.

## Commit Message Format

Every commit must contain:

1. a concise commit title;
2. a descriptive commit body when the change is non-trivial.

The title must follow:

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

---

# Commit Body

For non-trivial commits, include a body that explains the semantic meaning of the change.

The purpose of the body is to allow a developer or agent to understand the commit without having to reconstruct its intent entirely from the Git diff.

Use:

```text
Summary:
<high-level description>

Changes:
- <meaningful change>
- <meaningful change>

Reason:
<why this change was needed>

Impact:
- <effect on existing architecture, APIs, behavior, or consumers>
```

Optional sections:

```text
Testing:
- <how the change was verified>

Related:
- <decision, issue, task, or other relevant reference>
```

Do not include sections that provide no useful information.

---

## Summary

Describe the overall change in one or two sentences.

The summary should explain what the commit accomplishes at a higher level than the Git diff.

Example:

```text
Summary:
Move currency persistence responsibility from the UI layer into CurrencyService.
```

Avoid simply repeating the commit title.

---

## Changes

Describe the meaningful technical changes introduced by the commit.

Focus on:

* behavior;
* responsibilities;
* APIs;
* dependencies;
* architecture;
* data flow;
* events;
* ownership;
* important implementation structure.

Example:

```text
Changes:
- Add ICurrencyRepository abstraction.
- Move persistence logic from CurrencyView to CurrencyService.
- Publish currency updates through OnAmountChanged.
```

Do not use the commit body as a file-change listing.

Avoid:

```text
Changes:
- Modified CurrencyService.cs.
- Modified CurrencyView.cs.
- Added 25 lines.
```

The Git diff already provides this information.

---

## Reason

Explain why the change was necessary.

The reason should describe the problem, limitation, bug, architectural issue, or requirement that caused the change.

Example:

```text
Reason:
CurrencyView previously handled both presentation and persistence,
causing the View layer to own application-level state management.
```

Avoid vague reasoning such as:

```text
Reason:
Cleaner code.
Better architecture.
Improvement.
```

Explain the concrete problem being solved.

---

## Impact

Describe how the change affects the rest of the codebase.

Relevant impact may include:

* changed ownership;
* changed dependency direction;
* public API changes;
* event flow changes;
* behavior changes;
* consumers that must adapt;
* backwards compatibility;
* breaking changes;
* new assumptions or constraints.

Example:

```text
Impact:
- CurrencyView no longer depends on persistence infrastructure.
- CurrencyService becomes the owner of currency persistence.
- Existing consumers of CurrencyService do not require changes.
```

If there is no meaningful external impact, the section may be omitted.

---

## Testing

When relevant, record how the change was verified.

Examples:

```text
Testing:
- Added EditMode tests for TrySpend.
- Verified insufficient currency returns false.
- Verified successful purchases update the displayed balance.
```

Or:

```text
Testing:
- Verified manually in Unity Play Mode.
```

Never claim tests were performed when they were not.

---

## Related

Use this section to reference related project context.

Examples:

```text
Related:
- DEC-012
- Issue #45
```

When the commit implements or follows an architectural decision recorded in `decisions.md`, reference the corresponding decision ID.

Do not duplicate the entire decision reasoning inside the commit.

---

# Prefixes

Choose exactly one prefix based on the primary purpose of the commit.

| Prefix     | Usage                                                                  |
| ---------- | ---------------------------------------------------------------------- |
| `feat`     | Add a new feature, behavior, or capability                             |
| `fix`      | Fix a bug or incorrect behavior                                        |
| `refactor` | Restructure existing code without changing its behavior                |
| `chore`    | Project maintenance, configuration, setup, tooling, or file management |
| `docs`     | Add or update documentation                                            |

## Prefix Rules

* Use `feat` when introducing new functionality.
* Use `fix` when correcting existing behavior.
* Use `refactor` when changing implementation while preserving behavior.
* Use `chore` for non-functional project work such as configuration, setup, dependencies, tooling, or project structure.
* Use `docs` only for documentation changes.

Do not use a prefix merely because it sounds appropriate.

Choose the prefix based on the actual purpose of the change.

---

# Scope

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

## Scope Rules

* Keep the scope specific and meaningful.
* Prefer namespaces, modules, systems, or components over generic scopes.
* Avoid vague scopes such as `misc`, `stuff`, or `changes`.
* Do not use the repository name as the scope.
* If a change genuinely affects multiple unrelated systems, consider splitting it into multiple commits instead of using a broad scope.

---

# Commit Granularity

Each commit must represent **one logical change**.

A commit should be:

* focused on a single purpose;
* independently understandable;
* small enough to review easily;
* free of unrelated changes.

Avoid:

```text
feat(player): add dash + fix inventory + update README
```

Instead:

```text
feat(Game.Core.Player): add dash ability
fix(Game.Core.Inventory): prevent duplicated items
docs(readme): update usage instructions
```

The commit body must describe only the logical change represented by that commit.

---

# Description

The title description should be:

* brief;
* specific;
* written in imperative or concise action-oriented form;
* focused on what changed.

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

---

# Small Commits

A commit body is optional for trivial and self-explanatory changes.

Example:

```text
docs(readme): fix installation typo
```

Do not create meaningless bodies such as:

```text
Summary:
Fix typo.

Changes:
- Fix typo.

Reason:
There was a typo.
```

Use the body when additional context would help a future developer or agent understand the change.

---

# Example

```text
refactor(Game.Core.Currency): move persistence into currency service

Summary:
Move currency persistence responsibility from the presentation layer into CurrencyService.

Changes:
- Add ICurrencyRepository as a CurrencyService dependency.
- Move load and save operations out of CurrencyView.
- Keep UI synchronization through OnAmountChanged.

Reason:
CurrencyView previously handled persistence in addition to presentation,
which mixed application state management with UI responsibilities.

Impact:
- CurrencyService now owns currency state and persistence.
- CurrencyView only displays state and reacts to currency events.
- Existing currency consumers continue using the same public API.

Testing:
- Verified currency loading on game startup.
- Verified purchases persist after restarting Play Mode.

Related:
- DEC-006
```

---

# Final Checklist

Before creating a commit, verify:

1. The prefix accurately represents the change.
2. The scope identifies the affected system or namespace.
3. The title clearly states the change.
4. The commit contains only one logical change.
5. Unrelated changes are split into separate commits.
6. The body explains meaningful changes rather than repeating the Git diff.
7. The reason describes the actual problem being solved.
8. Architectural or API impact is documented when relevant.
9. Testing information is accurate.
10. Relevant entries from `decisions.md` are referenced when applicable.
