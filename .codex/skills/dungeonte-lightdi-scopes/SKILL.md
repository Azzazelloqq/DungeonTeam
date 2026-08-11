---
name: dungeonte-lightdi-scopes
description: Choose and review LightDI use in DungeonTeam, including application and module scopes, explicit construction, disposal, and prohibited service-locator usage. Use when adding registrations, containers, injected services, roots, or factories.
---

# DungeonTeam LightDI Scopes

Read `Docs/AI/libraries/lightdi.md`, `Docs/AI/architecture.md`, and `Docs/AI/lifecycle.md`.

Choose the smallest valid composition mechanism:

1. For a stable application or isolated assembly-module graph backed by a LightDI container, prefer generated factories and constructor-parameter `[Inject]`.
2. Use direct constructor injection for a small graph, runtime/per-instance values, or objects that do not need container services.
3. Use an explicit root/factory for a repeatable feature or scene graph; do not create a local container per feature instance.

Call generated factories and register services only at composition boundaries. Hand-written `DiContainerProvider.Resolve<T>()` is prohibited in domain, application, gameplay, and UI; the call emitted inside a generated factory is allowed. Prefer constructor-parameter injection; field injection hides required dependencies and uses reflection. `[Inject]` does not replace or restrict the constructor, so tests should instantiate the class directly with fakes.

`CreateLocalContainer()` binds the container to its calling assembly, while a generated factory resolves against the target class assembly. Local module composition must therefore call `CreateLocalContainer()` from the same asmdef as its injected targets. Resolution checks that local container first and then falls back to globals. LightDI permits one active local container per assembly; sequential recreation works only after disposing the previous container, but DungeonTeam does not use this static assembly slot as a per-instance scope.

The container owner must dispose it with its scope and must not duplicate ownership of registered disposables. LightDI retains resolved disposable transients until container disposal and disposes tracked instances in creation order, not reverse dependency order; keep strict lifecycle ordering under an explicit root owner.
