# DungeonTeam — World Map and Application Flow Technical Design

**Статус:** GH-6 IMPLEMENTED AND AUTOMATION-VALIDATED; MANUAL FLOW SMOKE NOT RUN

**Версия:** 1.1

**Дата:** 16 августа 2026

**Product scope:** [Guild Hall GDD](../Product/GuildHallGDD.md)

**Implementation order:** [Guild Hall Implementation Plan](./GuildHallImplementationPlan.md)

---

## 1. Цель GH-6

GH-6 создаёт production-путь:

```text
Application start → Guild Hall → World Map → Guild Hall или existing Dungeon Run
```

Карта публикует только stable `locationId`. Она не знает о контрактах, launch presets, Dungeon Run, Guild Hall prefab, рангах, профиле или сохранениях. Конкретное навигационное решение и владение активным feature root остаются в application composition boundary.

GH-6 также выводит старый `MainMenuRoot` из активного production-flow. Его код и assets пока остаются физически в проекте и удаляются в GH-7 после доказанного сквозного цикла.

## 2. Scope и non-goals

В scope:

- standalone World Map MVVM screen через существующий `UIService`;
- immutable start context из текущего `WorldMapCatalog`;
- динамический список locations без фиксированного количества;
- выбор доступной location и возврат назад;
- application-owned session state, destination resolution и переходы;
- production startup сразу в Guild Hall;
- один application transition guard;
- сохранение development-only прямого запуска Dungeon Run;
- focused EditMode/PlayMode validation и actual Addressable prefab lifecycle.

Не входят:

- обработка terminal `DungeonRunResult`, summary и возврат Dungeon → Guild Hall — GH-7;
- физическое удаление MainMenu code/assets/addressable entry — GH-7;
- Player Profile/SaveStore ownership внутри World Map; profile bridge и persistence принадлежат Bootstrap/PlayerProfile;
- Forest и другие новые destinations;
- сцены, общий navigation framework, event bus или service locator;
- новый module DI-container;
- cache/preload/pool World Map UI;
- build artifact и внешний playtest.

## 3. Границы модулей

### 3.1. `DungeonTeam.WorldMap`

Модуль владеет:

- `WorldMapCatalog`, `WorldLocationSnapshot` и config materialization;
- `WorldMapStartContext` и UI text snapshots;
- `WorldMapRoot`;
- World Map MVVM family и location item families;
- UI behavior и lifecycle tests.

Модуль не ссылается на Bootstrap, Guild Hall, Dungeon Run, contracts или session state. Его output-контракт:

```csharp
Action<string> locationSelected
Action backRequested
```

`locationSelected` вызывается только для доступной location и возвращает её исходный stable ID.

Существующий `DungeonTeam.WorldMap` asmdef расширяется только реальными dependencies: `Root`, `UniTask`, `DungeonTeam.UIService`, `DungeonTeam.Addressables`, `MVVM.Core`, `Disposable`, UGUI и TextMeshPro. Новый feature asmdef и локальный LightDI scope не создаются.

### 3.2. `Bootstrap`

`ApplicationRoot` остаётся единственным владельцем:

- `GuildSessionState`;
- активного player-facing root/host;
- решения `locationId → destination`;
- порядка loading, disposal, initialization и input activation;
- application cancellation;
- developer console composition.

Для чистой логики разрешены только две небольшие internal concrete сущности внутри Bootstrap:

- `ApplicationTransitionGate` — хранит текущий `PlayerFlowState` и допускает ровно один переход;
- `WorldMapDestinationResolver` — валидирует location/contract/session и создаёт существующий `DungeonRunStartRequest`.

Они не регистрируются в DI, не являются универсальным router/framework и тестируются как pure C#.

### 3.3. Направление зависимостей

```text
Bootstrap
├─ GuildHall.Application + GuildHall.Runtime
├─ WorldMap
└─ DungeonRun.Application + DungeonRun.Runtime

WorldMap
├─ Configuration
├─ UIService
├─ generated AddressableIds
└─ presentation/lifecycle libraries
```

Обратных ссылок в Bootstrap и cross-feature ссылок WorldMap → GuildHall/DungeonRun нет.

## 4. Data contracts

### 4.1. `WorldMapStartContext`

Контекст содержит defensive ordered snapshot:

- `IReadOnlyList<WorldLocationSnapshot> Locations`;
- `WorldMapUiTextSnapshot Texts`.

Допускается любое количество locations, включая ноль. Пустая карта отображает configured empty-state text и Back. Порядок полностью задаётся catalog/config и не сортируется ViewModel.

### 4.2. `WorldMapUiTextSnapshot`

Минимальный набор localization-ready текстов:

