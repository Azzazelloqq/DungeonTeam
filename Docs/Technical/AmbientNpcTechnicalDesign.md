# DungeonTeam — Ambient NPC Technical Design

**Статус:** IMPLEMENTED FOR GH-4

**Версия:** 0.1

**Дата:** 15 августа 2026

**Первый consumer:** [Guild Hall GDD](../Product/GuildHallGDD.md)

**Порядок реализации:** [Guild Hall Implementation Plan](./GuildHallImplementationPlan.md), milestone GH-4

---

## 1. Назначение и граница

`AmbientNpc` — переиспользуемый project-owned модуль для небоевых NPC в вручную собранных локациях: Guild Hall, будущих городских интерьерах и других спокойных пространствах.

Модуль предоставляет только уже необходимое GH-4 поведение:

- authored idle/activity/route поведение;
- один общий parent-driven tick без `Update` на каждом NPC;
- stable-ID binding между config snapshot и prefab;
- координацию короткой парной vignette;
- однофразовый разговор из подготовленного dialogue pool;
- корректное создание и освобождение набора NPC и dialogue presentation.

Модуль не является универсальной системой NPC. Он не владеет квестами, торговлей, рангами, отношениями, расписанием, AI боя, сохранениями, навигацией между локациями и процедурной расстановкой.

## 2. Ответственность consumer-локации

Каждая локация остаётся владельцем своего runtime graph и контента:

- выбирает NPC и semantic IDs для активной сессии;
- хранит authored `Transform`, anchors, routes, места сидения и участников vignette в своём prefab;
- создаёт и освобождает `AmbientNpcSet`;
- вызывает `Tick(deltaTime)` из своей единственной tick-подписки;
- связывает world interaction со stable `npcId`;
- блокирует или разрешает собственный player input при открытом dialogue;
- решает, какие действия, кроме разговора, доступны у конкретного NPC.

`AmbientNpc` не знает о Guild Hall, World Map, стойке регистрации или application flow.

## 3. Assembly boundaries

Добавляются две meaningful assembly:

| Assembly | Ответственность | Разрешённые project references |
| --- | --- | --- |
| `DungeonTeam.AmbientNpc.Application` | Immutable NPC/dialogue/profile snapshots, catalogs, deterministic selector и state transitions без Unity | BCL; `noEngineReferences: true` |
| `DungeonTeam.AmbientNpc.Runtime` | authored bindings, Ambient NPC MVP, dialogue MVVM, set/vignette ownership и Unity movement/facing | `AmbientNpc.Application`, MVP, MVVM, Disposable, Unity UI/runtime packages |

Изменение dependency graph:

```text
Bootstrap
├─> AmbientNpc.Application
├─> AmbientNpc.Runtime
└─> GuildHall.Runtime ─> GuildHall.Application
                      ├─> AmbientNpc.Application
                      └─> AmbientNpc.Runtime

GuildHall.Application ─> AmbientNpc.Application
AmbientNpc.* ─X─> GuildHall.* / WorldMap / DungeonRun / Bootstrap
```

Отдельные assemblies для `Dialogue`, `Vignette`, `Schedule` и каждой presentation-family не создаются. Новый LightDI scope не создаётся; graph собирается явными конструкторами владельца локации.

## 4. Public data contracts

### 4.1. NPC и profile snapshots

`AmbientNpcSnapshot` содержит только location-independent данные:

- `npcId`;
- display-name text snapshot;
- `dialoguePoolId`;
- `ambientProfileId`.

`AmbientNpcProfileSnapshot` содержит минимальные числовые настройки текущего authored поведения:

- `ambientProfileId`;
- movement speed и turn speed;
- валидированные диапазоны idle/activity duration;
- признак использования authored route.

Тип конкретной активности (`Stand`, `Watch`, `Sit`, `Drink`) и anchors остаются в prefab binding: они описывают постановку, а не reusable business data.

### 4.2. Dialogue

`DialogueLineSnapshot` содержит stable `lineId` и уже разрешённый display text. `DialoguePoolSnapshot` — непустой defensive список уникальных lines, индексируемый по `dialoguePoolId`.

`DialogueLineSelector` получает `System.Random` через конструктор и возвращает строку только из запрошенного валидированного pool. Отдельный RNG/selector interface не создаётся: одной реализации и детерминированного `Random(seed)` достаточно для текущих тестов.

