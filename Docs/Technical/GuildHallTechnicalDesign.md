# DungeonTeam — Guild Hall Technical Design

**Статус:** IMPLEMENTED AND AUTOMATION-VALIDATED THROUGH GH-7 + PP-6 regression; MANUAL FLOW SMOKE/BUILD NOT RUN

**Версия:** 0.7

**Дата:** 16 августа 2026

**Product scope:** [Guild Hall GDD](../Product/GuildHallGDD.md)

**Implementation order:** [Guild Hall Implementation Plan](./GuildHallImplementationPlan.md)

**GH-6 design:** [World Map and Application Flow Technical Design](./WorldMapApplicationFlowTechnicalDesign.md)

**GH-7 design:** [Dungeon Return and Guild Summary Technical Design](./DungeonReturnGuildSummaryTechnicalDesign.md)

**Reusable NPC subsystem:** [Ambient NPC Technical Design](./AmbientNpcTechnicalDesign.md)

**Notice Board subsystem:** [Notice Board Technical Design](./NoticeBoardTechnicalDesign.md)

---

## 1. Цель и граница решения

Нужно добавить три последовательно активируемых player-facing состояния:

```text
Guild Hall Runtime ↔ World Map UI → existing Dungeon Run Runtime
        ↑                               │
        └──────── session result ───────┘
```

`ApplicationRoot` остаётся composition и flow boundary существующего приложения. Он владеет текущим активным root, application-lifetime Player Profile/persistence, собирает входные snapshots и обрабатывает semantic outputs. Guild Hall и World Map не вызывают друг друга и не создают `DungeonRunStartRequest`.

Исторический GH-дизайн не переносит Player Profile/persistence/economy/rank/item ownership в Guild Hall. Текущий PP-срез подключает их только через подготовленные snapshots и Bootstrap callbacks; полноценные квесты остаются вне scope.

## 2. Проверенные ограничения текущего проекта

- `ApplicationRoot` сейчас напрямую оркестрирует `MainMenuRoot` и `DungeonRunHost`.
- `DungeonRunRoot` является concrete runtime root, которым владеет application composition.
- `HeroController` и `IHeroInput` зависят от боевых actor/skill contracts; переиспользовать их как locomotion хаба нельзя без неправильной зависимости.
- `ContextActions` уже предоставляет MVVM-представление списка доступных действий и может быть переиспользован как UI, но не как владелец world interaction rules.
- `UIService` создаёт и освобождает Addressable UI prefab и не принимает решений о навигации.
- Единственная Unity-сцена проекта — `Init`; текущий runtime материализует gameplay внутри неё. Для Guild Hall не требуется новая scene boundary.
- Config инициализируется до запуска player flow и отдаёт типизированные `IConfigPage`.

## 3. Архитектурное решение

### 3.1. Feature boundaries

Foundation GH-0…GH-3 использует три meaningful assembly:

| Assembly | Ответственность | Разрешённые project references |
| --- | --- | --- |
| `DungeonTeam.GuildHall.Application` | Immutable вход/выход Guild Hall, catalogs и подготовка snapshots без Unity | `AmbientNpc.Application`; без Unity, Runtime, UI и Bootstrap |
| `DungeonTeam.GuildHall.Runtime` | Guild Hall root, config adapters, загрузка world prefab, gameplay MVP, input, interactions и композиция reusable ambient NPC/dialogue | `GuildHall.Application`, `AmbientNpc.Application`, `AmbientNpc.Runtime`, `ContextActions.Runtime`, presentation/runtime packages, `ResourceLoader`, Config, generated Addressables |
| `DungeonTeam.WorldMap` | Config, immutable map snapshots, World Map root и MVVM screen | `UIService`, Config, MVVM, generated Addressables |

Для GH-4 добавляются две assemblies reusable-подсистемы:

| Assembly | Ответственность |
| --- | --- |
| `DungeonTeam.AmbientNpc.Application` | Location-neutral NPC/dialogue/profile snapshots, catalogs, selector и state transitions без Unity |
| `DungeonTeam.AmbientNpc.Runtime` | Authored bindings, Ambient NPC MVP, dialogue MVVM и owned NPC set/vignette runtime |

