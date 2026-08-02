# DungeonTeam: технический дизайн модульной архитектуры

## Статус и границы решения

Статус: **READY как архитектурная база для отдельного technical design первой gameplay feature**. Это не разрешение на реализацию.

Документ фиксирует структуру проекта до разработки gameplay. Уже принятые рамки не пересматриваются:

- `GameBootstrapper` — единственная Unity-точка входа;
- application composition выполняется в `Architecture.Composition`;
- один global application container LightDI;
- повторно создаваемый feature graph создаётся явной фабрикой и принадлежит `FeatureRoot`/session;
- gameplay presentation использует MVP, полноценный UI screen/state — MVVM.

Вне scope этого документа:

- механика, сущности, настройки и результат конкретного `SquadCombat`;
- универсальная gameplay-платформа или общий framework запуска feature;
- Stage 1+ и предполагаемые будущие режимы;
- создание кода, сцен, prefab, `.meta`, `.asmdef` и изменение `ProjectSettings`.

## 1. Taxonomy: Area → Module → Layer → Assembly → Scope

Эти понятия описывают разные оси и не взаимозаменяемы.

| Понятие | Что означает | Правило |
| --- | --- | --- |
| **Area** | Организационная область исходников и ответственности верхнего уровня | Не является runtime-объектом, DI scope или обязательной assembly |
| **Module** | Самостоятельная ответственность с владельцем и узким публичным контрактом | Вертикальная feature является модулем; sibling modules не знают concrete-типы друг друга |
| **Layer** | Роль кода внутри модуля и направление зависимостей | Для gameplay starter slice: `Domain`, `Application`, `Runtime`; `Infrastructure` появляется только для реального внешнего adapter |
| **Assembly** | Физическая compile boundary (`asmdef`) | Создаётся только вместе с реальным кодом и доказуемой изоляцией; layer и assembly не обязаны быть 1:1 во всех модулях |
| **Scope** | Runtime-время жизни и владелец object graph/resources | Не определяется assembly; одна assembly может создавать много последовательных feature scopes |

Следствия:

- `Gameplay` — Area, а не общий слой, manager и не assembly.
- `SquadCombat` — первый вертикальный Module внутри `Gameplay`, а не global `Combat`, `AI` или `Input` service.
- `Domain` одного gameplay-модуля не становится «общим Domain проекта».
- Наличие папки не создаёт module или scope; наличие assembly не создаёт container.
- Scope задаётся владельцем и lifecycle, а не namespace, папкой или asmdef.

## 2. Карта областей и модулей

| Area | Module | Состояние | Ответственность | Scope |
| --- | --- | --- | --- | --- |
| `Bootstrap` | `Bootstrap` | Существует | Единственный Unity entry point; создать global container и передать ownership в application root | Unity application lifetime |
| `Architecture` | `Architecture.Composition` | Существует | Собрать application graph, знать concrete Runtime/Infrastructure и внедрить их Application-контракты потребителям | Application |
| `Gameplay` | одна vertical feature на сценарий; первой будет `SquadCombat` | Extension point, кода ещё нет | Независимый запускаемый gameplay-сценарий | Один scope на owned session |
| `GameFlow` | будущий orchestration module | Только extension point | Выбирать и последовательно запускать feature через их Application-контракты | Application/flow, определяется при реальном сценарии |
| `Modes` | будущий module на реальный режим | Только extension point | Оркестрация правил конкретного режима через Application-контракты feature | Mode/flow |
| `UI` | будущие application screens/states | Только extension point | Меню, навигационные экраны и application UI через MVVM | Screen/state |
| `Development` | будущий test launcher/harness | Только extension point | Запуск feature в изоляции через тот же Application-контракт | Время жизни test host |

Для extension points сейчас не создаются папки, asmdef, root, container, пустые интерфейсы или регистрации. Они материализуются только при первом реальном consumer/use case.

`Bootstrap` не знает конкретные gameplay modules. `Architecture.Composition` является единственным production-местом, которое одновременно знает concrete feature Runtime и consumer вроде будущего `GameFlow`. Test launcher может быть отдельным development consumer, но его создание и injection всё равно выполняет composition; он не становится второй точкой входа.

## 3. Вертикальный gameplay module

### 3.1 Starter slice

Каждая новая gameplay feature начинает только с трёх слоёв:

