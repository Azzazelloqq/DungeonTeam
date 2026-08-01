# DungeonTeam — Stage 0 Application Flow Technical Design

## Статус, версия и границы решения

**Статус:** **READY для отдельной реализации по milestone-gates AF0 → AF3**.

**Версия:** 1.0

**Дата:** 31 июля 2026

Этот документ разрешает проектировать и реализовывать только минимальный application flow shell:

```text
Boot -> Loading -> MainMenu -> EnteringGameplay -> Gameplay -> Returning -> MainMenu
                                                              \-> Error
```

Это не production metagame, не menu/navigation platform и не разрешение расширять Stage 0 combat scope.

Иерархия источников:

1. `Docs/Product/CoreCombatPrototypeGDD.md`;
2. `Docs/Technical/ProjectArchitectureTechnicalDesign.md`;
3. `Docs/Technical/SquadCombatStage0TechnicalDesign.md`;
4. этот документ;
5. implementation backlog и код.

При расхождении документации custom package с совпадающим `Library/PackageCache` источником API является PackageCache. Текущий код и сцена являются источником истины для уже реализованного SquadCombat M0.

---

## 1. Purpose, scope и non-goals

### 1.1. Responsibility

`ApplicationFlow` отвечает только за application-lifetime последовательность экранов и одной gameplay session:

- выполнить preflight и показать состояние запуска;
- показать Main Menu;
- принять единственный Stage 0 intent `Start`;
- создать и передать ownership одной gameplay session;
- дождаться gameplay result либо обработать `Return`/`Reset`;
- остановить и освободить active child graph до создания следующего;
- показать контролируемую ошибку, если startup/transition не завершился;
- наблюдаемо логировать transitions и связанные session IDs.

### 1.2. In scope

- один постоянный host scene;
- состояния `Boot`, `Loading`, `MainMenu`, `EnteringGameplay`, `Gameplay`, `Returning`, `Error`;
- динамически создаваемые Loading, MainMenu и gameplay presentation/session graphs;
- один serialized Unity context boundary;
- `ApplicationRoot -> ApplicationFlowRoot -> active child graph` ownership;
- MainMenu и Loading как реальные MVVM screen states;
- gameplay shell как MVP;
- запуск существующего SquadCombat через его Application factory/session contract и immutable settings;
- обработка double-click, concurrent transition, reset, return, focus loss, disposal и errors;
- pure state-machine, lifecycle, PlayMode и Unity MCP validation.

### 1.3. Non-goals

- отдельные Loading, MainMenu или Combat scenes;
- production metagame, hub, profile, settings, roster, save, economy или content menu;
- route table, navigation stack, history, deep links или universal screen framework;
- mode registry, string mode IDs, reflection discovery или generic feature runner;
- создание будущих modes/menus до появления реальных consumers;
- SceneSwitcher integration, wrapper, adapter или migration;
- Addressables runtime loading до появления generated-key boundary;
- loading simulation, fake production delay или progress, не связанный с реальной операцией;
- global manager, service locator, static mutable flow state или feature LightDI scope;
- изменение внутренних actor/AI/combat rules SquadCombat;
- новый scene entry point кроме `GameBootstrapper`.

### 1.4. Главный invariant

После synchronous `Boot` preflight каждое успешно опубликованное operational state `ApplicationFlowRoot` имеет ровно один root-level active child graph и никогда больше одного. Ноль children допустим только во время `Boot`, после начала disposal или после fatal failure, при котором сам Status prefab создать невозможно. Для transition единственным child является internal `TransitionChildGraph`: он временно владеет Status screen и переданным ему outgoing/incoming partial handle; Root не хранит их как параллельных children:

| State | Единственный active child graph |
| --- | --- |
| `Boot` | Нет; выполняется synchronous preflight |
| `Loading` | Status/Loading MVVM screen |
| `MainMenu` | MainMenu MVVM screen |
| `EnteringGameplay` | Один Entering `TransitionChildGraph`: Status/Loading screen + зарегистрированный partial incoming gameplay handle |
| `Gameplay` | Owned gameplay session graph |
| `Returning` | Один Returning `TransitionChildGraph`: Status/Loading screen + переданная ему outgoing gameplay session до controlled stop/dispose |
| `Error` | Status screen в error mode, если он был успешно создан; иначе только один logged fatal boundary |

`EnteringGameplay` и `Returning` не разрешают одновременно интерактивные menu и gameplay graphs. Передача session/partial handle в `TransitionChildGraph` меняет owner, а не дублирует ownership. Короткое visual overlap Status с неинтерактивным partial/outgoing graph существует только внутри этого одного child graph и очищается до публикации следующего stable state.

---

## 2. Проверенный baseline и API findings

### 2.1. Текущий main checkout

Проверено в `C:\UnityProjects\DungeonTeam`:

- Unity `6000.7.0a3`; MCP подключён к этому checkout;
- активна `Assets/Scenes/SquadCombatStage0.unity`;
- build settings содержат одну enabled scene: `SquadCombatStage0.unity`;
- `GameBootstrapper : RootBehaviour` остаётся единственным `Awake` application entry;
- `ApplicationRoot` создаёт текущий `Stage0SquadCombatLauncher`;
- SquadCombat M0 уже имеет Domain/Application/Runtime/Infrastructure assemblies, immutable settings, `ISquadCombatSessionFactory`, `ISquadCombatSession`, root/session lifecycle и JSONL telemetry;
- baseline M0 по переданному результату: EditMode `16/16`, PlayMode `1/1`, Unity MCP visual smoke и telemetry JSONL — READY;
- Unity MCP read-only inspection: scene содержит 6 root objects; orchestration/wiring components сейчас распределены по `GameBootstrapper`, `Stage0SquadCombatHost`, `Stage0SquadCombatHarnessView`, `SquadCombatSceneBindings` и `SquadCombatArenaShellView`;
- Unity Console при design inspection: 0 errors, 0 warnings.

Текущий `SquadCombatStage0TechnicalDesign.md` описывает historical M0 baseline до реализации. Его contract/ownership решения сохраняются, но утверждения об отсутствии SquadCombat code/assets больше не являются текущим состоянием.

### 2.2. Root, Disposable и LightDI

Exact PackageCache findings:

