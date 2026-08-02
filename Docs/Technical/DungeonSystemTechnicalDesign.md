# DungeonTeam — Dungeon System Technical Design

**Статус:** PROPOSED FOR APPROVAL

**Дата:** 1 августа 2026

**Связанный дизайн:** `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md`

## 1. Цель

Система создаёт переиспользуемый экземпляр данжа из конфигурации и seed. Она поддерживает:

1. полностью authored-карту;
2. процедурную сборку из authored-чанков, включая обязательные authored-чанки;
3. полностью процедурную grid-карту без authored-комнат;
4. authored, procedural и mixed-размещение врагов, интересных точек и целей.

Карта остаётся нейтральной. Она описывает геометрию, связность и места размещения, но не управляет боем, врагами, сундуками, ловушками, квестами или выдачей лута.

## 2. Зафиксированные решения

- Одна feature `Dungeon`, без отдельных feature для карты, чанков, population и каждого генератора.
- Три assembly слоя: `Domain`, `Application`, `Runtime`. Editor и test asmdef создаются только вместе с соответствующим кодом.
- Один публичный контракт создания данжа. Внутренние builders не получают публичные интерфейсы.
- В первой версии весь данж строится до начала забега и целиком живёт до завершения попытки.
- Authored-карта хранится как project-owned prefab. Отдельная additive scene и streaming не вводятся.
- Враги и интересные точки описываются authoring-компонентами. Runtime gameplay-объекты создаются и освобождаются их gameplay-владельцами.
- Config хранит правила и стабильные IDs. Unity assets загружаются только в Runtime/Infrastructure через сгенерированные `AddressableIds`.
- Генерация детерминирована seed, имеет ограниченное число попыток и не использует скрытый fallback.
- LightDI для экземпляра данжа не используется. Граф создаётся явными конструкторами в composition.

## 3. Ответственность и границы

### 3.1. `Dungeon` владеет

- выбором layout-definition по ID;
- построением authored, chunked или procedural карты;
- проверкой связности и корректности результата;
- сбором immutable snapshot карты;
- разрешением fixed/slot/optional placements в immutable content plan;
- Unity-геометрией карты и Addressables-ресурсами до `Dispose` экземпляра.

### 3.2. `Dungeon` не владеет

- жизненным циклом врагов, сундуков, ловушек и квестовых объектов;
- combat/encounter state;
- логикой открытия сундука и срабатывания ловушки;
- loot tables, inventory и economy;
- прогрессом квеста или экспедиции;
- камерой, HUD, input и telemetry режима;
- application navigation и сменой сцен.

### 3.3. Публичный контракт

Контракт принадлежит `Dungeon.Application` и не содержит Unity типов, Addressables keys или handles:

```text
IDungeonFactory.CreateAsync(request, ownerToken) -> IDungeonInstance

DungeonBuildRequest
  DungeonId
  ScenarioId
  DifficultyId
  Seed

IDungeonInstance
  MapSnapshot
  ContentPlan
  Dispose()
```

`IDungeonFactory` оправдан реальной границей: режим зависит от нейтрального контракта, а реализация использует Unity и Addressables. Для layout builders, random, validators и content planners дополнительные интерфейсы не создаются.

`CreateAsync`:

- возвращает полностью готовый экземпляр;
- пробрасывает отмену как cancellation;
- пробрасывает один `DungeonBuildException` с причиной `InvalidConfig`, `MissingAsset`, `InvalidAuthoring` или `GenerationFailed`;
- при ошибке освобождает весь частично созданный graph до выхода из метода.

## 4. Владение и lifecycle

```text
ApplicationFlowRoot
└─ DungeonExpeditionSession
   └─ DungeonExpeditionRoot
      ├─ IDungeonInstance
      │  ├─ map GameObjects
      │  ├─ loaded asset handles
      │  └─ navigation data owned by the map
      ├─ enemy/encounter owners
      ├─ interest-point owners
      └─ objective owners
```

Порядок запуска:

