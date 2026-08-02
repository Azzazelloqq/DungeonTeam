# DungeonTeam — SquadCombat Stage 0 Technical Design

## Статус, версия и иерархия

**Статус:** **READY для реализации по milestone-gates M0 → M9**. READY разрешает отдельной implementation-задаче начать только M0; переход к следующему milestone разрешён лишь после validation и review текущего. Этот документ не является утверждением Stage 1+.

**Версия:** 1.0

**Дата:** 31 июля 2026

**Владелец технического решения:** engineering

**Продуктовый владелец tuning/content/control decisions:** product/game design

Иерархия источников:

1. `Docs/Product/ProductDirection.md`;
2. `Docs/Product/ExperienceDirection.md`;
3. `Docs/Product/ProductValidationPlan.md`;
4. `Docs/Product/CoreCombatPrototypeGDD.md`;
5. `Docs/Technical/ProjectArchitectureTechnicalDesign.md`;
6. этот документ;
7. implementation backlog и код.

При расхождении документации custom package с исходником совпадающего `Library/PackageCache` источником API является исходник. При изменении GDD contract, module ownership, public session contract или M0 gate выпускается новая версия этого design до продолжения реализации.

---

## 1. Scope, non-goals и change control

### 1.1. Назначение

Документ фиксирует исполнимый technical design одной Stage 0 feature `SquadCombat`: один повторно создаваемый combat attempt в одной graybox arena, лидер, до трёх автономных спутников, враги, два control variants, HUD, test harness, локальная telemetry и доказательства на Android.

`Gameplay` — Area. `SquadCombat` — один вертикальный Module. Это не общий combat framework и не набор горизонтальных managers.

### 1.2. В scope

- точный owner запуска, session и `SquadCombatRoot`;
- минимальный Application contract;
- pure C# Domain/Application rules;
- Runtime Unity adapters, world MVP и feature-owned HUD MVVM;
- frozen immutable tuning/encounter/composition data;
- deterministic rule/order boundaries, seed и clocks;
- локальная JSON Lines telemetry и test harness;
- lifecycle `start/pause/resume/reset/stop/focus loss`;
- M0–M9 с обязательным gate после каждого milestone;
- EditMode, lifecycle, compile, PlayMode/manual Unity, Android и Profiler proof.

### 1.3. Non-goals

- Stage 1 expedition, rooms, route, risk/return, hub, roster/meta, loot/economy;
- production ability/status/AI/combat platform;
- production GameFlow/menu/modes/navigation;
- Addressables, save, backend, analytics SDK, cloud/session persistence;
- production art/audio/content pipeline;
- global combat/input/AI managers, service locator и scene singleton;
- generic feature runner, shared gameplay domain или shared utility dumping ground;
- ручное управление спутниками, hero switching и дополнительные action bars;
- pooling, ECS/Jobs/Burst или custom tick framework без profiler evidence;
- empty future assemblies и extension-point code без текущего consumer.

### 1.4. Стабильное и сменное

Стабильны responsibilities, ownership, public attempt/session boundary, action/event data seams, deterministic ordering, intent pipeline и observable causality.

Сменны immutable policies/data: camera, movement/attack/dodge/ability tuning, target scoring, role thresholds/actions, command variant policy, encounter phases/spawns, presentation proxy, telemetry sampling cadence. Замена этих данных не меняет actor logic по scattered `if variant`.

### 1.5. Change control

До comparative test замораживаются GDD version, design version, build ID, schema version, variant definitions, tuning/config ID, encounter ID/seed, composition, target device/profile и known issues. Любое изменение central hypothesis, party size, direct-control model, auto attack contract, A/B composition, input budget, encounter purpose или owner/lifecycle требует GDD review и новой версии design. Обычный tuning получает новый `config_id`.

---

## 2. Current baseline, assumptions и API proof

### 2.1. Проверенный baseline

- Unity `6000.7.0a3`.
- `GameBootstrapper : MonoBehaviour` — единственная Unity entry point и project-specific lifecycle adapter; `Awake` запускает и наблюдает `ApplicationRoot.InitializeAsync`.
- `GameBootstrapper` создаёт один `DiContainerFactory.CreateGlobalContainer()`, logger и `ApplicationRoot`.
- `ApplicationRoot : Root` владеет global container; gameplay state отсутствует.
- В `Assets/Game` есть только `Bootstrap` и `Architecture/Composition`.
- Game assemblies: `Game.Bootstrap` и `Game.Architecture.Composition`.
- `SquadCombat`, gameplay scenes/prefabs/input actions/tests отсутствуют.
- Установлены Root, Disposable, LightDI, MVP/MVVM, Config, UniTask, Input System `1.19.0`, AI Navigation `2.0.14`.
- `Packages/manifest.json` текущего worktree уже изменён вне этой задачи; design не меняет package files.

### 2.2. PackageCache proof и drift

В текущем worktree и в `98b9` cache отсутствует. `98b9` имеет тот же `packages-lock.json`, но manifest отличается отсутствием только `com.coplaydev.unity-mcp`; поэтому он не дал исходников.

В `C:\UnityProjects\DungeonTeam\Library\PackageCache` общий manifest/lock отличается от текущего, но для Root, Disposable, LightDI, MVP, MVVM и Config версии и полные git hashes в обоих locks совпадают, а cache directories имеют соответствующий hash-prefix. Для этих package исходники признаны точным API proof:

- `Root` не generic, разрешает один `InitializeAsync(CancellationToken)`, отменяет свой token до `OnDispose`, `Dispose` идемпотентен после завершения;
- `Root.InitializeAsync` при ошибке переводит root в `InitializationFailed`, отменяет token, но не вызывает `OnDispose` автоматически: owner обязан вызвать `Dispose` для partial graph;
- `RootBehaviour` отсутствует: project-specific `GameBootstrapper : MonoBehaviour` создаёт root, наблюдает `InitializeAsync` и освобождает его в `OnDestroy`;
- `DisposableBase` отменяет свой token после managed/composite disposal, поэтому не заменяет Root для feature shutdown semantics;
- `CompositeDisposable` не имеет remove и освобождает synchronous items в порядке добавления;
- LightDI `CreateContainer()` является global alias; local container один на calling assembly; registered disposables освобождаются в порядке регистрации;
- `DiContainerProvider.Resolve*` помечен obsolete и проектом запрещён;
- MVP выбирает ровно один `Initialize`/`InitializeAsync`; package Presenter владеет View/Model, а child presenters с order-sensitive lifetime должны освобождаться явно в `OnDispose`;
- MVVM runtime type называется `ViewMonoBehavior<T>`; `ViewModelBase` владеет model; reactive `Subscription<T>` disposable; async hooks используют `ValueTask` даже при UniTask-facing initialize;
- Config `IConfig.InitializeAsync` возвращает `Task`, не `UniTask`; `IConfigPage` mutable по API и требует snapshot boundary.

Текущий lock ожидает UniTask hash `ceac8d…`, но доступный cache directory содержит другой commit `dc216d…`. Точный исходник текущего UniTask не подтверждён. Design использует только базовые `UniTask<T>`/`await` и `CancellationToken`, без version-specific extensions. Перед первым M0 compile Unity должна materialize cache с lock hash; несовместимость — `HOLD` M0, но не blocker архитектурного READY.

### 2.3. Assumptions

- Stage 0 arena уже загружена вместе с bootstrap scene либо входит в одну bootstrap/test scene; scene switching и Addressables не нужны.
- Одновременно существует не более одной session, потому что она владеет единственным набором scene bindings.
- Full deterministic replay input stream не нужен; `Replay same` означает новый attempt с теми же frozen settings и seed.
- Physics/NavMesh не обещают bit-exact determinism; deterministic contract относится к pure decisions, action ordering, phase rules и seed-controlled content.
- Открытые числовые/content decisions находятся в immutable data и не блокируют layer/contract design.

