# DungeonTeam — Quest Q-0 Technical Design

**Status:** IMPLEMENTED AND AUTOMATION-VALIDATED (Q-0); manual flow smoke/build not run

## 1. Boundary and ownership

Create `Quests.Domain`, `Quests.Application`, `Quests.Infrastructure` and one EditMode test assembly. Domain remains BCL-only. Application depends only on Domain. Infrastructure owns the V2 SaveStore repository. Bootstrap owns application-lifetime `QuestSession` and is the only bridge to Guild Hall dialogue, the settled terminal result and Board snapshots.

```text
Bootstrap -> Quests.Infrastructure -> Quests.Application -> Quests.Domain
Bootstrap -> GuildHall board snapshots / dialogue-completed callback
Bootstrap -> settled DungeonRun terminal result -> QuestSession
GuildHall.Runtime -X-> QuestSession, SaveStore, quest config
DungeonRun -X-> Quests
PlayerProfile -X-> Quest state
```

No Quest root, DI scope, event bus, global static dispatcher or generic objective pipeline is introduced. `ApplicationRoot` owns the session and persistence disposal with its existing application lifetime.

## 2. Config and domain contracts

`QuestConfigPage` owns ordered authored definitions. Each definition has `questId`, title/summary text snapshots and exactly one descriptor:

- `CompleteDungeonObjective(dungeonId)`;
- `CollectResourceObjective(resourceDefinitionId, requiredAmount)`;
- `CompleteDialogueObjective(npcId)`.

`QuestChainDefinition` is a second config record with `chainId`, localization-ready title and an ordered non-empty `questIds` array. A `QuestDefinition` either has no `chainId` or belongs to exactly one chain. Its `stepIndex` is inferred from that chain array; the first step is available, and completion unlocks the immediately next step. A chain has no branches, joins or cycles in Q-0.

The catalog validates unique quest IDs, positive required amounts and non-empty target IDs. Bootstrap performs startup cross-content validation: dungeon IDs against launch presets/current terminal content, resource IDs against `ItemCatalog`, NPC IDs against `GuildHallCatalog`.

`QuestState` holds defensive snapshots of accepted quest progress and completed quest IDs. `Accept(questId, catalog)` rejects unknown, duplicate-active, completed and locked chain steps. `ApplyDungeonCompleted`, `ApplySettledResources` and `ApplyDialogueCompleted` inspect only accepted Q-0 definitions, increment matching state, and return a mutation result. State completion is idempotent and removes the active progress record only after adding the completed ID.

There is intentionally no public generic `ApplyEvent` payload. Three typed methods match the three concrete sources, keeping trigger ownership explicit.

## 3. Persistence

`SaveStoreQuestRepository` persists `QuestSaveV1` under `SaveKey<QuestSaveV1>("guild.quests")`. DTO contains an ordered list of `{ questId, progress }` accepted records and completed IDs. The repository uses tagged V2 SaveStore data, version 1, save-before-publish semantics and the existing fresh-store verification pattern. Missing data loads an empty state. Invalid persisted IDs/progress fail visibly; they are never silently reset.

The Quest key is independent from `player.profile` and `guild.contracts`; Q-0 claims no cross-key transaction. Bootstrap calls quest progress only after profile settlement returned `IsApplied`, so duplicate terminal results do not advance quest state.

## 4. Application flow

1. Bootstrap creates `QuestPersistence` and `QuestSession` before Guild Hall startup, loads `QuestConfigPage`, validates its targets and builds immutable Board snapshots.
2. Board accept command reaches Bootstrap through a synchronous `Func<string, bool>` callback. Bootstrap validates the prepared available snapshot, then `QuestSession.Accept` persists before UI reflects accepted state.
3. `GuildHallRoot.CloseDialogue` invokes a narrow `Action<string> dialogueCompleted` callback once, after closing a dialogue with its active NPC. Bootstrap forwards the NPC ID to `QuestSession`.
4. `ApplicationRoot.ReturnFromFinishedDungeonRunAsync` first banks the profile result. Only after `IsApplied`, it forwards the completed dungeon and receipt resource grants to `QuestSession`; then it handles contract completion and rebuilds Hall as today.
5. Any persistence exception leaves QuestSession and outgoing Board snapshot unchanged. Terminal recovery still belongs to existing Bootstrap flow.

## 5. Notice Board presentation

Do not force Quest state into Contract types. Extend the existing Board input with a second immutable ordered `QuestBoardEntrySnapshot` collection and a `Func<string, bool> questAccepted` callback. The Board ViewModel owns a second row family but reuses the existing serialized row visual only when its fields are sufficient; otherwise it gets a dedicated inactive Quest row template in the current Guild Hall prefab.

Each quest row exposes title, summary, objective/progress text, state text and an accept command. It publishes no persistence/config operation. Contract selection and Quest acceptance remain distinct semantic outputs; a failed acceptance leaves the row unchanged. The Board may show zero quests and preserves config order. The current contract section keeps its one-active rule unchanged.

## 6. Test design

EditMode:

- catalog/chain validation, defensive state and variable ordered definitions;
- only the first incomplete chain step can be accepted; completion unlocks only its immediate successor;
- accepted-only progression for each of the three objective types;
- no progress from wrong dungeon/resource/NPC, defeat, duplicate terminal result or pre-accept action;
- resource accumulation across multiple settled receipts and one-time completion;
- save-before-publish, reload and malformed V1 behavior;
- Bootstrap snapshots preserve separate contract/quest collections, show progress/status, and reject unavailable/duplicate accepts;
- dialogue callback fires once after close, not on open.

PlayMode/manual:

- actual Guild Hall prefab Board bindings for quest rows, modal/input lifecycle and repeated open/close;
- manual smoke: accept all three → talk to debater → complete runs with crystals → restart → observe completed states.

## 7. Delivery plan

1. Add router/product/design documents and Q-0 config content contract.
2. TDD Domain state and three typed progress paths with variable fixtures.
3. Add directed assemblies, V1 repository and persistence tests.
4. Compose Bootstrap session, config validation and immutable quest Board snapshots.
5. Extend Board and dialogue output contracts; author only required prefab bindings/texts.
6. Connect post-settlement terminal and post-close dialogue boundaries.
7. Run focused EditMode, compile all affected assemblies, PlayMode/prefab proof when available, mechanical validation and scoped diff check.

## 8. Done criteria

Q-0 is done when the three authored examples visibly demonstrate binary dungeon completion, persisted accumulating resources and completed NPC dialogue; their progress survives restart; they stay separate from Contracts/Profile state; and no additional objective/reward/quest framework is introduced.

## 9. Implementation status and validation

Implemented `Quests.Domain`, `.Application`, `.Infrastructure`, `.Runtime` and focused EditMode tests. `guild.quests` is a separate V1 tagged SaveStore record. The production config contains the two-step `chain.first-expedition` (`quest.clear-crypt` → `quest.crystal-supply`) and standalone `quest.speak-debater`.

Bootstrap owns Quest persistence/session, only forwards terminal data after a successful profile settlement, and receives completed dialogue NPC IDs from Guild Hall. Notice Board has separate quest snapshots and rows, so contract state and quest state are not conflated.

Independently validated in Unity `6000.7.0a3`:

- Quest EditMode: `3/3 passed`;
- Bootstrap + GuildHall EditMode regression: `52/52 passed`;
- `dotnet build Bootstrap.csproj --no-restore -v:minimal`: `0` warnings, `0` errors.

Manual Hall → accept chain → run → crystal progress → dialogue → restart smoke, player build and external playtest were not run.