1. `DungeonExpeditionRoot` создаёт concrete factory прямым конструктором.
2. Root регистрирует создаваемый экземпляр в своём явном поле владения.
3. Root вызывает `CreateAsync` со своим token.
4. После успешной сборки root создаёт gameplay-объекты по `ContentPlan`.
5. Только после этого забег переходит в активное состояние.

Порядок остановки:

1. Root отменяет свой token и запрещает новые операции.
2. Освобождает objectives, interest points и enemies/encounters.
3. Вызывает `IDungeonInstance.Dispose()`.
4. Instance уничтожает локальные map objects и освобождает Addressables handles.

Карта освобождается последней, потому что gameplay-объекты могут находиться в её иерархии или использовать её navigation/placement data.

`IDungeonInstance.Dispose()` идемпотентен. После успешной сборки factory не сохраняет владение ресурсами: оно целиком передаётся instance.

## 5. Слои и asmdef

Целевая структура:

```text
Assets/Game/Features/Dungeon/
  Domain/
    Map/
    Scenario/
  Application/
  Runtime/
    Authoring/
    Infrastructure/
    Composition/
  Editor/                  # появляется вместе с asset validation
  Tests/
    EditMode/
    PlayMode/
```

Assemblies:

```text
DungeonTeam.Dungeon.Domain
  noEngineReferences: true

DungeonTeam.Dungeon.Application
  -> DungeonTeam.Dungeon.Domain
  -> UniTask
  noEngineReferences: true

DungeonTeam.Dungeon.Runtime
  -> DungeonTeam.Dungeon.Application
  -> DungeonTeam.Dungeon.Domain
  -> UniTask
  -> Unity Addressables
  -> AI Navigation only when navigation code exists

DungeonTeam.Dungeon.Editor
  -> DungeonTeam.Dungeon.Runtime
  Editor only
```

`DungeonExpedition.Application` зависит только от `DungeonTeam.Dungeon.Application`. Concrete Runtime соединяется в composition.

Пустой `Assets/Code/Gameplay/Dungeon/LevelGenerator/DungeonLevelGenerator.asmdef` заменён новой feature-структурой; отдельная compatibility assembly не оставляется.

Presentation assembly у `Dungeon` не создаётся: карта не является самостоятельной MVP/MVVM-семьёй. Gameplay presentation остаётся у режима и у конкретных gameplay-feature.

## 6. Модель данных

### 6.1. Map snapshot

`DungeonMapSnapshot` — immutable Unity-free data:

```text
DungeonMapSnapshot
  DungeonId
  Seed
  EntryPose
  ExitPose
  Rooms[]
  EnemyPlacements[]
  InterestPointPlacements[]
  ObjectivePlacements[]
```

Pose хранится как проектное value data с position и rotation, а в Unity преобразуется на Runtime boundary.

`DungeonRoomSnapshot` содержит только факты карты:

- стабильный `RoomId` внутри конкретного результата;
- room function tags;
- соседние rooms/connections;
- bounds, необходимые для validation и placement;
- признак main route, если он был определён генератором.

### 6.2. Placement modes

```text
Fixed
  Всегда использует указанный content ID.

Slot
  Сценарий выбирает content из допустимого pool.

OptionalFixed
  Имеет конкретный content ID, но сценарий явно включает placement.
```

Скрытая вероятность внутри authored-карты запрещена. Вся случайность принадлежит scenario planner и seed.

Разделяются три concrete placement data, чтобы не создавать один объект с набором несвязанных optional-полей:

```text
EnemyPlacement
  PlacementId, RoomId, Pose, Mode, SlotTag,
  FixedEnemyId?, EncounterGroupId?

InterestPointPlacement
  PlacementId, RoomId, Pose, Mode, SlotTag,
  FixedInterestPointId?, FixedRewardProfileId?

ObjectivePlacement
  PlacementId, RoomId, Pose, SlotTag
```

`EnemyId`, `InterestPointId`, `RewardProfileId` и `ObjectiveId` — стабильные opaque IDs. `Dungeon` может переносить их в plan, но не знает их runtime-реализации.

### 6.3. Content plan

