---
name: dungeonte-module-boundaries
description: Plan or review DungeonTeam Unity modular architecture, feature boundaries, root ownership, MVP gameplay, MVVM UI, composition, and dependency direction. Use before structural code, a new feature, a refactor, or an asmdef split.
---

# DungeonTeam Module Boundaries

Read `Docs/AI/architecture.md` and `Docs/AI/module-rules.md` first.

For every proposal, state feature responsibility and public contract; owner of state, side effects, resources, and cancellation; layer boundaries; dependency direction; smallest vertical slice; and non-goals.

Use MVP for gameplay presentation and MVVM for UI. Keep Domain independent of Unity and infrastructure. Prefer a small local implementation over a shared abstraction until at least two consumers have the same semantics and lifecycle. Do not create a module, root, service, or interface only for anticipated growth.
