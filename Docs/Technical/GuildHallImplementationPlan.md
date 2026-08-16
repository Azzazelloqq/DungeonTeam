# DungeonTeam — Guild Hall Implementation Plan

**Статус:** GH-0…GH-7 IMPLEMENTED AND AUTOMATION-VALIDATED; manual flow smoke not run

**Версия:** 0.6

**Дата:** 16 августа 2026

**Design:** [Guild Hall Technical Design](./GuildHallTechnicalDesign.md)

**Product scope:** [Guild Hall GDD](../Product/GuildHallGDD.md)

---

## 1. Результат реализации

Готовый сквозной путь:

```text
Application start
→ walkable primitive Guild Hall
→ NPC dialogue / Notice Board
→ World Map
→ existing Dungeon Run
→ session-only result
→ new Guild Hall session
```

Реализация не включает Player Profile, saves, деньги, продажу, ранги, inventory и quest system.

## 2. Правила выполнения

- Каждый milestone заканчивается компиляцией и релевантной проверкой поведения.
- Тесты проверяют snapshots, IDs, transitions и lifecycle, а не фиксированное число контентных записей.
- Layout Guild Hall собирается вручную в Unity из primitives; код не генерирует помещение.
- Новый production flow не остаётся параллельно со старым MainMenu после завершения миграции.
- Не создаются пустые future modules/interfaces.
- Не меняются Dungeon Run rules, generation и combat, кроме узкой точки возврата в application flow.

Текущий прогресс:

| Milestone | Статус |
| --- | --- |
| GH-0. Contracts и compile boundaries | Реализован и проверен |
| GH-1. Typed content config | Реализован и проверен |
| GH-2. Guild Hall graybox и resource ownership | Реализован и проверен |
| GH-3. Semantic interactions | Реализован и проверен |
| GH-4. Reusable ambient NPC и разговоры | Реализован и независимо провалидирован; исправлен repeated-dialogue lifecycle |
| GH-5. Notice Board | Реализован и независимо провалидирован: EditMode, PlayMode, actual Addressable prefab lifecycle |
| GH-6. World Map и application transitions | Реализован и независимо провалидирован: compile, focused EditMode, regression EditMode/PlayMode, actual WorldMap Addressable lifecycle; manual flow smoke не запускался |
| GH-7. Dungeon return, summary и cleanup | Реализован и automation-validated |

## 3. Целевая структура

Финальные имена уточняются при реализации по существующим conventions, но ответственность фиксирована:

```text
Assets/Code/Gameplay/GuildHall/
├─ Application/
│  ├─ snapshots, catalogs, start context, session state builder
│  └─ DungeonTeam.GuildHall.Application.asmdef
├─ Runtime/
│  ├─ Config/
│  ├─ Composition/
│  ├─ Input/
│  ├─ Interaction/
│  ├─ Presentation/Gameplay/GuildHall/
│  ├─ Presentation/UI/NoticeBoard/
│  └─ DungeonTeam.GuildHall.Runtime.asmdef
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/

Assets/Code/Gameplay/AmbientNpc/
├─ Application/
│  ├─ NPC/dialogue/profile snapshots, catalogs, selector, state transitions
│  └─ DungeonTeam.AmbientNpc.Application.asmdef
├─ Runtime/
│  ├─ Config/
│  ├─ AmbientNpcSet + vignette bindings
│  ├─ Presentation/Gameplay/AmbientNpc/
│  ├─ Presentation/UI/Dialogue/
│  └─ DungeonTeam.AmbientNpc.Runtime.asmdef
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/

Assets/Code/UI/WorldMap/
├─ Config/
├─ WorldMapRoot.cs
├─ Presentation/
├─ DungeonTeam.WorldMap.asmdef
└─ Tests/EditMode + Tests/PlayMode

Assets/Content/Gameplay/GuildHall/
└─ GuildHallGraybox.prefab

Assets/Prefabs/UI/WorldMap/
└─ WorldMap.prefab
```

Папки и asmdef создаются только в milestone, где появляется реальный код. Отдельная `Infrastructure` assembly не планируется.

## 4. Milestones

### GH-0. Contracts и compile boundaries

**Цель:** зафиксировать data boundary до Unity presentation.

Работы:

1. Создать `DungeonTeam.GuildHall.Application` с immutable/defensive типами:
   - `GuildTextSnapshot`;
   - location-owned NPC snapshot, extracted as `AmbientNpcSnapshot` in GH-4;
   - `NoticeBoardOfferSnapshot`;
   - `GuildRunSummarySnapshot`;
   - `GuildHallStartContext`;
   - `GuildSessionState` или узкий application-owned state object.
2. Добавить validation constructors/catalog types для stable IDs.
3. Создать EditMode tests на пустые ID, defensive collections и variable content counts.
4. Создать пустой только по содержанию не assembly, а минимальный consumer compile test из Bootstrap/test assembly.

**Не делать:** Unity Views, config pages, roots, interfaces экономики/квестов.

**Готово, когда:** Application assembly компилируется без Unity references, а tests подтверждают поведение snapshots и catalogs.

### GH-1. Typed content config

**Цель:** весь текущий контент материализуется из валидированных config pages.

Работы:

1. Добавить `GuildHallConfigPage`, `DialogueConfigPage`, `ContractConfigPage`.
2. Добавить `WorldMapConfigPage` и локальный `WorldMapTextSnapshot` вместе с первым реальным `DungeonTeam.WorldMap` кодом.
3. Реализовать преобразование pages в immutable catalogs на application initialization.
4. Проверить ссылки:
   - NPC → dialogue pool;
   - NPC → ambient profile;
   - contract → location;
   - location/destination → поддерживаемый application mapping.
5. Добавить production config entries без ожидания фиксированного количества записей.
6. Зарегистрировать pages в существующем `ConfigCatalog` asset.

**Готово, когда:** application initialization либо создаёт полностью согласованные catalogs, либо падает с конкретной ошибкой данных.

### GH-2. Guild Hall graybox и resource ownership

**Цель:** загрузить, показать и корректно освободить пустой walkable зал.

Работы в коде:

1. Создать `DungeonTeam.GuildHall.Runtime` asmdef с направленными references.
2. Реализовать `GuildHallRoot`, локальный Addressable loader/lease и top-level Guild Hall MVP family.
3. Добавить `IGuildHallInput` и Editor input adapter без combat/skill dependencies.
4. Реализовать camera-relative movement через `CharacterController` и одну `ITickHandler` подписку.
5. Добавить transition/input guards и порядок disposal.

Работы в Unity authoring:

1. Вручную собрать `GuildHallGraybox.prefab` из primitives.
2. Разместить player spawn, камеру, colliders, доску, стойку и выход.
3. Добавить обязательные serialized bindings корневого View.
4. Зарегистрировать prefab в Addressables и сгенерировать `AddressableIds`.

**Проверка:**

- EditMode compile/contract tests;
- PlayMode load/cancel/dispose/release tests;
- mechanical prefab/addressable validation;
- короткий manual smoke движения и collision.

**Готово, когда:** hall можно несколько раз создать и освободить без дублей, удержанных ресурсов и оставшегося input.

### GH-3. Semantic interactions

**Цель:** доска, стойка, выход и NPC обнаруживаются по authored bindings.

Работы:

1. Добавить interaction authoring type с semantic ID, kind, anchor и radius.
2. Реализовать локальный nearest-interaction controller с interval scan.
3. Создать/подключить существующую `ContextActions` family как owned child хаба.
4. Разрешать labels из text definitions/snapshots.
5. Повторно валидировать target и дистанцию при execution.
6. Блокировать movement и interactions при активном modal UI/transition.

**Тесты поведения:**

- выбирается ближайшая доступная точка;
- список любого размера обрабатывается без фиксированного count;
- выход из радиуса очищает action;
- stale action не выполняется;
- disposal снимает tick и очищает model.

**Готово, когда:** все обязательные точки вызывают semantic callback, не используя GameObject names/tags.

### GH-4. NPC ambient и разговоры

**Цель:** зал создаёт дешёвую иллюзию активности, с каждым NPC можно поговорить.

Работы:

1. Реализовать [Ambient NPC Technical Design](./AmbientNpcTechnicalDesign.md) двумя assemblies: location-neutral Application и reusable Unity Runtime.
2. Перенести NPC/dialogue-specific snapshots, catalogs и `DialogueConfigPage` из Guild Hall в AmbientNpc, сохранив config asset identity и направленные references.
3. Добавить `AmbientNpcConfigPage` с валидированными behavior profiles; Guild Hall config продолжает выбирать NPC и ссылаться на profile/pool IDs.
4. Создать reusable `AmbientNpc` MVP family и `AmbientNpcSet`; set валидирует exact ID-set config ↔ prefab, но не exact count, и не подписывается на tick сам.
5. Реализовать parent-driven state transitions `Idle → MoveToAnchor → FaceAnchor → Activity → Idle` и authored routes/anchors/activity bindings в Guild Hall prefab.
6. Реализовать один coordinated vignette controller для спора двух NPC, не синхронизируя две независимые state machines случайно.
7. Создать reusable Dialogue MVVM popup, deterministic line selector и serialized dialogue binding внутри Guild Hall prefab.
8. Маршрутизировать `Npc` interaction локально: приостановить выбранного NPC, повернуть к игроку, заблокировать world input, открыть line из его pool, затем восстановить routine/input при закрытии.
9. Добавить production pools русских fallback-фраз для всех configured NPC без тестового требования их фиксированного количества.

**Тесты поведения:**

- неизвестный/повторяющийся `npcId` отклоняется;
- каждый configured NPC получает существующий pool;
- selector всегда возвращает line из pool;
- NPC достигает authored state transitions;
- parent disposal освобождает все child presenters;
- partial initialization failure освобождает уже созданных children;
- dialogue close возвращает world input и routine выбранного NPC;
- тест не требует конкретного числа NPC/реплик.

**Готово, когда:** несколько разных authored-активностей видны одновременно, каждый NPC открывает корректную реплику, а тот же `AmbientNpc` runtime не зависит от Guild Hall и может быть скомпонован другим location root.

### GH-5. Notice Board

**Цель:** выбрать контракт, не связывая UI с dungeon/profile rules.

Работы:

1. Реализовать [Notice Board Technical Design](./NoticeBoardTechnicalDesign.md) внутри существующего `GuildHall.Runtime`, без нового asmdef.
2. Создать один serialized inactive Notice Board MVVM family внутри `GuildHallGraybox.prefab`; обычный Close только скрывает family, final disposal принадлежит `GuildHallRoot`.
3. Создавать item ViewModels по `contractId` из `GuildHallStartContext.Offers`, сохраняя входной порядок и не предполагая fixed count.
4. Отображать selected/available/disabled state и все UI labels из immutable localization-ready snapshots.
5. Возвращать `ContractSelected(contractId)` в Application owner, который обновляет `GuildSessionState`; board не знает profile/dungeon rules.
6. Блокировать world input на время modal и гарантировать симметричный repeated open/close lifecycle без дублей listeners.
7. Не создавать `DungeonRunStartRequest` внутри board/root и не добавлять отдельный Addressable/UIService lifecycle без второго consumer.

**Тесты поведения:**

- все переданные offers отображаются независимо от количества;
- выбор доступного offer возвращает его ID;
- disabled offer не выбирается;
- закрытие/повторное открытие не дублирует item subscriptions;
- open блокирует world input, close восстанавливает его;
- тесты не требуют production count или конкретного набора contract IDs.

**Готово, когда:** выбранный contract хранится в application session-state и переживает Hall → Map в пределах запуска приложения.

### GH-6. World Map и application transitions

**Цель:** заменить прямой Play из MainMenu на Hall → Map → destination.

**Detailed design:** [World Map and Application Flow Technical Design](./WorldMapApplicationFlowTechnicalDesign.md)

Работы:

1. Реализовать `WorldMapRoot`, config catalog и MVVM screen через `UIService`.
2. Строить `WorldMapStartContext` в Application из актуального catalog/state.
3. Обрабатывать:
   - Guild Hall location → новый `GuildHallRoot`;
   - Dungeon location → проверка выбранного contract и создание текущего `DungeonRunStartRequest`;
   - unavailable location → отсутствие transition.
4. Ввести один application transition guard вместо разрозненных boolean по features.
5. На каждом переходе полностью освобождать outgoing root до активации incoming input.
6. Сохранить developer console как отдельный development-only путь запуска Dungeon Run.
7. Убрать `MainMenuRoot` из активного production wiring Bootstrap, не удаляя пока сам module/assets.

**Проверка:**

- ViewModel behavior tests;
- lifecycle tests для repeated Hall ↔ Map;
- pure application gate/destination policy tests без production abstraction ради fake;
- actual UIService/Addressables prefab lifecycle в PlayMode;
- manual smoke полного application flow остаётся manual-only.

**Готово, когда:** карта возвращает stable location ID, а Application является единственным владельцем навигационного решения.

