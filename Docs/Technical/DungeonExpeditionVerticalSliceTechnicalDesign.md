# DungeonTeam — Dungeon Expedition Vertical Slice Technical Design

**Статус:** READY FOR IMPLEMENTATION

**Дата:** 1 августа 2026

**Product source:** `Docs/Product/DungeonExpeditionVerticalSliceGDD.md`

## 1. Responsibility and boundaries

`DungeonExpedition` владеет одной попыткой: linear route progression, party intent, chest state, encounter phase, auto-cast decisions, camera presentation state, result и telemetry boundary.

Feature не владеет application navigation, imported package storage, inventory/economy, procgen и generic gameplay frameworks.

Минимальный public contract принадлежит `DungeonExpedition.Application`:

```text
IDungeonExpeditionSessionFactory.Start(settings, ownerToken) -> session
IDungeonExpeditionSession.WaitForResultAsync(waitToken) -> result
IDungeonExpeditionSession.StopAsync(reason, stopToken) -> result
IDungeonExpeditionSession.Dispose()
```

Settings/result/events содержат только immutable data и не раскрывают Unity objects, root, presenter, Addressables handle или concrete Runtime.

## 2. Ownership

```text
GameBootstrapper
└─ ApplicationRoot
   └─ ApplicationFlowRoot
      └─ active DungeonExpeditionSession
         └─ DungeonExpeditionRoot
            ├─ coordinator/domain state
            ├─ presentation lease
            ├─ DungeonRunPresenter
            │  ├─ ActorPresenter x active actor
            │  └─ ChestPresenter
            ├─ DungeonCameraPresenter
            ├─ DungeonHudViewModel
            ├─ input/tick/navigation adapters
            └─ telemetry session
```

`ApplicationFlowRoot` ждёт controlled stop и освобождает session перед публикацией следующего child graph. `DungeonExpeditionRoot` освобождает children, subscriptions, telemetry и presentation lease. LightDI используется только в application scope и не владеет feature graph.

## 3. Layers and assemblies

```text
Assets/Game/Features/DungeonExpedition/
  Domain/         Game.Gameplay.DungeonExpedition.Domain
  Application/    Game.Gameplay.DungeonExpedition.Application
  Runtime/        Game.Gameplay.DungeonExpedition.Runtime
  Infrastructure/ Game.Gameplay.DungeonExpedition.Infrastructure
  Tests/
```

Allowed direction:

```text
Runtime -> Application -> Domain
Infrastructure -> Application -> Domain
Architecture.Composition -> Runtime + Infrastructure + Application
ApplicationFlow -> Application
```

`Domain` and `Infrastructure` use `noEngineReferences: true`. Runtime may reference Unity, Root, UniTask, MVP, MVVM, Input System, UGUI and AI Navigation only when their API is used. No Runtime↔Infrastructure reference.

## 4. Domain model

The first slice uses closed concrete concepts:

- `DungeonRunPhase`: `Entering`, `Exploring`, `ChestFocus`, `Encounter`, `Continuing`, `Completed`, `Failed`;
- ordered `RouteCheckpoint` progression;
- `ChestState`: `Locked`, `Available`, `Opening`, `Opened`;
- `ActorRole`, `CombatActionDefinition`, cooldown state and deterministic `AutoCastPolicy`;
- `DungeonIntent`: movement, dodge, target, command, leader ability, open chest;
- immutable events for phase, actor action, damage, chest and result.

Auto-cast priority:

```text
valid actor
→ emergency role response
→ active squad command
→ ready role skill with valid target
→ ready basic attack
→ formation recovery
```

The policy returns an action decision; animation, VFX, audio and NavMesh remain Runtime reactions.

## 5. Presentation families

Gameplay uses MVP and each independent node has `Base/` contracts:

```text
Presentation/Gameplay/DungeonRun/
  Base/DungeonRunViewBase.cs
  Base/DungeonRunModelBase.cs
  Base/DungeonRunPresenterBase.cs
  DungeonRunView.cs
  DungeonRunModel.cs
  DungeonRunPresenter.cs

Presentation/Gameplay/Actor/
Presentation/Gameplay/Chest/
Presentation/Gameplay/DungeonCamera/
```