Отдельные `Domain`, `Infrastructure`, `Composition`, `Dialogue`, `Vignette`, `Quest`, `Economy` и `Rank` assemblies сейчас не создаются:

- в Guild Hall ещё нет самостоятельных бизнес-правил, оправдывающих Domain assembly;
- Addressable adapter мал и локален feature runtime;
- ambient NPC и однофразовый dialogue имеют подтверждённый reuse между локациями, но остаются одной небольшой subsystem без внутренних micro-assemblies;
- World Map — самостоятельное application UI state, но пока не требует слоя Application/Runtime из двух assemblies.

Если при реализации фактический код не оправдает `GuildHall.Application`, чистые snapshots остаются в ней всё равно: это реальная compile boundary между flow и Unity runtime, а не заготовка на будущее.

### 3.2. Dependency direction

```text
Bootstrap/ApplicationRoot
├─> AmbientNpc.Application
├─> AmbientNpc.Runtime
├─> GuildHall.Application
├─> GuildHall.Runtime ─> GuildHall.Application + AmbientNpc.Application/Runtime
├─> WorldMap
└─> DungeonRun.Application + DungeonRun.Runtime

GuildHall.Runtime ─X─> WorldMap
GuildHall.Runtime ─X─> DungeonRun.Runtime
WorldMap ─X─> GuildHall.Runtime / DungeonRun.Runtime
AmbientNpc.* ─X─> GuildHall.* / WorldMap / DungeonRun / Bootstrap
```

`ApplicationRoot` — единственное production-место, где одновременно видны concrete roots этих features.

## 4. Public contracts

### 4.1. Guild Hall input

`GuildHallStartContext` — immutable defensive snapshot:

```text
GuildHallStartContext
├─ IReadOnlyList<AmbientNpcSnapshot> Npcs
├─ IReadOnlyList<NoticeBoardOfferSnapshot> Offers
├─ string? SelectedContractId
├─ GuildRunSummarySnapshot? LastRunSummary
└─ GuildProfileSnapshot Profile
```

Минимальные типы:

- `AmbientNpcSnapshot`: location-neutral `npcId`, display-name text snapshot, `dialoguePoolId`, `ambientProfileId` из `AmbientNpc.Application`;
- `NoticeBoardOfferSnapshot`: `contractId`, title, summary, `locationId`, `isAvailable`, optional disabled reason;
- `GuildRunSummarySnapshot`: outcome, dungeon display text и подготовленные строки наград без Unity/config objects.
- `GuildProfileSnapshot`: prepared Gold/rank, roster/team/loadout, equipment/resources and commands through a narrow edit callback; it contains no persistence/config objects.

Snapshot создаётся после валидации catalogs. Runtime не перечитывает mutable config во время активного lifecycle.

### 4.2. Guild Hall output

Guild Hall публикует только semantic commands/callbacks:

- `ContractSelected(string contractId)`;
- `WorldMapRequested()`.

Разговор, открытие/закрытие доски и ambient state локальны активному Guild Hall root и наружу не выходят.

### 4.3. World Map contract

`WorldMapStartContext` содержит defensive snapshot точек:

```text
WorldLocationSnapshot
├─ string LocationId
├─ WorldMapTextSnapshot Title
├─ WorldMapTextSnapshot Description
├─ bool IsAvailable
└─ WorldMapTextSnapshot? DisabledReason
```

World Map возвращает `LocationSelected(string locationId)` или `BackRequested()`. Она не знает, означает ли ID Guild Hall, Dungeon Run, лес или будущий городской экран.

### 4.4. Application session state

`GuildSessionState` принадлежит application flow и не сохраняется на диск:

- `SelectedContractId`;
- optional уже подготовленный `GuildRunSummarySnapshot`;
- guard текущего transition.