- `Root<TContext>` требует `struct, IRootContext`, допускает одну `Initialize`, отменяет root token до `OnDispose` и делает завершённый `Dispose` идемпотентным;
- initialization failure отменяет token, но не вызывает `OnDispose`: creator обязан очистить partial root;
- `RootBehaviour.InitializeRoot()` создаёт root один раз, а `OnDestroy` вызывает `DisposeRoot()`;
- `CompositeDisposable` не имеет remove API и освобождает sync items в порядке добавления;
- replaceable/order-sensitive screen/session children поэтому хранятся в явных полях, не в terminal composite;
- LightDI local container — один на calling assembly; он непригоден как повторно создаваемый flow/screen/session scope;
- LightDI container владеет зарегистрированными `IDisposable` и освобождает их в прямом порядке;
- `DiContainerProvider.Resolve*` obsolete и в проекте запрещён.

Решение: существующий global container остаётся только application-lifetime ownership. `ApplicationFlowRoot`, screens и gameplay session создаются явными конструкторами/factories и не регистрируются в LightDI.

### 2.3. MVP и MVVM

Exact PackageCache findings:

- gameplay package использует `MVP.Presenter<TView,TModel>` и `MVP.ViewMonoBehaviour<TPresenter>`;
- Presenter выбирает ровно один sync/async initialization path и сам освобождает View/Model;
- UI package type называется точно `Azzazelloqq.MVVM.Core.ViewMonoBehavior<TViewModel>`;
- `ViewModelBase<TModel>` владеет model, а View может dispose вместе с ViewModel;
- `ActionCommand` поддерживает `CanExecute`; запрет double-click всё равно дублируется state machine guard, а не доверяется только UI;
- reactive subscriptions являются disposable ownership;
- при установленном UniTask package public initialize path alias-ится на `UniTask`, но async hooks package используют `ValueTask`.

Решение: Loading/MainMenu — минимальные MVVM screen nodes. Gameplay presentation и Stage 0 gameplay controls — MVP. Flow state machine и transition orchestration не помещаются во ViewModel/Presenter.

### 2.4. UniTask drift

`packages-lock.json` фиксирует UniTask hash `ceac8d6946b1...`, но materialized PackageCache имеет `com.cysharp.unitask@dc216dc4183d`, package version `2.5.11`. Используемые текущим M0 APIs (`UniTask`, `UniTaskCompletionSource`, `AttachExternalCancellation`, observed `Forget`) присутствуют, но hash drift должен быть отмечен implementation compile proof. Источник текущего materialized API — PackageCache; lock/cache mismatch не разрешается этой design-задачей.

### 2.5. SceneSwitcher — rejected alternative

`com.azzazello.sceneswitcher` package version `1.0.3` предоставляет только Unity scene operations:

- `SwitchToScene[Async]` и `UnloadScene[Async]` принимают строковый `sceneId`;
- generic overload только извлекает `ISceneContext` из root GameObject загруженной scene;
- произвольные screen/feature states API не поддерживает;
- implementation напрямую вызывает Addressables scene load/unload, использует `System.Threading.Tasks.Task`, synchronous `WaitForCompletion`, возвращает `default` при части failures/cancellations и не выражает требуемый owner/release/error contract.

**Решение окончательное:** SceneSwitcher полностью исключён из Stage 0 application flow. Wrapper, adapter и migration вокруг него не проектируются. Он может быть заново рассмотрен только в отдельном design реального multi-scene use case.

### 2.6. ResourceLoader и Addressables 3.1.0

Проверено:

- установлен `com.unity.addressables` version `3.1.0`;
- project generated-key API отсутствует;
- `Docs/AI/libraries/addressables.md` запрещает новый runtime Addressables code до генератора;
- текущий game runtime не использует Addressables keys;
- `IResourceLoader` принимает строковый `resourceId` и возвращает `Task`;
- `AddressableResourceLoader` содержит synchronous `WaitForCompletion` paths;
- `LoadAndCreateAsync` делает `LoadAssetAsync<GameObject>` + `Object.Instantiate`, но tracking/release относится к load handle и не выражает ownership/destroy созданного instance как отдельного handle;
- cancellation paths могут вернуть `default`, что нарушает запрет silent fallback.

**Решение Stage 0:** не использовать `Addressables`, `AssetReference`, `ResourceLoader` или string keys. Prefab assets передаются как direct serialized references через host context; instances создаются `Object.Instantiate` и уничтожаются единственным owned handle через `Object.Destroy`.

Будущий loader допустим только после отдельного generated-key milestone и design revision. Тогда он обязан:

- принимать generated resource ID, не string;
- возвращать owned handle, зарегистрированный до initialization;
- различать success, cancellation и error;
- при Addressables instantiation сохранять matching handle и использовать `ReleaseInstance`;
- при load-asset + manual instantiate отдельно уничтожать instance и release load handle;
- не возвращать `null/default` как cancellation/error;
- не менять flow state semantics.

---

## 3. Архитектурное решение

### 3.1. Ownership tree

```text
GameBootstrapper : RootBehaviour                 # единственный Unity entry
└─ ApplicationRoot : Root<ApplicationContext>
   ├─ application services/global LightDI container
   ├─ application-lifetime concrete adapters
   └─ ApplicationFlowRoot : Root<ApplicationFlowContext>
      ├─ pure ApplicationFlowStateMachine
      ├─ current transition operation/token
      └─ at most one active child graph/transition owner
         ├─ Status/Loading MVVM screen
         ├─ MainMenu MVVM screen
         └─ Application gameplay session
            ├─ Stage 0 gameplay-shell MVP
            ├─ ISquadCombatSession
            │  └─ SquadCombatRoot and its owned graph
            └─ owned SquadCombat presentation prefab instance
```

Ownership не дублируется в LightDI, `CompositeDisposable`, scene singleton или static registry.

### 3.2. Responsibilities

| Owner/type | Responsibility | Не знает |
| --- | --- | --- |
| `GameBootstrapper` | Создать application container/logger; преобразовать serialized host references в validated context; создать `ApplicationRoot` | Flow transitions, SquadCombat session, UI behavior |
| `ApplicationRoot` | Создать application-lifetime adapters/factories и `ApplicationFlowRoot`; dispose flow до container | Screen internals, actor/AI rules |
| `ApplicationFlowRoot` | State/transition orchestration; один child graph; cancellation/error boundary | SquadCombat actors, AI, arena internals, concrete Views |
| `ApplicationFlowStateMachine` | Pure allowed transitions и rejection results | Unity, async, factories, DI |
| Screen factory | Instantiate/validate/destroy Loading/MainMenu prefab instances | Flow policy, gameplay |
| Loading/MainMenu ViewModels | Состояние конкретного screen и commands | Unity View, SquadCombat Runtime |
| Gameplay gateway/session | Адаптировать flow contract к existing SquadCombat Application contract; ownership wrapper | Future mode registry, menu content |
| SquadCombat factory/session/root | Immutable attempt settings, combat lifecycle, presentation, telemetry | Application navigation/menu |

