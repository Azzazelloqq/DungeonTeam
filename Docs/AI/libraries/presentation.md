# MVP и MVVM

## Семья presentation-узла

Каждый самостоятельный MVP- или MVVM-узел оформляется отдельной папкой семьи. Самостоятельной `View` вне семьи быть не может: она всегда относится ровно к одной семье и связывается только с уже созданным Presenter/ViewModel этой семьи. Внутри семьи всегда есть `Base`: в ней находятся абстракции конкретной семьи, а рядом — их единственные конкретные реализации.

```text
Gameplay/
  Combat/
    Base/
      CombatViewBase.cs
      CombatModelBase.cs
      CombatPresenterBase.cs
    CombatView.cs
    CombatModel.cs
    CombatPresenter.cs

UI/
  Inventory/
    Base/
      InventoryViewBase.cs
      InventoryModelBase.cs
      InventoryViewModelBase.cs
    InventoryView.cs
    InventoryModel.cs
    InventoryViewModel.cs
```

`<Family>ViewBase`, `<Family>ModelBase`, `<Family>PresenterBase` и `<Family>ViewModelBase` — это абстрактные контрактные типы одной конкретной семьи. Они не являются самостоятельными общими типами и не используются вне своей семьи как отдельная абстракция. Создавать нужно только применимые к паттерну типы: MVP использует View/Model/Presenter, MVVM — View/Model/ViewModel. Не добавлять неиспользуемый `PresenterBase` в MVVM ради симметрии.

Base-класс наследуется от соответствующего базового типа подключённого пакета и объявляет только API, нужный его потребителям: UI-привязки, команды, события или узкие операции отображения. В нём не размещаются сценарная логика, Unity-ссылки, создание зависимостей или дублирующая реализация; допустимо только унаследованное lifecycle-поведение пакета. Конкретный класс наследуется от Base-класса и содержит реализацию.

Зависимости между узлами передаются через Base-классы, а не конкретные реализации. Например, конструктор `CombatPresenter` принимает `CombatViewBase` и `CombatModelBase`, а View получает `CombatPresenterBase`; в MVVM `InventoryView` привязывается к `InventoryViewModelBase`, тогда как ViewModel по-прежнему не зависит от View. Это позволяет заменять реализации и передавать тестовые реализации через тот же контракт.

Иерархия создания повторяет иерархию владения: root/factory создаёт семью верхнего уровня, а parent Presenter/ViewModel создаёт и освобождает каждую дочернюю семью целиком. Parent получает или создаёт child View, создаёт его Model и Presenter/ViewModel, выбирает единственный путь инициализации и остаётся единственным владельцем. Child View не создаёт Presenter/ViewModel; пассивные вложенные View без собственного состояния, команд, подписок и lifecycle остаются частью View родительской семьи, а не становятся отдельной семьёй.

## MVP — gameplay

Использовать пакет `MVP` для объектов мира и игровых сценариев.

- `Model` — состояние и правила конкретного presentation-сценария.
- `ViewMonoBehaviour<TPresenter>` — Unity bindings, анимации и input events.
- `Presenter<TView, TModel>` — связывает Model и View, владеет подписками.

В конкретном коде Presenter наследуется от `<Family>PresenterBase`; последний параметризуется `<Family>ViewBase` и `<Family>ModelBase`, а не конкретными View/Model. Конкретные View и Model наследуются от соответствующих Base-классов.

Для текущего API MVP это означает: `<Family>ModelBase` наследует `MVP.Model`, `<Family>PresenterBase` — `MVP.Presenter<<Family>ViewBase, <Family>ModelBase>`, а `<Family>ViewBase` — `MVP.ViewMonoBehaviour<<Family>PresenterBase>` (либо `MVP.View` для не-Unity View).

Presenter создаётся root-ом/фабрикой, затем вызывается `Initialize` или `InitializeAsync`. Не вызывать оба варианта для одного экземпляра.

Parent presenter может владеть child presenter-ами. Child получает только свою View, Model и минимальные зависимости; действие, которое должен обработать parent, передаётся через контракт parent-а, а не через поиск родителя или sibling. Replaceable child presenter освобождается явно в `OnDispose`: базовый `Presenter` освобождает собственные View/Model до `CompositeDisposable`, поэтому порядок нельзя оставлять неявным.

## MVVM — UI

Использовать пакет `MVVM.Core` и `MVVM.Reactive` для экранов, окон и HUD.

- ViewModel получает use cases в конструкторе.
- `ReactiveProperty<T>` меняется только владельцем ViewModel/модели; View читает и подписывается.
- Команды создаются во ViewModel; View лишь привязывает UI-события.
- Возвращённый `Subscription<T>` должен быть добавлен в disposable-владельца.

Конкретный ViewModel наследуется от `<Family>ViewModelBase` и работает с `<Family>ModelBase`. Конкретный View наследуется от `<Family>ViewBase` и получает `<Family>ViewModelBase` для привязок. ViewModel не получает View ни напрямую, ни через Base-класс.

Для текущего API MVVM: `<Family>ModelBase` наследует `MVVM.Core.ModelBase`, `<Family>ViewModelBase` — `MVVM.Core.ViewModelBase<<Family>ModelBase>`, а Unity `<Family>ViewBase` — `MVVM.Core.ViewMonoBehavior<<Family>ViewModelBase>`. `ViewModelBase<TModel>` требует Model и владеет её lifecycle; use case и сервисы передаются в ViewModel дополнительно. Написание `ViewMonoBehavior` в MVVM — с одной `u`.

Parent ViewModel владеет item/page ViewModel. Для списка parent отвечает за создание, обновление и освобождение children; item ViewModel не управляет коллекцией sibling-ов. Replaceable children хранятся в явной коллекции parent-а, а не навсегда добавляются в `CompositeDisposable`. Для вложенной страницы ViewModel существует, пока её owner хранит активной или в собственном cache. View привязывается к уже созданному ViewModel и не создаёт его.

Не применять MVVM для каждого gameplay-объекта в мире и не помещать игровую бизнес-логику в UI ViewModel.