Состояние не содержит root, View, config page или Addressables resource. Persistent profile state/session остаётся отдельным application owner; `GuildSessionState` не превращается в профиль.

## 5. Config и catalogs

### 5.1. Статические страницы

| Page | Runtime product |
| --- | --- |
| `GuildHallConfigPage` | `GuildHallCatalog`: Guild Hall NPC selection, movement/interaction settings |
| `AmbientNpcConfigPage` | `AmbientNpcProfileCatalog`: reusable authored behavior parameters |
| `DialogueConfigPage` | reusable `DialogueCatalog`: `dialoguePoolId` → валидированный непустой набор line snapshots |
| `ContractConfigPage` | `ContractCatalog`: `contractId` → definition с `locationId` и текстами |
| `WorldMapConfigPage` | `WorldMapCatalog`: упорядоченные location definitions |

Каждая page создаёт immutable runtime catalog на cold initialization. Catalog:

- запрещает пустые и повторяющиеся ID;
- проверяет все ссылки между definitions;
- возвращает read-only/defensive collections;
- завершает initialization ошибкой при неизвестной ссылке;
- не предоставляет silent fallback для отсутствующего контента.

### 5.2. Text data и локализация

Текущий config-тип текста:

```text
TextDefinition
├─ string TextId
└─ string FallbackRu
```

Каждый feature contract владеет своим маленьким value type (`GuildTextSnapshot` или `WorldMapTextSnapshot`) с `textId` и уже выбранным display text. Общий shared text module ради совпадающей формы не создаётся. Пока display text равен `FallbackRu`. Позже composition подключит localization resolver при сборке snapshots; ViewModel и feature contracts не меняются.

Текст interaction labels также проходит через definitions/snapshot, а не создаётся строковым литералом в controller.

### 5.3. Что остаётся в prefab authoring

Config не хранит:

- позиции и rotation;
- `Transform` маршрутов;
- ссылки на стулья, стойку и доску;
- пары участников сценки;
- colliders, NavMesh/CharacterController bindings;
- camera, lights и materials.

Эти данные принадлежат вручную собранному Guild Hall prefab и проверяются как serialized bindings.

## 6. Guild Hall runtime

### 6.1. Loading model

Guild Hall загружается как project-owned Addressable prefab в существующую `Init` scene.

- Runtime использует `IResourceLoader` и только сгенерированный `AddressableIds`.
- Локальный loader возвращает owned lease: загруженный prefab asset, созданный instance и корневой `GuildHallViewBase`.
- До успешного возврата loader владеет partial state и освобождает его при exception/cancellation.
- После возврата lease принадлежит `GuildHallRoot`.
- На shutdown сначала останавливаются input/tick/presentation children, затем уничтожается instance, затем освобождается загруженный prefab ровно один раз.

Raw key, prefab и resource handle не выходят в Application contracts.

### 6.2. Ownership tree

```text
ApplicationRoot
└─ GuildHallRoot
   ├─ GuildHall world lease
   ├─ GuildHall MVP family
   │  ├─ GuildHallModel
   │  ├─ GuildHallView
   │  ├─ GuildHallPresenter
   │  └─ AmbientNpcSet из reusable subsystem
   ├─ GuildHallInput
   ├─ ContextActions MVVM family
   ├─ active/hidden reusable Dialogue MVVM family
   └─ optional active NoticeBoard MVVM family
```

Root создаёт graph явными конструкторами. Новый LightDI-container не создаётся. Replaceable dialogue/board UI хранится в явном поле и освобождается при замене/закрытии; оно не добавляется навсегда в terminal `CompositeDisposable`.

### 6.3. Guild Hall MVP

`GuildHallView` хранит только serialized bindings:

- player body и movement collider;
- camera;
- player spawn;
- массив interaction points;
- массив `GuildNpcView`/authoring bindings;
- доску, стойку и выход;
- world root для корректного уничтожения.

`GuildHallModel` хранит transient presentation state:

- текущий nearest interaction ID;
- флаг заблокированного world input при открытом modal UI;
- runtime ambient states keyed by stable `npcId`.

