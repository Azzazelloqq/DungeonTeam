---
name: dungeonte-asmdef
description: Design or review DungeonTeam Unity asmdef boundaries, directional references, Editor/runtime/test splits, and compile isolation. Use when adding a feature assembly, changing references, diagnosing circular dependencies, or planning modular compilation.
---

# DungeonTeam Asmdef

Read `Docs/AI/architecture.md` and `Docs/AI/module-rules.md`. Inspect existing asmdefs before proposing changes.

Use a few meaningful assemblies, not one per folder or type. Keep references shallow and directional:

```text
Bootstrap/Composition -> Feature Presentation/Infrastructure -> Application -> Domain
Editor -> Runtime contracts only when required
Tests -> target assembly
```

Before adding an asmdef, name the responsibility, actual consumers, allowed references, and validation command. Keep Editor code in an Editor-only assembly. Never solve a circular dependency through a dumping-ground shared assembly; move the contract to the lowest owner or redesign the direction.
