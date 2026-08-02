# Root-pattern и Disposable

## Root-pattern

`Root` — явный владелец object graph. Зависимости и Unity-ссылки конкретного root передаются ему через конструктор или явную фабрику; generic context и `IRootContext` в текущем API пакета отсутствуют.

```csharp
using RootTask = Cysharp.Threading.Tasks.UniTask;

public sealed class CombatRoot : Root
{
    protected override RootTask OnInitializeAsync(CancellationToken token)
    {
        return RootTask.CompletedTask;
    }

    protected override void OnDispose() { }
}
```

`InitializeAsync(CancellationToken)` и `OnInitializeAsync(CancellationToken)` возвращают `RootTask` — package-local alias для `Cysharp.Threading.Tasks.UniTask`. Инициализация разрешена ровно один раз и переводит root из `Created` через `Initializing` в `Initialized`. Переданный token относится к операции инициализации и передаётся в `OnInitializeAsync`; он не объединяется автоматически с `Root.CancellationToken`.

`Root.CancellationToken` отменяется при начале `Dispose` и при ошибке инициализации. При ошибке `OnInitializeAsync` пакет переводит root в `InitializationFailed`, отменяет root token и пробрасывает исключение, но не вызывает `OnDispose` автоматически: владелец обязан вызвать `Dispose` для очистки уже созданного graph.

`Dispose` отменяет root token до `OnDispose`, завершает state как `Disposed` даже при ошибке cleanup и идемпотентен после завершения. Для Unity нужен проектный `MonoBehaviour`-адаптер: он создаёт root, запускает и наблюдает `InitializeAsync`, а в `OnDestroy` вызывает `Dispose`. В пакете типа `RootBehaviour` больше нет; business logic в адаптере запрещена.

Не использовать Root как DI container, глобальный singleton или update loop.

## Disposable

`DisposableBase` — основа долгоживущего C#-объекта с ресурсами. `CompositeDisposable` хранит subscriptions и дочерние disposable-ресурсы.

`MonoBehaviourDisposable` — только для Unity View/adapter, которым нужно автоматически освободиться в `OnDestroy`.

```csharp
private readonly CompositeDisposable _disposables = new();
_disposables.AddDisposable(subscription);
```

Не добавлять объект в два владельца: это создаёт двойное освобождение и неясный lifecycle.