---

## 3. Responsibility, inputs, outputs и public contract

### 3.1. Responsibility

`SquadCombat` отвечает за одну активную попытку:

- authoritative combat/encounter state;
- leader, companion, enemy and threat rules;
- player intents и A/B squad command resolution;
- deterministic action/damage/outcome resolution;
- world/HUD presentation;
- feature-lifetime resources, subscriptions и cancellation;
- observable events, result summary и local telemetry session.

Не отвечает за application bootstrap, выбор будущего mode, scene navigation, Stage 1 expedition/meta и production analytics.

### 3.2. Входы

На start поступает один immutable `SquadCombatAttemptSettings` snapshot:

- `AttemptIdentity`: `session_id`, anonymous `participant_id`, `build_id`, `schema_version`;
- `Variant`: `A_Rally` или `B_Context`;
- `config_id` и полный `SquadCombatTuningSnapshot`;
- `encounter_id`, `seed` и `EncounterDefinitionSnapshot`;
- `CompositionSnapshot` лидера и до трёх role slots;
- `DeviceProfileSnapshot`;
- test flags, не меняющие боевые правила: blind overlay, observer mode, validity notes.

Во время session поступают:

- player intents: move, target selection, dodge, ability, command;
- lifecycle intents: manual pause, focus loss/gain, resume, stop;
- Runtime observations: navigation/path status, contact/animation resolution, camera visibility и performance samples.

### 3.3. Выходы

- ordered `SquadCombatEvent` stream: intents, targets, actions, telegraphs, damage, companion decisions, phases, lifecycle, performance, error/recovery;
- immutable `SquadCombatAttemptResult`;
- presentation state snapshots для world/HUD;
- local telemetry artifact/health status;
- explicit exceptions до ownership transfer и explicit technical invalidation после start.

### 3.4. Minimal public Application contract

Normative M0 surface:

```text
ISquadCombatSessionFactory
  Start(SquadCombatAttemptSettings settings, CancellationToken ownerToken)
    -> ISquadCombatSession

ISquadCombatSession : IDisposable
  State: SquadCombatSessionState
  Events: IReadOnlySquadCombatEventStream
  RequestPause(SquadCombatPauseReason reason)
  RequestResume(SquadCombatPauseReason reason)
  WaitForResultAsync(CancellationToken waitToken)
    -> UniTask<SquadCombatAttemptResult>
  StopAsync(SquadCombatStopReason reason, CancellationToken stopToken)
    -> UniTask<SquadCombatAttemptResult>
```

Семантика:

- `Start` синхронный: Stage 0 bindings уже загружены, resource loading отсутствует. Если появится реальная async load boundary, contract меняется отдельной design revision, а не скрытым fire-and-forget.
- Factory владеет partial graph до успешного return; затем caller владеет session.
- Factory запрещает concurrent session на одних bindings. После disposal/lease release можно создать новую.
- `ownerToken` завершает жизнь session. `waitToken` отменяет только ожидание caller.
- result completion single-assignment; повторный wait возвращает тот же result, parallel waits допустимы.
- controlled result/reset/shutdown обязан вызвать и await `StopAsync`, затем `Dispose` в `finally`.
- `Dispose` — idempotent emergency safety net: прекращает работу и освобождает graph, но не обещает полный async telemetry flush; незавершённый artifact помечается invalid/incomplete, когда это ещё возможно.
- normal outcome не маскирует exception/cancellation. Start/config/binding error бросается; cancellation остаётся cancellation.

`SquadCombatAttemptResult` содержит outcome (`Victory`, `Defeat`, `AbandonedReset`, `AbandonedStop`, `TechnicalInvalid`), active duration, identity/variant/config/seed, final leader/companions state, command counts, key companion action summary, primary failure attribution proxy и telemetry health/reference. Unity objects отсутствуют.

### 3.5. Runtime-integration contracts в Application assembly

Эти public-типы нужны реальному `Runtime`, но не являются GameFlow API:

- `ISquadCombatIntentSink.Submit(in SquadCombatPlayerIntent) -> IntentSubmissionResult`;
- `IReadOnlySquadCombatEventStream.Subscribe(ISquadCombatEventObserver) -> IDisposable`;
- `ISquadCombatTelemetrySink` для begin/append/complete/failure status;
- `ISquadCombatDiagnostics` для observed error boundary;
- immutable navigation/contact/performance observations.

Никакой contract не раскрывает `SquadCombatRoot`, Presenter, View, NavMeshAgent, GameObject, ScriptableObject, file handle или concrete Runtime type.

---

## 4. Кто запускает и уничтожает SquadCombatRoot

### 4.1. Рассмотренные варианты

| Вариант | Плюсы | Проблемы | Решение |
| --- | --- | --- | --- |
| Application GameFlow/scene scope | Production-подобная orchestration | GameFlow и scene flow сейчас не существуют; создаёт пустой future module и преждевременный contract | Отклонён для Stage 0 |
| Scene-owned self-start adapter | Простой serialized wiring | `Awake/Start` adapter становится второй entry point, скрывает ownership и связывает business lifecycle со сценой | Отклонён |
| Minimal Stage 0 launcher в `Architecture.Composition` | Реальный текущий consumer; explicit wiring; не создаёт future module; Bootstrap остаётся единственным entry | Временный Stage 0 code потребуется удалить/заменить при настоящем GameFlow | Выбран |

### 4.2. Выбранный path

```text
GameBootstrapper (единственный Awake/entry)
└─ ApplicationRoot
   ├─ global LightDI container (только application services)
   ├─ Stage0SquadCombatLauncher (plain C#, Architecture.Composition)
   │  └─ ISquadCombatSession (owned field)
   │     └─ SquadCombatRoot (Runtime)
   │        ├─ attempt/application/domain graph
   │        ├─ input/tick/navigation/presentation adapters
   │        └─ telemetry session port
   └─ global container disposed last
```

`Stage0SquadCombatHost` — passive serialized `MonoBehaviour` в `Game.Architecture.Composition`. Он не имеет `Awake/Start/Update` business logic и не создаёт root. Он хранит ссылку на frozen Stage 0 definition, Runtime scene bindings и test-harness View.

`GameBootstrapper` получает serialized reference только на `Stage0SquadCombatHost`, тип своей уже разрешённой `Architecture.Composition` assembly. Bootstrap не ссылается на `SquadCombat.Application/Runtime` и не создаёт feature.

`ApplicationRoot.OnInitialize`:

1. проверяет host presence;
2. создаёт diagnostics/telemetry concrete adapters;
3. создаёт concrete `SquadCombatSessionFactory` из Runtime;
4. внедряет его как `ISquadCombatSessionFactory` в `Stage0SquadCombatLauncher`;
5. сохраняет launcher в owned field до initialization;
6. запускает launcher с application root token;
7. при любой ошибке очищает partial launcher/factory/adapters и пробрасывает error.

`ApplicationRoot.OnDispose` сначала останавливает/dispose launcher и активную session, затем explicit application owners, затем global container. Feature graph никогда не регистрируется в LightDI.

Текущий `Root` имеет synchronous `OnDispose`, поэтому application destruction является emergency synchronous boundary: launcher отменяет/dispose active session и явно помечает telemetry incomplete, если controlled `StopAsync` не был завершён. Обычные reset/result/operator-exit flows обязаны заранее await `StopAsync`; synchronous root disposal не блокирует main thread ожиданием async I/O и не выдаёт неполный flush за успешный.

Future GameFlow заменит только `Stage0SquadCombatLauncher` как consumer. Он получит тот же `ISquadCombatSessionFactory`; Runtime, Domain и session contract не знают GameFlow. Empty GameFlow assembly сейчас не создаётся.