`DungeonContentPlan` содержит окончательные решения текущего запуска:

```text
DungeonContentPlan
  EnemySpawns[]
  InterestPointSpawns[]
  ObjectiveSpawns[]
  RewardBudgetMultiplier
```

Каждый spawn содержит выбранный content ID, placement ID, pose и необходимые group/profile IDs. Сам объект создаёт соответствующий gameplay owner.

## 7. Config

Используется одна независимая `DungeonConfigPage : IConfigPage`, загружаемая в bootstrap. Новые config pages и `DependencyAwareConfigParser` не нужны.

```text
DungeonConfigPage
  AuthoredDungeons[]
  ChunkedDungeons[]
  ProceduralDungeons[]
  Scenarios[]
  Difficulties[]
```

Раздельные массивы layout definitions исключают комбинации optional-полей вроде `AuthoredPrefabId + ChunkPool + ProceduralSize` в одной записи.

### 7.1. Authored definition

```text
AuthoredDungeonDefinition
  DungeonId
  MapAssetId
```

Вся геометрия, entry/exit и placements находятся в authored prefab.

### 7.2. Chunked definition

```text
ChunkedDungeonDefinition
  DungeonId
  EntryChunkId
  ExitChunkId
  MandatoryChunks[]       # ordered along entry -> exit route
  ChunkPool[]
  TargetChunkCount
  MaxGenerationAttempts
```

- `MandatoryChunks` задают обязательные authored-моменты и их порядок на основном маршруте; позиции определяет генератор.
- Остальные чанки выбираются процедурно.
- `TargetChunkCount` включает entry, exit и mandatory chunks. Если их суммарное число равно target, дополнительные чанки не выбираются.
- Отдельный hybrid layout type не создаётся.

### 7.3. Procedural definition

Первая реализация использует один concrete grid-алгоритм:

```text
ProceduralDungeonDefinition
  DungeonId
  TileSetAssetId
  RoomCount
  CellSize
  MaxGenerationAttempts
```

Никаких algorithm IDs, plugin registries, WFC или generic solver API не вводится.

### 7.4. Scenario definition

```text
DungeonScenarioDefinition
  ScenarioId
  BaseThreatBudget
  EnemyCandidates[]
  InterestPointRules[]
  EnabledOptionalPlacementIds[]
  RequiredObjectives[]
```

Enemy candidate:

```text
EnemyId
Cost
Weight
AllowedSlotTags[]
```

`Cost` и `Weight` — положительные integers, чтобы budget и weighted selection не зависели от погрешности floating-point.

Interest-point rule:

```text
SlotTag
MinCount
MaxCount
Candidates[]  # InterestPointId, Weight, RewardProfileId?
```

Required objective:

```text
ObjectiveId
RequiredSlotTag
```

Quest или режим выбирает другой `ScenarioId`; словарь runtime-overrides и универсальный modifier pipeline не вводятся.

### 7.5. Difficulty definition

```text
DungeonDifficultyDefinition
  DifficultyId
  ThreatBudgetMultiplier
  InterestPointCountMultiplier
  RewardBudgetMultiplier
```

Difficulty не меняет topology и не выбирает concrete Unity assets. Она масштабирует только явно заданные scenario budgets.

Масштабированное количество interest points округляется вниз и ограничивается доступным числом совместимых slots.

`RewardProfileId` выбирает таблицу награды, а `RewardBudgetMultiplier` передаётся loot owner. Сам `Dungeon` не генерирует предметы.

## 8. Unity authoring

### 8.1. Authored map prefab

```text
DungeonMapAuthoring
├─ Geometry
├─ Entry / Exit
├─ Rooms
├─ EnemyPlacementAuthoring...
├─ InterestPointPlacementAuthoring...
├─ ObjectivePlacementAuthoring...
└─ Navigation
```

Level designer задаёт точные positions, rotations, modes, slot tags и fixed content IDs в Inspector.

В первой версии authoring root один раз использует `GetComponentsInChildren` после загрузки и немедленно кэширует результат. Это cold initialization, поэтому отдельный bake pipeline и custom graph editor не вводятся. `Find`, tag lookup и повторный hierarchy traversal в runtime запрещены.

