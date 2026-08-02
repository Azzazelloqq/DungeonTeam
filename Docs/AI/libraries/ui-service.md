# UIService

## Назначение

`UIService` — единая точка создания и управления runtime UI, загружаемым через `IResourceLoader`.

Сервис:

- асинхронно загружает prefab по сгенерированному `AddressableIds`;
- создаёт его сразу под parent нужной UI-группы;
- гарантирует скрытое состояние до первого активного кадра;
- сериализует `Show`, `Hide` и `Close` внутри каждой группы;
- управляет взаимоисключающими окнами и очередями;
- владеет созданными экземплярами и освобождает их ресурсы.

Сервис не создаёт ViewModel, не выполняет binding и не принимает продуктовые решения о навигации. Это остаётся ответственностью feature/root/presenter слоя.

## Модуль и зависимости

- Runtime assembly: `DungeonTeam.UIService`.
- Root namespace: `Code.UIService`.
- Допустимые зависимости: UnityEngine, UniTask и assembly `ResourceLoader`.
- Прямое использование Addressables внутри модуля запрещено.
- `IUIFactory` и отдельный интерфейс сервиса не вводятся без появления второй реализации.

`UIService` создаётся в composition root явным конструктором. `IResourceLoader` передаётся извне и не принадлежит сервису: `UIService.Dispose()` не должен вызывать `Dispose()` загрузчика.

```text
Root
├── IResourceLoader
├── UICanvasContext
└── UIService
    └── созданные IUIElement и их runtime-записи
```

Root обязан освободить `UIService` до уничтожения canvas и `IResourceLoader`.

## Контракт UI-элемента

Корневой компонент prefab реализует `IUIElement`:

```csharp
public interface IUIElement
{
    UIElementSettings Settings { get; }
    void HideImmediately();
    UniTask ShowAsync(CancellationToken cancellationToken);
    UniTask HideAsync(CancellationToken cancellationToken);
}
```

- `Settings` задаёт группу и поведение после скрытия.
- `HideImmediately()` мгновенно переводит элемент в полностью невидимое и неинтерактивное состояние. Метод вызывается только сервисом до активации экземпляра и при аварийном завершении lifecycle.
- `ShowAsync()` и `HideAsync()` содержат анимацию перехода, если она нужна. Вызывать их напрямую снаружи запрещено: иначе сервис потеряет согласованность активного элемента и очереди.
- `Settings` должен безопасно читаться с неактивного prefab asset и не зависеть от `Awake`, `Start` или runtime binding.

В prefab должен быть ровно один корневой компонент, совместимый с запрошенным `TUI : IUIElement`.

## Настройки элемента

`UIElementSettings` содержит:

- `Group` — группа и canvas parent элемента;
- `HideBehavior` — что сервис делает после обычного скрытия.

`UIElementHideBehavior.KeepInQueue` сохраняет загруженный экземпляр для последующего показа. `UIElementHideBehavior.Close` после скрытия удаляет экземпляр из управления и освобождает его ресурсы.

`Close` отличается от `Hide`: `CloseAsync` всегда окончательно удаляет элемент независимо от `HideBehavior`.

## Canvas context и группы

`UICanvasContext` принимает отдельный `RectTransform` для каждой группы:

| Группа | Поведение показа | Порядок сохранённых элементов |
| --- | --- | --- |
| `Background` | Одновременно виден один элемент | LIFO: возврат к предыдущему |
| `FullScreen` | Одновременно виден один элемент | LIFO: возврат к предыдущему |
| `Popup` | Одновременно виден один элемент | FIFO: ожидающие popup показываются по очереди |
| `OverlayElement` | Элементы показываются независимо | Очереди нет |
| `DynamicOverlayElement` | Элементы показываются независимо | Очереди нет |

Все операции одной группы сериализуются. Переходы разных групп друг друга не блокируют.

`UIElementGroup` — единственная классификация поведения. Не вводить дополнительный mode enum. Долгоживущее состояние хранится раздельно по форме группы: history содержит active и LIFO-историю, popup — active и FIFO-очередь, parallel — множество видимых элементов. Универсальный state со всеми коллекциями сразу запрещён, поскольку создаёт недопустимые сочетания состояния.

Для `Background` и `FullScreen` показ нового элемента скрывает текущий. Элемент с `KeepInQueue` попадает в историю группы, а с `Close` освобождается. После скрытия или закрытия активного элемента сервис показывает предыдущий из истории.

Для `Popup` новый элемент при занятой группе становится в FIFO-очередь. После скрытия или закрытия активного popup сервис показывает следующий. Скрытый popup с `KeepInQueue` переносится в конец оставшейся очереди.

Overlay-группы не ограничивают количество одновременно видимых элементов. Их `Hide` только скрывает либо закрывает конкретный экземпляр согласно `HideBehavior`.

## Создание

Основной API:

```csharp
UniTask<TUI> CreateAsync<TUI>(
    string addressableId,
    bool hideOnCreate = true,
    CancellationToken token = default)
    where TUI : class, IUIElement;
```

