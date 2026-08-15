# Character-Owned Entity BASE Design

## 1. Purpose

Project MS needs a shared API for networked entities created and owned by a character skill. Examples include stationary nodes, mines, zones, turrets, clones, pets, and AI summons.

This design keeps character scripts focused on character-specific rules. Character authors must not directly manage Fusion spawning, despawning, authority, ownership, lifetime, health, or registry bookkeeping.

The first implementation delivers only the reusable BASE layer. It does not modify Spark, Spark's Q/E skills, or Spark prefabs.

## 2. Scope

### Included

- A shared network base for character-owned entities
- A deployable base for stationary or placed entities
- An extension point for future autonomous summons
- Ownership, team, grouping, and creation-order metadata
- Optional health and optional lifetime, usable independently or together
- A shared damage-target contract for characters and owned entities
- Self-, friendly-, and enemy-damage policies
- Per-character registration, lookup, count limits, and bulk commands
- A single destruction path with explicit destruction reasons
- `CharacterBase` APIs for character authors
- Tests, source-contract checks, and developer documentation

### Excluded

- Spark Q/E integration or behavior changes
- Changes to Spark prefabs
- Turret targeting, autonomous movement, pathfinding, or summon AI
- Health-bar UI and character-specific effects
- Integration with map `StructureBase`
- State-authority transfer in Shared Mode

## 3. Domain Boundaries

Map structures and character-owned entities have different ownership and lifecycle rules and must remain separate.

```text
Map/Structure
├── StaticStructure
├── BreakableStructure
└── PushableStructure

CharacterOwnedEntity
├── CharacterDeployable
│   ├── node
│   ├── mine
│   ├── area emitter
│   └── turret shell
└── CharacterSummon (future extension point)
    ├── pet
    ├── clone
    └── AI summon
```

`CharacterOwnedEntity` owns only the invariants shared by all character-created entities. Health, lifetime, movement, targeting, and attacks remain optional capabilities instead of accumulating in one large base class.

## 4. Architecture

### 4.1 CharacterOwnedEntity

`CharacterOwnedEntity : NetworkBehaviour` is the network lifecycle and ownership boundary. It stores or exposes:

- Owner character `NetworkId`
- Owner `PlayerRef`
- Owner team identifier
- `OwnedEntityGroupId`
- Monotonic creation sequence
- Active and destroying state
- Final `OwnedEntityDestroyReason`
- Owner-exit policy

It resolves the owning character when available but must continue to expose stable owner and team data if the character object has already despawned.

It owns the idempotent destruction gate. Every destruction request, regardless of source, passes through this gate exactly once.

### 4.2 CharacterDeployable

`CharacterDeployable : CharacterOwnedEntity` adds an installation lifecycle:

```text
Spawning -> Launching -> Deploying -> Active -> Destroying -> Despawned
```

Not every deployable uses every intermediate state. A mine placed directly on the ground may transition from `Spawning` to `Active`. Effects that depend on installation must check `IsActive`, not object existence.

### 4.3 CharacterSummon

`CharacterSummon : CharacterOwnedEntity` is a documented future extension point. The BASE work may provide the type boundary, but it must not implement AI, movement, targeting, or attacks yet.

Future capabilities should be added as focused components or interfaces:

- `IOwnedEntityMovement`
- `IOwnedEntityTargeting`
- `IOwnedEntityAttack`

They must not be added as mandatory behavior on `CharacterOwnedEntity`.

### 4.4 CharacterOwnedEntityRegistry

Each `CharacterBase` owns one registry module. The registry is responsible for:

- Registering and unregistering owned entities
- Rejecting duplicate registration
- Grouped, read-only lookup
- Preserving deterministic creation order
- Enforcing count and overflow policies
- Removing stale or invalid network references
- Applying owner death, despawn, disconnect, or character replacement policy
- Relaying one final destruction notification to the owner

The registry is not a scene-global singleton. Ownership rules and limits belong to the character that created the entities.

## 5. Public API for Character Authors

Character scripts use protected `CharacterBase` APIs only. They do not call `Runner.Spawn`, `Runner.Despawn`, RPCs, or owned-entity initialization directly.

```csharp
protected OwnedEntitySpawnResult<T> SpawnOwnedEntity<T>(
    T prefab,
    in OwnedEntitySpawnRequest request)
    where T : CharacterOwnedEntity;

protected bool DestroyOwnedEntity(
    CharacterOwnedEntity entity,
    OwnedEntityDestroyReason reason);

protected IReadOnlyList<T> GetOwnedEntities<T>(
    OwnedEntityGroupId group)
    where T : CharacterOwnedEntity;

protected int DestroyOwnedEntities(
    OwnedEntityGroupId group,
    OwnedEntityDestroyReason reason);
```