### 4.3. Миграция текущих Guild Hall contracts

NPC/dialogue-specific типы переносятся из `DungeonTeam.GuildHall.Application` в `DungeonTeam.AmbientNpc.Application`. Guild Hall сохраняет собственные `GuildTextSnapshot`, contracts, offers, session state и interaction labels.

`GuildHallStartContext.Npcs` становится defensive списком `AmbientNpcSnapshot`. `GuildContentValidator` проверяет ссылки Guild Hall NPC через переданные `AmbientNpcProfileCatalog` и `DialogueCatalog`.

Это изменение убирает зависимость reusable NPC semantics от первой локации, не создавая общего text/localization module.

## 5. Config ownership

### 5.1. Общий behavior config

`AmbientNpcConfigPage` создаёт immutable `AmbientNpcProfileCatalog`. Он регистрируется в существующем `ConfigCatalog` и валидирует:

- непустые и уникальные profile IDs;
- положительные speeds;
- корректные min/max duration ranges.

### 5.2. Dialogue config

Существующий `DialogueConfigPage` и его asset переносятся из Guild Hall runtime в `AmbientNpc.Runtime.Config` с сохранением `.meta`/asset identity. Menu name становится location-neutral. Содержимое остаётся configurable и не ожидает фиксированного числа pools или lines.

### 5.3. Location config

`GuildHallConfigPage` продолжает определять, какие NPC присутствуют в Guild Hall, но ссылается на общие `ambientProfileId` и `dialoguePoolId`. Будущая локация сможет создать свои NPC snapshots и использовать те же catalogs/runtime без ссылки на Guild Hall.

## 6. Runtime composition и ownership

```text
GuildHallRoot
├─ GuildHall world lease
├─ GuildHall MVP family
│  └─ AmbientNpcSet
│     ├─ AmbientNpc MVP families keyed by npcId
│     └─ AmbientNpcVignetteController(s)
├─ Dialogue MVVM family (replaceable/active state)
├─ GuildHallInput
└─ ContextActions MVVM family
```

`AmbientNpcSet` — обычный `IDisposable`, не root и не service. Он:

1. принимает defensive NPC/profile snapshots и authored bindings;
2. проверяет точное совпадение ID-set config ↔ prefab без требования фиксированного количества;
3. создаёт и инициализирует дочерние MVP families;
4. индексирует их по `npcId`;
5. выполняет `Tick(deltaTime)` по явному вызову parent;
6. сначала освобождает vignette controllers, затем child presenters.

Consumer создаёт set, регистрирует его владельцем до инициализации и освобождает до уничтожения world prefab.

## 7. Ambient NPC MVP

### 7.1. Family

```text
Presentation/Gameplay/AmbientNpc/
├─ Base/
│  ├─ AmbientNpcViewBase
│  ├─ AmbientNpcModelBase
│  └─ AmbientNpcPresenterBase
├─ AmbientNpcView
├─ AmbientNpcModel
└─ AmbientNpcPresenter
```

- Model хранит текущее state, target anchor index и elapsed duration.
- View хранит body/root transform, authored route/activity/facing bindings и применяет movement/rotation/pose.
- Presenter выполняет state transitions и Unity presentation без собственной tick-подписки.

### 7.2. Минимальные состояния

```text
Idle → MoveToAnchor → FaceAnchor → Activity → Idle
```

Профиль и наличие bindings определяют допустимые переходы. Stationary NPC может переходить `Idle ↔ Activity`, route NPC проходит authored anchors. Runtime не ищет anchors по имени и не строит NavMesh/pathfinding. Для graybox движение выполняется по прямому authored отрезку; layout обязан не ставить маршрут через препятствие.

Во время разговора выбранный presenter временно приостанавливает routine и разворачивает NPC к переданной позиции игрока. После закрытия dialogue routine продолжается; остальные NPC не останавливаются.

## 8. Authored bindings и vignette

`AmbientNpcView` содержит stable `npcId`, body transform, optional route anchors, activity anchor/pose kind и interaction anchor. ID не выводится из имени GameObject.

`AmbientNpcVignetteBinding` содержит stable `vignetteId`, два разных `npcId` и authored anchors/facing targets. Один `AmbientNpcVignetteController` координирует обоих участников; независимые state machines не пытаются синхронизировать спор случайными таймерами.

