# Agent Instructions

This file defines the global behavior, workflow, and repository-level rules that all coding agents must follow.

Detailed procedures should remain in their corresponding skills. This file should define **when** those procedures apply and the global constraints under which the agent operates.

---

# 1. Interaction Mode

The agent must distinguish between **discussion** and **implementation**.

## Discussion / Question Mode

When the user:

* asks a question;
* asks for an explanation;
* asks for an opinion or evaluation;
* asks how something works;
* asks for architectural or design advice;
* asks what should be changed;
* asks for possible approaches;
* asks to review or analyze code without explicitly requesting modification;

the agent must **only answer the question**.

Do not:

* modify files;
* write implementation code;
* create new files;
* apply patches;
* refactor the repository;
* perform unrelated implementation work.

Code snippets may be included only when they are useful for explaining the answer, and should remain illustrative rather than being applied to the repository.

### Examples

User:

```text
Should this system use events or direct references?
```

Answer the architectural question only.

User:

```text
Why is this class coupled to the View layer?
```

Explain the coupling only.

User:

```text
How should I refactor this?
```

Describe the recommended refactor.

Do not perform it.

---

## Implementation Mode

Modify or create code only when the user explicitly asks for implementation.

Examples of implementation requests:

```text
Implement this.

Code this.

Fix this bug.

Refactor this.

Apply the change.

Create this system.

Add this feature.

Update the code.

Remove this dependency.

Write the implementation.
```

Once implementation is explicitly requested, the agent may:

* inspect relevant source files;
* modify existing files;
* create necessary files;
* remove obsolete code;
* run relevant verification;
* update repository context.

Do not require the user to repeat implementation permission once it has already been clearly given for the current task.

---

# 2. Source of Truth

The repository source code is the ultimate source of truth.

Repository context files are navigation, memory, and reasoning aids.

If documentation or context conflicts with the source:

1. trust the source code;
2. investigate the discrepancy;
3. update stale context when appropriate.

Never modify correct source code merely to make it match outdated context documentation.

---

# 3. Repository Context

Persistent repository context may include:

```text
codebase.md
decisions.md
```

and other project-specific context files when present.

These files exist to reduce unnecessary repository rescanning and preserve information across agent sessions.

---

# 4. Session Startup

Before performing substantial repository work:

1. Locate and read `codebase.md`.
2. If `codebase.md` does not exist, use the `codebase` skill to inspect the repository and create it.
3. Read relevant entries from `decisions.md` when the task involves architecture, dependencies, APIs, design choices, or previously established technical direction.
4. Inspect additional context only when relevant to the current task.

Do not blindly read the entire repository when existing context can identify the relevant systems and files.

Use progressive context loading:

```text
repository context
→ relevant subsystem
→ relevant source files
→ implementation details
```

---

# 5. Codebase Context

Use the `codebase` skill when creating or maintaining `codebase.md`.

`codebase.md` describes the **current state** of the repository.

It may contain:

* project overview;
* tech stack and versions;
* project structure;
* architecture;
* layers and modules;
* class responsibilities;
* fields and their types/purposes;
* methods, parameters, return types, and purposes;
* events;
* dependencies;
* class relationships;
* important code flows.

Do not use `codebase.md` as a historical changelog.

---

# 6. Technical Decisions

Use the `decision-log` skill when an important technical or architectural decision is made.

A decision should normally be recorded when:

1. multiple reasonable alternatives exist;
2. one option is deliberately selected;
3. the choice can affect future architecture, implementation, APIs, dependencies, workflow, or maintainability.

Examples:

* direct references vs events;
* dependency injection strategy;
* state machine architecture;
* package or framework selection;
* data ownership;
* persistence architecture;
* public API design;
* communication between layers.

Do not log routine implementation details as decisions.

Before making a significant architectural change, inspect relevant existing decisions.

Do not silently contradict an active accepted decision.

If a previous decision is intentionally replaced, record the new decision and supersede the old one according to the `decision-log` skill.

---

# 7. Context Maintenance

Repository context must remain synchronized with meaningful source changes.

After completing a non-trivial implementation task, determine whether repository context was affected.

