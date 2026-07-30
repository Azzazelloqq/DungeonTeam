# Рецепт: новый gameplay-объект

1. Определить feature-owner и способ создания объекта.
2. Создать Model без Unity API.
3. Создать `ViewMonoBehaviour<TPresenter>` для Unity-ссылок и пользовательских событий.
4. Создать Presenter: он связывает View и Model, запускает сценарий, управляет подписками.
5. FeatureRoot/factory создаёт Model и Presenter, вызывает `Initialize`/`InitializeAsync`.
6. При удалении объекта освобождается Presenter; он освобождает Model, View и свои подписки.

Не помещать правила урона, наград или состояния боя во View.
