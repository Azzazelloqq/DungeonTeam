# Правила модуля feature

Новая feature создаётся в существующем project-owned дереве `Assets/Code`: gameplay — в
`Assets/Code/Gameplay/<FeatureName>`, UI — в `Assets/Code/UI/<FeatureName>`, application/dev
tooling — в соответствующем верхнеуровневом модуле под `Assets/Code`. Не создавать
параллельное дерево `Assets/Game`.

```text
Assets/Code/<Area>/<FeatureName>/
  Domain/           # сущности, value objects, правила; без Unity
  Application/      # use cases, интерфейсы портов
  Infrastructure/   # реализации портов feature
  Presentation/
    Gameplay/       # MVP: отдельная папка на каждую presentation-семью
    UI/             # MVVM: отдельная папка на каждую presentation-семью
  Composition/      # FeatureRoot, factory, assembly scope при необходимости
  <FeatureName>.Domain.asmdef
  <FeatureName>.Application.asmdef
  <FeatureName>.Presentation.asmdef
```

Не создавайте папку или assembly заранее: они появляются, когда в feature есть реальный код.

## Перед началом feature

1. Сформулировать ответственность feature и её публичные контракты.
2. Выбрать owner/lifecycle: application, scene или feature root.
3. Определить, какие ресурсы и сохранения feature владеет.
4. Добавить asmdef зависимости только в направлении слоёв.
5. Добавить тесты поведения: Domain/Application — EditMode, Unity-сценарии — PlayMode.

## Нейминг

- Интерфейс: `I<Responsibility>`.
- Use case: глагол + предмет (`StartDungeonRun`, `ApplyDamage`).
- Root: `<Feature>Root`.
- MVP: `<Name>Presenter`, `<Name>Model`, `<Name>View`.
- MVVM: `<Name>ViewModel`, `<Name>View`.
- Каждая MVP/MVVM-семья: `<Family>/Base/<Family>…Base` и конкретные типы рядом с `Base/`. Самостоятельная View и Base-класс вне конкретной семьи запрещены. Использовать только применимые Base-типы: MVP — View/Model/Presenter, MVVM — View/Model/ViewModel.
