# Feedback, звук, музыка и вибрации

## Граница сервиса

`FeedbackService` оркестрирует список `IFeedbackPlayer` и не знает конкретные каналы. Один `FeedbackCue` содержит сериализуемые `FeedbackPayload`; каждый player обрабатывает только знакомые ему payload-типы. Добавление VFX или другого канала требует нового payload и player, но не изменения сервиса.

Идентичность cue задаётся типизированным полем feature-bank, а не общим `enum` или строковым ID. Feature определяет наследника `FeedbackBank` с именованными полями (`Hit`, `Death`, `ButtonClick`) и возвращает их из `Cues` для подготовки и освобождения.

## Lifecycle и Addressables

- Банк загружается через `FeedbackBankLoader.LoadAsync<TBank>` только по сгенерированному `AddressableIds`.
- `LoadAsync` загружает Addressable, валидирует банк и вызывает `PrepareAsync` всех players. Для аудио это также дожидается `AudioClip.LoadAudioData`, поэтому первый `Play` не должен инициировать загрузку.
- Владеющий root хранит `FeedbackBankLease<TBank>` весь период использования банка и освобождает его при dispose.
- Lease сначала останавливает и освобождает cues, затем вызывает `IResourceLoader.ReleaseResource`. Играть cue после dispose lease запрещено.
- Не вызывать `WaitForCompletion` и не загружать банк в момент игрового события; подготовка выполняется на loading boundary.

## Ограничение перегрузки

`AudioFeedbackPlayer` использует фиксированный пул `AudioSource`. Общий лимит задаёт `FeedbackRuntimeSettings.SfxVoiceLimit`; payload дополнительно задаёт лимит одновременных голосов, retrigger interval и priority. При заполнении пула новый звук заменяет только более низкоприоритетный голос, иначе отклоняется. `Metrics` показывает активные голоса и причины отклонений.

Haptics используют такой же принцип: фиксированный лимит импульсов, per-payload limit, cooldown и priority. Одновременные кривые смешиваются через максимум, а не суммируются, чтобы моторы не перегружались. При pause, смене устройства и dispose haptics сбрасываются.

## Музыка

Музыка не является one-shot feedback и управляется отдельным `IMusicPlayer`: prepare, play, stop, release. Не добавлять музыку в `FeedbackCue`, пока не появится подтверждённая потребность синхронизировать её с одноразовым feedback.

## Подключение feature

Feature получает `IFeedbackService` и свой типизированный bank/lease через composition root. Gameplay не загружает Addressables и не обращается к DI container самостоятельно. Для spatial feedback передаётся `FeedbackContext.At(position)`; для UI и глобальных событий — `FeedbackContext.Global()`.