Несмотря на строковый параметр API загрузчика, вызывающий код обязан передавать сгенерированное значение `AddressableIds`, а не строковый литерал.
Конкретное имя имеет вид `AddressableIds.<GeneratedGroup>.<GeneratedEntry>` и определяется текущими Addressables Settings.

Порядок создания принципиален:

1. Загрузить prefab через `IResourceLoader`.
2. Проверить контракт и получить настройки с неактивного prefab.
3. Создать экземпляр сразу под конечным parent группы.
4. Вызвать `HideImmediately()` до активации экземпляра.
5. Активировать экземпляр.
6. Если `hideOnCreate == false`, провести его через обычный `ShowAsync` сервиса до возврата результата.

Prefab asset обязан иметь неактивный корневой `GameObject`. Это защищает от первого видимого кадра без временного parent, перепривязки `Transform` и принудительной перестройки Canvas.

Сервис не должен использовать `SetParent` после создания и не должен вызывать `Canvas.ForceUpdateCanvases` или `LayoutRebuilder.ForceRebuildLayoutImmediate` для исправления порядка инициализации.

## Управление экземпляром

`CreateAsync<TUI>` возвращает непосредственно запрошенный UI-контракт. `TUI` может быть конкретным компонентом либо feature-интерфейсом, наследующим `IUIElement`.

Сервис хранит приватную runtime-запись элемента: prefab для последующего `ReleaseResource`, корневой `GameObject`, стабильные настройки и текущее состояние перехода. Эта запись не выходит в публичный API.

Элементом управляют только через создавший его сервис:

```csharp
UniTask ShowAsync(IUIElement element, CancellationToken token = default);
UniTask HideAsync(IUIElement element, CancellationToken token = default);
UniTask CloseAsync(IUIElement element, CancellationToken token = default);

await uiService.ShowAsync(inventory, cancellationToken);
await uiService.HideAsync(inventory, cancellationToken);
await uiService.CloseAsync(inventory, cancellationToken);
```

Запрещено:

- напрямую вызывать `ShowAsync`, `HideAsync` или `HideImmediately` элемента;
- самостоятельно уничтожать его `GameObject`;
- самостоятельно освобождать загруженный prefab;
- передавать элемент другому экземпляру `UIService`.

`CloseAsync` удаляет элемент из active/history/pending состояния группы, при необходимости проигрывает скрытие и затем освобождает экземпляр и загруженный asset. Повторная операция с закрытым элементом считается ошибкой использования контракта.

## Lifecycle и отмена

- Внешний owner передаёт токен своего scope во все async-операции.
- `ShowAsync`, `HideAsync` и `CloseAsync` объединяют токен операции с lifetime token сервиса.
- `CreateAsync` передаёт токен вызывающего кода в `IResourceLoader` и повторно проверяет lifecycle при регистрации результата.
- `Dispose()` отменяет незавершённые переходы и ожидание очередей, мгновенно скрывает все принадлежащие сервису элементы и освобождает каждый ресурс ровно один раз.
- Ошибки загрузки, проверки prefab, анимации и отмена не проглатываются.
- Если создание прервано после загрузки, сервис обязан уничтожить частичный экземпляр и освободить prefab.

После `CloseAsync` или `Dispose()` элемент нельзя повторно показывать или скрывать.

## Ошибки конфигурации

Сервис должен завершать операцию ошибкой при следующих нарушениях:

- пустой `addressableId`;
- активный корневой `GameObject` prefab;
- отсутствующий или неоднозначный корневой компонент `TUI`;
- отсутствующий/destroyed parent группы;
- изменение `Group` между prefab и созданным экземпляром;
- передача закрытого или созданного другим сервисом элемента.

При ошибке уже полученные ресурсы должны быть освобождены.

## Что не входит в ответственность сервиса

- создание и lifecycle ViewModel;
- DI внутри prefab;
- бизнес-навигация и выбор следующего экрана;
- preload, кеширование и pooling;
- управление сценами;
- исправление layout prefab в runtime.

Добавлять эти обязанности в `UIService` без отдельной подтверждённой потребности нельзя.

## Проверка изменений

При изменении сервиса или контракта:

1. Собрать `DungeonTeam.UIService` и затронутые зависимые asmdef.
2. Запустить EditMode-тесты из `Assets/Code/UIService/Tests/EditMode`.
3. В Unity проверить реальный Addressable prefab:
   - нет видимого кадра до `ShowAsync`;
   - экземпляр сразу создаётся под правильным parent;
   - `FullScreen`/`Background` возвращаются по LIFO;
   - `Popup` показываются по FIFO;
   - overlay-элементы не скрывают друг друга;
   - `Close` и `Dispose` освобождают экземпляр и asset ровно один раз.

Для изменений загрузки дополнительно следовать `Docs/AI/libraries/addressables.md`, а для async lifecycle — `Docs/AI/lifecycle.md`.