### 3.3. Dependency direction

```text
Game.Bootstrap
  -> Game.Architecture.Composition

Game.Architecture.Composition
  -> Game.ApplicationFlow.Application
  -> Game.ApplicationFlow.Runtime
  -> Game.Gameplay.SquadCombat.Application
  -> Game.Gameplay.SquadCombat.Runtime
  -> Game.Gameplay.SquadCombat.Infrastructure

Game.ApplicationFlow.Runtime
  -> Game.ApplicationFlow.Application
  -> Root / Disposable / UniTask / MVP / MVVM / Unity

Game.ApplicationFlow.Application
  -> UniTask
  -> no Unity, no LightDI, no SquadCombat Runtime

SquadCombat Runtime/Infrastructure
  -> existing SquadCombat Application/Domain directions
```

`ApplicationFlow` не ссылается на concrete SquadCombat Runtime. Concrete connection принадлежит `Architecture.Composition`.

### 3.4. Composition boundary для gameplay

`ApplicationFlow.Application` объявляет один текущий contract, отражающий реальную потребность flow:

```text
IApplicationGameplaySessionFactory
  StartAsync(ownerToken) -> UniTask<IApplicationGameplaySession>

IApplicationGameplaySession : IDisposable
  SessionId
  ReturnRequested
  ResetRequested
  RequestPause(reason)
  RequestResume(reason)
  WaitForResultAsync(waitToken) -> UniTask<ApplicationGameplayResult>
  StopAsync(reason, stopToken) -> UniTask<ApplicationGameplayResult>
```

Это не generic mode platform:

- factory ровно одна и передаётся constructor/context injection;
- нет mode ID, registry, route, reflection или switch по feature type;
- MainMenu имеет одну Start command;
- будущий mode может заменить injected implementation либо потребовать новый отдельный selector design;
- текущий flow не меняется из-за внутренних actors/AI SquadCombat.

Concrete `SquadCombatApplicationGameplaySessionFactory` в `Architecture.Composition`:

1. создаёт/регистрирует owned Stage 0 application gameplay-shell MVP для `Return` и `Reset`;
2. создаёт immutable `SquadCombatAttemptSettings` snapshot с новым session ID через текущую Stage 0 definition/validator;
3. вызывает существующий application-lifetime `ISquadCombatSessionFactory.Start(settings, ownerToken)`;
4. `SquadCombatSessionFactory` через feature-specific presentation factory динамически создаёт и регистрирует owned combat presentation lease до `SquadCombatRoot.Initialize`;
5. связывает shell intents и returned SquadCombat session только через wrapper;
6. возвращает wrapper session только после полного success;
7. при любой exception/cancellation освобождает уже созданные элементы в reverse semantic order и пробрасывает исходный результат.

Existing public SquadCombat Application contract не меняется. Реализация `SquadCombatSessionFactory` меняет только Runtime dependency: вместо application-lifetime fixed `SquadCombatSceneBindings` она получает feature-specific `ISquadCombatPresentationFactory`. Его direct-prefab implementation создаёт `SquadCombatPresentationLease` (`bindings + owned instance`). Lease передаётся в `SquadCombatRootContext` и освобождается Root после presenter/bindings teardown. Single-session lease остаётся в одном application-lifetime factory, поэтому создание нового factory на каждый launch запрещено.

Wrapper переводит только application-level lifecycle:

- normal SquadCombat result -> `ApplicationGameplayResult.Completed`;
- Return -> `SquadCombatStopReason.OperatorStop`;
- Reset -> `SquadCombatStopReason.Reset`;
- application disposal -> `SquadCombatStopReason.ApplicationShutdown` там, где controlled async stop ещё возможен;
- synchronous emergency dispose остаётся safety net и не выдаёт неполный telemetry flush за success.

Полный `SquadCombatAttemptResult` и combat telemetry остаются в SquadCombat boundary; flow использует только session ID и terminal application outcome.

### 3.5. Startup sequence и observed async boundary

1. Unity вызывает только `GameBootstrapper.Awake -> InitializeRoot()`.
2. `GameBootstrapper.CreateRoot()` создаёт global container/logger, строит validated immutable `ApplicationContext` и возвращает `ApplicationRoot`.
3. `ApplicationRoot.OnInitialize()` создаёт application adapters и `ApplicationFlowRoot`, записывает root в owned field до его initialization и вызывает `Initialize()`.
4. `ApplicationFlowRoot.OnInitialize()` выполняет только synchronous preflight, создаёт pure controller и фиксирует `Boot`.
5. После successful initialize `ApplicationRoot` вызывает один `ApplicationFlowRoot.StartAsync()` и наблюдает его через единственную explicit fire-and-forget boundary с exception logger callback.
6. `StartAsync()` владеет последовательностью `Boot -> Loading -> MainMenu` и не вызывается повторно.
7. Ожидаемые startup/transition failures обрабатываются внутри flow и приводят к `Error`; callback boundary ловит только unexpected escaped exception, логирует его и инициирует safety cleanup.
8. `ApplicationRoot.OnDispose()` сначала освобождает `ApplicationFlowRoot`, затем explicit application adapters, затем global container.

Ни View, ни scene context, ни gameplay feature не запускают flow из `Awake/Start`. Async work всегда получает root/transition token и имеет наблюдаемый completion/error path.

---

## 4. State machine и transitions

### 4.1. States

| State | Entry work | Allowed intents/results | Exit target |
| --- | --- | --- | --- |
| `Boot` | Synchronous context/factory preflight; создать transition controller | successful preflight; failure | `Loading` / `Error` |
| `Loading` | Создать Status screen в startup mode; выполнить только реальную cold initialization и visibility gate | success; cancellation; error | `MainMenu` / dispose / `Error` |
| `MainMenu` | Создать MainMenu screen; enable Start | first Start | `EnteringGameplay` |
| `EnteringGameplay` | Немедленно disable menu; dispose menu; показать Status screen; создать gameplay session | success; cancellation; error | `Gameplay` / dispose / `Error` |
| `Gameplay` | Dispose Status; активировать gameplay shell; observe result/return/reset/focus | result; Return; Reset; dispose | `Returning` |
| `Returning` | Disable gameplay input; показать Status; controlled stop; dispose gameplay graph | Return complete; Reset complete; error | `MainMenu` / `EnteringGameplay` / `Error` |
| `Error` | Показать sanitized error code/message и log full exception | application dispose; explicit retry не входит в Stage 0 | dispose |