---

## 5. Lifecycle, ownership и error boundaries

### 5.1. Session states

```text
Created -> Starting -> Running <-> Paused -> Completing -> Completed
   \          \          \          \             \-> Stopping -> Stopped
    \---------- initialization failure --------------------------> Disposed
```

Externally observable states: `Starting`, `Running`, `Paused`, `Completed`, `Stopping`, `Stopped`, `Disposed`. Invalid transition fails explicitly; repeated same pause reason/stop/dispose is idempotent.

### 5.2. Start sequence

1. Launcher validates test selection and asks Runtime authoring validator to create immutable settings/bindings snapshot.
2. Factory atomically acquires single-session lease; concurrent start is rejected.
3. Factory creates root context and `SquadCombatRoot`.
4. Root creates children in order `create -> register with sole owner -> initialize`.
5. Telemetry artifact must open and accept `session_started`; failure before successful start aborts and cleans partial graph.
6. Tick/input remain disabled until every child is registered and initialized.
7. Root enters `Running`; factory returns session and transfers ownership.
8. Launcher stores session before starting observed async result wait.

The result wait is one explicit fire-and-forget boundary in launcher only; it uses application token and a terminal exception logger. Feature/application code does not call `.Forget()` elsewhere.

### 5.3. Pause/focus/resume

Pause reasons form a set/bit mask, not a single bool: `Manual`, `FocusLost`, `SystemInterruption`, `Observer`. Gameplay resumes only when every active reason is removed.

On first pause reason:

1. disable intake of combat input and clear queued/buffered input;
2. stop simulation advancement and AI decisions;
3. freeze gameplay clock, cooldowns, telegraphs, command windows and encounter phase timers;
4. keep immutable state and presentation visible;
5. emit pause event with reason/state.

On final resume reason removal:

1. refresh camera/HUD snapshots;
2. clear stale input again;
3. run short countdown on unscaled Runtime clock;
4. enable input;
5. resume simulation from the same tick without advancing paused time;
6. emit resume/recovery result.

Focus callbacks are Runtime observations routed to session lifecycle; they do not call Domain or dispose roots directly. Pause never heals, resets threats or changes seed.

### 5.4. Reset/replay

Reset is launcher orchestration, not in-place root reinitialization:

1. capture selected next settings; variant can change only here, before new attempt;
2. `await active.StopAsync(Reset, token)` producing `AbandonedReset`;
3. `Dispose` active session in `finally` and clear field;
4. create a new settings snapshot with new `session_id`, same requested variant/config/encounter/seed;
5. call factory `Start` to create a new root graph.

No object from the previous attempt, including navigation, Presenter, ViewModel subscription or telemetry handle, is reused. Scene GameObjects may be stable passive bindings, but Runtime restores all mutable adapter state from the new snapshot before enabling tick.

### 5.5. Completion and stop

Normal completion:

1. finish current deterministic resolution batch;
2. freeze intake/tick;
3. cancel future unresolved actions/telegraphs with events;
4. build immutable result;
5. flush/finalize telemetry;
6. complete result once;
7. launcher shows summary, then disposes session when replay/reset/exit is selected.

Controlled stop uses the same path with an abandoned reason. `stopToken` controls waiting for shutdown, not owner cancellation. Timeout/failure is surfaced, session remains disposable, telemetry becomes incomplete/invalid.

### 5.6. Shutdown order

`SquadCombatRoot` relies on actual Root behavior: its token is cancelled before `OnDispose`.

`OnDispose` order:

1. stop input/focus/tick sources and unsubscribe callbacks;
2. stop encounter/application coordinator;
3. cancel/close active action, telegraph and navigation operations;
4. explicitly dispose replaceable child presenters/viewmodels;
5. dispose remaining presenters, HUD, subscriptions and adapters in declared dependency order;
6. close telemetry session exactly once;
7. clear scene binding mutable state;
8. release factory lease.

Order-sensitive/replaceable children use explicit fields/collections. `CompositeDisposable` is only for same-lifetime subscriptions where forward order is harmless. No object is owned by both Root and LightDI/container/composite.

### 5.7. Error boundaries

- Authoring/config/binding/start error: fail `Start`, clean partial graph, no session transfer.
- Domain invariant violation: stop attempt as `TechnicalInvalid`, record last valid state, propagate/log at launcher boundary.
- Expected invalid player action: reject result + feedback/event; not an exception.
- Navigation/action invalidation: deterministic cancel/decline event; repeated recovery may mark attempt invalid.
- Telemetry failure after start: gameplay continues, attempt immediately marked invalid, observer warning shown; writer stops retrying every frame.
- Presentation-only proxy failure that breaks readability: technical invalid; no silent fallback.
- Cancellation: observed separately from normal result/error.

---

## 6. Layers, assemblies и compile proof

### 6.1. Layer responsibilities

| Layer | Responsibility | Real consumers | Allowed refs |
| --- | --- | --- | --- |
| `Domain` | Pure state/value objects/invariants/decisions/action order/command policies | Application; Domain tests | BCL only; no Unity/DI/async/storage |
| `Application` | Public session contract, intents/use cases, orchestration, event/result DTOs, ports | Runtime, Infrastructure, Composition launcher, tests | Domain; UniTask only for wait/stop |
| `Runtime` | Root/factory/session, Unity adapters, tick, navigation, MVP/MVVM, authoring snapshot builder | Composition; Runtime/PlayMode tests | Application, Domain, Unity and packages actually used |
| `Infrastructure` | Local JSONL file writer/export implementation | Composition; Infrastructure tests | Application, BCL I/O; UniTask only if implementation needs it |
| `Architecture.Composition` | Stage0 host/launcher, concrete wiring, application ownership | Bootstrap | Application, Runtime, Infrastructure, existing application packages |

`Infrastructure` is justified by a present external side effect: local file I/O with independent failure/flush lifecycle and an Application port. It appears when M0 writes the first session artifact; it is not an empty placeholder.

### 6.2. Proposed asmdefs

Created only with first real code:

```text
Game.Gameplay.SquadCombat.Domain
  -> no project references
  -> noEngineReferences: true

Game.Gameplay.SquadCombat.Application
  -> Game.Gameplay.SquadCombat.Domain
  -> UniTask

Game.Gameplay.SquadCombat.Runtime
  -> Game.Gameplay.SquadCombat.Application
  -> Game.Gameplay.SquadCombat.Domain only where direct Domain types are used
  -> Root, Unity; later InputSystem/MVP/MVVM only with first user

Game.Gameplay.SquadCombat.Infrastructure
  -> Game.Gameplay.SquadCombat.Application
  -> no Unity engine reference if persistent path is injected as string

Game.Architecture.Composition
  -> SquadCombat.Application, Runtime, Infrastructure

Game.Gameplay.SquadCombat.Tests.EditMode
  -> Domain, Application; consumer contract proof without Runtime

Game.Gameplay.SquadCombat.Runtime.Tests.EditMode
  -> Runtime, Application, Domain, Root

Game.Gameplay.SquadCombat.Tests.PlayMode
  -> Runtime, Architecture.Composition and required Unity test packages
```

Bootstrap keeps only its existing `Game.Architecture.Composition` project reference. No sibling gameplay feature references exist.

MVP/MVVM/InputSystem/AI Navigation refs are added to Runtime asmdef only in the milestone where code first uses them. Test assemblies are separated because a contract-only consumer proof and a Runtime lifecycle proof require intentionally different compile visibility.

### 6.3. Forbidden directions

