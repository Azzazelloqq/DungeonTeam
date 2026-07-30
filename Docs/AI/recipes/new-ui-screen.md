# Рецепт: новый UI-экран

1. Определить owner экрана: scene root или feature root.
2. Создать Model только если экран владеет самостоятельным presentation-state; иначе ViewModel получает use case/сервис через конструктор.
3. Создать ViewModel на базе MVVM-библиотеки, reactive properties и команды.
4. Создать Unity View, подписать её на ViewModel и сохранить подписки в `CompositeDisposable`.
5. Root создаёт ViewModel, вызывает `InitializeAsync`, затем инициализирует View.
6. При закрытии root освобождает ViewModel; View освобождается вместе с ним по lifecycle библиотеки.

View не вызывает сервисы и не изменяет модель напрямую.