### 4.2. Allowed transition graph

```text
Boot -> Loading -> MainMenu -> EnteringGameplay -> Gameplay -> Returning -> MainMenu
                              ^                              |             |
                              |                              +-- Reset -----+
                              |                                  via EnteringGameplay
                              +---------------------------------------------+

Boot/Loading/EnteringGameplay/Gameplay/Returning -> Error
Any state -> Disposed                         # root lifecycle, не flow state
```

`Disposed` не добавляется в product state enum: это lifecycle `Root.State`. После начала root disposal новые intents отвергаются.

### 4.3. Transition protocol

Для каждой transition:

1. state machine синхронно проверяет intent и выдаёт новый monotonic `transitionId`;
2. stable state меняется на transition state до любого await;
3. команды активного View немедленно становятся non-executable;
4. previous stable child либо disposes, либо атомарно передаёт outgoing session handle единственному `TransitionChildGraph`; два owners одновременно запрещены;
5. создаётся linked transition token от `ApplicationFlowRoot.CancellationToken`;
6. каждый новый child/handle после создания сразу регистрируется внутри этого transition owner;
7. initialization получает owner token;
8. success передаёт ровно один completed child новому stable owner, освобождает остаток transition graph и публикует `ApplicationFlowTransitionResult`;
9. cancellation из-за root disposal очищает transition graph и не показывает error;
10. другая cancellation/error очищает transition graph, публикует failed result и переводит flow в `Error`;
11. никакая async операция не запускается внутри lock.

Flow выполняется на Unity main thread. Atomic state/operation guard всё равно обязателен, чтобы callback, result completion и disposal не начали два перехода на одной frame boundary.

### 4.4. Double-click и concurrent intents

- первая Start command атомарно переводит `MainMenu -> EnteringGameplay`;
- `CanExecute` становится false в том же synchronous call;
- повторный click получает `RejectedAlreadyTransitioning` без новой task/session/log file;
- Return/Reset во время `Returning` также отвергаются;
- simultaneous gameplay result и Return/Reset выбираются по первому принятому transition intent; второй terminal signal только наблюдается и не создаёт новый flow;
- transition ID и session ID позволяют доказать отсутствие duplicate launch.

### 4.5. Reset и Return

Return:

1. `Gameplay -> Returning`;
2. gameplay input/shell intents disabled;
3. Status screen создаётся;
4. `StopAsync(Return)` await;
5. gameplay session/presenter/prefab disposed exactly once;
6. `Returning -> MainMenu`;
7. Status disposed; новый MainMenu graph создан.

Reset:

1. использует тот же `Returning` cleanup с stop reason Reset;
2. old session полностью stop/dispose до нового start;
3. `Returning -> EnteringGameplay` без показа interactive menu;
4. создаётся новый immutable settings snapshot и новый session ID;
5. один и тот же frozen variant/config/seed сохраняется по правилам SquadCombat M0 reset; изменяется только session identity.

### 4.6. Gameplay result

- `WaitForResultAsync` запускается один раз и наблюдается owner-операцией;
- normal result инициирует `Gameplay -> Returning -> MainMenu`;
- result не заменяет обязательный `Dispose`;
- exception не маскируется как victory/defeat и переводит flow в controlled `Error` после cleanup;
- cancellation wait token не останавливает session; session lifetime определяет owner token/Stop/Dispose.

### 4.7. Focus loss

- в `Gameplay` focus loss/gain передаётся session как отдельная pause reason и сохраняет существующую SquadCombat pause stacking семантику;
- focus loss не создаёт новый flow state и не уничтожает session;
- Return/Reset при paused gameplay используют обычный controlled stop path;
- во время Loading/Entering/Returning focus не создаёт дополнительной transition; root cancellation остаётся единственным shutdown signal;
- stale UI/gameplay input очищается соответствующим View/feature adapter до resume.

### 4.8. Disposal during transition

`Root<TContext>` отменяет token до `OnDispose`, поэтому порядок `ApplicationFlowRoot.OnDispose`:

1. запретить новые intents/callbacks;
2. отписать transition and gameplay callbacks;
3. cancel/observe current transition via root token;
4. dispose partial/active gameplay session safety-net path;
5. dispose active screen/presenter handle;
6. очистить owned fields;
7. вернуть управление `ApplicationRoot`, который затем освобождает application adapters/container.

`OnDispose` не блокирует Unity main thread ожиданием async I/O. Нормальные Return/Reset/result paths заранее await controlled stop. Shutdown во время transition логируется как cancelled transition, не как successful return.

---

## 5. Host scene и immutable context boundary

### 5.1. Единственная host scene

Сохраняется существующий path `Assets/Scenes/SquadCombatStage0.unity`; новая scene не создаётся. Имя historical, но rename не нужен для Stage 0 и добавил бы asset churn.

Host scene содержит:

- `GameBootstrapper` — единственный orchestration `MonoBehaviour` и Unity entry;
- Camera и Light;
- один пустой `Transform` content root для динамических instances;
- никаких заранее размещённых Loading/MainMenu/Combat presentation objects;
- никаких `Stage0SquadCombatHost`, `Stage0SquadCombatHarnessView` или self-start components.

### 5.2. Serialized boundary

Предпочтительная минимальная форма — один component `GameBootstrapper` с одним nested serializable field `Stage0ApplicationHostReferences`, тип которого принадлежит `Game.Architecture.Composition`.

Authoring references:

- content root `Transform` из host scene;
- Status/Loading screen prefab;
- MainMenu screen prefab;
- Stage 0 application gameplay-shell prefab;
- SquadCombat Stage 0 presentation prefab;
- immutable Stage 0 definition asset;
- `minimumVisibleDuration` development setting.

`GameBootstrapper.CreateRoot()` вызывает один validation/snapshot method и передаёт в `ApplicationContext` `readonly struct Stage0ApplicationHostContext`. Root после этого не читает mutable authoring wrapper.

Допустимый fallback, только если Unity serialization nested type создаёт подтверждённую проблему: один passive `Stage0ApplicationSceneContext : MonoBehaviour` рядом с `GameBootstrapper`. Он не имеет `Awake`, `Start`, `Update`, async или business logic. Третий orchestration component запрещён.

