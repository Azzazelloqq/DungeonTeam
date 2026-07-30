# Root-pattern и Disposable

## Root-pattern

`Root<TContext>` — явный владелец object graph. Context — `readonly struct`, содержащий только зависимости и Unity-ссылки, необходимые конкретному root.

```csharp
public sealed class CombatRoot : Root<CombatContext>
{
    protected override void OnInitialize() { }
    protected override void OnDispose() { }
}
```

`RootBehaviour` — Unity-адаптер. В нём разрешены сериализованные ссылки и вызов `InitializeRoot` из `Awake`/`Start`; business logic в нём запрещена.

Не использовать Root как DI container, глобальный singleton или update loop.

## Disposable

`DisposableBase` — основа долгоживущего C#-объекта с ресурсами. `CompositeDisposable` хранит subscriptions и дочерние disposable-ресурсы.

`MonoBehaviourDisposable` — только для Unity View/adapter, которым нужно автоматически освободиться в `OnDestroy`.

```csharp
private readonly CompositeDisposable _disposables = new();
_disposables.AddDisposable(subscription);
```

Не добавлять объект в два владельца: это создаёт двойное освобождение и неясный lifecycle.
