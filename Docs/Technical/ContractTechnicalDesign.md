# DungeonTeam — Contract CQ-0 Technical Design

**Status:** DESIGNED

## Boundary

Add `Contracts.Domain`, `Contracts.Application`, `Contracts.Infrastructure` and one EditMode test assembly. Domain is BCL-only; Application depends only on Domain; Infrastructure implements the SaveStore V2 repository/config adapter. No Contracts Runtime/root/DI scope: the existing Notice Board and Dungeon Run roots keep their lifecycles. Bootstrap owns one application-lifetime `ContractSession` and is the only bridge to Guild Hall, World Map and terminal Dungeon Run.

```text
Bootstrap -> Contracts.Infrastructure -> Contracts.Application -> Contracts.Domain
Bootstrap -> GuildHall snapshots / DungeonRun terminal result
GuildHall.Runtime -X-> ContractSession, SaveStore, config
DungeonRun -X-> Contracts
PlayerProfile -X-> Contracts state
```

`ContractState` holds optional `ActiveContractId` and defensive completed IDs. `Accept(contractId, catalog)` rejects unknown, unavailable, already-completed or a second active contract. `CompleteActive(contractId)` only succeeds for exactly the active ID and is idempotent for duplicate callbacks. `ContractSession` save-before-publishes every accepted mutation through `SaveKey<ContractSaveV1>("guild.contracts")`; first load creates empty state. No cross-key transaction with Profile is claimed or needed because CQ-0 does not grant profile rewards.

`ContractConfigPage` remains definition owner. Its catalog validates unique IDs and supported locations. `ContractSnapshotBuilder` in Bootstrap combines definition, profile-prepared rank availability and ContractState into immutable board snapshots: available, active, completed or disabled. Extend `NoticeBoardOfferSnapshot` only with prepared status/action text if the existing selection contract cannot distinguish acceptance; Board emits `AcceptContract(contractId)` and never evaluates state/config.

World Map resolution requires the persisted active contract, not merely `GuildSessionState.SelectedContractId`. Bootstrap maps its definition to the existing launch request and records the accepted contract ID with that run boundary. On successful terminal result, after existing profile settlement succeeds, Bootstrap calls `CompleteActive` before creating the Guild summary/Hall. A persistence failure leaves the contract active and does not claim completion; existing run recovery remains responsible for returning to Hall.

## Delivery plan

1. TDD Domain transitions, defensive state and duplicate/mismatch terminal behavior with variable contract fixtures.
2. Add directed assemblies, V1 save DTO/key/repository and filesystem load/save tests.
3. Add typed contract catalog validation and Bootstrap snapshot/acceptance bridge; preserve rank gating and author-disabled reasons.
4. Change map resolver to require active contract; wire terminal success completion after banked profile result.
5. Extend the existing Notice Board MVVM/View only as needed to expose active/completed/accept actions; no new screen/prefab family unless an existing binding is insufficient.
6. Author the two current contract definitions/config texts, then run focused EditMode, compile, asmdef/GUID mechanical validation. Manual Hall→accept→run→return/restart is explicitly unrun; no build/playtest.

## Acceptance and non-goals

- restart restores active/completed contract state;
- rejection performs zero saves; accepted state performs one save before UI snapshot changes;
- only the matching successful production run completes the active contract once;
- board/map/DungeonRun remain free of persistence/config/domain access outside Bootstrap;
- no quest reward, count/reputation/rank mutation or generic quest system is added.