## Update `codebase.md` when changes affect:

* architecture;
* project structure;
* modules or layers;
* class responsibilities;
* important fields;
* methods or APIs;
* interfaces;
* inheritance;
* dependencies;
* events;
* ownership;
* communication mechanisms;
* important runtime code flows;
* technology or package versions.

Use the `codebase` skill for these updates.

## Update `decisions.md` when:

* a meaningful technical decision was made;
* an existing architectural decision was replaced;
* a previously undocumented but clearly established decision was discovered.

Use the `decision-log` skill.

## Do not perform context maintenance for:

* formatting-only changes;
* typo fixes;
* comments;
* trivial local implementation changes that do not affect documented semantics.

Remove stale context when documented classes, APIs, dependencies, or flows no longer exist.

---

# 8. Scope Discipline

Only modify code required by the current task.

Do not perform opportunistic changes unrelated to the user's request.

Avoid:

* unrelated refactors;
* renaming unrelated symbols;
* reformatting unrelated files;
* replacing working systems without a task-related reason;
* introducing new abstractions merely because they appear cleaner.

If an unrelated issue is discovered, mention it rather than silently expanding the scope.

---

# 9. Existing Architecture

Understand the existing architecture before introducing new patterns.

Prefer consistency with established project architecture unless:

* the existing approach causes the problem being solved;
* a new requirement cannot reasonably fit the current design;
* the user explicitly requests architectural changes.

Do not introduce a new pattern merely because it is theoretically preferable.

---

# 10. Implementation Principles

When implementation is requested:

1. understand the relevant code path first;
2. identify existing abstractions and conventions;
3. make the smallest coherent change that solves the problem;
4. preserve existing behavior unless behavior change is intentional;
5. avoid unnecessary dependencies;
6. avoid speculative abstractions;
7. keep responsibilities clear;
8. verify affected call sites and consumers.

Prefer solving the actual problem over maximizing abstraction.

---

# 11. Verification

After modifying code, perform verification appropriate to the change when tooling is available.

This may include:

* compilation;
* tests;
* static analysis;
* relevant project validation;
* inspection of affected call sites.

Do not claim a test or validation was performed unless it was actually performed.

If verification cannot be performed, state that clearly.

---

# 12. Git Commits

When creating, modifying, or preparing a Git commit, use the `commit-convention` skill.

A commit should represent one logical change.

Do not include unrelated modifications in the same commit.

When an architectural decision is relevant to a commit, reference its decision ID when useful.

Do not create a commit unless the user explicitly asks for one or the active workflow explicitly requires it.

---

# 13. Skill Usage

Skills contain specialized procedures.

Use a skill when the current task falls within its responsibility.

Current core skills include:

```text
codebase
decision-log
commit-convention
```

Global repository behavior belongs in `AGENTS.md`.

Detailed task-specific procedures belong in skills.

Avoid duplicating full skill instructions inside this file.

---

# 14. Context Efficiency

Do not load context indiscriminately.

Prefer the smallest amount of information required to correctly perform the task.

Use:

```text
high-level context
→ subsystem context
→ relevant classes
→ relevant implementation
```

rather than:

```text
scan entire repository
→ reconstruct everything
→ begin task
```

When existing repository context is stale or insufficient, inspect the source as needed.

Accuracy takes precedence over context efficiency.

---

# 15. Communication

Keep responses focused on the user's actual request.

When the user asks a question:

> Answer the question.

When the user asks for analysis:

> Analyze and explain.

When the user asks for implementation:

> Implement the requested change.

Do not turn an informational request into an implementation task.

Do not modify code merely because a possible improvement was identified during discussion.

---

# 16. Core Rules

The following rules take precedence during normal repository work:

1. **Questions require answers, not code changes.**
2. **Code is modified only when implementation is explicitly requested.**
3. **Source code is the ultimate source of truth.**
4. **Understand relevant context before making changes.**
5. **Keep repository context synchronized with meaningful code changes.**
6. **Preserve important technical reasoning through the decision log.**
7. **Do not expand the scope beyond the requested task without a concrete reason.**
8. **Use specialized skills for their respective procedures.**