`OwnedEntitySpawnResult<T>` contains:

- `Success`
- `Entity`
- `FailureReason`

A failed spawn must never leave an unregistered network object in the world.

`OwnedEntitySpawnRequest` contains:

- Position and rotation
- Optional initial velocity
- Group identifier
- Maximum active count
- Overflow policy
- Owner-exit policy

Groups use a value type rather than free-form object-name matching. A group identifies entities controlled together by one skill or mechanic.

## 6. Count and Owner-Exit Policies

### 6.1 Overflow

`OwnedEntityOverflowPolicy` supports:

- `RejectNew`: fail without spawning
- `DestroyOldest`: destroy the lowest creation sequence, then spawn
- `DestroyNewest`: destroy the highest creation sequence, then spawn
- `Unlimited`: do not enforce a count

`RejectNew` is the safe default. Count validation happens before spawning when possible. Replacement uses `LimitExceeded` as the destruction reason.

### 6.2 Owner Exit

`OwnedEntityOwnerExitPolicy` supports:

- `Destroy`: immediately destroy the entity
- `ExpireNormally`: stop accepting owner commands but retain its normal lifetime
- `TransferStateAuthority`: reserved for a future authority model

Shared Mode initially implements `Destroy` and `ExpireNormally`. Selecting `TransferStateAuthority` returns an explicit unsupported-policy failure; it must not silently fall back.

Owner death and owner network departure are separate events. The entity definition states which owner event applies its exit policy.

## 7. Optional Durability and Lifetime

Health and lifetime are independent optional capabilities. A deployable may have neither, either, or both.

`OwnedEntityLifetimeMode` supports:

- `Manual`: removed only by an explicit command or owner policy
- `Health`: destroyed when HP reaches zero
- `Duration`: destroyed when its timer expires
- `HealthOrDuration`: destroyed by the first completed condition

Configuration includes:

- Maximum health
- Duration
- Allow self damage
- Allow friendly damage

Defaults are:

- Self damage blocked
- Friendly damage blocked
- Enemy damage allowed
- Zero, negative, NaN, and infinite damage rejected
- Damage rejected after destruction begins

When health depletion and lifetime expiration are observed in the same simulation tick, `HealthDepleted` has deterministic priority.

Timers use Fusion simulation time and are evaluated only by State Authority. Client wall-clock time is not authoritative.

## 8. Shared Damage Contract

Characters and damageable owned entities implement a common target contract without sharing their health implementation.

```csharp
public interface IDamageable
{
    bool CanReceiveDamage(in DamageRequest request);
    DamageResult RequestDamage(in DamageRequest request);
}
```

`DamageRequest` contains:

- Requested damage
- Attacker `PlayerRef`
- Attacker/source `NetworkId`, when available
- Attacker team
- Damage source category: direct, projectile, area, periodic, or environment
- Optional skill identifier
- Optional hit position and direction

`DamageResult` contains:

- Whether damage was accepted
- Requested and applied damage
- Remaining health when applicable
- Whether the hit caused destruction
- A rejection reason when not accepted

The State Authority is the only writer of owned-entity HP and destruction state. Non-authoritative detection routes a request to the authoritative path. The request must be validated again by State Authority.

Damage processing order is:

1. Resolve `IDamageable` from the hit collider's parent.
2. Reject duplicate hits according to the attack's existing hit policy.
3. Validate entity state and numeric input.
4. Evaluate self, friendly, and enemy damage policy.
5. Apply `ModifyIncomingDamage`.
6. Change HP on State Authority.
7. Emit damage and health-change hooks.
8. If HP reached zero, enter the common destruction gate with `HealthDepleted`.

Supported extension hooks are:

```csharp
protected virtual float ModifyIncomingDamage(in DamageRequest request, float damage);
protected virtual void OnDamageReceived(in DamageRequest request, in DamageResult result);
protected virtual void OnHealthChanged(float previous, float current);
protected virtual void OnOwnedEntityDestroyed(OwnedEntityDestroyReason reason);
```

Hooks may customize behavior but may not directly despawn the network object.

The existing character damage callbacks must preserve their current behavior. Migrating collision lookup from `CharacterBase` to `IDamageable` must not cause character hits to be applied or reported twice.

## 9. Destruction Contract

All removal flows use `DestroyOwnedEntity`. `OwnedEntityDestroyReason` includes:

- `HealthDepleted`
- `LifetimeExpired`
- `LimitExceeded`
- `OwnerDied`
- `OwnerDespawned`
- `OwnerDisconnected`
- `SkillTriggered`
- `Manual`

The destruction gate performs this order:

