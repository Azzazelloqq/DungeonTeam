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
- `ActorId + Level` выбирают immutable runtime stats и rank combat loadout; `ActorDefinition` хранит только visual prefab и не является источником gameplay-баланса.
- `CombatLoadoutId` определяет набор доступных combat actions, а rank выбирает параметры конкретного действия из Combat config. Hero, companion и enemy controller используют один и тот же runtime combat-контур.
- `BehaviorId` определяет профиль runtime-controller; он не хранится во View, ActorDefinition или visual config.
- Dungeon authoring, scenario и `EnemySpawnPlan` только переносят opaque `ActorId`/`BehaviorId` вместе с Pose и semantic data, не завися от Actor/EnemyAI/MVP/Addressables.
- Конкретный режим materializes Actor MVP по `ActorId`, выбирает controller settings по `BehaviorId`, создаёт controller и владеет его lifecycle.
