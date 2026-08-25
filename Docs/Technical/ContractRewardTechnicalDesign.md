# DungeonTeam — Contract CQ-1 Reward Claim Technical Design

**Status:** approved for implementation

## 1. Scope and ownership

CQ-1 adds optional Gold/stackable-resource rewards to completed Contracts. `Contracts` owns its reward definition, whether the completed contract reward is unclaimed, and `guild.contracts` persistence. `PlayerProfile` continues to own Gold/resources and durable idempotent banking in `player.profile`. `GuildHall` renders immutable entries; `Bootstrap` coordinates the two saves.

```text
Bootstrap
 ├─ ContractSession (completion / contract reward claim marker)
 ├─ QuestSession (unchanged, second RewardCollection source)
 ├─ PlayerProfileSession (ClaimReward)
 └─ GuildHallRoot → RewardCollection MVVM

Contracts -X-> Quests, GuildHall, PlayerProfile, Unity, SaveStore
Quests -X-> Contracts
GuildHall.Runtime -X-> sessions, configs, repositories
PlayerProfile -X-> Contracts/Quests
```

There is no new root, DI scope, event bus, reward registry or shared persistence owner.

## 2. Minimal reuse boundary

Q-1 established one source; CQ-1 supplies the second concrete source that justifies making presentation names neutral:

- `QuestRewardCollection*` becomes `RewardCollection*`;
- its Guild Hall snapshots use `RewardClaimPointSnapshot`, `RewardClaimIdentity` (`Quest`/`Contract` plus stable source ID), `RewardCollectionEntrySnapshot` and `RewardClaimRequest`;
- the ViewModel only returns that typed identity/point to Bootstrap. It neither branches on string prefixes nor accesses config/state.

This is a presentation contract, not a universal rewards subsystem. Each Domain keeps its own private reward definition, claim-point value and state transition. Bootstrap explicitly maps the typed identity to `QuestRewardClaimCoordinator` or `ContractRewardClaimCoordinator`.

## 3. Contract data/state/save

`ContractDefinition` gains optional `ContractRewardDefinition`: non-negative Gold; defensive unique resource list with positive amounts; `Reception` or `Npc(npcId)` claim point; localization-ready claim hint. An empty serialized optional shell maps to no reward, matching Q-1 Unity serialization behavior.

`ContractState` gains ordered `claimedRewardContractIds`, with `IsRewardClaimed`, `GetClaimableAt` and `TryMarkRewardClaimed`. Only a completed reward-bearing contract can be marked. `ContractSaveV2` adds the list and migrates V1 with no claimed rewards. Legacy completed rewardless contracts remain valid.

Bootstrap startup validates contract reward resources against `ItemCatalog` and NPC targets against `GuildHallCatalog`. Current authored data: `contract.demo` has Reception Gold/resources; E-gated `contract.veteran` has a registrar NPC reward.

## 4. Completion and exact-once claim

Existing terminal ordering remains: only a matching successful production Dungeon Run first settles normal run results to Player Profile, then completes the active Contract. After that completion save succeeds, its optional reward may be collected.

On an explicit claim request Bootstrap:

1. validates source identity is Contract, the contract is completed/unclaimed and the point equals its config;
2. calls `PlayerProfileSession.ClaimReward` with `claimId = contract.reward:<contractId>`;
3. on `Applied` or `AlreadyApplied`, calls `ContractSession.MarkRewardClaimed`;
4. refreshes/removes the collection row only after that succeeds.

Profile save failure leaves Contract unclaimed. Contract save failure after Profile success is retried safely: Profile returns `AlreadyApplied`, then Contract can mark its reward without paying again. No distributed transaction, rollback or optimistic UI update is introduced.

## 5. Guild Hall flow

The Notice Board remains non-claiming; a completed reward-bearing contract row adds a localized collect-at hint and claimed state. Reception reaches the neutral collection from the existing Guild Profile action. An NPC collection opens after its ordinary Ambient NPC dialogue closes, alongside the existing quest behavior. The collection combines all unclaimed entries assigned to that point, preserving deterministic source/config order. A row's explicit Receive action routes through the typed identity; closing dialogue/panels never grants value.

The parent root owns the collection family exactly as Q-1: create/register/initialize; dispose replaced rows/family before close/root disposal. No new tick, async flow, Addressables ownership or AmbientNpc dependency.

## 6. Tests and delivery

EditMode behavior tests:

- Contract reward config validation; Contract V1→V2 migration; completion-only/point-matching/once-only state transitions with variable fixtures;
- profile-first coordinator, wrong source/point rejection, Profile failure and Profile-applied/Contract-save-failure retry;
- existing terminal flow makes a claim available only after matching successful settlement;
- neutral collection preserves source identity, explicit receive and failed-callback row retention; Quest regression proves both source kinds coexist;
- Board snapshots show hint/claimed state without changing contract selection/acceptance rules.

Author config and renamed/bound prefab components through Unity Editor only. Run affected Contracts, Quests, PlayerProfile, GuildHall and Bootstrap EditMode assemblies, compile all affected asmdefs, Editor config/prefab binding checks and mechanical validator. Manual Reception/NPC/restart smoke and player build stay separate evidence.

## 7. Non-goals

No other reward source, automatic award, reward choices/unique equipment/history/refund, repeatability, generic loot framework, contract chains, rank/reputation counts or Q-2 availability conditions.