### 5.3. Validation

До создания `ApplicationFlowRoot` проверяется:

- content root существует, active и принадлежит текущей valid scene;
- все required prefab references и definition существуют;
- prefab root содержит ожидаемый View/bindings contract ровно один раз;
- prefab templates являются asset references, не scene instances;
- content root не является child будущего dynamic prefab;
- `minimumVisibleDuration >= 0`;
- gameplay definition создаёт валидный immutable snapshot.

Missing context/prefab/component/binding не заменяется `Find`, `Resources.Load`, default asset, inactive scene object или hard-coded key. Startup/transition получает explicit exception; logger сохраняет полный detail, UI — безопасный error message/code.

### 5.4. Dynamic graph creation

Status/Loading:

- direct prefab instantiate под content root;
- создать Model/ViewModel, зарегистрировать owned node, initialize ViewModel и View;
- View показывает phase и реальный/indeterminate progress;
- dispose ViewModel/View subscriptions, затем destroy instance.

MainMenu:

- direct prefab instantiate;
- создать MainMenu Model/ViewModel с одной Start command;
- bind View; root владеет node;
- dispose node до gameplay presentation creation.

Gameplay:

- gameplay gateway direct-instantiates application gameplay-shell prefab под content root и создаёт его MVP presenter/model;
- application-lifetime `SquadCombatSessionFactory` через feature-specific presentation factory direct-instantiates combat prefab под content root;
- combat prefab содержит graybox arena, passive `SquadCombatSceneBindings` и `SquadCombatArenaShellView`;
- SquadCombat factory/root динамически создают feature gameplay MVP presenters/models и session;
- никаких scene-owned combat bindings после migration;
- wrapper session владеет application gameplay-shell node и `ISquadCombatSession`; SquadCombat Root владеет combat presentation lease. При teardown сначала stop/dispose SquadCombat session/root/presenters/bindings/combat instance, затем dispose application shell node.

### 5.5. Current M0 wiring refactor

После AF2/AF3 integration:

- `Stage0SquadCombatLauncher` удаляется: его orchestration заменяет `ApplicationFlowRoot` + gameplay gateway;
- `Stage0SquadCombatHost` удаляется: definition/prefab refs переходят в host context snapshot;
- `Stage0SquadCombatHarnessView` удаляется либо его только нужные controls переносятся в gameplay-shell MVP View; IMGUI harness не остаётся production path;
- `SquadCombatSceneBindings` остаётся passive binding, но живёт на dynamically instantiated prefab;
- `SquadCombatArenaShellView` остаётся feature View, но не scene entry/orchestrator;
- graybox arena переносится в presentation prefab;
- `LoggerSquadCombatDiagnostics` и JSONL adapter остаются application composition adapters;
- `GameBootstrapper` остаётся единственным scene behavior, создающим root graph.

Миграция атомарна: final scene не содержит одновременно старый launcher path и новый flow path.

---

## 6. Presentation contracts и lifecycle

### 6.1. Loading/MainMenu MVVM

Каждый screen — реальный replaceable child node с собственными ViewModel/View subscriptions:

- `ApplicationStatusModel/ViewModel/View` представляет `Loading`, `EnteringGameplay`, `Returning` и `Error` display mode;
- `MainMenuModel/ViewModel/View` представляет только `MainMenu` и одну Start command;
- models не содержат gameplay rules;
- ViewModels получают narrow flow intent sink/callback, не `ApplicationFlowRoot` и не container;
- Views только bind reactive properties/commands;
- root создаёт ViewModel до View initialization и освобождает node ровно один раз;
- screen node владеет ViewModel и prefab instance handle; View инициализируется с package `disposeWithViewModel: true`;
- dispose node сначала disposes ViewModel (она владеет Model, а View получает dispose notification), затем destroys prefab instance; View не регистрируется вторым отдельным owner;
- screen node хранится в явном replaceable field.

Один Status prefab для Loading/transition/Error допустим: меняется ViewModel state, а не создаётся menu framework. MainMenu — отдельный prefab и ViewModel, потому что это отдельное interactive screen state.

### 6.2. Gameplay shell MVP

Stage 0 gameplay controls (`Return to Menu`, `Reset`) оформляются как узкий MVP node внутри owned gameplay graph:

- View публикует только user intents;
- Model хранит только shell interaction state (`enabled/disabled`), не combat state;
- Presenter блокирует повторные intents и передаёт их wrapper session;
- Presenter не знает actors, AI, damage, telemetry files или application root;
- gameplay node владеет Presenter и instance handle; package Presenter владеет View/Model, поэтому outer node сначала disposes Presenter, затем destroys prefab instance;
- application flow подписывается на wrapper session intents и освобождает subscription до session;
- SquadCombat world presentation остаётся в собственной Runtime boundary и MVP.

### 6.3. Screen/presentation factory

`Game.ApplicationFlow.Runtime` имеет узкий `IApplicationScreenFactory` с текущими operations `CreateStatus` и `CreateMainMenu`. Он возвращает owned node/handle, а не raw GameObject. Concrete direct-prefab implementation получает только validated context refs и parent.

Contract semantics:

- owner известен до начала initialization;
- instantiate success + later validation failure => instance destroyed;
- cancellation до instantiate => ничего не создаётся;
- cancellation после instantiate => handle destroyed, затем `OperationCanceledException`;
- dispose idempotent;
- release через `Object.Destroy` exactly once;
- null/missing component => exception, не fallback;
- no caching/pooling в Stage 0.

Application gameplay-shell creation принадлежит concrete gameplay gateway, а не screen factory. Combat prefab creation принадлежит feature-specific factory внутри SquadCombat Runtime. Это сохраняет dependency direction и не заставляет ApplicationFlow.Runtime знать SquadCombat Runtime types.

### 6.4. Loading state/progress seam

`ApplicationLoadingSnapshot` immutable:

- `transitionId`;
- phase: `Startup`, `EnteringGameplay`, `Returning`;
- localized-ready status token/plain prototype message;
- `hasProgress`;
- normalized `progress` только если источник даёт реальный progress;
- optional development minimum visibility remaining.

Direct prefab/session Stage 0 operations не выдумывают percentage. `hasProgress=false` показывает indeterminate state.

### 6.5. Minimum visible duration

