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

Граница Addressables ещё не реализована. Когда она появится, runtime-код будет обращаться к ней через собственные контракты; прямые вызовы `Addressables`, `ResourceLoader` и `SceneSwitcher` вне composition/infrastructure запрещены.
