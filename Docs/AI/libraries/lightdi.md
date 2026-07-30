# LightDI

## Использование

Регистрация и сборка объектов происходят только в Bootstrap, Composition root или изолированном module composition.

```csharp
var container = DiContainerFactory.CreateGlobalContainer();
container.RegisterAsSingleton<IClock>(new UnityClock());
```

Для короткого графа используйте явные конструкторы:

```csharp
var model = new CombatModel(clock);
var presenter = new CombatPresenter(view, model);
```

## Не использовать

- `DiContainerProvider.Resolve<T>()` как обычный способ получить сервис.
- Global container для временных feature-объектов.
- Local container для нескольких одновременно/последовательно пересоздаваемых экземпляров одного assembly: LightDI хранит один local container на assembly.

## Освобождение

Контейнер владеет `IDisposable`-сервисами, зарегистрированными в нём. Его владелец обязан вызвать `Dispose` ровно при завершении scope.