`DungeonRunPresenter` owns actor/chest/camera child families. Views expose serialized bindings and rendering operations only. They do not choose targets, skills, route progress or child presenters.

HUD and terminal summary use MVVM families:

```text
Presentation/UI/DungeonHud/{Base, concrete Model/ViewModel/View}
Presentation/UI/DungeonRunSummary/{Base, concrete Model/ViewModel/View}
```

View binds to an already-created ViewModel. ViewModel never references View. World-space health bars and passive markers remain Actor View bindings, not separate ViewModels.

## 6. Level authoring

One project-owned `DungeonCorridorStage.prefab` contains:

```text
DungeonCorridorStageView
├─ Geometry
├─ Route
│  └─ ordered checkpoint transforms
├─ CameraShots
│  └─ shot anchors: camera offset, look-ahead, activation/blend range
├─ PartyFormation
│  └─ role offsets
├─ Encounter
│  ├─ trigger/exit
│  ├─ enemy spawn anchors
│  └─ role-specific tactical anchors
├─ Chest
│  └─ interaction/focus anchors
└─ Navigation
```

All collections are serialized and validated for nulls, duplicate roles and ordering. Runtime uses direct references; no `Find`, tag lookup or string IDs.

The corridor camera follows a smoothed route tangent and leader look target. A shot anchor contributes position/look weights within its blend range. Activity focus is a time-bounded presentation request owned by the camera presenter; it never changes Domain state by itself.

## 7. Content structure

```text
Assets/Game/Content/DungeonExpedition/
  Levels/CrystalPassage/{Models,Materials,Textures,Prefabs,Lighting}
  Characters/Heroes/{Leader,Protector,DamageCaster,Support}/...
  Characters/Enemies/{Goblin,Minotaur,Skeleton}/...
  Interactables/Chests/...
  Shared/{Materials,Textures,VFX}
  UI/{Sprites,Fonts,Prefabs}
  Definitions/
```

Imported assets are migrated through Unity Editor tooling. Reusable project-owned prefabs are rebuilt against migrated models/materials. An Editor validation test calls `AssetDatabase.GetDependencies` for production scenes, prefabs and definitions and fails on any path under `Assets/ImportedAssets`.

No new runtime Addressables code is introduced until generated keys exist.

## 8. Navigation and runtime quality

- Route and combat decisions are event/fixed-step driven.
- Per-frame Unity work is limited to input sampling, actor interpolation and camera `LateUpdate` through one owned adapter each.
- No LINQ/string formatting/material creation/hierarchy search in hot paths.
- No pooling until measured churn justifies it.
- Navigation reports value observations to Application/Domain; no `NavMeshAgent` crosses inward.
- Actors use project-owned prefabs and cached bindings; production actors are never primitives.

## 9. Vertical milestones

1. `DE0`: normative docs, Domain/Application asmdefs and behavior tests.
2. `DE1`: authored corridor bindings, route progression, party follow and camera turn blend.
3. `DE2`: project-owned party/enemy/chest content with zero imported dependencies.
4. `DE3`: encounter, deterministic auto-cast, actor VFX slots and role tactical anchors.
5. `DE4`: chest focus/opening, HUD/summary and result/replay.
6. `DE5`: application-flow cutover; old monolithic launch removed after replacement is green.
7. `DE6`: compile/EditMode/PlayMode/dependency audit/manual corridor smoke/Android/profiler.

Each milestone must keep a single runtime launch path and explicit ownership. No compatibility flag or permanent dual implementation is allowed.

## 10. Validation

- EditMode Domain: route order, chest transitions, auto-cast priority/cooldown/range, outcome.
- EditMode Application: intents/events, session completion, cancellation/disposal.
- EditMode Runtime: presenter family lifecycle and authoring validation.
- PlayMode: scene/prefab wiring, two camera turns, formation recovery, chest once-only, encounter/result/replay.
- Editor dependency test: zero `ImportedAssets` paths from all production roots.
- Manual Unity smoke: framing before/during/after turns, focus transitions, no console errors.
- Android: landscape build and device input/focus recovery.
- Profiler: 30 FPS target on selected device; report CPU/frame, GC/frame, batches and triangles separately.