| Layer | Ответственность | Разрешённые зависимости | Запрещено |
| --- | --- | --- | --- |
| `Domain` | Сущности, value objects, инварианты, детерминированные правила | BCL; локальные типы этого Domain | Unity, DI, async orchestration, Presentation, sibling feature, persistence/Addressables |
| `Application` | Публичный контракт запуска, use cases, ports, immutable settings/result | Свой `Domain`; UniTask только при реальной async-границе | Concrete Runtime/Infrastructure, Unity View, LightDI, sibling feature concrete types |
| `Runtime` | FeatureRoot/session/factory implementation, Unity adapters и gameplay MVP; локальный MVVM только если feature реально владеет screen/state | Свои `Application` и `Domain`, Unity и нужные presentation packages | Global resolution, создание application container, прямой доступ к sibling Runtime |

`Runtime` — внешний executable слой feature. Внутри него допустимы организационные папки `Composition` и `Presentation`, но они не требуют отдельных asmdef в starter slice.

### 3.2 Когда появляется Infrastructure

`Infrastructure` добавляется только если feature имеет реальный внешний port из `Application`, например загрузку ресурса, persistence, сеть или engine integration, которую полезно отделить от Runtime. Условия появления:

1. внешний dependency и его adapter уже нужны текущему slice;
2. port принадлежит семантике feature либо существующему application module;
3. ownership ресурса и release path определены;
4. отдельная compile boundary удаляет реальную зависимость, а не готовится к гипотетическому будущему.

Если adapter мал, локален и не отделяет внешний dependency, он остаётся в `Runtime`. Пустой `Infrastructure` layer/assembly запрещён.

`Runtime` и отдельная `Infrastructure` являются sibling outer layers и не ссылаются друг на друга. Оба реализуют/используют Application-контракты, а `Architecture.Composition` создаёт concrete adapter и передаёт его Runtime factory через конструктор.

### 3.3 Non-goals gameplay area

Не создаются global `CombatManager`, `AIManager`, `InputManager`, `GameplayService`, общий `FeatureRunner` или «shared gameplay domain». Input, AI и combat-правила начинаются внутри feature-owner. Их выделение рассматривается только по правилу shared modules из раздела 11.

## 4. Публичный контракт запуска feature

### 4.1 Контракт принадлежит конкретной feature

Публичный API располагается в `<Feature>.Application`. Каждая feature определяет собственные типы, например:

```text
I<Feature>SessionFactory
  StartAsync(<Feature>Settings, ownerToken) -> I<Feature>Session

I<Feature>Session : IDisposable
  WaitForResultAsync(waitToken) -> <Feature>Result
```

Это форма per-feature API, а не требование создать общий generic interface или shared assembly. Имена и поля конкретных типов определяются в technical design самой feature.

### 4.2 Settings

`<Feature>Settings`:

- является immutable snapshot на момент запуска;
- не содержит `GameObject`, `MonoBehaviour`, mutable `ScriptableObject`, container или service locator;
- не отдаёт наружу mutable collections; используются immutable/read-only значения либо defensive copy;
- содержит только входные данные сценария, а не runtime state;
- валидируется до передачи ownership session вызывающему коду;
- не меняется при повторном чтении config/save после запуска.

Если нужны Unity assets, settings хранит стабильное значение/идентификатор либо подготовленный immutable config snapshot. Загрузка и release относятся к Runtime/Infrastructure и владельцу scope.

### 4.3 Session, lifecycle и result

`I<Feature>SessionFactory` — application-lifetime factory, implementation которой находится в Runtime. Она создаёт новый graph для каждого запуска явными конструкторами.

`I<Feature>Session` — единственная ownership handle запущенного feature:

- после успешного `StartAsync` ownership session передаётся caller;
- до успешного возврата factory владеет частично созданным graph и очищает его при любой ошибке/cancellation;
- caller хранит session в явном поле/owned collection и вызывает `Dispose` в `finally` или при завершении своего scope;
- `Dispose` останавливает незавершённый feature и идемпотентен;
- повторный запуск создаёт новую session; повторная инициализация того же root запрещена;
- одновременность запусков является решением конкретной feature, а не скрытым singleton-ограничением архитектуры.

`ownerToken` связывает session с lifetime caller. Его отмена завершает активный feature. `waitToken` отменяет только ожидание конкретного caller; остановка самой session выполняется её owner через `Dispose` либо отмену `ownerToken`.

