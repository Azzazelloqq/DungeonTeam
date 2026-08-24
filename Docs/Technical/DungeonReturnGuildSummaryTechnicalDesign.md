# DungeonTeam — Dungeon Return and Guild Summary Technical Design

**Статус:** IMPLEMENTED AND AUTOMATION-VALIDATED (GH-7 + PP-4/PP-6); manual flow smoke/build not run

**Версия:** 1.1

**Дата:** 16 августа 2026

**Product scope:** [Guild Hall GDD](../Product/GuildHallGDD.md)

**Implementation order:** [Guild Hall Implementation Plan](./GuildHallImplementationPlan.md)

**Previous flow milestone:** [World Map and Application Flow Technical Design](./WorldMapApplicationFlowTechnicalDesign.md)

---

## 1. Цель GH-7

GH-7 завершает один production-цикл:

```text
Dungeon Run terminal result
→ Bootstrap profile settlement
→ application-owned committed summary
→ полный stop/dispose Dungeon Run
→ новая Guild Hall session
→ summary по взаимодействию со стойкой регистрации
```

После автоматического доказательства нового пути физически удаляется уже отключённый в GH-6 `MainMenu`: runtime/UI assemblies, тесты, prefab assets, Addressable entry и generated ID. Второй production flow не сохраняется.

PP-4 adds the narrow Bootstrap reward mapper and verified exactly-once profile settlement before the summary is shown. `RewardCatalog` still owns display definitions; DungeonRun/Rewards do not know the profile or persistence.

## 2. Scope и non-goals

В scope:

- ровно одна application-подписка на `DungeonRunRoot.Finished` для каждого production или developer run;
- защита от повторной обработки terminal result;
- преобразование результата в immutable `GuildRunSummarySnapshot` любого размера;
- полный `DungeonRunHost.Stop()` до создания нового `GuildHallRoot`;
- session-only хранение последнего committed summary в существующем `GuildSessionState`;
- verified Gold/resource settlement through the application-owned Player Profile record before stopping the run;
- локальное окно summary у interaction kind `Reception`;
- явное состояние application flow после невосстановимой ошибки перехода;
- физическое удаление доказанно неиспользуемого MainMenu и очистка Addressables/generated IDs;
- focused EditMode/PlayMode и regression validation.

Не входят:

- Player Profile/SaveStore ownership inside DungeonRun/Rewards; the Bootstrap bridge and PlayerProfile application are the only settlement owners;
- начисление или повторная генерация наград;
- изменение `DungeonRunResult`, боевых правил или reward collection;
- новый navigation framework, event bus, DI scope или feature assembly;
- универсальный result processor;
- отдельный Addressable/UIService lifecycle для вложенного окна summary;
- build artifact, внешний playtest и обязательный manual smoke.

## 3. Проверенное исходное состояние

- `ApplicationRoot` уже владеет `DungeonRunHost`, `GuildSessionState`, `RewardCatalog` и application transition gate.
- `DungeonRunRoot.Finished` публикует immutable `DungeonRunResult` только после `DungeonRunProgress.TryFinish`; root очищает event при disposal.
- `DungeonRunHost.Stop()` сначала убирает ссылку на active run, затем синхронно вызывает его `Dispose()`.
- `DungeonRunResult.CollectedRewards` уже содержит агрегированные по `rewardId` grants и не требует fixed count.
- `GuildRunSummarySnapshot` и `GuildSessionState.LastRunSummary` are populated only after a committed settlement receipt.
- `GuildHallStartContextBuilder` уже переносит `LastRunSummary` в новый Hall context.
- `Reception` now opens the prepared Profile/summary families inside `GuildHallRoot`; the legacy external registrar callback remains a no-op because business actions use the narrow profile edit bridge.
- `MainMenuRoot` отсутствует в active Bootstrap wiring; `rg` показывает consumers только внутри двух MainMenu code trees, их тестов, двух prefab assets, Addressable entry и generated constant.
- Addressables package в текущем `Library/PackageCache` — `3.1.0`.

## 4. Архитектурное решение

### 4.1. Владение и зависимости

```text
ApplicationRoot
├─ DungeonRunHost
│  └─ active DungeonRunRoot --Finished(result)--> ApplicationRoot
├─ RewardCatalog
├─ PlayerProfilePersistence / PlayerProfileSession
├─ GuildSessionState
└─ new GuildHallRoot
   └─ RunSummary MVVM family (serialized child prefab)
```

