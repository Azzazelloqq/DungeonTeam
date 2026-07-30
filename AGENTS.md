# DungeonTeam: инструкции для агентов

## Непереговорные правила

- Не вносить изменения без явной команды пользователя на реализацию.
- Для новой проблемы или feature: сначала обсудить варианты, затем дать подробный план; писать код только после явного разрешения.
- Не изменять чужие или не относящиеся к задаче файлы в dirty worktree.
- Соблюдать DRY, KISS, SOLID, чистый код и существующую архитектуру. Не использовать костыли.
- Тестировать ожидаемое поведение, а не подгонять тесты или реализацию друг под друга.
- Перед завершением проверить полноту результата и убрать добавленное без требования.

## Как читать документацию

`Docs/AI` не читается автоматически. До изменения кода прочитай только документы, соответствующие задаче:

| Задача | Обязательные документы |
| --- | --- |
| Новая feature, границы модулей, asmdef | `Docs/AI/architecture.md`, `Docs/AI/module-rules.md` |
| Root, DI scope, async, subscriptions, disposal | `Docs/AI/architecture.md`, `Docs/AI/lifecycle.md`, `Docs/AI/libraries/lightdi.md`, `Docs/AI/libraries/roots-and-disposal.md` |
| Gameplay | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-gameplay-object.md` |
| UI | `Docs/AI/libraries/presentation.md`, `Docs/AI/recipes/new-ui-screen.md` |
| Addressables, prefab, сцены | `Docs/AI/libraries/addressables.md`, `Docs/AI/architecture.md` |
| Config | `Docs/AI/libraries/config.md` |
| Сохранения | `Docs/AI/libraries/persistence.md` |
| Tick, logger, utility extensions | `Docs/AI/libraries/runtime-services.md`, `Docs/AI/lifecycle.md` |

Если задача покрывает несколько строк, прочитай объединение документов. Не загружай всю документацию без необходимости.

## Архитектурные ограничения

- `Bootstrap` — единственная Unity-точка входа; создание application-сервисов только в composition root.
- Root владеет созданными объектами, cancellation token и освобождением ресурсов.
- Domain не зависит от Unity, DI, Addressables, UI, сохранений и конкретных feature.
- Gameplay использует MVP; UI использует MVVM.
- Зависимости передаются конструктором. `DiContainerProvider.Resolve<T>()` запрещён вне документированного инфраструктурного исключения.
- LightDI-container создаётся только на реальном application/module scope. Для малого или повторно создаваемого object graph используй явные конструкторы/фабрики.
- Когда появится Addressables-генератор, runtime-код не должен использовать строковые keys; использовать следует только сгенерированный API ключей.
- Для сохранений использовать только `SaveStore` V2 и `SaveKey<T>`; legacy `UnityBinaryLocalSaveSystem` запрещён.

## Проверка результата

- Перед изменением изучи затронутые asmdef и ближайшие архитектурные документы.
- После изменения запусти релевантные проверки. Если Unity уже открыта и batchmode недоступен, не закрывай редактор без команды пользователя; сообщи, какую проверку нужно запустить в редакторе.
- В финальном ответе укажи изменённые файлы, проверку и оставшиеся ручные действия.