Baseline использует синхронный `IDisposable`, потому что текущий `Root` имеет синхронный lifecycle. Если реальный adapter потребует ожидаемого async shutdown, technical design feature обязан добавить явный `StopAsync`/`IAsyncDisposable` boundary; прятать async cleanup в fire-and-forget `Dispose` запрещено.

`<Feature>Result`:

- immutable и не содержит Unity objects/resources;
- описывает только нормальный observable outcome feature;
- возвращается один раз через completion boundary session;
- не используется для маскировки exception или cancellation: ошибка пробрасывается на orchestration boundary, отмена остаётся cancellation;
- после получения result caller всё равно освобождает session.

Application contract не раскрывает `FeatureRoot`, Presenter, View, Addressables handle или concrete Runtime type.

## 5. Composition и consumers

### 5.1 Кто знает concrete Runtime

Только:

- собственная assembly `Game.Gameplay.<Feature>.Runtime`;
- `Game.Architecture.Composition`, которая создаёт implementation factory и внедряет её как Application-контракт;
- узкие Runtime/PlayMode tests конкретной feature.

Будущие `GameFlow`, mode и test launcher знают только `Game.Gameplay.<Feature>.Application`. Они получают `I<Feature>SessionFactory` через constructor/context injection.

### 5.2 Wiring

Production wiring:

```text
GameBootstrapper
  -> создаёт один global application container
  -> создаёт ApplicationRoot и передаёт container ownership

ApplicationRoot / Architecture.Composition
  -> создаёт concrete <Feature>SessionFactory из Feature.Runtime
  -> создаёт GameFlow/Mode consumer
  -> передаёт factory как I<Feature>SessionFactory

GameFlow/Mode
  -> StartAsync(immutable settings, owner token)
  -> владеет returned session
  -> ждёт result
  -> Dispose session
```

Test wiring использует ту же нижнюю половину. Composition создаёт concrete factory и внедряет Application-контракт в test launcher. Test launcher не вызывает `DiContainerProvider.Resolve`, не создаёт global/local container, не создаёт `ApplicationRoot` и не использует `RuntimeInitializeOnLoadMethod`. Наличие Unity callback у test View/adapter не делает его entry point: application graph по-прежнему создаёт только `GameBootstrapper`.

### 5.3 Orchestration нескольких feature

Если flow должен вызвать несколько sibling feature, он зависит от их Application assemblies и координирует sessions. Feature A не вызывает Runtime feature B и не владеет её root. Общая последовательность принадлежит `GameFlow`/Mode, а не одной из siblings.

## 6. Assembly graph и compile proof

### 6.1 Существующие assemblies

```text
Game.Bootstrap
  -> Game.Architecture.Composition
  -> LightDI, Logger, Root

Game.Architecture.Composition
  -> Root, LightDI, Logger
```

При подключении первой feature composition получит зависимости на её Application и Runtime. Bootstrap не получает прямую ссылку на feature.

### 6.2 Assemblies gameplay starter slice

Создаются только вместе с кодом соответствующего слоя:

```text
Game.Gameplay.<Feature>.Domain
  -> no project assemblies
  -> no Unity engine references

Game.Gameplay.<Feature>.Application
  -> Game.Gameplay.<Feature>.Domain
  -> UniTask, только если контракт/use case действительно async

Game.Gameplay.<Feature>.Runtime
  -> Game.Gameplay.<Feature>.Application
  -> Game.Gameplay.<Feature>.Domain, только если использует Domain-типы напрямую
  -> Unity / MVP / MVVM packages по фактическому коду

Game.Gameplay.<Feature>.Infrastructure          [только при реальном adapter]
  -> Game.Gameplay.<Feature>.Application
  -> Game.Gameplay.<Feature>.Domain, если нужно
  -> конкретный внешний package

Game.Architecture.Composition
  -> Game.Gameplay.<Feature>.Application
  -> Game.Gameplay.<Feature>.Runtime
  -> Game.Gameplay.<Feature>.Infrastructure, если оно существует

Game.Gameplay.<Feature>.Tests.EditMode          [при первых Domain/Application tests]
  -> только тестируемые Domain/Application assemblies

Game.Gameplay.<Feature>.Tests.PlayMode          [только при Unity wiring/lifecycle tests]
  -> Runtime и необходимые test packages
```