`ApplicationRoot` остаётся единственным местом, где одновременно видны Dungeon Run runtime, Reward catalog и Guild Hall input contract. Поэтому преобразователь результата размещается как маленький internal concrete pure C# class в `Bootstrap`, а не в `GuildHall.Application`:

- `GuildHall.Application` не получает зависимость на `DungeonRun.Runtime` и `Rewards.Runtime`;
- `DungeonRun` не знает о Guild Hall;
- `GuildHall.Runtime` получает только готовый immutable summary;
- новый asmdef, interface, service registration или container не создаётся.

### 4.2. `GuildRunSummaryBuilder`

Builder получает:

- `DungeonRunResult`;
- committed `ProfileSettlementReceipt` for the same `RunId`;
- `RewardCatalog`;
- validated localization-ready summary texts из `GuildHallCatalog`.

Builder:

1. Сопоставляет `Completed`/`Defeated` с configured outcome snapshot без `Enum.ToString()` как пользовательского текста.
2. Создаёт dungeon text snapshot со stable `DungeonId`; текущий fallback display также равен `DungeonId`, потому что result contract не несёт отдельного localization ID. Отдельный dungeon-content refactor в GH-7 не вводится.
3. Для Gold/resource values in the committed receipt вызывает `RewardCatalog.Require(rewardId)` and builds display lines from configured format, display name and amount.
4. Сохраняет входной порядок и любое количество строк, включая ноль.
5. Не изменяет catalog, result, session state, bank или save; settlement has already completed before the builder runs.

Unknown reward, пустой ID или некорректный format являются configuration/programming error. Summary целиком строится до остановки run и до mutation session state, поэтому частичный summary не публикуется.

### 4.3. Изменение immutable contracts

`GuildRunSummarySnapshot.Outcome` меняется со stable string на `GuildTextSnapshot`: UI должен получать и text ID, и текущий fallback, а не отображать имя enum.

Добавляется `GuildRunSummaryTextSnapshot` в `GuildHall.Application`:

- header;
- completed outcome;
- defeated outcome;
- dungeon label;
- rewards label;
- reward line format;
- empty rewards;
- close label.

`GuildHallCatalog` получает обязательный `RunSummaryText`. `GuildHallConfigPage` материализует его из новой serialized секции `_runSummaryText`. Это текущий реальный UI content, а не будущая economy abstraction.

Формат reward line валидируется на cold config materialization: он обязан принимать display name и amount. Runtime View/ViewModel не читает config asset и не форматирует business data.

## 5. Terminal result flow

### 5.1. Подписка

После каждого успешного `DungeonRunHost.StartAsync` `ApplicationRoot` сразу подписывает возвращённый root на один именованный handler. Это применяется и к World Map start, и к development-only start; development Back по-прежнему делает clean return без summary.

Перед `Stop`, заменой run и application disposal handler явно снимается. Владение подпиской не полагается только на `Finished = null` внутри чужого root.

### 5.2. Обработка результата

```text
Finished(result)
→ TryBegin(expected DungeonRun)
→ map supported rewards to profile terminal request
→ verified PlayerProfileSession.BankTerminalResult(request)
→ build complete summary from committed receipt (pure, no mutation)
→ show Loading
→ unsubscribe Finished
→ DungeonRunHost.Stop()
→ GuildSessionState.SetLastRunSummary(summary)
→ create and initialize new GuildHallRoot
→ Complete(GuildHall)
→ hide Loading
```

Guard ставится до первого `await`. Поэтому повторный callback, double terminal command или competing developer transition не создают второй переход. `DungeonRunRoot` дополнительно сам публикует terminal result только при первом успешном `TryFinish`; тесты не зависят от внутреннего числа вызовов.

Application cancellation:

- не создаёт игровой result;
- не очищает предыдущее summary;
- передаётся loading/Hall initialization;
- оставляет финальную очистку `ApplicationRoot.Dispose()`.

### 5.3. Failure state

Текущий transition lease без `Complete` возвращает прежний state. Это корректно только пока прежний owner действительно существует. После `Stop()` Dungeon Run восстановить нельзя, поэтому возврат в `PlayerFlowState.DungeonRun` был бы ложью.

В `PlayerFlowState` добавляется `Faulted`. Если outgoing owner уже уничтожен, создание Hall и единственная повторная попытка `TryRestoreGuildHallAsync` обе завершились ошибкой:

- partial Hall очищается;
- Loading остаётся видимым;
- lease завершается как `Faulted`;
- обычные player-facing transitions отклоняются;
- exception логируется на application boundary.

