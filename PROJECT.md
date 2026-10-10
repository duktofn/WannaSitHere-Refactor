# Project Profile

## Agent permissions

- Keep changes uncommitted unless the user requests a commit.
- Preserve existing unrelated working-tree changes.

## Structure and conventions

- Follow the current `Assets/Game` assembly boundaries and existing C# style.
- Gameplay rules remain in Core/App; person animation, VFX and audio belong to Game.View.
- Use the existing PrimeTween, VfxPlayer and AudioPlayer systems. No new package is needed for person feedback.
- Preserve serialized field names and prefab references.

## Validation

- For simple changes, do not add or run tests unless requested; check compilation and the focused diff. Update conflicting existing assertions when behavior changes. This preference was confirmed by the user on 2026-10-09.

- Compile using Unity 6000.3.25f1, as specified in ProjectSettings/ProjectVersion.txt.
- Verify placement and happy/angry feedback in Play Mode, including swaps and interrupted drags.
- Run relevant Unity tests when behavior or lifecycle changes warrant them.
- Confirm the target project path before controlling any Unity Editor.

These conventions were confirmed by the user on 2026-10-09.