`GuildHallPresenter`:

- валидирует соответствие `npcId` prefab bindings и start snapshots;
- обрабатывает movement и interaction availability;
- владеет `AmbientNpcSet` и вызывает его из общей tick-подписки;
- открывает dialogue/board через feature-owned MVVM creation callbacks;
- публикует наружу только contract selection и map request.

Unity lifecycle callbacks не используются в Presenter.

### 6.4. Player movement и input

Ввод хаба не зависит от combat/skills:

```text
IGuildHallInput : IDisposable
├─ Vector2 Movement
└─ Enable()
```

Interaction выполняется через отображаемое `ContextAction`, поэтому отдельная универсальная interact-команда в input contract сейчас не нужна.

`GuildHallPresenter` один раз подписывается на `ITickHandler.FrameUpdate` и передаёт camera-relative direction в player binding с `CharacterController`. Speed/acceleration берутся из validated settings snapshot. При открытом modal UI movement равен нулю.

Текущие `HeroController`, `IHeroInput`, actor combat graph и skill input не используются. Общий locomotion module не выделяется до появления второго production consumer с одинаковой семантикой.

### 6.5. Interactions

Каждая authored point имеет:

- стабильный semantic ID;
- kind: `Npc`, `NoticeBoard`, `Reception` или `Exit`;
- interaction anchor;
- радиус;
- прямую serialized reference на target binding там, где она нужна.

Guild Hall controller с фиксированным интервалом обновляет ближайшую доступную точку без LINQ и временных коллекций. При изменении выбора он обновляет существующий `ContextActionsModel`.

Execution повторно проверяет дистанцию и состояние target. По имени GameObject, tag или обходу hierarchy interaction не разрешается.

### 6.6. NPC ambient state

Один reusable `AmbientNpcPresenter` создаётся на каждый configured `npcId`. `AmbientNpcSet` хранит их по ID, а Guild Hall parent presenter вызывает `Tick(deltaTime)` из одной общей tick-подписки.

Минимальные состояния:

```text
Idle → MoveToAnchor → FaceAnchor → Activity → Idle
```

Доступный набор переходов определяется ambient profile и authored binding. Парная сценка спора использует один authored vignette binding с прямыми ссылками на двух участников; два независимых state machine не пытаются случайно синхронизироваться.

Парная сценка и state machine реализуются в `AmbientNpc.Runtime`, не зависят от Guild Hall и могут быть скомпонованы другим location root. Конкретные anchors, activity kinds и participant IDs принадлежат prefab локации. Полный контракт, ownership и non-goals описаны в [Ambient NPC Technical Design](./AmbientNpcTechnicalDesign.md).

На текущем масштабе pooling не нужен: NPC создаются один раз на lifecycle локации и уничтожаются вместе с ней.

## 7. Feature-owned UI

### 7.1. Dialogue

Dialogue — reusable MVVM popup/overlay из `AmbientNpc.Runtime` с одним текущим line snapshot и командой закрытия.

- NPC interaction передаёт `dialoguePoolId` в локальный dialogue coordinator.
- Coordinator выбирает одну line из валидированного pool.
- Выбор не изменяет business state и не сохраняется.
- ViewModel не читает config и не ищет NPC.

Coordinator получает `System.Random` через конструктор, чтобы выбор можно было детерминировать в EditMode test. Отдельный selector-interface и универсальная dialogue graph abstraction не создаются.

В GH-4 View является serialized child Guild Hall prefab; отдельный Addressable/UIService lifecycle для одного простого popup не создаётся.

### 7.2. Notice Board

Notice Board — MVVM FullScreen/Popup family:

- parent ViewModel владеет item ViewModels по `contractId`;
- item показывает переданный snapshot и возвращает command с ID;
- недоступный item не исполняет выбор и показывает подготовленную причину;
- board не обращается к profile/config/dungeon runtime;
- после выбора обновлённый selected state приходит от owner, а не меняется скрыто во View.

### 7.3. Context actions

