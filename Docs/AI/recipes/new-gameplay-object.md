# Рецепт: новый gameplay-объект

1. Определить feature-owner и способ создания объекта.
2. Создать папку MVP-семьи `<ObjectName>/Base` с абстракциями `<ObjectName>ViewBase`, `<ObjectName>ModelBase` и `<ObjectName>PresenterBase`.
3. Создать конкретные Model без Unity API, `ViewMonoBehaviour<TPresenter>` для Unity-ссылок и пользовательских событий и Presenter рядом с `Base/`; каждый тип наследуется от соответствующей абстракции семьи.
4. Presenter принимает `<ObjectName>ViewBase` и `<ObjectName>ModelBase`, связывает их, запускает сценарий и управляет подписками.
5. FeatureRoot/factory создаёт Model и Presenter, вызывает `Initialize`/`InitializeAsync`.
6. При удалении объекта освобождается Presenter; он освобождает Model, View и свои подписки.

Не помещать правила урона, наград или состояния боя во View. Base-классы содержат только контракт, а не сценарную реализацию.