- production/default value: `TimeSpan.Zero`;
- immutable snapshot создаётся из serialized development setting;
- prototype может задать ненулевое значение, чтобы Loading визуально проверялся;
- задержка использует UniTask и transition token;
- delay не подменяет operation progress и не скрывает completion/error;
- build/profile может выключить её без изменения flow code;
- один render yield после создания Loading допустим как presentation handoff, не fake load.

---

## 7. Event/data seams и diagnostics

### 7.1. Intents

Минимальные immutable intents:

- `MainMenuStartIntent`;
- `GameplayReturnIntent`;
- `GameplayResetIntent`;
- `ApplicationFocusChanged` только как adapter input к active gameplay session.

View callbacks не запускают async transition напрямую. Они передают intent state machine/controller, который выдаёт accepted/rejected result.

### 7.2. Transition result

`ApplicationFlowTransitionResult` содержит:

- application flow instance ID;
- monotonic transition ID;
- from/to state;
- `Succeeded`, `Rejected`, `Cancelled`, `Failed`;
- stable error code без exception stack в UI;
- optional gameplay session ID;
- elapsed duration для diagnostics, не gameplay time.

Full exception остаётся в `IInGameLogger`. Ошибка не конвертируется в success или menu fallback.

### 7.3. Gameplay result/stop seam

`ApplicationGameplayResult` содержит только:

- source session ID;
- terminal kind: `Completed`, `Returned`, `Reset`, `TechnicalFailure`;
- whether controlled stop completed;
- optional telemetry artifact reference/health, если SquadCombat result его предоставляет без раскрытия Runtime;
- error code для flow diagnostics.

Flow не интерпретирует combat actor lists, AI reasons или victory/defeat rules.

### 7.4. Diagnostics и telemetry

Stage 0 не создаёт вторую file telemetry platform. Application transitions логируются через existing application logger:

```text
flow_instance_id, transition_id, from, to, status, gameplay_session_id, error_code
```

SquadCombat продолжает владеть JSONL attempt telemetry. Round-trip proof связывает application transition log и combat artifact по тому же `gameplay_session_id`.

No per-frame logging. Каждая transition пишет bounded start/terminal records; repeated rejected clicks могут агрегироваться либо логироваться только diagnostics level без storm.

---

## 8. Error model

### 8.1. Startup errors

- missing host context/content root/prefab/definition: synchronous preflight failure;
- `ApplicationRoot` очищает созданный `ApplicationFlowRoot`/adapters и container по существующему failure pattern;
- если Status screen ещё не может быть создан, full error пишется logger/Unity Console; никакой alternative scene/menu не загружается;
- если Status screen создан, state становится `Error` и отображает safe code/message.

### 8.2. Transition errors

- screen instantiate/bind/initialize failure;
- immutable settings validation failure;
- gameplay presentation binding failure;
- SquadCombat factory/session start failure;
- controlled stop/telemetry completion failure;
- unexpected result observer failure.

Для каждого случая сначала очищается partial/active child graph, затем публикуется failed transition и `Error`. Старый interactive graph не восстанавливается автоматически: automatic fallback мог бы скрыть leaked ownership или invalid session.

### 8.3. Cancellation

- root/application shutdown cancellation не считается error UI;
- user double-click rejection не cancellation и не exception;
- transition cancellation вне root disposal должна иметь явного caller/reason; Stage 0 не вводит arbitrary cancel button;
- cancellation никогда не возвращает `null/default` session/screen.

### 8.4. Retry

Error retry не входит в Stage 0. Его добавление потребует определить, какие failed adapters/resources безопасно пересоздавать. Для текущего prototype controlled error + application restart достаточны и честнее silent recovery.

---

## 9. Assemblies и future file proposal

Файлы создаются только в implementation milestone, когда в них появляется реальный code.

### 9.1. Production assemblies

```text
Assets/Game/ApplicationFlow/
  Application/
    ApplicationFlowState.cs
    ApplicationFlowStateMachine.cs
    ApplicationFlowContracts.cs
    Game.ApplicationFlow.Application.asmdef
  Runtime/
    Composition/
      ApplicationFlowRoot.cs
      ApplicationFlowContext.cs
      ApplicationScreenFactory.cs
      OwnedApplicationScreen.cs
    Presentation/
      UI/
        ApplicationStatusModel.cs
        ApplicationStatusViewModel.cs
        ApplicationStatusView.cs
        MainMenuModel.cs
        MainMenuViewModel.cs
        MainMenuView.cs
      Gameplay/
        ApplicationGameplayShellModel.cs
        ApplicationGameplayShellPresenter.cs
        ApplicationGameplayShellView.cs
      Prefabs/
        Stage0ApplicationStatusScreen.prefab
        Stage0MainMenuScreen.prefab
        Stage0ApplicationGameplayShell.prefab
    Game.ApplicationFlow.Runtime.asmdef
```

`Game.ApplicationFlow.Application`:

- responsibility: pure state/transition rules and flow-facing gameplay contracts;
- consumers: `Game.ApplicationFlow.Runtime`, `Game.Architecture.Composition`, EditMode tests;
- references: `UniTask` only when async session contract is present;
- `noEngineReferences: true`;
- forbidden: Unity, Root, MVVM/MVP, LightDI, SquadCombat Runtime.

`Game.ApplicationFlow.Runtime`:

- responsibility: `ApplicationFlowRoot`, screen ownership, Unity presentation and MVVM/MVP nodes;
- consumers: `Game.Architecture.Composition`, Runtime/PlayMode tests;
- references: `Game.ApplicationFlow.Application`, `Root`, `Disposable`, `UniTask`, `MVP`, `MVVM.Core`, `MVVM.Reactive`, Unity;
- no direct SquadCombat Runtime reference.

### 9.2. Architecture.Composition additions

```text
Assets/Game/Architecture/Composition/
  Stage0ApplicationHostReferences.cs
  SquadCombatApplicationGameplaySessionFactory.cs
  SquadCombatApplicationGameplaySession.cs
```

Existing `ApplicationContext`, `ApplicationRoot`, `GameBootstrapper` wiring changes remain in their current assemblies. No new composition asmdef создаётся.

### 9.3. SquadCombat presentation proposal

```text
Assets/Game/Gameplay/SquadCombat/Runtime/Presentation/Prefabs/
  SquadCombatStage0Presentation.prefab

Assets/Game/Gameplay/SquadCombat/Runtime/Composition/
  ISquadCombatPresentationFactory.cs
  SquadCombatPresentationLease.cs
  DirectSquadCombatPresentationFactory.cs
```