- title;
- back label;
- empty-state text.

Каждый текст содержит stable `textId` и текущий русский fallback. Строки не хардкодятся во View/ViewModel. Эти definitions добавляются в `WorldMapConfigPage`; resolver локализации появится позже без изменения UI contracts.

### 4.3. Destination resolution

`WorldMapDestinationResolver` получает выбранный `locationId` и текущие:

- `WorldMapCatalog`;
- `ContractCatalog`;
- `GuildSessionState`;
- `DungeonRunLaunchPresetCatalog`;
- latest profile-derived team selection prepared by Bootstrap.

Алгоритм:

1. Найти location по ID; неизвестный ID — configuration/programming error.
2. Если location недоступна — не начинать переход.
3. `GuildHall` возвращает typed decision без destination ID.
4. Для `DungeonRun` потребовать выбранный contract.
5. Потребовать, чтобы contract существовал, был доступен и его `LocationId` совпадал с выбранной location.
6. Использовать `location.DestinationId` как ID существующего dungeon launch preset.
7. Создать текущий `DungeonRunStartRequest` через launch preset catalog и latest profile-derived team selection.

Нет silent fallback на default preset или первый contract. Resolver не изменяет session state.

## 5. World Map presentation

### 5.1. Root contract

```text
WorldMapRoot
├─ WorldMapViewModel
├─ WorldMapView (UIService-owned instance/resource)
└─ location item ViewModels keyed by locationId
```

Constructor dependencies:

- `IUiService`;
- immutable `WorldMapStartContext`;
- `Action<string> locationSelected`;
- `Action backRequested`.

Root владеет ViewModel и subscriptions. `UIService` владеет созданным UI instance и Addressables resource.

### 5.2. ViewModel behavior

ViewModel:

- создаёт один item ViewModel на каждый входной snapshot;
- сохраняет catalog order;
- показывает title, description, availability и disabled reason;
- игнорирует selection disabled item;
- после первого принятого location/back command блокирует все navigation commands;
- не знает destination kind и не выполняет навигацию;
- позволяет Application повторно включить interaction только при восстановлении после неудачного перехода.

Item identity — `locationId`, а не индекс или display text. Тесты не ожидают фиксированное количество или конкретный production-набор locations.

### 5.3. View и prefab

`WorldMapView` — `IUIElement` группы `FullScreen` с `KeepInQueue`. Prefab root сохранён inactive и создаётся только через `IUiService.CreateAsync<T>` по сгенерированному `AddressableIds`.

Serialized bindings:

- header;
- Back button;
- empty-state label;
- items container;
- inactive item template;
- `CanvasGroup` для interaction block.

View выполняет только binding и визуальное отражение state. Она не ищет объекты по names/tags и не загружает resources.

## 6. World Map lifecycle

### 6.1. Initialize/show/close

`InitializeAsync`:

1. Создаёт inactive View через UIService.
2. Создаёт и инициализирует ViewModel.
3. Bind View ↔ ViewModel.
4. Оставляет screen hidden до явного `ShowAsync`.

`ShowAsync(ownerToken)` активирует screen через UIService. `CloseAsync(ownerToken)` вызывается ровно один раз владельцем перехода и освобождает UIService resource. После успешного close Root disposes только собственные ViewModel/subscriptions.

Если initialization падает после создания View, Root выполняет async cleanup через `CloseAsync` перед rethrow. На application shutdown сначала dispose активных feature roots, затем global container/UIService остаётся final fallback owner ресурсов.

Повторный `CloseAsync` закрытого элемента запрещён. Cache/preload между посещениями карты отсутствуют.

### 6.2. Cancellation

- Application cancellation token передаётся во все async UI operations.
- Cancellation не публикует location/back и не меняет session state.
- Feature не делает fire-and-forget; `.Forget(Debug.LogException)` допустим только на callback boundary в `ApplicationRoot`.

## 7. Application flow

### 7.1. State и guard

`PlayerFlowState` содержит только фактические состояния текущего milestone:

- `Initializing`;
- `GuildHall`;
- `WorldMap`;
- `DungeonRun`;
- `Disposed`.

`ApplicationTransitionGate` выдаёт disposable/explicit completion lease только для допустимого перехода и отклоняет повторный запрос, пока предыдущий не завершён. Он заменяет `_isDungeonTransitioning`; feature-level bool для application navigation больше нет.

`_hasPublishedTerminalResult` не переносится в новый flow GH-6: terminal result orchestration появляется целиком в GH-7.

### 7.2. Startup