1. Atomically mark the entity as destroying.
2. Record the final reason.
3. Disable new damage and commands.
4. Run the entity destruction hook once.
5. Notify and unregister from the owner's registry once.
6. Despawn the network object once.

Repeated destruction calls return false and have no side effects. Character-specific effects should react to the recorded reason rather than create alternate despawn paths.

## 10. Error Handling

Spawn failures use explicit result values for expected errors:

- Missing or invalid prefab
- Missing `NetworkObject`
- Invalid group
- Invalid count
- Authority unavailable
- Count limit rejected
- Unsupported policy
- Initialization or registration failure

Programmer configuration errors also produce a contextual Unity error referencing the character and prefab. Normal policy rejection, such as `RejectNew`, is not logged as an engine error.

Lookup of an unknown group returns an empty read-only list. Bulk destruction returns the number of accepted destruction requests.

## 11. Testing Strategy

### 11.1 Pure module tests

- Health-only, duration-only, combined, and manual lifetime modes
- Health-versus-duration deterministic priority
- Invalid numeric damage rejection
- Self, friendly, and enemy damage policy combinations
- Damage modification and result reporting
- Idempotent destruction
- Registry grouping and read-only results
- Deterministic oldest/newest ordering
- Every overflow policy
- Owner-exit policies and unsupported authority transfer

### 11.2 Source-contract tests

- Character examples cannot call `Runner.Spawn` or `Runner.Despawn` for owned entities
- Character examples cannot initialize owned entities directly
- BASE spawn routes through Fusion and registers the entity
- Every BASE removal route uses the destruction gate
- Direct and projectile attacks resolve `IDamageable`
- Existing character damage callbacks remain exactly-once

### 11.3 Unity/Fusion integration checklist

- Host, client, late joiner, and resimulation see matching owner, team, HP, lifetime, active state, and destruction reason
- Simultaneous hits cannot destroy an entity twice
- Lifetime expiration is consistent across clients
- Owner death and disconnect apply the configured policy once
- Count replacement selects the same entity on every peer
- Character targets continue to receive projectile and direct damage once
- Owned entities correctly receive projectile, direct, area, and periodic damage

## 12. Developer Guide Deliverables

When implementation is complete, update the character development guide with:

1. The distinction between map structures, deployables, and summons
2. The supported owned-entity lifecycle and destruction reasons
3. A minimal deployable prefab checklist
4. Examples of health-only, duration-only, and combined configuration
5. `CharacterBase` API examples without Fusion calls
6. Damage policy and `IDamageable` behavior
7. Count-limit and owner-exit policy examples
8. Debugging and multiplayer verification checklist
9. Explicitly prohibited patterns, including direct spawn/despawn and object-name lookup

The guide must also include a **Future Extensions** section describing how to extend the completed BASE safely:

- **Mine:** add trigger behavior to `CharacterDeployable`; reuse ownership, health, lifetime, and destruction
- **Area emitter:** use duration without health; expose effect activation independently of durability
- **Turret:** compose targeting and attack capabilities; do not move those concerns into the base
- **AI summon:** derive from `CharacterSummon` and compose movement, targeting, and attack components
- **Shields and healing targets:** add explicit capability contracts rather than overloading negative damage
- **Server/Host authority mode:** replace the authority transport behind the BASE API without changing character-author APIs
- **Authority transfer:** implement and test `TransferStateAuthority` before enabling the reserved policy
- **Persistence across owner death:** define team ownership and command rights before adding new policies

Each future-extension example must identify which existing contract is reused, which new focused capability is introduced, and which BASE classes must remain unchanged.

## 13. Implementation Order

1. Introduce shared damage request, result, and target contracts while preserving character behavior.
2. Add owned-entity identifiers, policies, results, and destruction reasons.
3. Implement `CharacterOwnedEntity` lifecycle and optional durability/lifetime modules.
4. Implement `CharacterDeployable` and the future `CharacterSummon` boundary.
5. Implement the per-character registry.
6. Expose protected APIs through `CharacterBase`.
7. Route projectile and common direct/area attacks through `IDamageable` without double reporting.
8. Add pure tests, source-contract tests, and Unity/Fusion verification cases.
9. Update the character framework and developer guide, including Future Extensions.

## 14. Acceptance Criteria

- A character author can create and manage a damageable and/or timed deployable without using Fusion APIs.
- Health and duration work independently and together.
- Enemy, friendly, and self damage follow configurable policies with safe defaults.
- All destruction causes converge on one exactly-once network removal path.
- Count limits and owner-exit behavior are deterministic.
- Existing character damage behavior and callbacks do not regress.
- No Spark implementation or prefab is changed.
- Documentation explains current use and future extension to mines, emitters, turrets, and AI summons.