Prefab owns graybox hierarchy and passive bindings/views. Presentation factory/lease are feature-specific Runtime contracts, not shared resource loading. Existing `SquadCombatSessionFactory` remains one application-lifetime factory and creates a fresh owned presentation lease inside each `Start`. Existing `Game.Gameplay.SquadCombat.Runtime.asmdef` adds `MVP` only if the implementation creates the required presenter code. Application contract remains unchanged.

### 9.4. Test assemblies

```text
Assets/Game/ApplicationFlow/Tests/
  EditMode/Application/
    Game.ApplicationFlow.Application.Tests.EditMode.asmdef
  EditMode/Runtime/
    Game.ApplicationFlow.Runtime.Tests.EditMode.asmdef
  PlayMode/
    Game.ApplicationFlow.Tests.PlayMode.asmdef
```

- Application tests reference only `Game.ApplicationFlow.Application`, UniTask/test packages; `noEngineReferences: true`;
- Runtime lifecycle tests reference Application/Runtime/Root/UniTask and fakes;
- PlayMode references Bootstrap/Composition/ApplicationFlow Runtime and SquadCombat Runtime as required;
- production assemblies never reference tests.

### 9.5. Compile proof

Implementation gate:

1. Application flow Application compiles without Runtime/Composition/Unity refs;
2. Runtime compiles without SquadCombat Runtime;
3. Architecture.Composition is единственная assembly, simultaneously knowing flow Runtime and concrete SquadCombat Runtime;
4. Bootstrap still directly references only Architecture.Composition plus application packages;
5. all affected asmdefs compile after old launcher/host removal;
6. no circular/transitive dumping-ground assembly added.

---

## 10. Test strategy

### 10.1. Pure EditMode behavior

`ApplicationFlowStateMachine` tests assert observable results:

- exact startup path `Boot -> Loading -> MainMenu`;
- Start accepted only from MainMenu;
- double Start produces one accepted transition and one rejection;
- success path `EnteringGameplay -> Gameplay`;
- Return/result path `Gameplay -> Returning -> MainMenu`;
- Reset path `Gameplay -> Returning -> EnteringGameplay -> Gameplay`;
- any defined failure transition enters Error;
- invalid transitions are rejected without state change;
- monotonic transition IDs;
- disposal rejects subsequent intents.

Tests do not assert private methods or implementation-specific callback order unless order is part of ownership behavior.

### 10.2. Root/lifecycle EditMode

С fakes/owned probes:

- create -> register -> initialize ordering;
- partial screen/gameplay start failure disposes every created child exactly once;
- old child disposed before new stable child publication;
- owner cancellation during Entering/Returning cleans partial graph;
- gameplay result vs Return race starts one Returning transition;
- Return/Reset unsubscribes callbacks before session dispose;
- controlled stop awaited before session/prefab destroy;
- emergency root dispose cancels operations and uses safety-net dispose;
- `ApplicationRoot` disposes `ApplicationFlowRoot` before global container;
- no feature/screen registered in LightDI.

### 10.3. PlayMode

- load existing host scene through sole `GameBootstrapper`;
- validate only allowed scene-owned dependency surface;
- Loading prefab dynamically instantiated, then destroyed;
- MainMenu prefab dynamically instantiated and Start available;
- double-click creates exactly one gameplay prefab/session;
- gameplay prefab binding validation succeeds;
- Return destroys gameplay graph and restores one MainMenu graph;
- Reset creates new session ID and old bindings/subscriptions are gone;
- missing prefab/component/binding fixture fails loudly and leaves no leaked instance;
- focus loss/resume forwards pause reason without changing flow state;
- no duplicate root/callback after two full round trips.

### 10.4. Unity MCP/manual smoke

Minimum recorded smoke:

1. editor ready, expected instance/scene selected;
2. Console cleared, compilation complete;
3. enter Play Mode;
4. visually observe Loading -> Main Menu;
5. press Start once and rapidly twice in a separate run;
6. visually observe Loading -> SquadCombat graybox;
7. verify one active combat session ID;
8. Return -> Loading -> Main Menu;
9. Start -> Reset -> new gameplay session ID;
10. focus loss/gain during gameplay;
11. exit Play Mode;
12. Console has no errors/warnings; hierarchy has no leaked dynamic screen/gameplay objects.

Automated test success, compile, MCP visual proof and Console proof report separately.

### 10.5. Round-trip telemetry proof

Required path:

```text
Boot -> Loading -> MainMenu -> Loading/Entering -> SquadCombat
     -> Returning/Loading -> MainMenu
```

Proof records:

- one application flow instance ID;
- unique ordered transition IDs;
- one SquadCombat session ID per start;
- matching session ID in application transition diagnostics and JSONL artifact;
- old JSONL session controlled terminal result on Return/Reset where possible;
- no second `session_started` from double-click;
- old session disposed and lease released before a new session;
- zero remaining dynamic gameplay instances/subscriptions after Return;
- new session ID after Reset/re-entry.

---

## 11. Runtime quality

- Flow is event/async driven; no `Update`, tick handler or per-frame polling.
- Loading progress updates only when value/phase changes.
- MainMenu command state changes only on transitions.
- No LINQ, closures, boxing or string formatting in any future hot gameplay path because flow has no hot path.
- Direct prefab instantiation/destruction occurs only at cold transition boundaries.
- Pooling is prohibited until repeated screen/gameplay lifecycle is measured as material.
- No hierarchy search/`Find`/reflection during transitions; typed serialized refs are prevalidated.
- No fake production delay. Development `minimumVisibleDuration` defaults to zero and is cancellable/immutable.
- Logger emits bounded transition records, not per-frame state.
- Future async loader must preserve owner/handle semantics; it cannot leak raw Addressables handles into flow/UI.

---

## 12. Vertical implementation milestones

### AF0 — Pure flow shell and compile boundary

Deliverables:

- Application flow Application assembly;
- pure states, transition results and state machine;
- flow-facing gameplay session contracts;
- Runtime assembly with `ApplicationFlowRoot` lifecycle shell using fakes/no production scene switch yet;
- focused pure/lifecycle tests;
- asmdef compile proof.

Exit gate:

- all tests green;
- single-transition/double-click/disposal semantics proven;
- no Unity scene/asset migration yet;
- no generic navigation platform.

### AF1 — Dynamic Loading/MainMenu UI and host context

Deliverables:

- Status/Loading and MainMenu MVVM nodes/prefabs;
- direct screen factory/owned handles;
- immutable validated host context;
- existing host scene displays Boot -> Loading -> MainMenu;
- no new scene.

