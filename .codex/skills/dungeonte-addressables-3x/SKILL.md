---
name: dungeonte-addressables-3x
description: Design or review DungeonTeam Addressables 3.1.0 loading, instantiation, scene transitions, handles, release ownership, AssetReference, and generated keys. Use before writing or reviewing any Addressables, ResourceLoader, SceneSwitcher, asset, prefab, or scene-loading code.
---

# DungeonTeam Addressables 3.1.0

Read `Docs/AI/libraries/addressables.md`, `Docs/AI/lifecycle.md`, and [references/addressables-3.1.md](references/addressables-3.1.md). Verify the installed package version before applying API advice. If it is not `3.1.0`, do not use the reference; update this skill from the installed source first.

Keep direct Addressables calls inside infrastructure/composition. Runtime consumers receive a project contract. Use only generated resource IDs after the generator is implemented.

For every operation, record the handle owner before starting it. Await async operations, inspect failure, and release the matching handle on the owner lifecycle. Match instantiation with `ReleaseInstance`; match `AssetReference` loading with its own release methods. Keep a scene handle until unloading. Do not use `WaitForCompletion` in production runtime or scene flows.
