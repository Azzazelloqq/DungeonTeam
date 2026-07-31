---
name: dungeonte-workflow-guard
description: Enforce DungeonTeam collaboration rules for feature work, bugs, refactors, architecture decisions, reviews, and any task that could change project code. Use before planning or editing when scope, ownership, validation, or explicit user authorization matters.
---

# DungeonTeam Workflow Guard

1. Read the repository `AGENTS.md` and only the task-routed `Docs/AI` files.
2. Inspect target code, nearby tests, dirty changes, and real Unity assets when relevant.
3. For a feature, bug, refactor, or architecture task: discuss direction first, then give an ordered plan with ownership, file targets, proof level, and non-goals.
4. Edit only after an explicit implementation command.
5. After edits, report changed files, validation performed, and unverified Unity paths.

Do not touch unrelated dirty files, generated files, or `.meta` files unless explicitly requested. If a small task crosses an ownership boundary, stop and re-plan instead of adding flags or compatibility branches.