- Domain → Application/Runtime/Infrastructure/Unity/DI;
- Application → Runtime/Infrastructure/Composition/Unity/LightDI;
- Runtime ↔ Infrastructure direct reference;
- Bootstrap → SquadCombat assembly;
- SquadCombat → GameFlow/Mode/Bootstrap/future feature;
- consumer → concrete `SquadCombatRoot`/Runtime;
- any feature code → `DiContainerProvider.Resolve*`;
- runtime cycle or Shared/Common assembly to hide a cycle.

### 6.4. Compile proof

1. Domain compiles with `noEngineReferences: true` and no project refs.
2. Application compiles after Runtime/Infrastructure references are removed/absent.
3. contract test assembly compiles with Application only and a fake caller/session.
4. Runtime compiles with one-way refs to own inner layers.
5. Infrastructure compiles without Runtime/Unity.
6. Composition is the only production assembly that sees consumer launcher and concrete Runtime/Infrastructure.
7. Bootstrap has no new feature reference.
8. Unity compiles all affected assemblies after every asmdef change; file graph inspection alone is insufficient.

---

## 7. Necessary pure C# model

### 7.1. Identity and state

Use small immutable Domain IDs (`CombatActorId`, `CombatActionId`, `CombatTelegraphId`, `EncounterPhaseId`) with deterministic comparison. IDs are allocated/validated at cold start; hot paths do not format strings. Они не входят в GameFlow-facing settings/result. Application boundary использует свои stable DTO/reference values и один раз map-ит их в Domain IDs, поэтому consumer public contract не требует прямой Domain reference.

Core state:

- `CombatAttemptState`: lifecycle, active simulation tick/time, phase, variant, validity, outcome;
- `CombatActorState`: id, team, role/archetype, health, position observation, activity/incapacity, current intent/action;
- `TargetState`: explicit priority target and per-actor local target;
- `ActionState`: `Requested -> Started -> Resolved | Cancelled`, unique source/target/context;
- `DamageInstance`: source actor/action, receiver, amount, telegraph, avoidable tag;
- `ThreatTelegraphState`: source, geometry descriptor, start/resolve tick, active status;
- `SquadIntentState`: command kind, target/anchor/protected actor snapshot, issue/expiry tick;
- `EncounterState`: current phase, spawned/required threats, transition facts.

These are feature-local. No generic status/ability/AI platform is introduced.

### 7.2. Pure rules

Behavior-focused pure services/functions:

- settings/definition validation;
- target validity and deterministic scoring/tie-break;
- health/damage clamp and incapacitation;
- action transition invariant and cancel reason;
- command preview/resolution policy;
- companion priority/emergency override decision;
- cooldown/window availability;
- encounter phase trigger/transition;
- victory/defeat precedence;
- telemetry mapping from facts to stable event data.

Each receives explicit inputs, tick/time and seed-derived values. No `Time`, `Random`, Transform, NavMesh or static Unity state.

### 7.3. Action and damage ordering

Within one simulation step:

1. apply accepted player/lifecycle intents in monotonic input sequence;
2. apply Runtime observations captured for that step;
3. start due actions;
4. resolve due actions ordered by `(resolve_tick, action_priority, actor_id, action_id)`;
5. build damage/control instances;
6. apply instances in the same stable order;
7. derive incapacitations;
8. evaluate phase and outcome;
9. emit facts/events after state commit.

An action resolves at most once. Low FPS/catch-up cannot duplicate resolution. If leader becomes lethal on the same tick as final threat dies, `Defeat` wins because GDD victory requires a living leader. Victory is emitted only after the full tick batch and only when no unresolved critical damage for that tick remains.

### 7.4. Random and seed

One session-scoped deterministic random source is created from settings seed. Random values may select declared spawn/order candidates only where GDD allows variation. Critical companion usefulness and command acceptance never use random. Random stream calls are centralized and evented enough to diagnose sequence drift; no actor creates its own Unity random state.

---

## 8. Application orchestration

### 8.1. Coordinator

`SquadCombatAttemptCoordinator` owns authoritative Domain state and implements `ISquadCombatIntentSink`. It does not know Unity Views, NavMesh, file paths or A/B HUD controls.

Responsibilities:

- validate lifecycle and intent availability;
- queue intents with monotonic sequence;
- advance one simulation step from explicit observations;
- invoke target/action/companion/encounter policies;
- publish committed events and presentation snapshots;
- complete immutable result exactly once.

### 8.2. Player intent data

`SquadCombatPlayerIntent` is a readonly allocation-free value with `Kind`, input source, input sequence, simulation timestamp and only relevant payload:

- movement uses normalized `Direction2`;
- target selection uses an adapter-resolved Application `SquadCombatActorRef` or explicit clear, mapped by coordinator to Domain ID;
- dodge captures direction/fallback decision at acceptance;
- ability captures target/context snapshot;
- command captures the previewed command snapshot shown at press time.

Rejected intents return stable reasons (`Paused`, `Cooldown`, `InvalidTarget`, `ActorInactive`, `NoDirection`, `SessionStopping`, etc.) and always produce required feedback/telemetry. Continuous move replaces previous move intent; discrete actions remain ordered.

### 8.3. Events and observers

Domain facts are mapped once into immutable `SquadCombatEvent` records. Event names and enum values are stable schema, while presentation and telemetry are separate observers. Observer failure cannot mutate Domain; required observer failure crosses the coordinator error boundary and marks technical invalid.

The current consumers are:

- world/HUD presentation;
- telemetry mapping/writer;
- observer summary/test harness;
- automated behavior tests.

No global event bus is created.

### 8.4. Ports

- navigation observation/actuation is Runtime-owned and feeds concrete data, not a universal navigation service;
- telemetry storage is an Application port implemented by Infrastructure;
- diagnostics is a narrow Application port adapted to the existing application logger in Composition;
- performance sampler is Runtime and submits stable sample DTOs;
- presentation consumes event/state snapshots and submits intents through the same sink.

Application ports exist only for these present consumers/side effects.

---

## 9. Runtime model: tick, clocks, cancellation и hot paths

### 9.1. Tick ownership

`SquadCombatTickAdapter` — единственный feature-owned Unity frame source. Это Runtime adapter, не Root и не manager. Root подписывает один callback после полной initialization и снимает его первым при stop/dispose.

Actor `MonoBehaviour.Update` запрещён. Presentation animation may use Unity Animator internally, но combat decision/resolution не распределяется по actor Updates.

### 9.2. Fixed simulation step

Runtime использует accumulator и frozen `simulation_step` tuning. Конкретное значение выбирается profiling/usability; это не product guarantee. Один Unity frame:

1. collect Input System callbacks into bounded intent buffer;
2. apply pause/focus state;
3. sample navigation/contact observations once;
4. execute zero or more fixed simulation steps;
5. publish one coalesced presentation snapshot where safe;
6. sample performance/flush telemetry outside rule resolution.

Есть configurable maximum catch-up steps. При превышении backlog simulation не ускоряется бесконечно и не отбрасывает resolution молча: событие performance violation помечает attempt technical-invalid для comparative data. Action IDs/state prevent double resolution.

### 9.3. Clocks

- `SimulationClock`: integer tick/derived active time, заморожен pause;
- `RealtimeClock`: unscaled Runtime time только для resume countdown, I/O timeout и performance sampling;
- UTC используется лишь в file/session metadata;
- Domain получает значения, а не читает часы.

Cooldowns, telegraphs, AI and encounter use only SimulationClock. Pause duration не попадает в active session time.

### 9.4. Determinism boundary

Гарантируется одинаковое pure resolution при одинаковых initial snapshots, ordered intents, observations и seed. NavMesh/physics/camera are observations and не обещают bit-identical coordinates между device runs. Любое engine recovery записывается; replay same восстанавливает seed/config/content, но не заявляется input replay.

### 9.5. Cancellation