Публичные settings/result предпочтительно принадлежат Application и не раскрывают Domain-типы, чтобы consumer требовал одну прямую ссылку на Application. Если конкретный design намеренно раскрывает Domain value object, consumer обязан явно сослаться и на Domain; это фиксируется как осознанное расширение API.

### 6.3 Allowed directions

```text
Bootstrap -> Architecture.Composition
Architecture.Composition -> consumer modules + Feature.Runtime/Infrastructure/Application
GameFlow/Mode/TestLauncher/UI -> Feature.Application
Feature.Runtime -> Feature.Application -> Feature.Domain
Feature.Infrastructure -> Feature.Application -> Feature.Domain
Tests -> target assembly
```

### 6.4 Forbidden directions

- `Domain -> Application/Runtime/Infrastructure/Unity/DI`;
- `Application -> Runtime/Infrastructure/Composition/Unity View/LightDI`;
- прямые `Runtime -> Infrastructure` и `Infrastructure -> Runtime`; concrete adapter соединяет только Composition;
- `Feature A -> Feature B Runtime/Infrastructure/Presentation`;
- `Feature Runtime -> GameFlow`, Mode, Bootstrap или application UI;
- `Bootstrap -> concrete Feature Runtime`;
- любой gameplay/UI consumer -> `DiContainerProvider.Resolve<T>()`;
- runtime-to-runtime cycle, asmdef cycle или «Shared» dumping ground для разрыва cycle;
- Editor/test assembly -> production assembly в обратном направлении.

### 6.5 Compile proof при реализации

Граница считается доказанной только после следующих проверок:

1. Domain компилируется с `noEngineReferences: true` и без project references.
2. Application компилируется без Runtime/Infrastructure assemblies.
3. Consumer test assembly компилируется, ссылаясь на Application contract, но не на Runtime.
4. Runtime компилируется с направлением только к своим Application/Domain и фактическим packages.
5. Composition является единственной production assembly, где одновременно видны consumer и concrete Runtime.
6. Удаление ссылки на sibling Runtime не ломает feature; orchestration остаётся в GameFlow/Mode.
7. Компилируются все затронутые assemblies, затем запускаются focused EditMode/PlayMode tests согласно разделу 13.

Существование asmdef-файла само по себе не является proof: Unity compilation должна пройти после каждого изменения reference graph.

## 7. DI scopes и explicit construction

### 7.1 Единственный global scope

- Только `GameBootstrapper` вызывает `DiContainerFactory.CreateGlobalContainer()`.
- Созданный container передаётся в `ApplicationRoot`; ownership transfer явный, и `ApplicationRoot` освобождает container при завершении application scope.
- Ни одна assembly не «выбирает» global container. Выбор происходит только в Bootstrap composition boundary.
- `CreateContainer()` запрещён: текущий LightDI API фактически регистрирует его как ещё один global container, несмотря на нейтральное имя.
- `DiContainerProvider.AllowMultipleGlobalContainers` не включается; проектное правило — ровно один global container.
- `DiContainerProvider.Resolve<T>()` и `ResolveFromContainer` запрещены в project runtime. Новое инфраструктурное исключение возможно только через отдельный ADR.

Global container предназначен для настоящих application-lifetime services. Он не хранит активные feature sessions, Presenter/ViewModel, scene objects или повторно создаваемые roots.

### 7.2 Feature scopes

Feature factory создаёт session graph явными конструкторами. `FeatureRoot` владеет дочерними объектами и token; session скрывает concrete root за Application-контрактом. На каждый запуск создаётся новый graph без local container.

Local LightDI container сейчас не нужен. Он допустим только для реально изолированного долгоживущего module scope с несколькими скрытыми services после отдельного design review. Текущий API допускает один local container на calling assembly, поэтому local container нельзя использовать как scope повторно/параллельно создаваемой feature.

### 7.3 Ownership зарегистрированных services

LightDI container при `disposeRegistered: true` владеет зарегистрированными `IDisposable` instances. Один объект нельзя одновременно освобождать из Root и container. Текущая реализация LightDI освобождает services в порядке их создания/регистрации, не в обратном порядке. Поэтому:

- dependency с важным shutdown order получает одного явного owner в `ApplicationRoot`, а не полагается на порядок container;
- container хранит только services, для которых такой порядок безопасен, либо один aggregate owner;
- feature graph не регистрируется в container.

## 8. Ownership, cancellation и disposal