Authoring-компоненты не создают gameplay objects и не запускают логику в `Awake`/`Start`. Они только сериализуют данные и рисуют Gizmos/labels.

### 8.2. Chunk prefab

`DungeonChunkAuthoring` содержит:

- bounds;
- room tags;
- connection ports;
- placements внутри чанка;
- navigation bindings;
- геометрию чанка.

`DungeonConnectionPortAuthoring` содержит pose и один `PortType`. Соединение допустимо, когда port types совпадают, направления противоположны и bounds чанков не пересекаются.

Сложные port rules, adapters и constraint graph не вводятся.

### 8.3. Preview

Обязательный первый уровень UX — Gizmo, label content/slot ID и validation errors в Inspector/Console.

Автоматическое создание полноценного enemy/chest prefab preview в Editor не входит в первый срез. Оно добавляется только после подтверждённой потребности level designer и не влияет на runtime contract.

## 9. Layout builders

Concrete `DungeonFactory` в Runtime выбирает mode по найденной definition. Для chunked/procedural paths он передаёт Unity-free входные данные в concrete Domain planners. Planners не имеют интерфейсов и DI-регистрации.

- Authored path не получает отдельный Domain planner: layout уже задан prefab-ом.
- `ChunkLayoutPlanner` получает immutable metadata загруженных chunk prefabs и возвращает placement plan.
- `ProceduralLayoutPlanner` получает procedural definition и возвращает grid plan.

После планирования Runtime отдельно создаёт Unity geometry. Такое разделение позволяет проверять правила генерации в EditMode без `GameObject`, не создавая универсальный generation framework.

### 9.1. Authored

1. Resolve `MapAssetId` в сгенерированный Addressables key.
2. Instantiate prefab.
3. Считать `DungeonMapAuthoring` и его дочерние authoring-компоненты.
4. Проверить IDs, entry/exit, room references и placements.
5. Построить snapshot без случайного изменения authored layout.

### 9.2. Chunked

1. Runtime загружает каждый unique chunk prefab из definition один раз и считывает его immutable bounds/ports metadata.
2. Domain planner размещает entry и mandatory chunks.
3. Выбирает открытый port.
4. Детерминированно выбирает compatible chunk и rotation.
5. Отклоняет placement при overlap или нарушении connection rule.
6. Повторяет до `TargetChunkCount`.
7. Подключает exit chunk.
8. Проверяет связность entry → exit.
9. При неуспехе повторяет всю попытку не более `MaxGenerationAttempts`.
10. Runtime создаёт GameObject instances по готовому plan.

Выбор первого подходящего чанка после deterministic shuffle достаточен. Backtracking solver не вводится.

### 9.3. Procedural grid

1. Создать entry cell `(0, 0)`.
2. Расширять connected set через свободных cardinal neighbours до `RoomCount`.
3. При тупике перезапустить попытку в пределах `MaxGenerationAttempts`.
4. Выбрать exit как наиболее удалённую по BFS комнату.
5. Создать floors, walls и doors из одного `TileSet` по соседям cells.
6. Создать стандартные placement slots по типу комнаты.
7. Проверить связность и отсутствие пересечений.

Первая версия создаёт прямоугольные grid rooms. Organic caves, multi-floor topology и runtime mesh boolean operations не входят в scope.

## 10. Scenario planning

Планирование выполняется после готовности `DungeonMapSnapshot`:

1. Добавить все `Fixed` placements без изменений.
2. Добавить только перечисленные сценарием `OptionalFixed` placements.
3. Умножить base budgets на difficulty.
4. Заполнить enemy slots кандидатами, совместимыми по tag и оставшемуся threat budget.
5. Заполнить interest-point slots по `MinCount/MaxCount` и weights.
6. Разместить required objectives в совместимые свободные objective slots.
7. Проверить уникальность использования placement и обязательные ограничения.

