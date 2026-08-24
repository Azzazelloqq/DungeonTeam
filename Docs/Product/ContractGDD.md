# DungeonTeam — Contract GDD

**Статус:** CQ-0 implemented; automated validation passed

## CQ-0 player loop

The Notice Board offers authored contracts. A player may accept exactly one available contract, travel to its existing destination, and complete it only when the matching Dungeon Run ends successfully. Completion is persistent and clears the active contract. The board then shows the completed state.

Contracts are distinct from Player Profile: they own their own state and save key. Profile/rank receives no completion counter in CQ-0. A future rank rule can consume a narrow read-only completion fact only after that rule is agreed.

## Rules

- Contract definitions are config: stable `contractId`, title/summary, supported `locationId`, optional `minimumRankId`; current authored availability still applies.
- One active contract at a time. Accepting another is rejected; completed contracts cannot be accepted again in CQ-0.
- Board acceptance, not visual selection, persists the active ID. A selected contract remains a presentation/session concern only until acceptance.
- A terminal Dungeon Run completes only the active contract whose destination matches the run's selected contract. Defeat, developer-console runs, return/back, mismatch and duplicate terminal callbacks change nothing.
- CQ-0 adds no contract reward, countdown, rotation, abandonment, chain, daily generation, NPC quest dialogue, objectives inside combat or generic quest framework. Existing Dungeon rewards remain unchanged.

## Initial content

`contract.demo` is the initial F-rank contract. `contract.veteran` remains rank E-gated. Both target the current supported dungeon, but are distinct authored contracts.