Exit gate:

- prefab/context PlayMode tests;
- MCP visual/Console smoke;
- scene has one allowed orchestration context boundary;
- no fake delay in production/default profile.

### AF2 — SquadCombat gameplay gateway and dynamic presentation

Deliverables:

- composition adapter to existing SquadCombat Application contract;
- application gameplay-shell prefab и dynamic SquadCombat presentation prefab;
- one application-lifetime SquadCombat factory refactored from fixed scene bindings to feature-specific owned presentation leases;
- gameplay-shell MVP Return/Reset intents;
- immutable settings creation per launch;
- controlled stop/result mapping;
- current graybox/bindings moved out of scene.

Exit gate:

- one session only under double-click/race;
- ownership and partial failure tests;
- Start enters existing SquadCombat M0 without knowledge of actors/AI in flow;
- no Addressables/ResourceLoader/SceneSwitcher.

### AF3 — Remove temporary M0 host path and round-trip validation

Deliverables:

- remove old Stage0 launcher/host/harness path;
- final `ApplicationRoot -> ApplicationFlowRoot` wiring;
- focus loss, Return, Reset, result and Error integration;
- full round-trip automated/MCP/manual proof;
- cleanup/session ID/telemetry evidence.

Exit gate:

- one host scene, no separate Loading/MainMenu/Combat scenes;
- only approved scene-owned dependency surface;
- old and new launch paths do not coexist;
- EditMode/PlayMode/compile/MCP/Console validation separately green;
- scoped review finds no service locator, feature LightDI, global manager or universal navigation platform.

Следующий milestone не начинается при HOLD предыдущего.

---

## 13. Risks, guards и rollback

| Risk | Guard |
| --- | --- |
| Two launch paths create duplicate sessions | Atomic AF3 cutover; delete old launcher/host after new gateway passes integration; PlayMode count proof |
| MainMenu grows into metagame platform | One Start command and one ViewModel; new content requires separate design |
| Flow becomes generic router | One injected gameplay factory; no IDs/registry/routes/switch |
| Scene regains orchestration components | One Bootstrap serialized boundary; passive fallback context maximum one component |
| Dynamic prefab loses binding | Preflight/prefab validation + PlayMode missing-binding failure test |
| Dispose races async stop | One transition guard; root token; controlled StopAsync before Dispose on normal paths |
| Loading hides failures | Failed transition -> Error, never automatic menu/gameplay fallback |
| Direct refs block future loading | Screen/gameplay factory boundaries own creation; future generated-key revision replaces adapters, not state machine |
| Addressables handle leak | No Addressables now; future adapter requires owned handle/release tests |
| MVVM/MVP package lifecycle surprise | Exact PackageCache names/order used; register before initialize; explicit replaceable child fields |
| UniTask lock/cache drift | Compile against materialized PackageCache and report mismatch; do not invent extensions |
| Dirty checkout damage | Modify only task-owned files; no stage/commit/push; do not rewrite unrelated dirty changes |

Rollback policy per milestone:

- AF0/AF1 additions can be removed without touching current working SquadCombat M0 path;
- AF2 keeps old path only while integration is not yet accepted, but never enables both at runtime;
- AF3 cutover is considered done only after full round-trip proof;
- if AF3 fails, revert only implementation-task-owned changes to the last green milestone/current M0 wiring; do not leave a runtime feature flag or dual launcher as permanent fallback;
- no stage/commit/push is part of this work.

---

## 14. Definition of Ready

- [x] Scope is an application flow shell, not metagame/menu platform.
- [x] Minimal states and allowed transitions are fixed.
- [x] One persistent host scene and no scene transitions are fixed.
- [x] Scene-owned dependency surface and immutable context conversion are fixed.
- [x] Current Host/Harness/Bindings scene-component reduction is defined.
- [x] Loading, MainMenu and gameplay graphs are dynamically created and owned.
- [x] Loading/MainMenu use MVVM; gameplay shell uses MVP.
- [x] Root/session/subscription/cancellation/release ownership is explicit.
- [x] SceneSwitcher is fully rejected after exact API inspection.
- [x] ResourceLoader/Addressables are excluded; future handle semantics are bounded.
- [x] Presentation prefabs and future loading adapter are replaceable at composition boundaries.
- [x] SquadCombat starts through existing Application factory/session with immutable settings.
- [x] Bootstrap -> ApplicationRoot -> ApplicationFlowRoot startup ownership is fixed.
- [x] Intent/loading/transition/gameplay result seams are fixed.
- [x] Double-click, races, reset/return, focus loss, disposal and errors are covered.
- [x] Missing bindings/resources fail explicitly; no silent fallback.
- [x] Real future files/asmdefs, consumers and compile proof are named.
- [x] EditMode/lifecycle/PlayMode/MCP/Console validation is specified.
- [x] No frame allocation/tick/fake production delay is introduced.
- [x] Full round trip, session IDs, cleanup and telemetry proof are specified.
- [x] Vertical milestones begin with flow shell, then UI, then SquadCombat integration/refactor.
- [x] Definition of Done, rollback and risks are fixed.
- [x] Future modes/meta/menu content are not designed beyond one injected launch contract.

No product, architecture or exact-API blocker remains for AF0 implementation.

---

## 15. Definition of Done

Stage 0 application flow is Done only when:

- existing host scene is the only enabled/used application scene;
- startup visibly and deterministically reaches Loading then MainMenu;
- Start reaches the existing SquadCombat M0 through the existing immutable Application contract;
- Return reaches MainMenu without scene load;
- Reset creates a new clean session without application restart;
- каждое успешно опубликованное operational state после Boot имеет ровно один active child graph и не более одной gameplay session;
- focus loss/resume does not advance gameplay in background or alter flow state;
- disposal during every transition leaves no dynamic instances/subscriptions/session lease;
- missing context/prefab/binding produces controlled Error/logging, not fallback;
- transition/session IDs connect application logs and SquadCombat telemetry;
- pure/lifecycle EditMode tests, affected assembly compile, PlayMode integration and Unity MCP visual/Console smoke are green and reported separately;
- final scene contains no old Stage0 host/harness/launcher path, duplicate entry, service locator, global manager, feature LightDI or separate flow scenes;
- no unrelated dirty file is modified, staged, committed or pushed.

**Final design decision: READY.** Реализация должна выполняться отдельной Codex task в основном checkout с `environment=local`, без worktree, с явным разрешением на code/assets/tests и обязательной Unity MCP validation.
