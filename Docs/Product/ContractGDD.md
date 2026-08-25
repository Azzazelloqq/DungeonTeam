# DungeonTeam — Contract GDD

**Статус:** CQ-0/CQ-1 implemented; focused automated validation passed; manual player-flow smoke not run

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

## CQ-1 — contract rewards

CQ-1 gives a completed contract one optional configured reward that the player explicitly collects from its configured point. The contract's terminal completion remains separate from collection: the Board shows it completed and, if relevant, tells the player where to take the reward.

```text
accept contract → complete matching production Dungeon Run
→ existing run rewards are settled
→ contract becomes completed
→ player interacts with its configured claim point
→ chooses Receive
→ contract reward is banked once into Player Profile
```

Claim points remain deliberately limited to `Reception` and a concrete Guild Hall `npcId`. The initial content demonstrates Reception on `contract.demo` and NPC collection on the E-gated `contract.veteran`. A reward contains only Gold and stackable resources; a contract without reward is valid.

The existing reward collection UI is renamed from a quest-specific family to a neutral Guild Hall collection because it now has two actual sources: quests and contracts. Its entry carries a typed source identity, so the View neither parses IDs nor knows persistence/config. Contracts and Quests keep their own state, config and save keys.

Profile banking keeps the same stable idempotency rule: Profile records `contract.reward:<contractId>` before Contract state marks the reward claimed. A crash between these saves is recoverable by retry without a duplicate payout. Collection is never automatic on completion, dialogue close, Board open or Reception open.

CQ-1 does not add rewards from chests, shops, mail, achievements or arbitrary objects; unique equipment rewards; choices/history/refunds; repeatable contracts; rank/reputation completion counters; or a general-purpose reward framework.