Weighted selection выполняет один маленький concrete `DeterministicRandom` с зафиксированным алгоритмом; интерфейс random provider не создаётся. Runtime не полагается на незадокументированную последовательность `System.Random` или `UnityEngine.Random`.

Seed делится deterministic hash-функцией на три потока:

```text
layout seed
enemy seed
interest/objective seed
```

Изменение enemy pool не должно менять геометрию карты.

Enemy budget заполняется, пока существует compatible candidate с `Cost <= remainingBudget`. Остаток меньше стоимости самого дешёвого подходящего кандидата является допустимым и не требует knapsack solver.

## 11. Addressables и ресурсы

Установленная версия Addressables подтверждена: `3.1.0`.

Текущий `ResourceLoader` не используется для владения экземпляром данжа: его `LoadAndCreateAsync` загружает prefab, затем создаёт обычный `Object.Instantiate` и не предоставляет instance-specific `ReleaseInstance` contract. Для данжа нужен точный owner каждого handle.

Прямые Addressables-вызовы находятся только в `Runtime/Infrastructure`:

- authored prefab, созданный через `InstantiateAsync`, освобождается через `ReleaseInstance`;
- unique chunk/tile prefab загружается один раз через `LoadAssetAsync`;
- локальные clones уничтожаются до release соответствующего load handle;
- pending operation остаётся у infrastructure до завершения и cleanup, даже если ожидание caller отменено;
- raw handles и keys не выходят из concrete `DungeonInstance`;
- `WaitForCompletion` не используется.

Config содержит `DungeonAssetId`, а не address string. `DungeonRuntimeAssetCatalog` в composition сопоставляет его только с константой из `AddressableIds`. Каждый новый production asset обязан иметь mapping и пройти Editor validation.

## 12. Navigation

- Authored prefab использует заранее подготовленную navigation data.
- Chunk prefab содержит собственные navigation bindings; соединённые ports создают только необходимые links.
- Procedural grid строит navigation один раз после геометрии и до возврата instance.

Runtime navigation build для procedural режима считается риском для mobile. До принятия procedural milestone обязателен profiler proof на target device. Если стоимость неприемлема, procedural geometry переводится на navigation-ready modules; параллельные реализации заранее не создаются.

Dungeon не передаёт `NavMeshAgent`, `NavMeshSurface` или другие Unity-типы в Application/Domain.

## 13. Ошибки и validation

Config validation при старте проверяет:

- уникальность и непустоту всех IDs;
- отсутствие пересечения Dungeon IDs между тремя layout arrays;
- существование scenario и difficulty references;
- положительные costs/weights/counts и допустимые multipliers;
- `MaxGenerationAttempts > 0` с проектным верхним лимитом;
- существование mapping каждого `DungeonAssetId`.

Asset/Editor validation проверяет:

- один authoring root;
- ровно один entry и exit;
- уникальные placement IDs;
- обязательные поля для `Fixed`/`Slot`/`OptionalFixed`;
- валидные room references;
- непустые compatible ports у chunk pools;
- отсутствие production dependencies на `Assets/ImportedAssets`.

Runtime build validation проверяет:

- полную связность entry → exit;
- отсутствие overlap чанков;
- достаточное число objective slots;
- возможность выполнить minimum interest-point counts;
- отсутствие двойного использования placement;
- наличие обязательных fixed assets.

Невалидный результат завершает build явной ошибкой. Запрещены:

- бесконечные retry;
- молчаливое удаление обязательного content;
- fallback на другой dungeon/scenario;
- продолжение запуска с частично созданной картой.

## 14. Runtime quality

Генерация и сборка относятся к cold initialization, а не к frame loop.

- У Dungeon нет `Update`/tick.
- Hierarchy scan выполняется один раз на созданный prefab/chunk.
- Config lookup предварительно преобразуется в immutable dictionaries.
- В generation loop нет LINQ, string formatting и Unity hierarchy lookup.
- Списки создаются с capacity из config limits.
- Pooling не добавляется до измеренного repeated instantiate/destroy churn.
- Streaming, background regeneration и live layout mutation отсутствуют.

## 15. Проверки

### EditMode — Domain

