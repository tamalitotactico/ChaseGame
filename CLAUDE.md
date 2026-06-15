## Approach
- Read existing files before writing. Don't re-read unless changed.
- Thorough in reasoning, concise in output.
- Skip files over 100KB unless required.
- No sycophantic openers or closing fluff.
- No emojis or em-dashes.
- Do not guess APIs, versions, flags, commit SHAs, or package names. Verify by reading code or docs before asserting.

## UI construction policy (updated 2026-06-09)
- Prefer prefab- or scene-authored UI with `[SerializeField]` fields for anything a designer/artist may tune (sprite, size, color, position, layout). Hardcoding those in C# blocks inspector customization.
- Runtime-built UI (created with `new GameObject`/`AddComponent` instead of a prefab) is still allowed when it must survive MCP scene edits, but it is a REVIEW CANDIDATE, not the default.
- MANDATORY when you build runtime UI and leave it done: (a) add a class-header comment token `RUNTIME-UI-REVIEW: <what to expose / convert to prefab>` (greppable), and (b) add an entry to `docs/RuntimeUIReview.md`. Expose its tunables through a serialized config on a scene/prefab object (pattern: the ghost pointer's sprite/size/colors live on `GhostModeController`, not hardcoded in `GhostBodyPointer`).