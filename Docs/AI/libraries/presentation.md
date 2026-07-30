# MVP и MVVM

## MVP — gameplay

Использовать пакет `MVP` для объектов мира и игровых сценариев.

- `Model` — состояние и правила конкретного presentation-сценария.
- `ViewMonoBehaviour<TPresenter>` — Unity bindings, анимации и input events.
- `Presenter<TView, TModel>` — связывает Model и View, владеет подписками.

Presenter создаётся root-ом/фабрикой, затем вызывается `Initialize` или `InitializeAsync`. Не вызывать оба варианта для одного экземпляра.

## MVVM — UI

Использовать пакет `MVVM.Core` и `MVVM.Reactive` для экранов, окон и HUD.

- ViewModel получает use cases в конструкторе.
- `ReactiveProperty<T>` меняется только владельцем ViewModel/модели; View читает и подписывается.
- Команды создаются во ViewModel; View лишь привязывает UI-события.
- Возвращённый `Subscription<T>` должен быть добавлен в disposable-владельца.

Не применять MVVM для каждого gameplay-объекта в мире и не помещать игровую бизнес-логику в UI ViewModel.