1. Создать application services/catalogs и показать Loading.
2. Загрузить/создать и валидировать application-lifetime Player Profile до Guild Hall startup.
3. Создать пустой `GuildSessionState` и построить актуальный `GuildHallStartContext` из profile-derived offers/snapshot.
4. Создать и полностью initialize `GuildHallRoot`.
5. Зафиксировать state `GuildHall`.
6. Скрыть Loading; только после этого доступен hall input.

`MainMenuRoot` на startup не создаётся и не показывается.

### 7.3. Guild Hall → World Map

1. Guard принимает запрос; Guild Hall блокирует world input.
2. Показать Loading.
3. Построить `WorldMapStartContext` и initialize скрытый `WorldMapRoot`.
4. Полностью dispose `GuildHallRoot`.
5. Зафиксировать state `WorldMap`.
6. Показать World Map как финальную UIService operation.

До successful initialization карты Guild Hall остаётся владельцем world/input. При ошибке partial WorldMap освобождается, Loading скрывается, Guild Hall input восстанавливается, state остаётся `GuildHall`.

### 7.4. World Map → Guild Hall / Back

1. ViewModel блокирует повторные commands, guard принимает переход.
2. Показать Loading; map уходит в FullScreen queue.
3. Await `WorldMapRoot.CloseAsync`, затем dispose Root.
4. Построить новый `GuildHallStartContext`, initialize `GuildHallRoot`.
5. Зафиксировать state `GuildHall` и скрыть Loading.

Если Guild Hall initialization падает после закрытия карты, Application один раз восстанавливает World Map из catalog и возвращает state `WorldMap`. Если recovery также падает, exception логируется на Application boundary, Loading остаётся активным, input остаётся заблокированным; частично созданные owners освобождены.

### 7.5. World Map → Dungeon Run

1. До mutation UI/root resolver полностью валидирует location, выбранный contract и launch preset и создаёт request с latest profile-derived team selection.
2. Guard принимает переход; показать Loading.
3. Await close и dispose World Map.
4. Запустить request через существующий `DungeonRunHost`.
5. Зафиксировать state `DungeonRun`, скрыть Loading.

При ошибке запуска partial run останавливается, затем Application один раз восстанавливает World Map. Fallback на default dungeon запрещён.

GH-6 не владеет profile settlement и terminal return orchestration: это единый scope GH-7/PP-4. После успешного profile settlement Application возвращает игрока в новый Guild Hall; Dungeon Run остаётся активным owner до terminal callback, application shutdown или development-only команды Back.

### 7.6. Unavailable и repeated requests

- Disabled item не вызывает callback уже на уровне World Map ViewModel.
- Application повторно проверяет availability в resolver как boundary validation.
- Repeated clicks отсекаются interaction lock и transition gate.
- Outgoing root всегда закрыт/disposed до активации input incoming root.

### 7.7. Developer console

Console остаётся только при текущем `DeveloperRunConsoleAvailability`. Прямой запуск проходит через тот же application guard, корректно освобождает текущий feature и может заменить active run. Development Back останавливает run без summary и создаёт чистый Guild Hall. Этот путь не становится production navigation API.

## 8. MainMenu migration boundary

В GH-6:

- удалить создание, callbacks и active references `MainMenuRoot` из `ApplicationRoot`;
- удалить MainMenu asmdef references из `Bootstrap.asmdef`, если `rg` подтверждает отсутствие другого Bootstrap consumer;
- не удалять сам MainMenu module, prefab, config и Addressable entry;
- не показывать terminal summary через MainMenu.

В GH-7 после end-to-end proof:

- добавить result → session summary → new Guild Hall flow;
- удалить неиспользуемый MainMenu code/assets/addressable/config по отдельной проверке consumers.

Так в production нет двух параллельных flow, но физическое удаление не смешивается с незавершённой миграцией.

## 9. Failure and ownership matrix

| Момент ошибки | Owner, который остаётся | Обязательная очистка | Recovery |
| --- | --- | --- | --- |
| World Map init из Hall | Guild Hall | partial World Map UI/root | вернуть Hall input |
| Hall init из Map | none после closed Map | partial Hall | один rebuild World Map |
| Dungeon start из Map | none после closed Map | partial DungeonRunHost state | один rebuild World Map |
| Application cancellation | ApplicationRoot shutdown | active feature → UIService/container | без recovery/navigation |
| Repeated command | текущий active owner | ничего нового не создано | ignore |

Ни один feature не disposes application services. `ApplicationRoot` disposes active feature/host до global container.

## 10. Validation design

### 10.1. EditMode