Тот же invariant применяется к существующим GH-6 recovery branches: после уничтожения outgoing owner и неудачного recovery state не может притворяться `WorldMap` или `DungeonRun`. Отдельный recovery framework не вводится.

## 6. Reception summary presentation

### 6.1. Placement

Summary является serialized inactive child существующего `GuildHallGraybox.prefab`. `GuildHallRoot` создаёт одну MVVM family на весь lifecycle Hall:

```text
Presentation/UI/RunSummary/
├─ Base/
│  ├─ RunSummaryModelBase
│  ├─ RunSummaryViewModelBase
│  └─ RunSummaryViewBase
├─ RunSummaryModel
├─ RunSummaryViewModel
└─ RunSummaryView
```

Новый root, Addressable, UIService screen, asmdef или DI scope не создаётся. `GuildHallRoot` владеет Model/ViewModel; View является частью world prefab и освобождается тем же явным lifecycle, что Notice Board и Dialogue.

### 6.2. Behavior

- `Reception` открывает summary только если `GuildHallStartContext.LastRunSummary != null`.
- При отсутствии summary interaction сохраняет semantic callback наружу; selling/rank/profile actions остаются отдельными narrow Bootstrap bridges.
- Окно показывает header, outcome, dungeon и все reward lines; для пустого списка показывает configured empty text.
- Открытие блокирует world movement и interaction controller.
- Close скрывает окно и восстанавливает input.
- Пока открыты Dialogue или Notice Board, summary не открывается; пока открыт summary, другие modal interactions не выполняются.
- Повторные open/close используют ту же family и не добавляют listeners/item children повторно.
- Последний summary не consume-ится и доступен повторно до следующего terminal result или конца application session.

### 6.3. Variable list ownership

Parent `RunSummaryViewModel` владеет отображаемыми строками по входному snapshot и не ожидает fixed count. Для простых immutable строк отдельный item Model/ViewModel не создаётся: `RunSummaryView` создаёт TMP row instances из inactive serialized template при binding и удаляет только собственные runtime rows при rebind/dispose. В строках нет команд, состояния или самостоятельного lifecycle, поэтому отдельная presentation family для каждой строки была бы лишней.

## 7. MainMenu physical removal

Удаление выполняется только после green compile и focused tests нового return flow.

Удаляются точные targets:

- `Assets/Code/MainMenu` и его folder meta;
- `Assets/Code/UI/MainMenu` и его folder meta;
- `Assets/Content/UI/Windows/Main/MainMenu.prefab` + meta;
- `Assets/Content/UI/Windows/Main/MainMenuTeamMemberRow.prefab` + meta;
- entry `Windows/Main/MainMenu.prefab` из `Assets/AddressableAssetsData/AssetGroups/UI.asset`;
- generated `AddressableIds.UI.WindowsMainMainMenuPrefab` только через штатную regeneration после изменения Addressables Settings.

`LoadingScreen.prefab`, UI group, canvas, materials и другие shared assets не удаляются.

Перед удалением повторяется `rg` по assembly name, namespaces, root type, prefab GUID, row prefab GUID и generated ID. Если найден внешний consumer, удаление соответствующего target останавливается до разбора; compatibility shim не создаётся.

После удаления:

- `rg` по runtime/tests/assets возвращает ноль MainMenu consumers;
- asmdef graph не содержит `MainMenu`/`DungeonTeam.MainMenu`;
- Addressable entry отсутствует, generated API перегенерирован;
- config catalog не изменяется, если фактический audit подтверждает отсутствие MainMenu config page;
- `Docs/AI/index.md` больше не маршрутизирует удалённый UI module.

## 8. Test design

### 8.1. Test-first pure behavior

До production-кода добавить focused EditMode tests:

- builder отображает `Completed` и `Defeated` configured texts;
- zero, one и arbitrary many rewards обрабатываются без fixed count;
- amount и catalog display name попадают в каждую строку;
- unknown reward отклоняет весь summary;
- входной result/collections не мутируются;
- transition gate принимает один terminal transition и переходит в `Faulted` после explicit failed recovery;
- session state заменяет прошлый summary новым только после успешного build;
- `RunSummaryViewModel` показывает входной snapshot, empty state и симметричный repeated open/close.

Expected values задаются product/config contract, а не вычисляются production builder-ом в тесте.

### 8.2. PlayMode

