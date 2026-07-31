---
name: dungeonte-lightdi-scopes
description: Choose and review LightDI use in DungeonTeam, including application and module scopes, explicit construction, disposal, and prohibited service-locator usage. Use when adding registrations, containers, injected services, roots, or factories.
---

# DungeonTeam LightDI Scopes

Read `Docs/AI/libraries/lightdi.md`, `Docs/AI/architecture.md`, and `Docs/AI/lifecycle.md`.

Choose the smallest valid composition mechanism:

1. Direct constructor injection for a small or short-lived object graph.
2. Root/factory construction for a repeatable feature or scene graph.
3. LightDI container only for application scope or a real isolated module scope with several hidden services.

Register at composition boundaries only. Do not use `DiContainerProvider.Resolve<T>()` in domain, application, gameplay, or UI. LightDI allows one local container per calling assembly; do not use it as a per-instance scope for re-creatable roots. The container owner must dispose it with its scope and must not duplicate ownership of registered disposables.
