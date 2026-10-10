# AGENTS.md

Universal agent rules. This file is identical in every repo; anything specific to one project lives in `PROJECT.md`. Do not add project details here.

**Precedence when instructions conflict:** the user's current request → `PROJECT.md` → this file → skill defaults. For facts, source code and git state beat `codebase.md` and task Handoff blocks.

---

## 1. Behavioral Rules

**Tradeoff:** these rules bias toward caution over speed. For trivial requests (questions, one-line edits) use judgment and skip the working-memory steps in §2.

### Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

- State assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them. Don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop, name what's confusing, ask.
- When a tradeoff ends in a lasting architectural or technical choice, record it with `decision-log`.

### Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No flexibility or configurability that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### Surgical Changes

**Touch only what you must. Clean up only your own mess.**

- Don't "improve" adjacent code, comments, or formatting. Don't refactor what isn't broken.
- Match existing style, even if you'd do it differently.
- Unrelated dead code or bugs: tell the user, don't fix or delete. Open a task for it only if they want one.
- Remove imports, variables and functions that *your* changes made unused. Leave pre-existing dead code.

The test: every changed line traces directly to the user's request.

### Goal-Driven Execution

**Define success criteria. Loop until verified.**

- "Add validation" → write tests for invalid inputs, then make them pass.
- "Fix the bug" → write a test that reproduces it, then make it pass.
- "Refactor X" → tests pass before and after.
- For multi-step work, state a brief plan, one check per step:
  ```
  1. [Step] → verify: [check]
  2. [Step] → verify: [check]
  ```
  When the work is tracked in `task.md`, write this plan there as the task's Steps instead of only in chat.
- Report only what you actually ran. If you cannot run a check (needs the editor, a device, credentials), write "Not verified" and name what the user must check.

---

## 2. Working Memory

Agents lose context between sessions and between each other. Four artifacts carry it, each owned by a skill that defines the rules.

| Artifact | Answers | Skill |
|---|---|---|
| `task.md` (+ `tasks/archive/`) | What is in flight? Where did work stop? | `task-logging` |
| `codebase.md` (+ `codebase/`) | What does the system look like now? | `codebase` |
| `decisions.md` | Why is it built this way? | `decision-log` |
| git log | What changed, and when? | `commit-convention` |

The entry-point skill is `session-startup`.

### Start of every non-trivial session

Use `session-startup`. If your environment has no skills, do this minimum (the skill is authoritative on details):

1. Run `git branch --show-current`, `git rev-parse --short HEAD`, `git status --short`.
2. Read `task.md`. An `On-Going` task means a session was interrupted: ask the user **Continue / Pause / Cancel** before implementing. Check its Handoff block against step 1.
3. Read `codebase.md`, then `git diff --stat <Last Reviewed Commit>..HEAD`. Update the map only for structural changes.
4. Read only the Index of `decisions.md`. Open entries that relate to the change.

Create a missing file when it is first needed, never empty.

### While working

Write at boundaries, not every turn: a step starts or finishes, a decision is made, a unit of work is committed, structure changes, the session ends. Stale files are worse than none.

---

## 3. Git

- Write commit messages per `commit-convention`. Scope is the namespace or module named in `PROJECT.md`.
- Commit only when the user asks or `PROJECT.md` grants agent commits. Otherwise leave changes uncommitted and list the files in the task Handoff.
- Never push, force-push, rebase, amend or reset unless asked.
- Context files (`task.md`, `codebase.md`, `decisions.md`) are committed separately from code.

---

## 4. Project Profile

Read `PROJECT.md` before changing code. It holds what differs per project:

- Agent permissions (for example, may it commit)
- Build, test and lint commands, and which checks an agent can run itself
- Structure and dependency rules (normative: what *must* be true)
- Coding conventions and hard prohibitions
- Project-specific tools and docs

`codebase.md` records what *is* true; `PROJECT.md` records what *must* be true. Don't copy one into the other.

If `PROJECT.md` is missing, ask the user for commands and conventions instead of inventing them, and offer to create it.