- Root token ограничивает все session operations.
- Linked tokens создаются только для конкретной replaceable operation и освобождаются owner-ом.
- Tick callback не запускает async work.
- File writer имеет один owned worker/queue; exception наблюдает session boundary.
- Никакой async operation не начинается под internal lock.
- `async void` допустим только Unity callback boundary с немедленной передачей в observed UniTask path; предпочтительно отсутствует.

### 9.6. Hot-path rules

- no LINQ, closures, boxing, string formatting, reflection и per-frame collection recreation;
- actor/action/telegraph buffers pre-sized по validated encounter maximum;
- no `Find`, `GetComponent`, hierarchy traversal or config reads after cold initialization;
- event records use value data; string serialization выполняется writer-ом вне simulation step;
- HUD updates only on changed snapshot, not every frame;
- no pooling until measured repeated instantiate/destroy hotspot.

---

## 10. Leader, targeting, auto attack, dodge и ability boundaries

### 10.1. Locomotion

Runtime converts camera-relative `Move` into desired movement. Domain/Application owns whether movement is allowed; Unity locomotion adapter owns Transform/CharacterController/NavMesh/physics mechanics. Releasing input submits zero intent. Bounds/path failure returns observation and visible recovery/error; no hidden reposition.

### 10.2. Targeting

Input gesture is replaceable Runtime policy. It maps touch/editor input to candidate ActorId; pure target rule then validates and selects.

Rules:

- explicit priority target wins while valid;
- invalid target clears with reason/event/marker update;
- auto target chooses nearest reasonable immediate threat by frozen score;
- tie-break is stable ActorId;
- target death/unreachable never silently retargets an already issued command/action;
- target selection gesture is identical in A/B.

Gesture decision gate is in section 20.4.

### 10.3. Auto basic attack

One local leader attack rule checks active actor, target validity, range/interaction, cadence and action lock. It requests an action but never moves leader through danger. Damage resolves at explicit hit/resolution tick. Target loss cancels with reason; attacks never continue against empty space beyond frozen recovery.

Dodge responsiveness/cancel relationship is an immutable action-priority rule. No universal animation/combo framework is created.

### 10.4. Dodge

At intent acceptance, direction snapshot is current movement input; no-direction fallback is a frozen explicit policy (`last_non_zero`, `facing` or reject) common to A/B. Dodge action owns start/active/recovery ticks and optional avoidance window. Locomotion adapter clamps/validates bounds and reports failure. Rejected dodge gives immediate HUD/world feedback.

### 10.5. Active ability

One proxy ability is local to leader and has `Ready -> Targeting -> Requested -> Started -> Resolved/Cancelled -> Cooldown`. Its concrete function remains data/design decision until the required gate, but boundary requires a spatial/temporal choice, explicit target/context, noticeable damage and/or short interrupt/control, and no duplication of squad command.

No generic ability/status graph, upgrade tree or effect registry is created. When function freezes, implement the smallest rule/action data needed for that one ability.

---

## 11. Companion pipeline

### 11.1. Per-step pipeline

```text
World/navigation observations
  -> validate actor/state/path
  -> active squad intent candidate
  -> emergency override evaluation
  -> role duty policy
  -> formation/basic offense fallback
  -> CompanionIntent
  -> action feasibility
  -> requested action + intent telegraph
  -> Runtime navigation/animation
  -> deterministic action result
  -> result telegraph + event
```

The authoritative pipeline is pure decisions plus explicit Runtime observations. Presenter visualizes it; View does not choose behavior.

### 11.2. Role policies

Three concrete policies share one current contract because they are real present roles:

- Protector: threat interception/protective position/short mitigation or displacement;
- Damage/Debuffer: priority pressure/burst or one readable debuff;
- Support/Controller: stabilization or one readable control/mitigation response.

Each policy returns intent, target, reason, urgency and expected action kind. Exact proxy actions are frozen data. Synergy is authored causal behavior of these actions, not a combo system.

### 11.3. Priority and emergency override

Normative order:

1. validate actor/state/navigation;
2. avoid obvious lethal hazard unless a declared role action requires exposure;
3. honor active accepted command;
4. emergency role duty only if delay would make prevention of imminent incapacitation impossible;
5. primary role duty;
6. restore squad distance/formation;
7. predictable basic offense.

Emergency override requires a concrete threatened actor, projected imminent lethal/critical state, feasible response and `command_override_reason`. It cannot use hidden random. Frequent override is reported as design failure, not intelligence.

### 11.4. Navigation observations and recovery

Runtime provides position, path status, remaining distance, blocked duration, hazard occupancy and reachability. Domain never sees NavMesh types.

Invalid/unreachable action is declined/cancelled with feedback. Companion may choose an explicitly allowed local fallback on the next decision; it may not silently change the shared target. Teleport is prohibited as normal movement. If a Stage 0 recovery proxy is unavoidable, it requires visible recovery, `error_or_recovery`, attempt invalidation for natural-AI evaluation and a threshold preventing loops.

### 11.5. Telegraphing

Every key companion action emits and presents:

- `intent_started` with trigger/target;
- at least two channels, one not color-only;
- `action_started` and `action_resolved/cancelled`;
- role-specific result on protected/affected target;
- reason for decline/override.

This preserves `cause -> intent -> action -> result` and enables failure attribution.

---

## 12. A Rally / B Context

### 12.1. Common command boundary

Only `ISquadCommandPolicy` knows variant. Input adapter always submits one `IssueSquadCommand` with the preview snapshot shown at press. Coordinator asks selected policy for `SquadIntent`; actor/enemy/action logic receives intent data and contains no `if A/B`.

Common immutable budget:

- one command HUD zone;
- one cooldown/active window/radius/latency budget;
- same role action availability and total power envelope;
- same target gesture, content, seed/order, onboarding and feedback quality;
- same acceptance/rejection telemetry.

Validation compares both frozen profiles and fails start if unequal fields without an explicit documented fairness exception.

### 12.2. Rally policy

- valid priority target -> `RallyOnTarget` snapshot;
- no target -> `RallyOnLeader` with leader/anchor snapshot;
- each role derives its response from the same intent;
- target death/unreachable cancels affected response or finishes an already valid action; no hidden retarget;
- new Rally replaces/ends old intent according to one frozen rule and never stacks conflicting intents.

### 12.3. Context policy

Resolver computes a preview snapshot before input:

1. valid hostile priority target -> `Focus`;
2. declared critical threat to a selected ally -> `Protect`;
3. otherwise -> `Regroup`.

If product freezes two contexts, exactly one declared rule is removed before M5; runtime does not opportunistically hide it. Conflicts use stable priority and ActorId tie-break. Debounce/stability is frozen tuning. Press uses the displayed snapshot or rejects explicitly if it became invalid; it never substitutes a new context silently.

### 12.4. Fairness proof

Before M5 exit:

- automated fixtures run identical encounter state through both policies;
- budgets/cooldowns/action availability are equal;
- no role/enemy/scene branch reads variant;
- telemetry can compare preview, issued command, acceptance latency, response/result and override;
- manual device smoke confirms same control location/size/feedback quality;
- any power difference is documented before frozen build, never inferred after data.

---

## 13. Unity adapters and presentation

### 13.1. Runtime scene bindings

`SquadCombatSceneBindings` is a passive validated data holder referenced by `Stage0SquadCombatHost`. It contains arena bounds, spawn points, actor/enemy Views, tick adapter, camera adapter, input/HUD views and navigation adapters. It never creates session/root or owns business state.

At start validator checks every required reference, uniqueness, capacity and scene active state. Missing/duplicate binding aborts start; no `Find` fallback.

### 13.2. Input System

One `SquadCombatInputAdapter` maps touch and Editor action maps to the same intent values. Service reset/variant select are separate harness actions and not combat input budget. Editor-only hover, exact mouse aim and extra hotkeys cannot affect product rules.

