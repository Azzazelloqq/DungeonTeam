# Runtime services: TickHandler, Logger, Utils

## TickHandler

Создаётся один раз на application scope на основе `UnityDispatcherBehaviour`. Сервисы, которым нужен tick, получают `ITickHandler` через конструктор.

Каждая подписка на `FrameUpdate`, `FrameLateUpdate`, `PhysicUpdate` снимается владельцем в `Dispose`. Не использовать tick для одноразовых задержек — применяйте `UniTask`.

## Logger

Зависеть от `IInGameLogger`, а не от `UnityInGameLogger`. Текущая Unity-реализация пишет сообщения только в Editor/Development build. Не логировать персональные данные, токены и большие payload.

## Utils

`Utils` содержит UI и async extension methods. Это удобство, не архитектурный слой. Extension method не должен заменять явный lifecycle: `SubscribeClickAsync` также требует корректного cancellation token владельца.
