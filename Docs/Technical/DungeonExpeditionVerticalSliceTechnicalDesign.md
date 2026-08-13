# DungeonTeam — Dungeon Expedition Vertical Slice Technical Design

**Статус:** READY FOR IMPLEMENTATION

**Версия:** 0.4

**Дата:** 13 августа 2026

**Product source:** `Docs/Product/DungeonExpeditionVerticalSliceGDD.md`

## 1. Responsibility and boundaries

`DungeonExpedition` владеет одной попыткой: linear route progression, ручные intents лидера, автономные решения спутников, chest state, encounter phase, camera presentation state, result и telemetry boundary.

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

Текущий playable собран из существующих модулей под `Assets/Code/Gameplay`: `DungeonRun` координирует попытку, `Team` владеет companion decision flow, а `Actors`, `Combat`, `Skills`, `EnemyAI`, `Chests` и `Rewards` сохраняют свои отдельные ответственности. Production content находится под `Assets/Content`. Отдельный монолитный `DungeonExpedition` module ради объединения этих ответственностей не создаётся.

Classic flow запускает только default product preset и не показывает seed, raw loadout IDs
или технический выбор dungeon. В Editor и Development Build отдельная runtime developer
console формирует тот же `DungeonRunStartRequest` из launch preset, seed и team selection.
Оба flow используют один application-owned `DungeonRunHost` и один `DungeonRunRoot` path;
release build не создаёт developer console.

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
- actor identity/loadout, `CombatActionDefinition`, cooldown state и минимальный deterministic selector действий спутника; role labels остаются описанием вклада, а не Domain-классом или отдельной policy;
- `DungeonIntent`: movement, target, manual `Primary`, `Active1`, one-shot hard `FOLLOW`, open chest;
- immutable events for phase, actor action, damage, chest and result.

Companion decision priority:

```text
valid actor
→ current committed action
→ active one-shot FOLLOW recall
→ current concrete emergency action, если она доступна
→ ready valid action из текущего loadout
→ formation recovery
```

Это небольшой pure ordered selector над текущими concrete candidates. Behavior Tree, utility-AI graph, универсальная role/capability platform и extension points для будущих тактических команд не создаются. Точная taxonomy ролей и контракт будущих tactical commands остаются открытыми. Selector возвращает action decision; animation, VFX, audio и NavMesh остаются Runtime reactions.

`FOLLOW` существует отдельно от selector-а тактических намерений: он отменяет только отменяемое pre-commit действие, очищает временную attack/retaliation цель, возвращает спутника к formation offset лидера и после завершения отдаёт управление обычной автономности. Это не persistent mode и не `Rally`/`Regroup`.

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
│  └─ per-companion offsets
├─ Encounter
│  ├─ trigger/exit
│  ├─ enemy spawn anchors
│  └─ authored tactical anchors
├─ Chest
│  └─ interaction/focus anchors
└─ Navigation
```

All collections are serialized and validated for nulls, duplicate actor bindings and ordering. Runtime uses direct references; no `Find`, tag lookup or string IDs.

The corridor camera follows a smoothed route tangent and leader look target. A shot anchor contributes position/look weights within its blend range. Activity focus is a time-bounded presentation request owned by the camera presenter; it never changes Domain state by itself.

## 7. Content structure

Production content использует существующие project-owned roots под `Assets/Content`: `Dungeon`, `Gameplay/Actors`, `Gameplay/Skills`, `Gameplay/Chests`, `Gameplay/Rewards`, `Configuration` и `UI`.

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
4. `DE3`: encounter, deterministic companion action selector, actor VFX slots and authored tactical anchors.
5. `DE4`: chest focus/opening, HUD/summary and result/replay.
6. `DE5`: application-flow cutover; old monolithic launch removed after replacement is green.
7. `DE6`: compile/EditMode/PlayMode/dependency audit/manual Editor/PC corridor smoke. Android build и device profiler отложены и не являются gate текущего slice.

Each milestone must keep a single runtime launch path and explicit ownership. No compatibility flag or permanent dual implementation is allowed.

## 10. Validation

- EditMode Domain: route order, chest transitions, companion selector priority/cooldown/range, outcome.
- EditMode Application: intents/events, session completion, cancellation/disposal.
- EditMode Runtime: presenter family lifecycle and authoring validation.
- PlayMode: scene/prefab wiring, manual leader `Primary`, one-shot `FOLLOW`, companion autonomy, two camera turns, formation recovery, chest once-only, encounter/result/replay.
- Editor dependency test: zero `ImportedAssets` paths from all production roots.
- Manual Unity smoke: framing before/during/after turns, focus transitions, no console errors.
- Editor/PC smoke: visible manual `Primary`, target/`Active1`/`FOLLOW`, три различимых спутника, полный encounter и replay без blocking console errors.
- Profiler запускается только при доказанном performance regression или перед возвращением к device validation; текущий slice не получает Android/device gate.