На primitives vignette доказывается позицией, взаимным facing и чередованием коротких authored activity phases. Animator, lip sync и универсальная timeline-система в GH-4 не добавляются.

## 9. Dialogue MVVM

Dialogue — одна reusable MVVM family внутри `AmbientNpc.Runtime`:

```text
Presentation/UI/Dialogue/
├─ Base/
│  ├─ DialogueViewBase
│  ├─ DialogueModelBase
│  └─ DialogueViewModelBase
├─ DialogueView
├─ DialogueModel
└─ DialogueViewModel
```

ViewModel получает speaker display snapshot, выбранную line и close callback. View только отображает speaker/line и привязывает close command. ViewModel не читает config, не ищет NPC и не меняет gameplay state.

Для GH-4 dialogue View является serialized child Guild Hall prefab и не требует отдельного Addressable/UIService lifecycle. Guild Hall root владеет ViewModel и связывает её с уже загруженной View. Будущая локация может встроить тот же View contract в свой prefab. Policy — один активный dialogue; повторное открытие сначала закрывает предыдущий.

## 10. Guild Hall integration GH-4

`GuildHallRoot` создаёт `AmbientNpcSet` и dialogue coordinator после валидации world bindings, но до активации world. `GuildHallPresenter` остаётся единственным subscriber `ITickHandler` и на каждом frame вызывает movement, interactions и `AmbientNpcSet.Tick(deltaTime)`.

При `GuildInteractionKind.Npc` Hall:

1. повторно требует NPC по stable ID;
2. получает его dialogue pool;
3. выбирает line;
4. приостанавливает выбранного NPC и разворачивает его к игроку;
5. блокирует player movement/interactions;
6. открывает dialogue;
7. при закрытии возобновляет NPC и world input.

Остальные interaction kinds сохраняют текущие semantic callbacks. Reception-specific summary, продажа и rank не входят в GH-4.

## 11. Lifecycle и ошибки

- Все child presenters/viewmodels регистрируются у owner до `Initialize`.
- `AmbientNpcSet.Dispose` и dialogue close/dispose идемпотентны.
- Ошибка неизвестного/повторяющегося ID, отсутствующего pool/profile или обязательного prefab binding останавливает root initialization с конкретным сообщением.
- При partial initialization Guild Hall root освобождает dialogue, set и world lease в обратном порядке.
- Set не подписывается на `ITickHandler`; единственная подписка остаётся у Guild Hall presenter.
- Никакие coroutine, `async void`, fire-and-forget и per-NPC `Update` не используются.

## 12. TDD и validation

### Red → green EditMode

- profile/pool catalogs отвергают пустые, повторяющиеся и некорректные данные;
- selector всегда возвращает line из указанного pool и детерминируется seed;
- state machine проходит допустимые переходы на произвольном валидном наборе данных;
- ID-set validator принимает совпадающие наборы любого размера и отклоняет missing/unknown/duplicate IDs;
- dialogue ViewModel публикует переданные speaker/line и выполняет close один раз;
- тесты не фиксируют production count NPC, profiles, pools, lines или anchors.

### PlayMode

- actual Guild Hall prefab содержит валидные NPC/dialogue/vignette bindings;
- parent tick двигает route NPC и не создаёт per-NPC Unity updates;
- NPC interaction открывает line из его pool, блокирует movement и закрытие возвращает управление;
- root disposal освобождает все presenters/viewmodels и world resource ровно один раз;
- init failure после partial NPC creation очищает уже созданных children.

### Manual Unity proof

- одновременно видны authored stationary, route и paired-vignette activities;
- NPC доходят до anchors, корректно поворачиваются и не проходят через graybox obstacles;
- разговор открывается у каждого configured NPC и закрывается без зависшего input;
- serialized text/button bindings и фактический prefab layout работают в Editor.

Build и внешний playtest не входят в GH-4.

## 13. Non-goals GH-4

- квестовые маркеры и ветвящиеся диалоги;
- память реплик/отношений;
- NavMesh и динамический obstacle avoidance;
- расписания, потребности и автономная social simulation;
- combat actors и reuse `Actor`/`EnemyAI`;
- Animator/Timeline framework;
- persistence, профиль, деньги, продажа и rank;
- отдельный Addressable dialogue window;
- общий localization/text module.