### GH-7. Dungeon return, reception summary и MainMenu removal

**Цель:** завершить один production vertical flow без параллельного старого меню.

**Detailed design:** [Dungeon Return and Guild Summary Technical Design](./DungeonReturnGuildSummaryTechnicalDesign.md)

Работы:

1. Перенаправить terminal `DungeonRunResult` в application session-state.
2. Полностью остановить Dungeon Run до создания нового Guild Hall.
3. Подготовить `GuildRunSummarySnapshot` из текущих reward definitions без bank/save semantics.
4. Показать короткий summary у стойки регистрации или в её dialogue UI.
5. Удалить физически неиспользуемые MainMenu Addressable/config/code только если `rg` и assembly references доказывают отсутствие других consumers; active production wiring уже убрано в GH-6.
6. Проверить, что после удаления не осталось orphaned generated IDs/config registrations.
7. Обновить документацию по фактическим именам, assemblies и validation result.

**End-to-end acceptance:**

- start → Guild Hall;
- board → contract selection;
- exit → World Map;
- map → existing Dungeon Run;
- terminal result → clean Guild Hall;
- reception → session-only summary;
- второй цикл запускается без рестарта;
- в hierarchy и tick/input ownership нет объектов прошлого цикла.

## 5. Отложенные milestones

Ниже не входят в текущий implementation plan и требуют отдельного product/technical design:

| Future feature | Точка подключения |
| --- | --- |
| Player Profile + SaveStore V2 | Application snapshot builder и result application use case |
| Money / inventory / selling | Конкретный reception use case и собственные immutable snapshots |
| Guild rank | Offer availability builder, reception commands, profile state |
| Quest system | Отдельные quest definitions/state; board adapter или отдельная вкладка |
| Forest destination | Новый destination feature и application mapping для `locationId` |
| Other city locations | Новые player-facing roots/screens, выбираемые Application по location ID |
| Localization tables | Text resolver при сборке feature-owned text snapshots, без изменения ViewModel contracts |

## 6. Validation matrix

| Изменение | Минимальное доказательство |
| --- | --- |
| Pure snapshots/catalogs | Focused EditMode tests |
| Config references | EditMode validation tests + production catalog creation |
| asmdef graph | Unity compile затронутых assemblies и consumer compile proof |
| MVP/MVVM behavior | Focused EditMode tests |
| Tick/input/disposal | PlayMode lifecycle tests |
| Addressable prefab/UI | Mechanical validation + PlayMode load/release test |
| Camera/collision/routes/serialized bindings | Unity manual smoke |
| Application transitions | Repeated Hall → Map → Run → Hall scenario |

Документационные изменения отдельно проверяются `git diff --check` и валидностью относительных ссылок. External playtest и build artifact в этот план не входят.

## 7. Основные риски и guards

| Риск | Guard |
| --- | --- |
| Хаб обрастает будущими системами до первой прогулки | Каждый milestone сохраняет non-goals; future services не создаются |
| Контентные тесты ломаются при добавлении NPC/реплик | Assertions по поведению, uniqueness и referential integrity, без fixed count |
| Reusable NPC превращается в общий framework «на будущее» | Узкий `AmbientNpc` scope: authored routine + one-line dialogue; quests/shops/schedules остаются вне модуля |
| NPC создают много `Update` и allocations | Одна tick-подписка у location parent, prebuilt bindings/state, без LINQ в tick |
| UI начинает решать rank/quest/dungeon rules | Только immutable snapshots in и stable IDs out |
| Старый MainMenu и новый flow конфликтуют | Убрать active production wiring в GH-6; физически удалить module/assets в GH-7 после end-to-end proof |
| Resource/input leak при переходах | Один active owner, transition guard, disposal до следующей activation |
| Конфиг превращается в сериализованную сцену | Unity refs и routes остаются в вручную authored prefab |
| Save schema блокирует будущие изменения | Сейчас save отсутствует; позже сохраняются только business values и stable IDs |

## 8. Следующий шаг

GH-7 завершён: ApplicationRoot владеет one-shot terminal subscription, строит session-only summary до `DungeonRunHost.Stop()`, создаёт новый Guild Hall и передаёт summary в дочернюю Reception MVVM family. MainMenu code, prefabs, Addressable entry и generated ID удалены после consumer audit. Автоматическая validation зафиксирована в detailed design; manual flow smoke, build и внешний playtest не запускались.