Input is enabled only in Running after resume countdown. Focus/pause/reset disables maps and clears pending touches. UI button callbacks submit intents; they do not call actor/View logic.

### 13.3. Camera

`SquadCombatCameraAdapter` follows frozen 3/4 tuning, leader/group/threat observations and emits visibility/edge-marker data. It owns Unity camera transforms only. No manual rotation/pinch zoom in Stage 0. Sudden camera angle change during action is prohibited. Camera values are snapshot data.

### 13.4. Locomotion/NavMesh

Leader locomotion and companion/enemy navigation are Runtime adapters. They receive desired movement/action position and return observations. They do not decide combat target, command or role priority. Path rebuild/query cadence is profiled and bounded; no path query per actor per rendered frame unless measured/justified.

### 13.5. World gameplay MVP

World/arena/actor presentation uses MVP:

- View: serialized Unity refs, render/animation/VFX/audio, input/animation completion events;
- presentation Model: non-authoritative view state derived from Application snapshots;
- Presenter: subscribes to Application events, updates View, relays narrow animation/contact observations.

`SquadCombatPresenter` owns replaceable actor/threat child presenters in explicit collections. Child communicates upward through narrow callbacks. Exact package API is honored: choose one sync/async initialize path; dispose child presenters explicitly in `OnDispose` before their required parent state disappears.

### 13.6. HUD MVVM

The permanent combat HUD is a real feature-owned UI state and uses MVVM:

- `SquadCombatHudModel` is presentation state only;
- `SquadCombatHudViewModel` exposes read-only health/role/cooldown/command preview properties and commands that submit Application intents;
- `SquadCombatHudView` binds Input System/UI and subscriptions;
- business combat/context rules stay Application/Domain.

Harness overlay/summary is separate from blind HUD and may live alongside it in Runtime/Composition without becoming a production menu framework. Package-owned Model/View lifetimes are not double-owned by Root.

### 13.7. Pause/focus and platform

One Runtime lifecycle adapter forwards `OnApplicationFocus`, `OnApplicationPause` and manual pause. It never directly changes clocks/AI. Haptics/audio are optional proxy outputs with explicit availability; absence cannot remove the non-color visual channel.

---

## 14. Frozen tuning/config and validation

### 14.1. Authoring decision

Stage 0 uses one or more Runtime `ScriptableObject` authoring assets such as `SquadCombatStage0Definition`, but authoritative attempt receives an immutable deep snapshot. Mutable asset references never enter Domain/Application hot paths.

The installed Config package is **not used in Stage 0** because:

- no application config initialization flow exists;
- only local frozen test profiles are required;
- using Config would add parser/page/application wiring without another consumer;
- its real async API is `Task`, adding a boundary with no current value.

If later a real application config exists, Composition may read an `IConfigPage` and adapt/copy it into the same `SquadCombatTuningSnapshot`; Application contract remains unchanged.

### 14.2. Snapshot contents

- camera/framing/edge visibility;
- leader locomotion, target scoring, basic attack, dodge, proxy ability;
- role actions, decision thresholds, formation and emergency override;
- command budgets and A/B policy data;
- enemy actions/telegraphs/damage;
- encounter phases/spawn candidates;
- HUD/readability and feedback intensity;
- simulation step/catch-up and performance sampling;
- telemetry queue/flush limits.

Collections are defensive copies/read-only arrays. Snapshot has stable `config_id` and optional content hash. No runtime mutation or global static lookup.

### 14.3. Validation

Cold start validation accumulates actionable errors, then fails atomically:

- non-empty stable IDs and unique actor/action/phase/telegraph/spawn IDs;
- leader plus 1–3 valid companion role slots as milestone content requires;
- positive health/range/durations, ordered action windows and bounded capacities;
- all references resolve; phase graph terminates; required victory threats defined;
- spawn/binding capacities cover maximum actors/telegraphs;
- A/B common budgets equal and selected context set declared;
- no color-only critical cue metadata;
- telemetry/build/config/seed/device identity present for measured milestones;
- target gesture/ability/context/device gates satisfied by their milestone deadlines.

Runtime binding validation separately checks Unity refs/NavMesh/arena. No default values silently repair invalid content.

---

## 15. Test harness, telemetry, reset/replay и write failure

### 15.1. Harness responsibility

`Stage0SquadCombatLauncher` plus passive harness View lets operator:

- select A/B, frozen profile and seed before start;
- start, pause, reset/replay same and switch variant between attempts;
- hide service UI for blind participant;
- set anonymous participant/device/build identity;
- mark invalid reason;
- view summary and telemetry health/reference;
- export/locate local log.

Harness is the sole current consumer of session factory. It does not become GameFlow/menu.

### 15.2. Telemetry pipeline

```text
Domain committed facts
 -> Application event/schema mapper
 -> bounded session queue
 -> Infrastructure JSONL writer
 -> local attempt file + completion marker
```

Every envelope contains schema/event/session/participant/sequence/active-time/build/variant/config/encounter/seed/device. Stable mandatory events follow GDD section 14.4 exactly; implementation may add versioned fields, not rename frozen ones silently.

Simulation thread only creates/enqueues bounded value records. Writer serializes outside simulation step and preserves sequence. Begin/end/flush are explicit. No analytics SDK/backend/account.

### 15.3. File and ownership

Composition obtains `Application.persistentDataPath` once and passes an explicit directory string into Infrastructure; Infrastructure has no Unity ref. Session owns one telemetry handle. File name uses non-personal session/build identifiers. Writer owns stream/queue/worker and closes exactly once.

### 15.4. Failure behavior

- Cannot create/open artifact or write `session_started`: `Start` fails; measured attempt cannot begin.
- Queue overflow, serialization or write failure after start: first failure stops further disk writes, records in-memory health/error, marks attempt invalid, shows observer warning, gameplay continues where safe.
- No per-frame retry/log storm.
- Flush timeout/error on completion: result is technical-invalid/incomplete and error surfaces to launcher.
- OS process kill may leave incomplete file; next harness scan reports it, no fake gameplay defeat.
- Telemetry cannot declare player comprehension; `telegraph_seen` remains technical visibility proxy only.

### 15.5. Reset/replay proof

Reset must restore composition, health, cooldowns, positions, phase, seed, variant/profile and adapter state. It creates a new session ID/file. Old session emits `AbandonedReset`, cancels active actions/telegraphs and releases subscriptions before new root starts. No application restart or scene edit is required.

---

## 16. Invalid states and required edge cases

| Case | Normative behavior |
| --- | --- |
| Target dies between input and response | Existing valid committed resolution may finish; otherwise cancel with reason; no hidden retarget |
| Actor/target unreachable | Decline/cancel, visible reason/event; recovery proxy only under declared invalidating rule |
| Companion incapacitated during command | It stops/cancels, emits no false acknowledgement/result, remains visibly inactive |
| Emergency override | Only imminent prevention rule; reason/event required |
| Simultaneous leader lethal and final threat death | Full batch resolves, then Defeat wins |
| Reset during telegraph/command/action | Controlled stop emits cancellations, disposes old graph, starts clean graph |
| Pause/focus during dodge/ability/command | Simulation ticks/windows freeze; stale input cleared; resume countdown |
| Rapid B context changes | Frozen debounce; press uses displayed snapshot or explicit reject |
| Leader/companion/threat outside camera | Edge marker/visibility event according to critical priority |
| Multiple allies threatened | Stable resolver + previewed selected actor |
| Command during cooldown | Immediate reject feedback/event; never lost silently |
| Rally target becomes invalid | Role-specific cancel/finish, no new shared target |
| Regroup while already grouped | Short acknowledgement, no unnecessary movement |
| All companions incapacitated | Attempt continues with leader; state/result/telemetry reflect loss |
| Leader reaches zero | Inputs/actions stop; Defeat path; technical error never mapped to defeat |
| Low FPS | Fixed-step catch-up, exactly-once actions; overflow marks technical invalid |
| Input adapter lost | Pause/technical invalid and explicit recovery; not gameplay failure |
| Missing scene/config binding | Atomic start failure; no Find/default fallback |
| Duplicate start/concurrent session | Explicit rejection; active session unchanged |
| Stop/result called repeatedly | Same terminal result; disposal exactly once |
| Telemetry write failure | Section 15.4 degraded/invalid behavior |

