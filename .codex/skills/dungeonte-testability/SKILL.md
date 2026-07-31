---
name: dungeonte-testability
description: Improve or review DungeonTeam Unity testability, pure C# seams, EditMode versus PlayMode coverage, deterministic fixtures, and behavior-focused assertions. Use when designing a feature, extracting gameplay logic, adding tests, or reviewing test quality.
---

# DungeonTeam Testability

Read `Docs/AI/module-rules.md` and `Docs/AI/lifecycle.md` when the task involves ownership or async behavior.

Ask whether a rule can execute without `MonoBehaviour`, `Transform`, `GameObject`, static Unity state, or serialized scene data. Move only meaningful rules and decisions into pure C#; keep binding, rendering, and Unity lifecycle in Views/adapters.

Use EditMode for deterministic Domain/Application behavior. Use PlayMode or manual Unity proof for scene bindings, prefab wiring, input, animation, and engine lifecycle. Assert externally observable behavior, order when it matters, and cleanup/cancellation when lifecycle matters. Do not add seams or interfaces to tiny scene-bound code solely for tests.
