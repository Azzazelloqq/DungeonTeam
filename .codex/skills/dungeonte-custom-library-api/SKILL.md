---
name: dungeonte-custom-library-api
description: Verify and use DungeonTeam custom library APIs without guessing. Use whenever code or a plan touches Azzazello Config, Disposable, Root, LightDI, LocalSaveSystem, Logger, MVP, MVVM, ResourceLoader, SceneSwitcher, TickHandler, Utils, or UniTask.
---

# DungeonTeam Custom Library API

1. Identify the library and read its routed `Docs/AI/libraries` document.
2. Verify the exact current API in `Library/PackageCache/com.<package>@*/`; do not rely on memory, another project, or a README from a different revision.
3. Inspect package tests/examples only when the public source is insufficient.
4. If documentation conflicts with installed source, use installed source and report the documentation drift.

| Library | Read first | Mandatory check |
| --- | --- | --- |
| Config | `libraries/config.md` | Initialization order and parser/page API |
| Disposable, Root | `libraries/roots-and-disposal.md`, `lifecycle.md` | Owner and exactly-once disposal |
| LightDI | `libraries/lightdi.md` | Scope and container disposal |
| LocalSaveSystem | `libraries/persistence.md` | V2 `SaveStore`, keys, migration |
| MVP, MVVM | `libraries/presentation.md` | Init/dispose order and presentation ownership |
| Logger, TickHandler, Utils | `libraries/runtime-services.md` | Subscription owner and runtime lifecycle |
| ResourceLoader, SceneSwitcher | `libraries/addressables.md` | Also invoke `$dungeonte-addressables-3x` |
| UniTask | `lifecycle.md` | Owner token, error boundary, stop path |

Do not create wrappers merely to hide a library. Add a project boundary only when it protects Domain/gameplay/UI from infrastructure or creates a real ownership boundary.
