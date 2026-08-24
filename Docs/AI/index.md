# Маршрутизатор документации для AI-агентов

`AGENTS.md` — обязательная точка входа с общими правилами проекта. Этот файл определяет, какие подробные документы нужно прочитать для конкретного модуля и аспекта задачи.

## Как пользоваться маршрутизатором

До анализа, плана или изменения проекта:

1. Определи затронутые модули, публичные контракты, assets и поведение.
2. Выбери все подходящие строки из разделов «Модули» и «Аспекты».
3. Полностью прочитай объединение документов из колонки «Обязательно».
4. Прочитай документ из колонки «Условно», только если совпал указанный trigger.
5. Перед завершением выбери подходящий уровень Unity validation.

Не читай весь `Docs` без необходимости. Если маршрут отсутствует или не покрывает задачу, сначала сообщи о пробеле в документации.

## Роли источников

- Текущее требование пользователя и согласованный продуктовый сценарий определяют ожидаемое поведение.
- `Docs/Product` фиксирует продуктовые цели и сценарии. Проверяй, что документ относится к текущему scope задачи.
- `Docs/AI` фиксирует действующие инженерные правила, lifecycle, библиотеки, рецепты и проверки.
- `Docs/Technical` содержит design конкретной системы. Перед применением проверь его статус, scope и соответствие текущим assembly и коду; `READY` или старый design не доказывает, что всё уже реализовано без изменений.
- Текущий combat playable-контракт задают `Docs/Product/ProductDirection.md`, `Docs/Product/ExperienceDirection.md`, `Docs/Product/ProductValidationPlan.md` и `Docs/Product/DungeonExpeditionVerticalSliceGDD.md`; соответствующий combat design — `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md`. Согласованный Guild Hall foundation задаёт `Docs/Product/GuildHallGDD.md`; его design и порядок — `Docs/Technical/GuildHallTechnicalDesign.md` и `Docs/Technical/GuildHallImplementationPlan.md`.
- `Docs/Product/CoreCombatPrototypeGDD.md` и `Docs/Technical/SquadCombatStage0TechnicalDesign.md` — архивные Stage 0 proposals. Читай их только для явно запрошенного исторического аудита; они не определяют текущее ожидаемое поведение.
- Production-код и assets доказывают текущее устройство, но не являются источником ожидаемого результата теста.
- Для API кастомной библиотеки источником правды является текущий исходник совпадающей версии в `Library/PackageCache`.

При конфликте источников не выбирай молча удобный вариант: зафиксируй расхождение и останови изменение затронутого контракта до уточнения.

## Модули