### 8.1 Ownership tree

```text
GameBootstrapper (MonoBehaviour adapter)
└─ ApplicationRoot
   ├─ global LightDI container -> application services
   ├─ application-lifetime feature factories
   └─ GameFlow / Mode / Development launcher owner
      └─ I<Feature>Session
         └─ <Feature>Root
            ├─ Application use cases / Domain state
            ├─ Presenter(s) -> Model + Unity View(s)
            ├─ feature-local ViewModel(s)/View(s), только если нужны
            └─ external resource handles/adapters, если нужны
```

Factory владеет создаваемым graph до успешного возврата session. После возврата один caller владеет session. Session владеет FeatureRoot. Root владеет всеми созданными им children/resources. Передача ownership всегда явная; двойная регистрация в Root/container/composite запрещена.

### 8.2 Initialization

Для каждого child/root/presenter/viewmodel действует порядок:

1. создать с явными dependencies;
2. зарегистрировать у единственного owner;
3. инициализировать с owner token;
4. при ошибке освободить уже зарегистрированный graph и пробросить ошибку.

Async-операция, способная пережить кадр, принимает `CancellationToken` и возвращает `UniTask`. `async void` и неотслеживаемый fire-and-forget запрещены; explicit boundary обязана наблюдать и логировать exception.

### 8.3 Cancellation и shutdown

Текущий `Root` отменяет свой token до вызова `OnDispose`. FeatureRoot использует это как основную lifecycle-семантику: сначала stop signal, затем освобождение owned children/resources в явно заданном порядке.

Shutdown session:

1. отменить root/session operations;
2. остановить приём input/events;
3. освободить replaceable child presenters/viewmodels из явных полей/коллекций;
4. освободить остальные presenters/views/subscriptions/resources ровно один раз;
5. завершить release external handles;
6. отбросить root/session.

При unload scene активные feature sessions освобождаются до scene root и до самой сцены. Application shutdown сначала останавливает flow/active sessions, затем explicit application owners, затем global container.

`CompositeDisposable` используется только когда lifetime child точно равен parent lifetime и порядок не важен. Текущая библиотека не поддерживает удаление элемента и освобождает синхронные элементы в порядке добавления. Replaceable children и order-sensitive resources хранятся явно.

`DisposableBase` не заменяет `Root` для feature lifecycle: его внутренний cancellation происходит после managed/composite disposal, тогда как `Root` отменяет token до `OnDispose`.

## 9. Presentation boundaries

### 9.1 World gameplay — MVP

- Unity world object и gameplay scenario используют `ViewMonoBehaviour`, Model и Presenter из MVP package.
- View хранит serialized bindings, отображает состояние и публикует input/animation events.
- Presenter координирует presentation-сценарий, подписки и вызовы Application use cases; Unity lifecycle callback в Presenter запрещён.
- Business rules и authoritative state находятся в Domain/Application, а не во View/Presenter.
- Parent presenter владеет child presenters. Child сообщает вверх через narrow callback/command/interface владельца, а не ищет parent/sibling.
- Runtime hot path не получает manager «на будущее»: `Update`, pooling, cache и tick вводятся только по доказанной необходимости; allocation/performance claim подтверждается profiler.

### 9.2 Full UI screen/state — MVVM

- Меню, окно, HUD screen/state и навигационный UI используют MVVM.
- ViewModel зависит от Application use cases/contracts, но не от View или concrete feature Runtime.
- View только bind-ит reactive properties/commands и владеет своими subscriptions.
- Parent ViewModel владеет item/page ViewModels; replaceable children хранятся в явной коллекции.
- UI ViewModel не содержит gameplay business rules.

Локальный экран, принадлежащий одной gameplay feature и совпадающий с её session lifetime, может жить в feature Runtime и использовать MVVM. Application-wide меню/навигация становится отдельным UI module только при появлении реального screen/state. MVVM не применяется к каждому world object; MVP не используется для полноценного application screen.

## 10. Runtime quality rules

- Feature session не использует static mutable state и не сохраняет Unity objects после disposal.
- Continuous tick/`Update` существует только для реально непрерывной работы; event/explicit call предпочтительнее.
- Unity lookups, hierarchy traversal и resource loads выполняются на cold initialization либо явно доказанной границе, а не неявно каждый frame.
- Pooling появляется только при измеренном повторном instantiate/destroy lifecycle.
- Settings snapshot исключает чтение mutable global config в hot path и недетерминированное изменение активной session.
- Silent fallback запрещён: invalid settings, missing binding/resource и lifecycle error завершают запуск наблюдаемой ошибкой.

