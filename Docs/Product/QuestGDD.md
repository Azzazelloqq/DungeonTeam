# DungeonTeam — Quest GDD

**Status:** Q-0 implemented; Q-1 reward-claim design approved for implementation

## Q-0 purpose

Quests are a separate configurable progression track, shown on the Guild Hall Notice Board alongside contracts. Q-0 demonstrates three real objective patterns and their player-facing status; it does not turn contracts, NPCs, rewards or ranks into a generic framework.

## Player loop

The Board shows authored quests with their objective, state and, where applicable, progress. The player can accept any number of available Q-0 quests. An accepted quest updates only from its matching real game action and completes persistently once. Completed quests remain visible as completed and cannot be accepted again.

Q-0 also supports an authored linear chain: its ordered steps unlock one after another. A quest is either standalone or belongs to one chain; branching is deliberately absent.

Initial authored examples:

| Quest | Objective pattern | Observable state |
| --- | --- | --- |
| `quest.clear-crypt` | Step 1 of `chain.first-expedition`: successfully complete `dungeon.crypt` | `0/1` → step 2 unlocks |
| `quest.crystal-supply` | Step 2 of `chain.first-expedition`: receive three `resource.monster-crystal` through settled expedition results | `0/3`, `1/3`, … → completed |
| `quest.speak-debater` | Finish one dialogue with `npc.debater` in the Guild Hall | `0/1` → completed |

The Board must make the differences legible: objective wording, `available / accepted / completed`, and a current/required counter for the crystal quest. Text uses stable localization IDs with Russian fallback text in config.

## Rules

- Quest definitions are config with stable ID, title, summary and one Q-0 objective descriptor.
- Acceptance is persistent, independent for each quest, and cannot affect Contract state. Q-0 has no single-active-quest limit.
- No progress is granted before acceptance. A matching event after completion is ignored.
- Dungeon/resource progress applies only after the existing Player Profile settlement succeeded exactly once. Defeat, developer runs, back and duplicate terminal callbacks grant no progress.
- Dialogue progress applies when the player closes a dialogue that was opened for the matching NPC; opening a line alone is not completion.
- Quest completion grants no Gold, items, rank, reputation, contract availability or dialogue branching in Q-0.

## Q-1 — rewards and claim points

Q-1 adds one deliberate step: a completed quest may expose one configured reward that the player actively takes at its configured point. A reward is not intrinsically tied to the reception: it may be collected at the reception or from a specified NPC. This models the real player-facing distinction without a global reward, shop, or loot-container framework.

```text
complete configured quest
→ Board row stays completed and indicates where to collect
→ interact with configured point
  → Reception: Guild Profile → available rewards
  → NPC: finish normal one-line dialogue → available rewards from that NPC
→ choose "Receive"
→ Gold/resources are added once to Player Profile
→ quest becomes claimed and disappears from that point's list
```

No reward is granted for completing a quest, opening a dialogue, or closing a panel. The player must explicitly press receive. A failed save grants nothing and leaves the reward available. Retry after an interrupted two-save sequence must not duplicate Gold or resources.

### Config contract

Every Q-1 reward-bearing quest has one optional bundle (non-negative Gold and zero or more unique resource definition/amount pairs), one claim point (`Reception`, or `Npc` with stable `npcId`) and localization-ready fallback texts for reward/action and Board hint. NPC targets are validated against the Guild Hall NPC catalog at Bootstrap startup. A rewardless Q-0 quest remains completed without a claim entry.

The Notice Board remains status-only. A completed reward-bearing row shows a localized collect-at hint. Reception exposes a compact rewards action/section in Guild Profile. At an NPC, the same list opens only after its ordinary dialogue is closed. Ambient NPC remains unaware of quests, money, inventory, or reward UI.

### Persistence and crash rule

Quest state stays in `guild.quests`; Profile currency/inventory stays in `player.profile`. A stable quest `claimId` is banked idempotently by Player Profile before the quest is marked claimed. If the application stops between those writes, retry observes the Profile claim as already applied and finalizes only the quest marker. The player never receives the same reward twice.

## Q-1 explicit non-goals

This is not a universal claim-point subsystem. Q-1 excludes rewards from contracts, dungeon chests, mail, shops, achievements or arbitrary world objects; other claim-point kinds; automatic delivery; unique equipment reward instances; reward choices/history/refunds; repeatables; and localization implementation itself.

## Q-0 explicit non-goals

No kill/enemy-counter template, quest rewards/claim screen, abandonment, repeatables, timers, branching/cyclic chains, daily rotation, quest markers, generic event bus, NPC quest dialogue, profile DTO fields or rank requirements are added. A new objective type is added only with a real authored trigger and its own behavior proof.