| Модуль или область | Обязательно | Условно |
| --- | --- | --- |
| `Bootstrap`, application composition, player flow | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/libraries/lightdi.md`, `Docs/AI/libraries/roots-and-disposal.md` | `Docs/Technical/WorldMapApplicationFlowTechnicalDesign.md` — Guild Hall/World Map/Dungeon Run flow; `Docs/Technical/DungeonReturnGuildSummaryTechnicalDesign.md` — terminal Dungeon result, session summary и возврат в Guild Hall; `Docs/Technical/ProjectArchitectureTechnicalDesign.md` — изменение общей модульной архитектуры; UI-маршрут — изменение конкретного UI screen |
| `Configuration` | `Docs/AI/libraries/config.md` | `Docs/AI/libraries/addressables.md` — загрузка config через Addressables; `Docs/AI/testing.md` — изменение parsing или правил config |
| `Feedback` | `Docs/AI/libraries/feedback.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/addressables.md` — banks/assets; `Docs/AI/libraries/dotween.md` — tween-переходы; `Docs/AI/testing.md` — изменение поведения mixer/player/service |
| `Actors` | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-gameplay-object.md` | `Docs/AI/libraries/config.md` — stats/definitions; `Docs/AI/libraries/addressables.md` — prefab/loaders; `Docs/AI/lifecycle.md` — factory/presenter ownership; `Docs/AI/testing.md` — Domain/Runtime behavior |
| `Combat` | `Docs/AI/architecture.md`, `Docs/AI/libraries/presentation.md` | `Docs/Product/DungeonExpeditionVerticalSliceGDD.md` — изменение текущего пользовательского combat-поведения; `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md` — изменение текущего runtime design; `Docs/AI/lifecycle.md` — tick/use/disposal; `Docs/AI/testing.md` — combat rules |
| `Skills` | `Docs/AI/architecture.md`, `Docs/AI/libraries/presentation.md` | `Docs/Product/DungeonExpeditionVerticalSliceGDD.md` — mechanic/loadout/target behavior текущего playable; `Docs/Technical/SkillVfxGuide.md` — VFX, presentation assets, projectile visuals или VFX Lab; `Docs/AI/libraries/config.md` — catalog/loadout/config; `Docs/AI/libraries/addressables.md` — prefab/VFX loading; `Docs/AI/lifecycle.md` — execution/handles/projectiles; `Docs/AI/libraries/feedback.md` — audio/haptic feedback; `Docs/AI/testing.md` — skill behavior |
| `Hero`, input, target selection | `Docs/AI/architecture.md`, `Docs/AI/libraries/presentation.md` | `Docs/Product/DungeonExpeditionVerticalSliceGDD.md` — текущий player-facing control/target contract; `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md` — текущий runtime design; `Docs/AI/lifecycle.md` — subscriptions/tick/disposal; `Docs/AI/testing.md` — input/target decisions; Unity validation — Input System, camera или scene binding |
| `Team`, companion AI, команды | `Docs/AI/architecture.md`, `Docs/AI/libraries/presentation.md` | `Docs/Product/DungeonExpeditionVerticalSliceGDD.md` — текущее companion/command behavior; `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md` — текущий selector/follow design; `Docs/AI/lifecycle.md` — controllers/tick/disposal; `Docs/AI/testing.md` — AI/command decisions |
| `EnemyAI` | `Docs/AI/architecture.md`, `Docs/AI/libraries/presentation.md` | `Docs/Product/DungeonExpeditionVerticalSliceGDD.md` — observable enemy behavior текущего playable; `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md` — текущий encounter design; `Docs/AI/libraries/config.md` — behavior profiles; `Docs/AI/lifecycle.md` — controller/tick; `Docs/AI/testing.md` — deterministic decisions |
| `ContextActions` | `Docs/AI/libraries/presentation.md`, `Docs/AI/lifecycle.md` | `Docs/Product/DungeonExpeditionVerticalSliceGDD.md` — текущая семантика interaction и `FOLLOW`; `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md` — текущий runtime design; `Docs/AI/recipes/new-ui-screen.md` — самостоятельный UI screen; `Docs/AI/testing.md` — model/viewmodel/action behavior; Unity validation — touch/UI binding |
| `Dungeon` | `Docs/AI/architecture.md`, `Docs/Technical/DungeonSystemTechnicalDesign.md` | `Docs/AI/module-rules.md` — границы/asmdef; `Docs/AI/libraries/config.md` — generation config; `Docs/AI/libraries/addressables.md` — maps/chunks/prefabs; `Docs/AI/lifecycle.md` — factory/instance ownership; `Docs/AI/testing.md` — planners/generation |
| `DungeonRun` | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/Product/DungeonExpeditionVerticalSliceGDD.md`, `Docs/Technical/DungeonExpeditionVerticalSliceTechnicalDesign.md` | `Docs/Technical/DungeonReturnGuildSummaryTechnicalDesign.md` — terminal result и возврат в Guild Hall; маршрут `Dungeon` — generation/map; маршруты `Hero`, `Team`, `Combat`, `ContextActions` или UI — соответствующая orchestration; `Docs/AI/testing.md` — progress/input/root behavior |
| `GuildHall`, Notice Board, reception summary | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/libraries/presentation.md`, `Docs/Product/GuildHallGDD.md`, `Docs/Technical/GuildHallTechnicalDesign.md`, `Docs/Technical/NoticeBoardTechnicalDesign.md` | `Docs/Technical/GuildHallImplementationPlan.md` — реализация; `Docs/Technical/DungeonReturnGuildSummaryTechnicalDesign.md` — terminal result/reception summary; маршрут `AmbientNpc` — NPC/dialogue; `Docs/AI/module-rules.md` — asmdef; `Docs/AI/libraries/config.md` — definitions/catalogs; `Docs/AI/libraries/addressables.md` — world prefab loading; `Docs/AI/libraries/runtime-services.md` — movement/tick; `Docs/AI/recipes/new-gameplay-object.md` и `Docs/AI/recipes/nested-presentation.md` — children; `Docs/AI/testing.md` — behavior/lifecycle; Unity validation — prefab/input/camera/bindings |
| `PlayerProfile`, roster, money, guild rank, profile save | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/module-rules.md`, `Docs/AI/libraries/persistence.md`, `Docs/Product/PlayerProfileGDD.md`, `Docs/Technical/PlayerProfileTechnicalDesign.md` | `Docs/Technical/PlayerProfileImplementationPlan.md` — milestone order; `Docs/Product/ProductMvpGDD.md` — persistent-profile/product-loop context; `Docs/AI/libraries/config.md` — static rank/item definitions; UI route — Guild Profile MVVM; `Docs/AI/testing.md` — state/persistence behavior; route `GuildHall` — reception consumer; Unity validation — prefab/bindings/asmdef |
| `Contracts`, contract acceptance/completion/save | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/module-rules.md`, `Docs/AI/libraries/persistence.md`, `Docs/AI/libraries/config.md`, `Docs/Product/ContractGDD.md`, `Docs/Technical/ContractTechnicalDesign.md` | `Docs/AI/testing.md` — state/migration behavior; route `GuildHall` — Notice Board consumer; route `DungeonRun` — terminal result boundary; Unity validation — existing board bindings |
| `Quests`, quest acceptance/progress/completion/save | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/module-rules.md`, `Docs/AI/libraries/persistence.md`, `Docs/AI/libraries/config.md`, `Docs/Product/QuestGDD.md`, `Docs/Technical/QuestTechnicalDesign.md` | `Docs/AI/testing.md` — state/migration behavior; route `GuildHall` — Notice Board and dialogue completion; route `DungeonRun` — settled terminal-result boundary; Unity validation — board prefab/bindings |
| `AmbientNpc`, authored ambient behavior, one-line dialogue, vignette | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/libraries/presentation.md`, `Docs/AI/module-rules.md`, `Docs/Technical/AmbientNpcTechnicalDesign.md` | Consumer Product/Technical design — observable location behavior; `Docs/AI/libraries/config.md` — profiles/dialogue pools; `Docs/AI/libraries/runtime-services.md` — parent-driven tick; `Docs/AI/recipes/new-gameplay-object.md` и `Docs/AI/recipes/nested-presentation.md` — MVP/MVVM families; `Docs/AI/testing.md` — state/lifecycle; Unity validation — prefab/routes/UI bindings |
| `WorldMap` | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-ui-screen.md`, `Docs/AI/lifecycle.md`, `Docs/Product/GuildHallGDD.md`, `Docs/Technical/WorldMapApplicationFlowTechnicalDesign.md` | `Docs/Technical/GuildHallImplementationPlan.md` — milestone order; `Docs/AI/libraries/config.md` — locations; `Docs/AI/libraries/ui-service.md` и `Docs/AI/libraries/addressables.md` — UI prefab; `Docs/AI/recipes/nested-presentation.md` — location items; `Docs/AI/testing.md` — selection/availability; Unity validation — UI prefab/bindings |
| `DeveloperTools`, developer console | `Docs/AI/architecture.md`, `Docs/AI/module-rules.md` | `Docs/AI/testing.md` — pure C# state/validation; `Docs/AI/lifecycle.md` — runtime host ownership; Unity validation — debug-only input/IMGUI binding |
| `Chests`, `Rewards` | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-gameplay-object.md` | `Docs/AI/libraries/config.md` — definitions/catalog; `Docs/AI/libraries/addressables.md` — prefab/loaders; `Docs/AI/lifecycle.md` — factory/loader ownership; `Docs/AI/testing.md` — collection/release behavior |
| UI `LoadingScreen`, `CombatHud` | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-ui-screen.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/ui-service.md` — Show/Hide/Close/queue; `Docs/AI/recipes/nested-presentation.md` — child viewmodel/page; соответствующий Product-документ — изменение пользовательского flow; маршруты `Skills`/`Hero` — skill slots или target state; `Docs/AI/testing.md` — ViewModel/View behavior |
| `UIService`, `UIUtills` | `Docs/AI/libraries/ui-service.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/addressables.md` — загрузка UI prefab; `Docs/AI/libraries/dotween.md` — transition/fade; `Docs/AI/testing.md` — queue/group/lifecycle behavior; Unity validation — prefab/Canvas/serialized binding |
| Product direction или vertical slice | Соответствующий документ из `Docs/Product` | Соответствующий `Docs/Technical` design и все затронутые module/aspect-маршруты |

## Аспекты задачи

Эти строки добавляются к маршруту модуля, а не заменяют его.

| Trigger | Обязательно | Условно |
| --- | --- | --- |
| Новая feature, изменение границ модулей или asmdef | `Docs/AI/architecture.md`, `Docs/AI/module-rules.md`, `.codex/skills/dungeonte-module-boundaries/SKILL.md`, `.codex/skills/dungeonte-asmdef/SKILL.md` | `Docs/Technical/ProjectArchitectureTechnicalDesign.md` — изменение общей архитектуры |
| Root, DI scope, composition, ownership | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/libraries/lightdi.md`, `Docs/AI/libraries/roots-and-disposal.md`, `.codex/skills/dungeonte-lightdi-scopes/SKILL.md` | `.codex/skills/dungeonte-module-boundaries/SKILL.md` — изменение feature boundary |
| Async, cancellation, subscriptions, tick, disposal | `Docs/AI/lifecycle.md`, `.codex/skills/dungeonte-async-lifecycle/SKILL.md` | `Docs/AI/libraries/runtime-services.md` — TickHandler/runtime service |
| Gameplay MVP, Presenter/View/Model | `Docs/AI/libraries/presentation.md` | `Docs/AI/recipes/new-gameplay-object.md` — новый gameplay object; `Docs/AI/recipes/nested-presentation.md` — child presentation |
| UI MVVM, screen, ViewModel | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-ui-screen.md` | `Docs/AI/recipes/nested-presentation.md` — список/page/child ViewModel; `Docs/AI/libraries/ui-service.md` — показ через UIService |
| UIService, UI groups, Show/Hide/Close, queues | `Docs/AI/libraries/ui-service.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/addressables.md` — создаваемый UI prefab; `Docs/AI/libraries/dotween.md` — animated transition |
| Addressables, ResourceLoader, prefab или scene loading | `Docs/AI/libraries/addressables.md`, `Docs/AI/architecture.md`, `.codex/skills/dungeonte-addressables-3x/SKILL.md` | `Docs/AI/lifecycle.md` — handles/cancellation/ownership; Unity validation — assets/scenes/asmdef |
| Config | `Docs/AI/libraries/config.md` | `Docs/AI/libraries/addressables.md` — Addressable config; `Docs/AI/testing.md` — parsing/validation behavior |
| Persistence/save | `Docs/AI/libraries/persistence.md` | `Docs/AI/lifecycle.md` — async save/load lifetime; `Docs/AI/testing.md` — persistence behavior |
| Feedback, звук, музыка, вибрация | `Docs/AI/libraries/feedback.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/addressables.md` — banks/assets |
| DOTween, fade, visual transition | `Docs/AI/libraries/dotween.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/feedback.md` — декоративный feedback; Unity validation — prefab/scene/serialized state |
| Skill VFX, projectile VFX, VFX Lab | `Docs/Technical/SkillVfxGuide.md`, `Docs/AI/libraries/addressables.md`, `Docs/AI/lifecycle.md` | `Docs/AI/libraries/feedback.md` — звук/feedback; `Docs/AI/libraries/dotween.md` — tween; маршрут `Skills` — runtime contract |
| Tick, logger, utility extension, runtime hot path | `Docs/AI/libraries/runtime-services.md`, `.codex/skills/dungeonte-runtime-quality/SKILL.md` | `Docs/AI/lifecycle.md` — owner/tick/subscriptions; profiler proof — заявленный hotspot |
| Автотест, TDD, исправление дефекта | `Docs/AI/testing.md`, `.codex/skills/dungeonte-testability/SKILL.md` | `Docs/AI/lifecycle.md` — async/disposal; соответствующий module route — источник ожидаемого поведения |
| Любое изменение Unity-проекта | `.codex/skills/dungeonte-unity-validation/SKILL.md` | Механический script из skill — assets/scenes/prefabs/meta/asmdef; manual smoke — prefab/scene/input/animation/serialized binding |

## Правила комбинации

- Маршрут модуля задаёт предметный контекст; маршрут аспекта добавляет архитектурные и библиотечные правила.
- Если задача затрагивает несколько модулей или аспектов, прочитай объединение всех маршрутов без дубликатов.
- Product-документ обязателен, когда меняется наблюдаемое пользователем поведение, баланс, управление или acceptance criteria; для механического рефакторинга без изменения поведения он условен.
- Technical design обязателен только при совпадении его scope с задачей. Всегда проверь статус документа и фактическое состояние текущего кода.
- Документ с путями, типами или assembly, расходящимися с проектом, считается кандидатом на documentation drift; не применяй его молча.
- Новая module/subsystem документация добавляется в этот индекс в той же задаче. Удаление или перенос документа требует обновить все его маршруты.

## Проверка результата

Для любой реализации прочитай `.codex/skills/dungeonte-unity-validation/SKILL.md` и выбери минимальное доказательство по типу изменения. В финальном отчёте разделяй:

- компиляцию;
- автоматические тесты;
- Unity/manual proof;
- непроверенные пути.

Зелёная компиляция не доказывает prefab, scene, input, animation, Addressables ownership или Unity lifecycle.
