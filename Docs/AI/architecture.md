# Архитектура

## Слои

```text
Bootstrap → Composition → Feature Root → Application → Domain
                           ├──────────→ Gameplay Presentation (MVP)
                           └──────────→ UI Presentation (MVVM)
Infrastructure implements contracts declared above it.
```

Зависимости направлены только вниз. Domain не ссылается на Unity, Addressables, DI, сохранения, UI и конкретные feature.

## Roots и scopes

| Scope | Владелец | Время жизни | Примеры |
| --- | --- | --- | --- |
| Application | `GameBootstrapper` → `ApplicationRoot` | запуск → закрытие приложения | логгер, конфиг, save store, навигация |
| Scene | `ApplicationRoot`/game flow | загрузка → выгрузка сцены | scene context, scene UI, camera bindings |
| Feature | Scene root или game flow | вход → выход из сценария | dungeon run, combat encounter, menu |

Root создаёт все принадлежащие ему объекты и уничтожает их в обратном порядке. Дочерний root не создаётся библиотекой автоматически: владелец обязан явно вызвать `Dispose`.

Для управления lifecycle проект активно использует паттерн Disposable: root, presenter, viewmodel и другие владельцы ресурсов явно освобождают принадлежащие им объекты, подписки и операции через `Dispose`. Правила владения, порядок освобождения и применение `CompositeDisposable` описаны в [lifecycle.md](lifecycle.md) и [roots-and-disposal.md](libraries/roots-and-disposal.md).

## Дерево композиции и владения

Слои отвечают на вопрос «от чего может зависеть код». Дерево композиции отвечает на вопрос «кто создаёт, использует и освобождает объект». Это разные оси и их нельзя смешивать.

```text
ApplicationRoot
└─ SceneRoot
   └─ DungeonRoot
      ├─ CombatRoot
      │  └─ CombatPresenter
      │     └─ EnemyPresenter(s)
      └─ HudRoot
         └─ InventoryViewModel
            └─ InventoryItemViewModel(s)
               └─ ItemDetailsViewModel
```

### Границы глубины

- `Root` создаётся только для application, scene, feature или самостоятельного режима с отдельным временем жизни и ресурсами.
- Parent presenter/viewmodel создаёт и владеет дочерней presentation-семьёй, если у ребёнка есть собственная ответственность, состояние, async-операция, подписки или переиспользуемый presentation-contract. Семья включает свою View, Model и Presenter/ViewModel по применимому паттерну.
- Unity View остаётся узлом отображения и хостом ссылок: самостоятельная View вне MVP/MVVM-семьи запрещена. Она не создаёт presentation-логику, Presenter/ViewModel, root или DI scope.
- Не создавать root, container, service или интерфейс для каждого list item, кнопки или визуального дочернего объекта.

### Связи в дереве

- Родитель передаёт ребёнку только необходимые зависимости через конструктор или контекст создания.
- Ребёнок не получает родителя, sibling или глобальный сервис через `Resolve`.
- Коммуникация вверх идёт через узкий callback, command или interface, определённый владельцем; коммуникация вниз — через явные методы/контекст.
- Parent владеет child до конца его lifecycle. Замена активной страницы/элемента означает явное освобождение старого child либо передачу его в явно принадлежащий parent cache.

Слой feature не повторяется для каждого дочернего узла: item presenter/viewmodel остаётся внутри Presentation той же feature, пока не появляется самостоятельная бизнес-ответственность и внешний контракт.

## DI

LightDI — инструмент composition, а не глобальный доступ к зависимостям.

- Глобальный контейнер допустим только для application-lifetime сервисов.
- Local container создаётся на границе изолированного assembly-модуля. Библиотека допускает только один local container на assembly, поэтому он не подходит для повторно создаваемых экземпляров одной feature.
- Для повторно создаваемых SceneRoot/FeatureRoot зависимости передаются напрямую конструктором или создаются фабрикой root-а.
- Контейнер оправдан, когда scope содержит несколько взаимосвязанных сервисов, скрытых от остального проекта. Для 1–3 объектов используется прямой конструктор.
- `Resolve` допустим только в документированном инфраструктурном исключении. Новое исключение требует ADR.

## Presentation

Gameplay использует MVP. Presenter владеет Model и View, координирует сценарий, но не содержит Unity lifecycle callbacks.

UI использует MVVM. ViewModel не зависит от View; View подписывается на reactive properties и команды. Один ViewModel не должен одновременно обслуживать несколько независимых экранов без явной причины.

## Addressables

Инфраструктурная база Addressables реализована: `AddressableIds` генерирует ключи из Addressables Settings, а `SceneSwitcher` 1.0.4 предоставляет async scene loading. Наличие пакета не означает автоматическое подключение навигации к application flow.

Runtime-код обращается к загрузке через узкий проектный контракт. Прямые вызовы `Addressables`, `ResourceLoader` и `AddressablesSceneSwitcher` вне composition/infrastructure запрещены; raw keys и handles не выходят в Domain, gameplay или UI. Владеющий application/scene root создаёт navigator, передаёт свой cancellation token и освобождает navigator до завершения собственного lifecycle.
