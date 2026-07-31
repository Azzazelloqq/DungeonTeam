---
name: dungeonte-async-lifecycle
description: Design or review DungeonTeam Unity UniTask, cancellation, disposal, tick, scene, root, presenter, and viewmodel lifecycles. Use for async loading, timers, subscriptions, background work, scene transitions, or any operation that can outlive its owner.
---

# DungeonTeam Async Lifecycle

Read `Docs/AI/lifecycle.md` and `Docs/AI/libraries/roots-and-disposal.md`; add the relevant library document for the changed subsystem.

For each operation, identify starter, owner, cancellation token, completion/error boundary, and release/stop path. Prefer explicit calls or events; use tick only for real continuous work. Use UniTask for project async flows and pass the owner token.

Create child objects in this order: create, register with the owner disposable/root, initialize with the owner token. Do not fire-and-forget except at an explicit boundary that logs failures. Do not start async work under internal locks. Dispose subscriptions and loaded resources exactly once, before the owning root/feature is discarded.
