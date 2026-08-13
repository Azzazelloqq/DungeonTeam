# DOTween

DOTween используется только в Unity presentation-слое для конечных декоративных переходов: UI fade/scale, damage numbers, кратких реакций и transform-анимаций. Он не заменяет игровой simulation tick, state machine, Animator-контроллер или хронометраж domain/application-логики.

## Границы

- Domain и Application не ссылаются на `DG.Tweening`.
- В gameplay DOTween допустим только внутри View/MonoBehaviour для визуального feedback. Движение, AI, cooldown, projectile и другие непрерывно пересчитываемые состояния остаются в существующем tick-контуре.
- Animator остаётся источником истины для клиповых анимаций персонажей и объектов. DOTween не должен конкурировать с ним за один и тот же `Transform` или свойство.
- Для UI, которым управляет `UIService`, переход запускается только из реализации `IUIElement.ShowAsync`/`HideAsync`; вызывающий код использует методы `UIService` и ожидает их завершения.

## Время жизни

- Tween принадлежит View, который его создал. Для tween, завязанного на `GameObject`, применять `SetLink(gameObject, LinkBehaviour.KillOnDestroy)`.
- Повторный запуск одного эффекта сначала останавливает предыдущий tween/sequence этого эффекта. Не использовать `DOTween.KillAll`.
- Если async UI-переход отменён, остановить tween и восстановить визуальное состояние, соответствующее состоянию `UIService`.
- Для UI fade использовать `CanvasGroupTween.FadeAsync`: он связывает tween с cancellation token и завершается только после окончания перехода.

## Производительность и стиль

- Не создавать tween каждый кадр и не tween-ить свойства, которые одновременно обновляются в `Update`, tick или reactive binding.
- Один короткий визуальный эффект оформлять одним tween/sequence; не вводить обобщённые animation service, фабрики или глобальные менеджеры без подтверждённой потребности.
- Параметры длительности и дизайна, которые должны настраиваться художником/дизайнером, хранить на View как serializable поля.

## Установка в этом проекте

DOTween подключён как precompiled `DOTween.dll` в `Assets/Plugins/Demigiant/DOTween`. Он доступен runtime asmdef автоматически. UI extension-модуль поставки находится исходником вне asmdef, поэтому из feature asmdef не использовать `CanvasGroup.DOFade` и другие UI shortcuts без отдельной проверки компиляции. Для `CanvasGroup` использовать `CanvasGroupTween`.
