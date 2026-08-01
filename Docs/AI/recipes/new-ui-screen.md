# Рецепт: новый UI-экран

1. Определить owner экрана: scene root или feature root.
2. Создать папку семьи `<ScreenName>/Base` и абстракции `<ScreenName>ViewBase`, `<ScreenName>ModelBase`, `<ScreenName>ViewModelBase`.
3. Создать конкретные Model, ViewModel и View рядом с `Base/`. ViewModel наследуется от `<ScreenName>ViewModelBase`, получает `<ScreenName>ModelBase`, use case и сервисы через конструктор; использовать reactive properties и команды.
4. Создать Unity View, наследовать её от `<ScreenName>ViewBase`, привязать к `<ScreenName>ViewModelBase` и сохранить подписки в `CompositeDisposable`.
5. Root создаёт ViewModel, вызывает `InitializeAsync`, затем инициализирует View.
6. При закрытии root освобождает ViewModel; View освобождается вместе с ним по lifecycle библиотеки.

View не вызывает сервисы и не изменяет модель напрямую; ViewModel не зависит от View. Base-классы содержат только контракт, а конкретные типы — реализацию.