## 11. Правило shared modules

Код остаётся локальным в первом module. Выделение shared module разрешено только когда одновременно выполнено всё:

1. существуют минимум два реальных production consumers;
2. у них одинаковая семантика, а не только похожая форма кода;
3. совпадают lifecycle, ownership, error и cancellation semantics;
4. извлечение удаляет конкретное дублирование/неправильное направление dependencies;
5. новый owner и публичный контракт можно назвать без слов `Common`, `Shared`, `Utils` или `Manager`;
6. compile graph остаётся направленным и не появляется sibling cycle.

До этого допускается небольшое локальное дублирование. Нельзя заранее создавать `SharedKernel`, общий combat domain, global AI/input abstraction или универсальный feature launcher.

## 12. Рекомендуемая структура

Текущее состояние сохраняется:

```text
Assets/Game/
  Bootstrap/
    Game.Bootstrap.asmdef
  Architecture/
    Composition/
      Game.Architecture.Composition.asmdef
```

При реализации первой feature, по мере появления кода:

```text
Assets/Game/
  Gameplay/
    <Feature>/
      Domain/
        Game.Gameplay.<Feature>.Domain.asmdef
      Application/
        Game.Gameplay.<Feature>.Application.asmdef
      Runtime/
        Composition/                 # FeatureRoot, session/factory implementation
        Presentation/
          Gameplay/                  # MVP
          UI/                        # только при реальном feature-owned screen/state
        Game.Gameplay.<Feature>.Runtime.asmdef
      Infrastructure/                # только при реальном external adapter
        Game.Gameplay.<Feature>.Infrastructure.asmdef
      Tests/
        EditMode/                    # когда есть Domain/Application behavior
        PlayMode/                    # когда есть Unity wiring/lifecycle
```

Future paths (`GameFlow`, `Modes`, `UI`, `Development`) не резервируются пустыми каталогами. Имена их assemblies определяются при реальном module design и следуют `Game.<Area>.<Module>[.<Layer>]`.

## 13. Milestones и validation

### 13.1 Минимальные milestones

1. **Architecture baseline** — принять этот документ и зафиксировать отсутствие архитектурных blockers.
2. **SquadCombat technical design** — отдельно определить ответственность, non-goals, settings/result, session concurrency, Domain/Application/Runtime responsibilities, resource ports, ownership tree и acceptance criteria без реализации.
3. **Compile skeleton** — только после разрешения создать первый vertical slice, три asmdef и минимальный Application contract; доказать dependency graph Unity compilation.
4. **Pure behavior slice** — реализовать минимальные Domain/Application rules и behavior-focused EditMode tests.
5. **Runtime session slice** — реализовать FeatureRoot/session/factory, MVP binding и lifecycle tests; подключить через Architecture.Composition.
6. **Isolated test launch** — создать только необходимый launcher, внедрить Application contract из composition и выполнить Unity manual smoke.

Каждый milestone должен давать запускаемый/проверяемый vertical result. Infrastructure, GameFlow, Modes и application UI не входят автоматически ни в один milestone.

### 13.2 Validation matrix будущей реализации

| Изменение | Минимальное доказательство |
| --- | --- |
| Domain/Application behavior | Focused EditMode tests + compile |
| Public contract/asmdef | Compile всех затронутых assemblies + consumer compile proof |
| FeatureRoot/session/cancellation/disposal | Focused lifecycle tests + compile |
| MVP/MVVM без serialized wiring | Focused behavior tests + compile |
| Scene/prefab/input/animation/serialized binding | Compile + Unity manual smoke |
| External resource loading | Handle/lifecycle test + Unity manual smoke |
| Заявленный runtime hotspot | Code review + profiler evidence |

Green compile не доказывает scene wiring, input, animation или release resources. Automated tests и manual proof отчётятся отдельно.

## 14. Definition of Ready

### 14.1 Ready для SquadCombat technical design

