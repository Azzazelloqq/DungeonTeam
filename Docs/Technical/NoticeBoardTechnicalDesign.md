# DungeonTeam — Notice Board Technical Design

**Статус:** IMPLEMENTED AND AUTOMATION-VALIDATED (GH-5)

**Версия:** 1.0

**Дата:** 15 августа 2026

**Product scope:** [Guild Hall GDD](../Product/GuildHallGDD.md)

**Parent design:** [Guild Hall Technical Design](./GuildHallTechnicalDesign.md)

**Implementation order:** [Guild Hall Implementation Plan](./GuildHallImplementationPlan.md)

---

## 1. Цель GH-5

Игрок взаимодействует с доской в Guild Hall, видит все переданные контракты, их доступность и текущий выбор, может выбрать доступный контракт и закрыть доску.

GH-5 не запускает Dungeon Run и не реализует profile, saves, деньги, ранги, квестовую систему или выдачу наград. Эти системы позже формируют те же immutable snapshots и принимают semantic output `contractId`.

## 2. Граница feature

Notice Board остаётся частью существующих assemblies:

| Assembly | Ответственность GH-5 |
| --- | --- |
| `DungeonTeam.GuildHall.Application` | Immutable offer/text snapshots и session-only выбранный `contractId` |
| `DungeonTeam.GuildHall.Runtime` | Notice Board MVVM, композиция с `GuildHallRoot`, serialized prefab bindings |
| `DungeonTeam.GuildHall.Tests.EditMode` | Чистое поведение model/viewmodel/session state |
| `DungeonTeam.GuildHall.Tests.PlayMode` | Реальные prefab bindings, input/modal lifecycle и повторные циклы |

Новые asmdef, DI scope, generic board framework, repository, quest/economy abstractions и interface на каждый класс не создаются. Отдельный модуль появится только при реальном втором consumer с отличающимися правилами.

## 3. Решение по UI lifecycle

Notice Board View — inactive serialized child `GuildHallGraybox.prefab`, а не отдельный Addressable через `UIService`.

Причины:

- доска существует только внутри текущего Guild Hall world lease;
- `GuildHallRoot` имеет синхронный interaction callback и синхронный disposal;
- отдельный Addressable добавил бы async open/close и второй resource owner без текущей пользы;
- world lease уже гарантированно освобождает View вместе с prefab.

Root один раз создаёт и инициализирует MVVM family. Открытие и закрытие меняют состояние model, но не dispose/reinitialize View. Это обязательный контракт для повторных циклов в одной сессии зала.

```text
Application owner
├─ GuildSessionState
└─ GuildHallRoot
   ├─ Guild Hall MVP
   ├─ AmbientNpcSet
   ├─ Dialogue MVVM
   └─ NoticeBoard MVVM
      ├─ NoticeBoardModel
      ├─ NoticeBoardViewModel
      ├─ NoticeBoardView (serialized child)
      └─ N NoticeBoardItemViewModel + item views
```

## 4. Входные данные и тексты

Board получает только:

- `GuildHallStartContext.Offers`;
- `GuildHallStartContext.SelectedContractId`;
- localization-ready `NoticeBoardTextSnapshot`.

`NoticeBoardTextSnapshot` содержит подготовленные тексты заголовка, кнопки выбора, состояния «выбрано», закрытия и пустого списка. Fallback-строки authorятся в `GuildHallConfigPage`; View и ViewModel не содержат пользовательские литералы.

Offer уже описан `NoticeBoardOfferSnapshot`:

- `ContractId`;
- `Title`;
- `Summary`;
- `LocationId`;
- `IsAvailable`;
- optional `DisabledReason`.

Количество offers не фиксируется. Нулевой список допустим и показывает configured empty-state. Порядок View совпадает с порядком входного snapshot.

Runtime не читает mutable config и не резолвит localization самостоятельно.

## 5. MVVM family

### 5.1. NoticeBoardModel

Model хранит UI-state одной hall session:

- defensive immutable list offers;
- nullable selected `contractId`;
- `IsVisible`;
- команду выбора, которая принимает только доступный item;
- команду закрытия.

Model не изменяет `GuildSessionState`, не создаёт dungeon request и не знает profile/config.

### 5.2. NoticeBoardItemViewModel

Для каждого offer parent ViewModel создаёт ровно один item ViewModel, keyed by `contractId`. Item предоставляет View:

- title и summary;
- availability;
- selected state;
- disabled reason;
- configured label кнопки;
- select command.

Disabled item не публикует выбор. Selected item может быть нажат повторно без повторного semantic callback.

### 5.3. NoticeBoardViewModel

Parent ViewModel:

- владеет item ViewModels и освобождает их один раз;
- сохраняет входной порядок;
- обновляет selected state всех items после принятого выбора;
- проксирует close command;
- не создаёт item Views и не обращается к Unity hierarchy.

### 5.4. NoticeBoardView

View содержит serialized references на:

- root panel;
- header label;
- empty-state label;
- item container;
- inactive item template;
- close button.

