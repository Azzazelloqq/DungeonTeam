# Рецепт: новый gameplay-объект

1. Определить feature-owner и способ создания объекта.
2. Создать папку MVP-семьи `<ObjectName>/Base` с абстракциями `<ObjectName>ViewBase`, `<ObjectName>ModelBase` и `<ObjectName>PresenterBase`.
3. Создать конкретные Model без Unity API, `ViewMonoBehaviour<TPresenter>` для Unity-ссылок и пользовательских событий и Presenter рядом с `Base/`; каждый тип наследуется от соответствующей абстракции семьи.
4. Presenter принимает `<ObjectName>ViewBase` и `<ObjectName>ModelBase`, связывает их, запускает сценарий и управляет подписками.
5. FeatureRoot/factory создаёт Model и Presenter, вызывает `Initialize`/`InitializeAsync`.
6. При удалении объекта освобождается Presenter; он освобождает Model, View и свои подписки.

Не помещать правила урона, наград или состояния боя во View. Base-классы содержат только контракт, а не сценарную реализацию.

## Единый стиль ID-driven MVP family

Для акторов, наград, сундуков и аналогичных gameplay-объектов используется один поток:

`Business ID -> ViewAssetCatalog -> AddressableIds -> ViewLoader -> LoadedViewSet -> Factory -> Model + View + Presenter`.

- Business/config хранит идентификатор и игровые параметры, но не `GameObject`, `AssetReference`, material, цвет или другой presentation asset.
- `ViewAssetCatalog` находится в infrastructure части своей feature и явно сопоставляет business ID со сгенерированным ключом `AddressableIds`.
- `ViewLoader` загружает prefab через `IResourceLoader` и проверяет наличие корневого `ViewBase`. View, Model и Presenter не обращаются к Addressables.
- Feature root владеет загруженным `ViewSet` и созданными MVP-экземплярами. Сначала освобождаются экземпляры, затем `ViewSet`, который освобождает загруженные prefab assets.
- Конкретный visual prefab является prefab variant базового prefab своей семьи. Модель из стороннего пакета вкладывается внутрь варианта; исходный asset пакета не изменяется.
- Семья имеет общий Animator Controller с едиными именами параметров и переходов. Конкретный вариант использует Animator Override Controller и заменяет только доступные clips.
- Базовый prefab задаёт общий контракт и настройки. Он может не быть самостоятельным runtime asset; каждый конкретный Addressable-вариант обязан иметь все обязательные View/Animator bindings.

Добавление нового типа состоит из четырёх действий: добавить business ID/config, создать prefab variant и Animator Override Controller, зарегистрировать вариант в Addressables, добавить ID -> `AddressableIds` mapping. Новые фабрики, DI-регистрации или ветки в View для этого не нужны.

Текущие семьи, следующие этому стилю: `Actor`, `RewardPickup`, `Chest`.

## Actor identity и enemy behavior

- `ActorId` определяет Actor MVP: identity, business stats и visual через Actor config и ViewAssetCatalog.
- `ActorId + ActorLevel` выбирают только Actor MVP, immutable stats и visual. `ActorDefinition` хранит visual prefab, а Actor level не выбирает loadout и не меняет skill level.
- `LoadoutId` независимо выбирает слоты. Каждый слот явно хранит `SkillId + SkillLevel`; Hero, companion и enemy controller используют слот, не ветвятся по ActorId или конкретному SkillId.
- `SkillId` выбирает typed mechanic из `SkillCatalog`, а `SkillLevel` — её damage/range/cooldown и другие параметры конкретного типа. Gameplay-config не содержит prefab, material, clip или `AssetReference`.
- `BehaviorId` определяет профиль runtime-controller; он не хранится во View, ActorDefinition или visual config.
- Dungeon authoring, scenario и `EnemySpawnPlan` только переносят opaque `ActorId`/`ActorLevel`/`BehaviorId`/`LoadoutId` вместе с Pose и semantic data, не завися от Skills/Combat/Actor MVP/Addressables.
- Конкретный режим materializes Actor MVP по `ActorId`, выбирает controller settings по `BehaviorId`, создаёт controller и владеет его lifecycle.

Skill visual идёт отдельным presentation-потоком:

`SkillId -> SkillViewAssetCatalog -> AddressableIds -> SkillViewLoader -> SkillViewSet -> SkillProjectile MVP`.

Actor предоставляет общий `SkillOriginAnchor` и semantic animation cues `Attack`/`Cast`. View не выбирает ID и не загружает Addressables. Root сначала освобождает controllers и активные skill executions/projectiles, затем Actor instances и только после этого `SkillViewSet` с загруженными prefab assets.

## Skill use process и visual sequence

- `SkillLevelDefinition.UseTiming` задаёт gameplay-authoritative `CommitDelay` и `RecoveryDuration`. Animation Event, VFX и presentation asset не применяют механику Skill.
- Одно использование проходит `Preparing -> Commit -> Recovering -> Completed` либо `Cancelled`. До `Commit` отмена не создаёт projectile, не наносит урон и не запускает cooldown; после `Commit` typed-механика не откатывается.
- `SkillPresentationAsset` содержит только typed animation/VFX cues относительно фаз `Start`, `Commit`, `Impact`, `Complete`, `Cancel`. Отложенные cues используют clock активного skill execution, а `Impact` приходит от фактического попадания projectile, не из фиксированного таймера.
- Animation cue семантический и общий для Actor variants; `SkillId` не попадает в Animator. VFX выбирает только semantic anchor (`SourceOrigin`, `TargetHit`, `ImpactPosition`).
- Presentation assets и их prefab dependencies загружаются на loading boundary через `SkillViewLoader`, остаются во владении `SkillViewSet`; активные cue instances принадлежат `SkillExecutionController` и уничтожаются до release `SkillViewSet`.
