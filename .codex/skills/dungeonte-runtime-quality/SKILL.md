---
name: dungeonte-runtime-quality
description: Review DungeonTeam Unity C# changes for clean design, ownership, minimal complexity, runtime performance risks, hot-path allocations, Update misuse, pooling candidates, and Unity API costs. Use for gameplay, UI, input, animation, tick, loading, or runtime refactors.
---

# DungeonTeam Runtime Quality

Inspect the execution context first: cold initialization, warm user action, or hot frame/tick/UI/physics path. State whether a finding is proven by code, profiler, or assumption.

Check only relevant risks: repeated Unity lookups, unnecessary `Update`, LINQ/closures/boxing/string allocation in hot paths, repeated instantiate/destroy, hierarchy traversal, UI rebuilds, reflection, retained Addressables assets, and unclear cache ownership.

Avoid needless managed allocations in proven hot paths. Use value types when the data has clear value semantics, and use `in`/`ref`/`out` for meaningful struct-copy or mutation costs; verify that this does not introduce boxing or obscure ownership. Do not force structs or by-reference parameters outside performance-sensitive code, or when they reduce API clarity, maintainability, or correctness.

Prefer correcting ownership or algorithmic shape over a cache, manager, or abstraction. Introduce pooling only for a demonstrated repeated lifecycle. Preserve readability outside hot paths. Reject silent fallbacks and speculative interfaces, flags, services, or wrappers; add an abstraction only when present consumers and removed coupling are concrete.