Существующая `ContextActions` family переиспользуется как presentation available action. Guild Hall владеет её Model/ViewModel/View и очищает actions до disposal.

Текущие dungeon-specific действия и labels в Guild Hall runtime не используются.

## 8. World Map runtime

World Map — отдельный MVVM screen/root, создаваемый через `UIService`.

```text
ApplicationRoot
└─ WorldMapRoot
   ├─ WorldMapViewModel
   ├─ WorldMapView
   └─ Location item ViewModels keyed by locationId
```

- Root получает `WorldMapStartContext` и callbacks.
- View создаётся через `UIService` по сгенерированному `AddressableIds`.
- Root владеет ViewModel, а `UIService` — UI instance/resource.
- При выборе доступной location карта только публикует ID.
- Application полностью закрывает World Map до запуска следующей feature.
- Disabled location не создаёт destination и не запускает fallback content.

Карта не кэшируется между переходами: текущий объём не оправдывает retained UI state/resource.

## 9. Application flow

### 9.1. Startup

1. Bootstrap создаёт application services и catalogs.
2. Bootstrap загружает/создаёт и валидирует application-lifetime Player Profile до показа Guild Hall.
3. Application создаёт пустой `GuildSessionState` и строит rank-filtered offers/profile snapshot.
4. Application создаёт и инициализирует `GuildHallRoot` из подготовленного `GuildHallStartContext`.
5. Только после успешной инициализации скрывается loading screen и разрешается input.

### 9.2. Guild Hall → World Map

1. Получить `WorldMapRequested` и поставить transition guard.
2. Показать loading/transition UI при необходимости.
3. Остановить и `Dispose` Guild Hall root.
4. Построить актуальный `WorldMapStartContext`.
5. Создать World Map root.
6. Снять guard и показать map.

### 9.3. World Map → Dungeon Run

1. Получить `locationId`.
2. Проверить, что location доступна в актуальном catalog.
3. Для dungeon destination потребовать выбранный `contractId`.
4. Application разрешает contract/destination в существующий `DungeonRunStartRequest` через текущий launch preset catalog и передаёт latest profile team selection.
5. Полностью закрыть World Map.
6. Запустить `DungeonRunHost` существующим lifecycle.

Contract definition хранит semantic destination/launch preset ID; UI не знает dungeon ID, seed и team setup.

### 9.4. Dungeon Run → Guild Hall

1. Принять ровно один terminal `DungeonRunResult`.
2. Bootstrap maps supported rewards to a profile terminal request and performs verified exactly-once settlement.
3. При успешном settlement построить summary только из committed receipt, затем остановить и освободить Dungeon Run.
4. Сохранить committed summary в `GuildSessionState` только для presentation текущей сессии.
5. Построить новый `GuildHallStartContext` с актуальным profile snapshot и создать новую Guild Hall session.

Application cancellation не превращается в игровой результат и не создаёт summary.

### 9.5. MainMenu migration

Guild Hall заменяет player-facing ответственность текущего `MainMenuRoot` по запуску run и показу terminal summary. Постоянно поддерживать два параллельных production flow запрещено.

В GH-6 production startup переведён на Guild Hall и active wiring `MainMenuRoot` удалён из `ApplicationRoot`. В GH-7 после consumer/GUID/asmdef audit физически удалены MainMenu code, tests, prefabs, Addressable entry и generated ID. Developer console продолжает запускать тот же Dungeon Run host напрямую как development-only consumer. Terminal result, session summary и cleanup описаны в [Dungeon Return and Guild Summary Technical Design](./DungeonReturnGuildSummaryTechnicalDesign.md).

## 10. Lifecycle, errors и input ownership

- В каждый момент только один player-facing root принимает input.
- Transition guard запрещает повторный запрос до завершения текущего перехода.
- Owner cancellation передаётся каждой async load/show/hide операции.
- Root cancellation сначала останавливает input и tick, затем освобождает children и resources.
- Ошибка частичной загрузки освобождает всё созданное и возвращает flow в последнее согласованное состояние; silent fallback не используется.
- Неинициализированный Guild Hall не становится active root.
- Неизвестные config ID и отсутствующие prefab bindings являются startup errors с конкретным сообщением.
- Повторный `Dispose` root/lease безопасен; повторное использование уже закрытого UI element запрещено контрактом `UIService`.