---

## 17. Performance and 30 FPS proof

### 17.1. Target

Directional Stage 0 target: sustainable 30 FPS in representative worst-case encounter on the frozen Android device. Exact percentile/spike budgets are set after device baseline before external test.

### 17.2. Initial engineering budgets

These are validation budgets, not product guarantees:

- one feature tick source;
- zero avoidable managed allocations in steady simulation/input/AI/action hot path after warm-up;
- no per-frame reflection/hierarchy lookup/config serialization;
- bounded actor/action/telegraph/event counts from frozen definition;
- HUD rebuild only on change;
- telemetry serialization/I/O outside simulation resolution;
- no regular instantiate/destroy churn during steady encounter unless measured acceptable.

Numeric actor/enemy/effect limits stay data until M6 profiling; readability may impose a lower ceiling than CPU/GPU.

### 17.3. Proof

- Unity Profiler CPU Timeline/GC/Memory captures for Entry and Crescendo;
- device frame-time samples grouped by phase;
- worst spike context and sustained degradation;
- memory trend across repeated reset/replay;
- thermal/battery conditions and render resolution/profile;
- navigation query and UI rebuild cost;
- telemetry enabled vs diagnostic-disabled comparison, without using disabled telemetry in product test.

Pooling is considered only after a capture proves repeated lifecycle is material. The decision records owner, reset invariant and measured before/after.

---

## 18. Test and validation strategy

### 18.1. EditMode pure behavior

- settings validation and immutable defensive copy;
- target validity/scoring/tie-break/clear;
- action lifecycle and exactly-once resolution;
- damage/incapacity and simultaneous outcome precedence;
- cooldown/pause time behavior;
- Rally/Context preview/resolution and equal budget;
- companion priorities/emergency override/decline reasons;
- deterministic phase transitions and seed use;
- telemetry mapping/event order;
- all section 16 pure edge cases.

Tests assert external state/events/results, not private method calls.

### 18.2. M0 lifecycle/compile tests

- contract-only fake consumer compiles without Runtime;
- factory partial-start failure disposes every created child and releases lease;
- one active session only; new session works after disposal;
- owner cancellation, controlled stop, repeated stop/dispose and result completion;
- pause reason stacking and no paused clock progress;
- reset disposes old root before new root starts;
- subscriptions/input/tick/telemetry handle removed exactly once;
- ApplicationRoot disposal stops launcher before container.

### 18.3. PlayMode/Unity

- bootstrap scene initializes through the sole `GameBootstrapper`;
- serialized Stage0 host/bindings valid; missing binding fails loudly;
- scene entry creates exactly one session;
- reset/replay restores adapters and no duplicate callbacks remain;
- MVP/MVVM bindings, spawn/despawn, animation/contact and phase wiring;
- focus/pause callbacks and resume countdown.

### 18.4. Manual Unity smoke

- actual touch zones and gesture;
- camera/framing/edge markers;
- NavMesh/pathing/bounds/recovery;
- telegraph/role/command non-color readability;
- HUD blind/observer separation;
- animation/VFX/audio/haptics and serialized prefab/scene refs.

### 18.5. Android and Profiler

- install/start/full encounter/reset/A↔B/pause/resume;
- minimum physical screen/readability;
- touch ergonomics and system gesture conflicts;
- device/OS/resolution/quality/thermal capture;
- 30 FPS feasibility, allocations, memory and repeat-run trend.

### 18.6. Reporting

Each milestone separately reports compile, automated tests, PlayMode, manual Unity, Android and Profiler proof. `Not run` is not green. If Unity is already open and batchmode cannot safely run, implementation task must not close it; milestone remains `HOLD` until required editor checks are provided.

---

## 19. Future file structure and vertical milestones

### 19.1. Structure

```text
Assets/Game/Gameplay/SquadCombat/
  Domain/
    Game.Gameplay.SquadCombat.Domain.asmdef
  Application/
    Game.Gameplay.SquadCombat.Application.asmdef
  Runtime/
    Composition/        # Root, context, factory, session, bindings snapshot builder
    Adapters/           # input, tick, camera, locomotion, navigation, lifecycle
    Presentation/
      Gameplay/         # MVP
      UI/               # feature HUD MVVM and harness views when real
    Authoring/           # Stage0 definition/validators
    Game.Gameplay.SquadCombat.Runtime.asmdef
  Infrastructure/       # real JSONL adapter only
    Game.Gameplay.SquadCombat.Infrastructure.asmdef
  Tests/
    EditMode/Contract/
    EditMode/Runtime/
    PlayMode/
```

`Assets/Game/Architecture/Composition` receives Stage0 host/launcher/wiring. No new GameFlow, Modes, Development or shared assembly/folder is created.

### 19.2. M0 — architecture/feature shell gate

M0 is the smallest real SquadCombat project/feature shell, not an abstract platform.

Deliverables:

- real Domain/Application/Runtime assemblies with non-empty responsibility-bearing code: M0 Domain содержит только pure attempt lifecycle transition, simulation tick/seed value invariants и typed inner IDs, а не combat mechanics;
- Infrastructure only with real empty-session JSONL begin/end writer;
- exact Application session/settings/result/event contract;
- `SquadCombatRoot`, context, session/factory and single-session lease;
- Composition-owned Stage0 host/launcher path wired from sole Bootstrap;
- one runnable empty graybox arena shell, build/config/seed identity, start/reset and empty telemetry session;
- immutable definition snapshot/validation needed by shell;
- contract compile, root/session lifecycle and scene-entry tests.

Forbidden in M0:

- leader/enemy/companion combat mechanics;
- generic combat/AI/ability/status systems;
- future GameFlow/menu/modes;
- empty presentation/infrastructure abstractions.

Exit gate:

1. all affected assemblies compile;
2. contract-only consumer proof passes;
3. lifecycle tests pass including failure cleanup/exactly-once disposal;
4. PlayMode or recorded Unity scene-entry/reset proof passes;
5. manual shell creates one session/log and resets without leaks;
6. critical review finds no duplicate entry, service locator, LightDI feature scope, sibling ref or speculative platform.

**No M1 mechanics may start while any M0 item is HOLD or unverified.**

### 19.3. M1 — Leader

Movement, 3/4 camera, selected target gesture candidate, priority target, auto basic attack, dodge, proxy active ability, simple target, HUD controls, EditMode rules and Android input smoke. Exit: no desktop-only advantage; source/action/dodge feedback explainable.

### 19.4. M2 — Basic threat

Pressure enemy, shape-coded telegraphed attack, health/damage/defeat and deterministic restart. Exit: source of damage and counterplay are explainable.

### 19.5. M3 — One companion

One autonomous role end-to-end through observation → intent → telegraph → action → result, formation distance and telemetry. Exit: useful without command/micromanagement.

### 19.6. M4 — Three roles

Protector, Damage/Debuffer, Support/Controller, role conflicts/emergency override, incapacity, synergies and readability baseline. Active ability and proxy role functions must be frozen for this milestone. Exit: roles distinguishable and stable.

### 19.7. M5 — A/B commands

