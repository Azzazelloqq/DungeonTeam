# LightDI

## Выбор способа сборки

Регистрация и вызов сгенерированных фабрик происходят только в Bootstrap, composition root или изолированном module composition.

Используйте минимальный подходящий механизм:

1. Для стабильного application/module graph, зависимости которого зарегистрированы в LightDI, предпочитайте generated factory и `[Inject]` на параметрах конструктора.
2. Для короткого графа, runtime/per-instance аргументов или объектов без container services используйте прямой вызов конструктора.
3. Для повторно создаваемого scene/feature graph используйте явную root-фабрику. Не создавайте local container на каждый экземпляр feature.

Global container предназначен для application-lifetime сервисов:

```csharp
var container = DiContainerFactory.CreateGlobalContainer();
container.RegisterAsSingleton<IClock>(new UnityClock());
```

Generated constructor injection предпочтителен, когда класс входит в container-backed graph:

```csharp
public sealed class CombatPresenter
{
    public CombatPresenter([Inject] IClock clock, CombatView view)
    {
        // ...
    }
}

var presenter = CombatPresenterFactory.CreateCombatPresenter(view);
```

Фабрика получает отмеченный `IClock` из LightDI, а неотмеченный runtime-аргумент `view` оставляет параметром метода. Обычный конструктор сохраняется, поэтому тест не зависит от контейнера и generated factory:

```csharp
var presenter = new CombatPresenter(fakeClock, fakeView);
```

Если generated factory нигде не вызывается, `[Inject]` не участвует в создании объекта. Предпочитайте constructor-parameter injection. Field injection скрывает обязательные зависимости и выполняется через reflection, поэтому без отдельно обоснованного исключения не используется.

## Local container и assembly

`CreateLocalContainer()` привязывает контейнер к `Assembly.GetCallingAssembly()`. Generated factory ищет local container по assembly целевого класса (`typeof(Target).Assembly`). Поэтому module composition, вызывающий `CreateLocalContainer()`, и создаваемые через `[Inject]` классы должны находиться в одном asmdef.

```text
Feature.Runtime.asmdef
├─ FeatureComposition → CreateLocalContainer()
└─ CombatPresenter    → CombatPresenterFactory
```

Если composition находится в `Bootstrap.asmdef`, а целевой класс — в `Feature.Runtime.asmdef`, фабрика не увидит bootstrap-local container. После local lookup LightDI проверяет global containers, поэтому global fallback может замаскировать ошибочную границу модуля.

LightDI допускает один активный local container на assembly. Попытка создать второй завершится исключением. После `Dispose` контейнер удаляется из assembly registry, поэтому последовательное пересоздание технически возможно, но проект не использует этот статический slot как per-instance scope для SceneRoot/FeatureRoot.

Несколько global containers технически допустимы; `AllowMultipleGlobalContainers` только отключает предупреждение. В DungeonTeam используется один application container, если отдельное решение явно не задокументировано.

## Запрещено

- Ручной `DiContainerProvider.Resolve<T>()` как способ получить сервис внутри Domain, Application, Gameplay или UI. Внутренний вызов, сгенерированный LightDI factory, разрешён.
- Global container для временных feature-объектов.
- Local container на каждый экземпляр повторно создаваемой feature или scene.
- Вызов generated factory вне composition boundary.
- `[Inject]` в Domain: Domain не зависит от DI framework.

## Освобождение

По умолчанию контейнер владеет созданными им `IDisposable`: eager singleton отслеживается при регистрации, lazy singleton — при первом resolve, каждый disposable transient — при каждом resolve. Transient остаётся у контейнера до завершения всего container scope.

Владелец контейнера обязан вызвать `Dispose` ровно при завершении scope и не должен одновременно регистрировать тот же объект у другого владельца. LightDI освобождает отслеживаемые объекты в порядке их создания, а не в обратном порядке зависимостей. Если cleanup требует строгого порядка или более короткого lifetime, объектом явно владеет root; container ownership для этого graph не используется. `disposeRegistered: false` допустим только когда composition root явно владеет и освобождает все зарегистрированные disposable-объекты.
