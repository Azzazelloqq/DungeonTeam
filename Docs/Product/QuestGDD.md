# DungeonTeam — Quest GDD

**Status:** Q-0 implemented; automated validation passed

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

## Explicit non-goals

No kill/enemy-counter template, quest rewards/claim screen, abandonment, repeatables, timers, branching/cyclic chains, daily rotation, quest markers, generic event bus, NPC quest dialogue, profile DTO fields or rank requirements are added. A new objective type is added only with a real authored trigger and its own behavior proof.