Common command intent boundary, Rally, frozen 2-or-3 Context set, equal budget/feedback, required edge cases and frozen target gesture. Exit: both variants play same content without actor/enemy/scene variant branches.

### 19.8. M6 — Full encounter, feedback and telemetry

3–5 minute phases, three threat archetypes, victory/defeat/summary, complete HUD/readability, mandatory telemetry, export and performance samples. Exit: reproducible session and reconstructable causality.

### 19.9. M7 — Internal usability/feasibility

Blockers fixed, pause/focus/reset/replay/A/B order verified, target device and frozen build/config/seeds/known issues recorded, Profiler/device pass, blind protocol ready. Exit: no technical/readability blocker invalidating external test.

### 19.10. M8 — External blind comparative test

15 valid external participants, counterbalanced A/B, invalid sessions separated, telemetry/observations/interviews collected without threshold changes. Shortfall is HOLD, not completion.

### 19.11. M9 — Decision report

`proceed / iterate / reduce scope / pivot / stop`, control direction or next cheapest distinguishing test, feasibility findings and required document changes. Stage 1 scope is not automatically authorized.

After every milestone: validation → critical review → `READY/HOLD`. On HOLD no next milestone work starts.

---

## 20. Risks, alternatives, ADR candidates и open decisions

### 20.1. Key technical/product risks

| Risk | Guard/evidence |
| --- | --- |
| Autoplay/passive leader | Spatial threats, target/dodge/ability/command decisions; observation at M1–M7 |
| Unfair/opaque AI | Pure priority rules, required intent/result telegraph, override reasons, attribution events |
| Three actors unreadable | Frozen VFX/attention budget; scope reduction to two is product gate, not architecture exception |
| A/B unfairness | One intent boundary, common budget validator, same content/gesture/feedback |
| Scene prototype debt | Composition launcher, explicit bindings, root/session ownership, no scene business logic |
| Navigation exceptions | Simplify arena/data; visible invalidating recovery, no universal workaround |
| Telemetry spikes/data loss | Bounded off-step writer, health/invalidation, Profiler comparison |
| Lifecycle leaks on reset | New root per attempt, ownership tests, exact stop/dispose order |
| Premature abstraction | Feature-local types, present consumers only, no shared extraction before two matching consumers |
| Package API drift | Exact-hash source proof; M0 compile gate; UniTask cache revalidation |

### 20.2. Considered/rejected alternatives

- GameFlow now: rejected as empty future responsibility.
- Self-starting scene root: rejected as duplicate Unity entry and hidden owner.
- LightDI per attempt/local container: rejected; local scope is per calling assembly and repeatable graph is explicit factory/root.
- Register active session in global LightDI: rejected; temporary ownership and shutdown order would be hidden.
- Config package for one frozen profile: rejected until real application config consumer exists.
- Full deterministic engine replay: rejected; not required for Stage 0 evidence and incompatible with cheap NavMesh/physics proxy.
- Generic combat/ability/status/AI frameworks: rejected; one current feature and proxy semantics.
- Pooling from start: rejected until profiling.
- Sync scene lookup fallback: rejected; passive serialized binding must be explicit and validated.

### 20.3. ADR candidates

Only create an ADR if a trigger occurs:

- session start becomes async due real Addressables/scene resource ownership;
- Stage0 launcher is replaced by production GameFlow/scene scope;
- full deterministic replay becomes a product requirement;
- shared combat/navigation module has two real consumers with identical semantics/lifecycle;
- LightDI exception or additional module scope is proposed;
- telemetry storage changes from local file to external service.

### 20.4. Open product decisions and deadlines

| Decision | Deadline | Design seam / consequence if missing |
| --- | --- | --- |
| Touch target-selection gesture | Candidate usable in M1; **frozen before M5** | Runtime input policy only; M5 HOLD without identical A/B gesture |
| Exact leader active ability function | Proxy needed in M1; **final before M4** | Local action/data; M4 HOLD if function/readability not chosen |
| Variant B uses 2 or 3 contexts | **Before M5 implementation/freeze** | Context policy/data only; M5 HOLD without declared resolver |
| Target Android device/minimum screen/profile | Proxy allowed earlier; **before M7** | Device metadata/performance budget; M7 HOLD without concrete device |

Other GDD tuning freezes by M7 or M6 as specified: dodge/cadence/command windows, camera, role proxies, arena/enemy counts, seeds, audio/haptics and performance percentiles.

### 20.5. Current blockers

No architectural blocker remains. Exact current UniTask source cache is unavailable despite matching lock hash in checked projects; this is an explicit M0 implementation validation, not a reason to invent API or widen design.

---

## 21. Requirement traceability, Definition of Ready и review checklist

### 21.1. GDD/design traceability

| GDD area | Design coverage |
| --- | --- |
| Purpose/hierarchy/contracts/non-goals | Sections 1–3 |
| Leader/input/camera/target/attack/dodge/ability | Sections 8–10, 13 |
| Combat model/time/space/fairness | Sections 7, 9, 16 |
| Companions/roles/trust/telegraph | Section 11 |
| Rally/Context/fairness | Section 12 |
| Enemies/arena/encounter phases | Sections 7, 14, M2/M6 |
| HUD/readability | Section 13 |
| Victory/defeat/reset/pause/focus | Sections 5, 15, 16 |
| Harness/telemetry/performance | Sections 15, 17 |
| Testing/edge cases | Sections 16, 18 |
| Architecture/ownership/config | Sections 4–6, 14 |
| M0–M9 | Section 19 |
| Risks/open decisions | Section 20 |

### 21.2. Definition of Ready for M0 implementation

- [x] responsibility/public contract/non-goals fixed;
- [x] one session concurrency and ownership transfer fixed;
- [x] chosen creator/owner/destroyer without duplicate Unity entry;
- [x] startup/failure/pause/resume/reset/stop/disposal sequences fixed;
- [x] Domain/Application/Runtime/Infrastructure responsibilities and actual consumers named;
- [x] asmdef direction and compile proof defined; asmdefs created only with first code;
- [x] pure model, deterministic ordering, clocks/seed and engine observation boundary defined;
- [x] leader/companion/A/B/Unity presentation seams defined without universal platform;
- [x] immutable authoring snapshot and validation fixed; Config omission justified;
- [x] telemetry schema pipeline, write failure and async stop boundary defined;
- [x] required GDD edge cases covered;
- [x] 30 FPS/hot-path/profiler proof defined;
- [x] EditMode/lifecycle/compile/PlayMode/manual/Android validation separated;
- [x] M0 strict no-mechanics gate and M0–M9 order fixed;
- [x] product decisions have deadlines and do not block M0;
- [x] no Stage 1+, sibling refs, service locator, second entry, global manager or speculative pooling;
- [x] Project Architecture READY document synchronized locally;
- [x] exact-hash custom package APIs checked; unavailable UniTask cache explicitly recorded.

### 21.3. Critical review before READY

| Review question | Result |
| --- | --- |
| Does design expand into Stage 1+? | No |
| Is there a universal platform/shared gameplay kernel? | No |
| Is Bootstrap still sole Unity entry? | Yes |
| Does Bootstrap reference concrete feature? | No |
| Does feature use LightDI or service locator? | No |
| Can sibling features/runtime reference each other? | No |
| Does scene contain business lifecycle/start logic? | No; passive validated bindings only |
| Are A/B branches isolated to command policy? | Yes |
| Are tuning/content unknowns immutable and deadline-bound? | Yes |
| Is M0 a real feature shell with hard validation gate? | Yes |
| Are telemetry and file I/O owned/released? | Yes |
| Are package drifts/unverified APIs disclosed? | Yes |

**Final decision: READY.** Implementation begins in a separate Codex task with M0 only. M1 is prohibited until M0 compile/lifecycle/scene-entry validation and review are READY.