- `WorldMapStartContext` defensive copy и variable/zero locations;
- ViewModel сохраняет порядок и stable IDs;
- available selection публикуется один раз;
- disabled selection ничего не публикует;
- Back и selection взаимно блокируют repeated commands;
- interaction можно восстановить только после failed transition;
- destination resolver проверяет missing selection, unavailable/mismatched contract и unknown preset;
- valid GuildHall/Dungeon decisions;
- transition gate допускает один request и корректно меняет state.

Никакой тест не фиксирует production count locations/contracts.

### 10.2. PlayMode

- actual Addressable World Map prefab: create → show → hide/queue → close → release;
- cancellation/partial initialization не оставляет instance/subscriptions;
- repeated `WorldMapRoot` create/dispose не дублирует listeners;
- test flow Hall callback → Map → fake accepted destination доказывает one-shot callback и отсутствие одновременно активного outgoing input; fake используется только в test harness, production interface ради него не создаётся.

### 10.3. Mechanical checks

- Unity compile всех затронутых asmdef;
- Addressable entry существует, prefab root inactive, generated ID regenerated инструментом;
- serialized bindings не null;
- нет runtime raw Addressable key;
- `rg` подтверждает отсутствие active MainMenu reference в Bootstrap;
- `git diff --check` и относительные ссылки документации.

### 10.4. Manual-only

Manual smoke, если выполняется владельцем проекта в Unity Editor:

```text
start → Hall → board select → exit → Map → Hall
start → Hall → board select → exit → Map → Dungeon
disabled location не переходит
double click не создаёт два root
```

Build и внешний playtest не являются критерием GH-6.

## 11. Implementation order

1. Дополнить immutable World Map contexts/text config и EditMode tests.
2. Реализовать World Map MVVM/root и tests без Bootstrap integration.
3. Собрать inactive UI prefab, зарегистрировать Addressable и сгенерировать ID.
4. Добавить pure transition gate/destination resolver и tests.
5. Перевести `ApplicationRoot` startup и Hall ↔ Map transitions.
6. Подключить Map → Dungeon и development-only replace/back.
7. Удалить active MainMenu wiring/references из Bootstrap, оставив module/assets до GH-7.
8. Запустить Unity compile, focused EditMode/PlayMode и actual Addressable lifecycle validation.

## 12. Done criteria

GH-6 готов, когда:

- production startup открывает walkable Guild Hall без MainMenu;
- Exit открывает World Map, Back создаёт чистый Guild Hall;
- карта отображает любое число configured locations и возвращает stable ID;
- disabled location не запускает transition;
- Dungeon destination требует выбранный matching contract и создаёт существующий request;
- в каждый момент есть один active player-facing owner и один application transition;
- World Map resource создаётся/освобождается через UIService без утечек;
- developer console остаётся development-only;
- автоматические проверки из §10 проходят, а manual-only проверки честно отмечены как выполненные или не выполненные.

## 13. Фактическая реализация и validation

Реализованы:

- `WorldMapStartContext`, UI text snapshots/config и динамические location item ViewModels;
- `WorldMapRoot`, `WorldMapView` и primitive inactive Addressable prefab;
- `ApplicationTransitionGate` и `WorldMapDestinationResolver` в Bootstrap;
- startup в Guild Hall, Hall ↔ Map, Map → Dungeon и development-only replace/back;
- восстановление interaction/предыдущего owner после отклонённого destination или transition failure;
- удаление active MainMenu dependencies из Bootstrap без физического удаления MainMenu module/assets.

Автоматически проверено в открытом Unity Editor `6000.7.0a3`:

- compile затронутых assemblies — без ошибок;
- WorldMap + Bootstrap focused EditMode — `11/11 passed`;
- GuildHall + AmbientNpc regression EditMode — `20/20 passed`;
- actual WorldMap Addressable create/show/items/close/release, два цикла — `1/1 passed`;
- GuildHall regression PlayMode, включая actual Addressable prefab lifecycle — `16/16 passed`;
- WorldMap prefab root inactive, item template inactive, serialized bindings и generated Addressable ID подтверждены mechanically;
- active MainMenu references в `Assets/Code/Bootstrap` отсутствуют.

Не выполнялись manual start → Hall → Map → Dungeon smoke, build и внешний playtest. Отдельный production interface/универсальный transition host только ради интеграционного fake-теста не добавлялся; ordering закрыт pure policy tests, actual feature lifecycle tests и явной application orchestration.

PP-6 current regression reran the full available Unity suites in the open Editor: EditMode `429/429 passed` and PlayMode `102/102 passed`; `Bootstrap.csproj` compiled with `0` warnings and `0` errors. The scoped diff check excluding the user-owned TMP fallback asset passed. Manual profile-mediated Hall → Map → Dungeon → return/restart smoke, player build and external playtest remain unrun.