- принята taxonomy и роль `SquadCombat` как самостоятельного gameplay module;
- согласовано, что public API принадлежит `SquadCombat.Application`, а concrete graph — `SquadCombat.Runtime`;
- подтверждены один global application container и explicit feature construction;
- подтверждено, что GameFlow/test launcher вызывают feature только через injected Application contract;
- отсутствует требование проектировать Stage 1+ или shared gameplay platform;
- известен product scope текущего prototype, но GDD не изменяется архитектурной задачей;
- feature-specific вопросы из раздела 16 переносятся в её technical design, а не угадываются здесь.

### 14.2 Ready для реализации SquadCombat

Реализацию начинать нельзя, пока отдельный SquadCombat technical design не содержит:

- responsibility, observable behavior, non-goals и acceptance criteria;
- точные immutable settings/result и validation rules;
- concurrency/re-entry policy session;
- Domain/Application/Runtime type responsibilities и public surface;
- file/namespace/asmdef map с allowed references;
- ownership tree, startup/failure/completion/cancellation/disposal sequences;
- перечень Unity Views/bindings и способ их получения composition root;
- реальные external ports/adapters и release ownership либо явное подтверждение, что Infrastructure не нужна;
- EditMode/PlayMode/manual validation plan, проверяющий ожидаемое поведение;
- явное разрешение пользователя на реализацию.

## 15. Критический review

| Риск | Проверка | Результат/guard |
| --- | --- | --- |
| Speculative architecture | Есть ли пустые future modules/asmdef/framework? | Нет; future areas только extension points |
| Hidden service locator | Может ли consumer получить feature через static resolve? | Нет; только constructor/context injection, `DiContainerProvider.Resolve` запрещён |
| Второй Unity entry point | Может ли test launcher/GameFlow создать application graph? | Нет; только `GameBootstrapper` создаёт ApplicationRoot/global container |
| Sibling dependency | Знает ли feature A concrete feature B? | Нет; orchestration зависит от Application contracts |
| Premature shared kernel | Есть ли общий Gameplay/Combat/AI/Input module без двух consumers? | Нет; действует правило двух consumers с одинаковой семантикой/lifecycle |
| Feature DI scope | Создаётся ли container на session? | Нет; explicit factory + FeatureRoot |
| Leaked ownership | Неясно, кто освобождает session/resources? | Factory до успешного start, затем caller → session → root → children/resources |
| Presentation leakage | UI/world View содержит business rules? | Нет; world MVP, screen/state MVVM, rules в Domain/Application |
| Compile cycle | Может ли direction замкнуться через Composition/Shared? | Нет; Composition — верхний wiring layer, shared dumping ground запрещён |
| Stage 1+ overdesign | Спроектированы ли режимы/flow/UI заранее? | Нет; только точки подключения |

## 16. Открытые вопросы и документационный drift

Архитектурных blockers для перехода к отдельному SquadCombat technical design нет.

Ниже feature-specific решения, которые намеренно не принимаются здесь и блокируют только реализацию, не следующий design step:

- точный состав `SquadCombatSettings` и `SquadCombatResult`;
- допускается ли более одной одновременной session;
- кто является первым caller: isolated test launcher, будущий flow или оба;
- какие scene/prefab bindings нужны Runtime и кто их предоставляет;
- есть ли реальные external adapters/resources, требующие Infrastructure;
- какие normal outcomes, errors и cancellation наблюдает caller.

Сверка текущих Docs/AI и PackageCache:

- `Docs/AI/module-rules.md` описывает старый feature template с отдельными `Presentation`/`Composition`; для утверждённого здесь starter slice целевая boundary — `Domain`/`Application`/`Runtime`. Синхронизация Docs/AI требует отдельной команды и не входит в эту задачу.
- API `Root` не generic: зависимости feature root передаются через его конструктор или явную factory, а `IRootContext` в пакете отсутствует.
- `Root.InitializeAsync(CancellationToken)` разрешён один раз; `Root.CancellationToken` отменяется до `OnDispose` и при initialization failure. Это подтверждает выбранный shutdown order.
- LightDI `CreateContainer()` является global alias, multiple globals технически разрешены с warning, local container один на calling assembly, а registered disposables освобождаются в прямом порядке. Проектные правила намеренно строже: один `CreateGlobalContainer()` в Bootstrap и явный ownership order.
- `CompositeDisposable` не имеет remove API и освобождает элементы в порядке добавления; поэтому он не используется для replaceable/order-sensitive children.

Эти расхождения не требуют изменения gameplay architecture и не создают HOLD, но должны учитываться при следующем approved documentation/code change.