- actual Addressable Guild Hall prefab содержит non-null inactive Run Summary View и row template;
- `Reception` с summary открывает окно, блокирует world input, Close восстанавливает его;
- `Reception` без summary не показывает пустое выдуманное вознаграждение;
- окно отображает переданное variable количество rows;
- два open/close cycle не дублируют listeners/rows;
- disposal Hall очищает viewmodel/view/runtime rows и world lease ровно один раз.

### 8.3. Regression и mechanical

- Bootstrap/application focused EditMode;
- GuildHall EditMode + PlayMode regression;
- DungeonRun terminal result/root regression;
- Rewards catalog regression;
- Unity compile затронутых assemblies;
- Addressables/generated ID consistency;
- `rg` audit MainMenu consumers;
- meta/GUID/asmdef checks из Unity validation skill;
- `git diff --check` и валидность относительных doc links.

Тесты не фиксируют количество rewards, NPC, contracts, locations или prefab children.

## 9. Implementation order

1. Добавить failing EditMode tests для summary builder, outcome text и gate failure state.
2. Расширить Guild Hall immutable/config text contracts и production config.
3. Реализовать internal `GuildRunSummaryBuilder` в Bootstrap и green pure tests.
4. Подключить единый named `Finished` subscription к обоим способам запуска run.
5. Реализовать result → stop → session → new Hall orchestration и `Faulted` invariant.
6. Добавить Run Summary MVVM family, prefab binding и reception routing.
7. Запустить focused compile/EditMode/PlayMode; исправить lifecycle и serialized binding errors.
8. Повторить MainMenu consumer/GUID audit, удалить только доказанно неиспользуемые targets.
9. Через Unity Addressables Settings убрать entry и перегенерировать `AddressableIds` штатным инструментом.
10. Запустить полный согласованный regression набор и обновить фактический validation status документов.

## 10. Done criteria

GH-7 готов, когда:

- каждый terminal result принимается Application ровно один раз;
- Dungeon Run полностью остановлен до создания новой Guild Hall;
- новый Hall получает committed summary, построенный из settlement receipt и всех поддержанных terminal reward values;
- стойка регистрации показывает этот summary и корректно управляет modal/input lifecycle;
- второй цикл Map → Dungeon → Hall проходит через тот же owner path без второго root/subscription;
- неподдержанная награда не сохраняется; поддержанные Gold/resource grants применяются ровно один раз через Player Profile до показа summary;
- невосстановимый переход не оставляет ложное active state;
- MainMenu code/assets/addressable/generated ID физически отсутствуют и не имеют consumers;
- automated checks из §8 зелёные;
- manual smoke, build и внешний playtest честно отмечены как не выполненные, если они не запускались.

## 11. Фактическая реализация и validation

Реализованы `GuildRunSummaryBuilder` и `RewardSettlementMapper` в `Bootstrap`, application-owned named `DungeonRunRoot.Finished` subscription для World Map и developer launches, verified exactly-once profile settlement before `DungeonRunHost.Stop()`, committed summary transfer после stop, `PlayerFlowState.Faulted` для неудачного recovery после уничтожения outgoing owner и вложенная `RunSummary` MVVM family в `GuildHallGraybox.prefab`.

Production config содержит localization-ready summary texts. Reception открывает окно только при наличии session summary; variable reward rows принадлежат View, без item ViewModel. MainMenu runtime/UI trees, оба prefabs, Addressable entry и generated ID удалены после `rg`/GUID/asmdef audit.

В открытом Unity Editor `6000.7.0a3` после независимого review выполнены:

- Unity refresh/compile завершён, compilation state ready, compile errors отсутствуют;
- полный project EditMode — `429/429 passed`;
- полный project PlayMode — `102/102 passed`, включая actual Guild Hall/World Map Addressable lifecycle;
- `rg`/GUID/asmdef/generated-ID audit — MainMenu consumers и orphaned references отсутствуют;
- `Bootstrap.csproj` compile — `0` warnings, `0` errors; scoped `git diff --check` excluding the user-owned TMP fallback asset — clean. Repository-wide `validate-unity-change.ps1 -AllAssets` reports 35 unresolved GUID diagnostics in pre-existing imported showcase assets and 8 TMP diff-check diagnostics.

На независимом review дополнительно исправлены: переход Hall → Map в `Faulted` при неудачном recovery после потери owner, обязательное наличие обоих placeholders в reward format и зависимость Base ViewModel от family ModelBase вместо concrete Model. Manual full-flow smoke, build и внешний playtest не запускались.