- одинаковые config + seed дают одинаковые map/content plans;
- изменение population не меняет layout plan;
- authored fixed placements сохраняются без изменений;
- slot placements соблюдают tags и threat budget;
- optional fixed создаются только при явном включении;
- required objective занимает совместимый slot или build завершается ошибкой;
- chunked result связан, не пересекается и прекращает retry;
- procedural grid связан, имеет entry/exit и требуемое число rooms;
- invalid config отклоняется до runtime loading.

Тесты проверяют наблюдаемое поведение и инварианты, а не конкретную последовательность внутренних random calls.

### PlayMode

- authored prefab создаётся, возвращает snapshot и полностью удаляется через `Dispose`;
- chunk instances соединяются по ports;
- cancellation/error освобождает partial graph;
- navigation entry → exit доступна для каждого layout mode;
- gameplay objects создаются по `ContentPlan` и освобождаются до карты.

### Editor/manual

- level designer создаёт authored map, fixed enemy, slot enemy, chest и objective без изменения runtime code;
- Inspector/Gizmos показывают placements и validation errors;
- production dependency audit не находит `Assets/ImportedAssets`;
- Addressables build и generated IDs актуальны;
- Unity smoke не содержит console errors после create/dispose/create.

### Performance

- отдельно измеряются authored load, chunk generation и procedural generation;
- для procedural navigation фиксируются build time, peak memory и allocation;
- steady gameplay не должен иметь Dungeon-owned per-frame cost.

## 16. План реализации

### D0 — Архитектурный каркас

- создать только `Domain` и `Application` asmdef с реальным кодом;
- добавить request, минимальные immutable snapshot/content plan и public factory contract;
- добавить focused EditMode tests для контрактных инвариантов;
- не создавать Runtime, Config и Unity authoring до authored-среза;
- legacy asmdef удалить после успешной компиляции новой структуры отдельным осознанным изменением.

### D1 — Authored vertical slice

- создать Runtime asmdef и минимальную `DungeonConfigPage` только для authored path;
- добавить authored root и три placement authoring component;
- реализовать Addressables instance ownership;
- реализовать fixed/slot/optional scenario planning;
- подключить один project-owned authored dungeon prefab;
- интегрировать результат с `DungeonExpeditionRoot`;
- пройти EditMode, PlayMode, Editor validation и manual smoke.

Это минимальный полезный вертикальный срез. До его завершения chunked/procedural builders не реализуются.

### D2 — Chunked

- добавить chunk/port authoring;
- реализовать mandatory + generated chunk placement;
- добавить overlap/connectivity validation;
- подготовить небольшой project-owned chunk set;
- проверить navigation links и bounded failure.

### D3 — Procedural grid

- реализовать один grid algorithm и один tile set;
- добавить стандартные procedural placements;
- собрать navigation до публикации instance;
- пройти profiler proof на target Android device.

### D4 — Полная проверка

- compile всех зависимых asmdef;
- focused EditMode и PlayMode suites;
- Addressables/dependency audit;
- authored/chunked/procedural manual smoke;
- create/dispose/recreate и cancellation smoke;
- зафиксировать неподтверждённые device/performance paths.

## 17. Non-goals

- streaming и выгрузка отдельных комнат;
- additive scenes для отдельных chunks;
- editor node graph;
- несколько procedural algorithms и plugin registry;
- WFC, generic constraint solver и backtracking search;
- organic caves, этажи и vertical generation;
- runtime save/resume состояния генератора;
- network replication;
- pooling без profiler evidence;
- Dungeon-owned enemy/chest/trap gameplay;
- универсальная система mode/quest overrides;
- автоматический full-prefab preview authoring tool в первом срезе.

## 18. Критерий готовности системы

Система считается завершённой в заявленном scope, когда один и тот же `DungeonId` может быть запущен с разными `ScenarioId`/`DifficultyId`, authored placements сохраняют решения level designer, procedural placements детерминированно заполняются конфигом, все три layout mode возвращают единый public contract, а остановка или ошибка не оставляет GameObjects и Addressables resources.