При `Initialize` View создаёт item Views из template и связывает их с уже созданными item ViewModels. При повторном Show/Hide строки не пересоздаются и listeners не дублируются. При final dispose View снимает свои listeners и уничтожает созданные row GameObjects; child ViewModels освобождает parent ViewModel, не View.

View не выбирает контракт, не меняет availability и не обращается к application state.

## 6. Композиция и semantic output

`GuildHallRoot` получает синхронный callback `Func<string, bool> contractAccepted` от application owner.

Поток выбора:

```text
item command
→ NoticeBoardModel validates prepared availability
→ GuildHallRoot callback(contractId)
→ Bootstrap повторно валидирует availability, сохраняет CQ-0 active contract и обновляет session hint
→ board applies state only when callback accepted it
```

CQ-0 сохраняет callback синхронным, но возвращает отказ: Bootstrap является единственной границей между UI и persistent contract state. Доска не читает config/save/domain и не завершает контракт самостоятельно.

`GuildHallRoot.HandleInteraction` маршрутизирует:

- `Npc` — в Dialogue;
- `NoticeBoard` — локально в Notice Board;
- `Reception` и `Exit` — в существующий внешний semantic callback.

## 7. Modal/input contract

При открытии доски Root:

1. проверяет, что другой modal/transition не активен;
2. скрывает current context action;
3. блокирует movement и новые interactions;
4. показывает Notice Board model.

При закрытии Root:

1. скрывает Notice Board model;
2. снимает modal block;
3. разрешает movement/interactions;
4. ближайшее context action восстанавливается обычным scan tick.

Повторное открытие уже видимой доски и повторное закрытие скрытой доски — безопасные no-op. Одновременно Dialogue и Notice Board открыты быть не могут.

## 8. Ownership и disposal

`GuildHallRoot` владеет Notice Board model, ViewModel и View binding:

- создаёт их после загрузки и проверки prefab bindings;
- при ошибке частичной инициализации освобождает уже созданные children;
- при final dispose сначала закрывает modal state, затем dispose View, затем ViewModel/model по фактическому контракту библиотеки;
- не dispose family на обычном Close;
- не добавляет replaceable одноразовые объекты в terminal composite.

Prefab instance и созданные row GameObjects принадлежат Guild Hall world lease/View соответственно. Addressables handle для доски не появляется.

## 9. Unity authoring

В существующий `GuildHallGraybox.prefab` добавляются:

- inactive Notice Board panel на текущем Canvas;
- header, empty-state и close controls;
- inactive item template с title, summary, reason, selected marker и button;
- обязательные serialized bindings в `GuildHallWorldView`/Notice Board View.

UI остаётся graybox на стандартных панелях, кнопках и TMP. Новые художественные assets и отдельная scene не нужны.

## 10. Тестирование ожидаемого поведения

### EditMode

- список любого размера сохраняет все offers и их порядок;
- нулевой список разрешён;
- начальный selected ID отражается ровно в одном существующем item;
- доступный невыбранный item публикует точный `contractId` один раз;
- disabled и уже selected items callback не публикуют;
- выбор обновляет selected state предыдущего и нового item;
- `GuildSessionState` хранит выбранный ID независимо от UI;
- dispose не оставляет активных subscriptions.

Тестовые данные используют variable fixtures и проверяют поведение, а не production count или конкретный набор IDs.

### PlayMode

- реальный Addressable Guild Hall prefab содержит все board bindings;
- open блокирует world input, close восстанавливает его;
- два последовательных open/select/close цикла не дублируют callback/listeners;
- disabled row визуально недоступна и не выбирается;
- dispose открытого board корректно очищает family;
- отсутствие/поломка обязательного prefab binding даёт конкретную initialization error в изолированной fixture.

### Mechanical/manual

- Unity compilation и console errors;
- focused GuildHall EditMode/PlayMode suites;
- полный EditMode regression suite;
- Unity change validator для prefab/meta/asmdef;
- ручной smoke только как отдельная непроведённая проверка: подойти к доске, открыть, выбрать, закрыть и снова открыть.

## 11. Порядок реализации

1. Добавить `NoticeBoardTextSnapshot` и config authoring/fallback texts.
2. Реализовать model и behavior-focused EditMode tests.
3. Реализовать parent/item ViewModels и tests повторных команд/selected state.
4. Реализовать View/item View с симметричными listeners.
5. Добавить serialized inactive UI в Guild Hall prefab.
6. Скомпоновать family в `GuildHallRoot` и подключить semantic callback.
7. Добавить PlayMode prefab/modal/repeated-cycle tests.
8. Запустить выбранный validation matrix и задокументировать только фактически полученные результаты.

## 12. Не входит в GH-5

- Player Profile и SaveStore;
- quest log, procedural quest generation и acceptance/completion rules;
- деньги, inventory, продажа, ранги и регистрация;
- запуск карты или dungeon из доски;
- filters, pagination, virtualization, refresh во время открытого Hall;
- generic NoticeBoard package или второй UI implementation;
- отдельный Addressable/UIService lifecycle для доски.