## 11. Будущие расширения без текущих заглушек

### 11.1. Player Profile и saves

Реализованный PP-срез строит Guild Hall/Board snapshots из profile + definitions в Bootstrap. SaveStore V2/V4 хранит profile business state и stable IDs; verified persistence и recovery принадлежат PlayerProfile/ApplicationRoot. Guild Hall runtime/UI получает только prepared snapshots и narrow edit callback, не SaveStore/session/catalog/config.

### 11.2. Rank

Реализованный PP-5 rank owner влияет на:

- фильтрацию/availability `NoticeBoardOfferSnapshot`;
- конкретные reception commands;
- profile mutation и persistence.

Board и NPC presentation не меняют контракт.

### 11.3. Selling, items и money

Реализованный PP-3/PP-4 путь передаёт в стойку concrete inventory/resource/price snapshots и narrow sell commands. Reward settlement выполняется Bootstrap до summary; Hall ViewModel не читает Reward config напрямую и не меняет кошелёк.

### 11.4. Quests

Quest definitions/state появляются отдельной feature только вместе с реальным quest behavior. Доска сможет показывать quest-derived offers через существующий offer snapshot либо отдельную вкладку с собственным контрактом. `ContractConfigPage` не превращается в generic quest engine.

## 12. Validation strategy

### EditMode

- catalogs отвергают пустые/повторяющиеся/неизвестные IDs;
- snapshot builder сохраняет все переданные definitions независимо от их количества;
- dialogue selector возвращает строку из указанного pool и детерминируется тестовым RNG;
- notice board ViewModel передаёт выбранный `contractId` и блокирует недоступный item;
- world map ViewModel передаёт выбранный `locationId` и блокирует недоступную точку;
- application session state обновляет выбор и session-only result без persistence;
- тесты не фиксируют число NPC, lines, offers или locations.

### PlayMode

- Addressable Guild Hall prefab имеет один корректный root contract и все обязательные bindings;
- input/tick подписываются и снимаются ровно один раз;
- nearest interaction меняется по дистанции и повторно валидируется при execution;
- modal UI блокирует movement и после закрытия возвращает управление;
- root disposal уничтожает instance и release resource ровно один раз;
- повторный цикл Hall → Map → Run → Hall не оставляет второй active root/input owner.

### Manual Unity proof

- graybox collision, camera и movement работают в реальном prefab;
- NPC визуально достигают authored anchors и парная vignette не рассинхронизируется;
- доска, диалог, выход и возврат доступны из реального layout;
- нет первого видимого кадра неинициализированного UI/world;
- после нескольких переходов не остаются дублированные объекты или активные inputs.

Build и внешний playtest не являются частью этого implementation slice.

PP-6 current automation evidence in the open Unity Editor `6000.7.0a3`: full EditMode `429/429 passed`, PlayMode `102/102 passed`, and `Bootstrap.csproj` compile `0` warnings/`0` errors. Manual Hall/Profile → Map → Run → return/restart smoke and player build remain unrun. Repository-wide `validate-unity-change.ps1 -AllAssets` retains only pre-existing imported showcase unresolved GUID diagnostics and the preserved TMP fallback whitespace; the scoped PP diff check excluding TMP passed.

## 13. Definition of Ready для реализации

Перед кодом пользователь отдельно подтверждает implementation plan. После подтверждения должны быть известны:

- точный Addressable group/name для Guild Hall prefab и World Map UI;
- выбранный input для Editor и текущей target platform;
- вручную собранный layout либо согласованный минимальный authoring checklist;
- production config с хотя бы одним contract, одним registrar pool и валидными location links;
- mapping contract destination в существующий dungeon launch preset;
- список старого MainMenu production wiring, удаляемого после сквозного перехода.
