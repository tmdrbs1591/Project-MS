"""Static integration-contract audit for CharacterBase.

This intentionally verifies source structure only.  The no-Unity module suite
is the behavioral evidence; Unity/Fusion runtime behavior still needs SDK QA.
"""

from pathlib import Path
import re
import sys


PROJECT_ROOT = Path(__file__).resolve().parents[3]
ROOT = PROJECT_ROOT / "Assets" / "00.Main" / "01.Script" / "Character" / "Framework"
SOURCE = (ROOT / "Runtime" / "Core" / "CharacterBase.cs").read_text(encoding="utf-8")
PROJECTILE_SOURCE = (ROOT / "Runtime" / "Combat" / "CharacterProjectile.cs").read_text(encoding="utf-8")
PIPELINE_SOURCE = (ROOT / "Runtime" / "Modules" / "CharacterDamagePipeline.cs").read_text(encoding="utf-8")
TEMPLATE_SOURCE = (ROOT / "Runtime" / "Examples" / "CharacterTemplate.cs").read_text(encoding="utf-8")
README_SOURCE = (PROJECT_ROOT / "Docs" / "Character" / "CharacterFramework.md").read_text(encoding="utf-8")
GUIDE_SOURCE = README_SOURCE
EXAMPLE_SOURCES = {
    path.name: path.read_text(encoding="utf-8")
    for path in (ROOT / "Runtime" / "Examples").glob("*.cs")
}

COMMON_SOURCE_FILES = (
    "Runtime/Core/CharacterBase.cs",
    "Runtime/Core/CharacterDamageSource.cs",
    "Runtime/Core/ProjectileDespawnReason.cs",
    "Runtime/Core/CharacterTimerHandle.cs",
    "Runtime/Modules/CharacterActionStateHandler.cs",
    "Runtime/Modules/CharacterStatusHandler.cs",
    "Runtime/Modules/CharacterTimerHandler.cs",
    "Runtime/Modules/CharacterDamagePipeline.cs",
    "Runtime/Combat/CharacterProjectile.cs",
    "Runtime/OwnedEntities/CharacterOwnedEntity.cs",
    "Runtime/OwnedEntities/CharacterDeployable.cs",
    "Runtime/OwnedEntities/CharacterSummon.cs",
    "Runtime/OwnedEntities/CharacterThrowable.cs",
    "Runtime/OwnedEntities/CharacterThrowableFuseRules.cs",
    "Runtime/OwnedEntities/CharacterOwnedEntityRegistry.cs",
    "Runtime/OwnedEntities/OwnedEntityPolicies.cs",
    "Runtime/OwnedEntities/OwnedEntityGroupId.cs",
    "Runtime/OwnedEntities/OwnedEntitySpawnRequest.cs",
    "Runtime/OwnedEntities/OwnedEntitySpawnResult.cs",
    "Runtime/OwnedEntities/OwnedEntityDurabilityRules.cs",
    "Runtime/Combat/IDamageable.cs",
    "Runtime/Combat/DamageRequest.cs",
    "Runtime/Combat/DamageResult.cs",
)

DOCUMENTED_FACADE_NAMES = (
    "SetActionEnabled", "IsActionEnabled", "SetActionCharges", "AddActionCharges",
    "GetActionCharges", "SetCooldownDuration", "ResetCooldownDuration",
    "SetAutoCooldown", "StartCooldown", "ClearCooldown", "GetCooldownRemaining",
    "SetMovementEnabled", "IsMovementEnabled", "ApplySlow", "SlowRatio", "IsSlowed",
    "ScheduleTimer", "CancelTimer", "FacingDirection", "AimDirection", "IsFacingRight",
    "IsFacingLeft", "IsBehindTarget", "ModifyOutgoingDamage",
    "OnDamageDealt", "OnProjectileDespawned", "DespawnProjectile", "SpawnProjectile",
    "SpawnOwnedEntity", "DestroyOwnedEntity", "GetOwnedEntities", "DestroyOwnedEntities",
    "SpawnThrowable", "StartThrowableFuse",
    "FindDamageablesInCircle", "FindDamageablesInBox", "FindDamageablesInLine", "FindDamageablesInArc",
)


def require_owned_entity_base_contract() -> None:
    owned_root = ROOT / "Runtime" / "OwnedEntities"
    owned_source = (owned_root / "CharacterOwnedEntity.cs").read_text(encoding="utf-8")
    deployable_source = (owned_root / "CharacterDeployable.cs").read_text(encoding="utf-8")
    summon_source = (owned_root / "CharacterSummon.cs").read_text(encoding="utf-8")
    registry_source = (owned_root / "CharacterOwnedEntityRegistry.cs").read_text(encoding="utf-8")
    policies_source = (owned_root / "OwnedEntityPolicies.cs").read_text(encoding="utf-8")
    damageable_source = (ROOT / "Runtime" / "Combat" / "IDamageable.cs").read_text(encoding="utf-8")
    throwable_source = (owned_root / "CharacterThrowable.cs").read_text(encoding="utf-8")

    require(r"abstract\s+class\s+CharacterOwnedEntity\s*:\s*NetworkBehaviour\s*,\s*IDamageable\s*,\s*IStateAuthorityChanged",
            "owned entity is the shared network damageable base", owned_source)
    require(r"class\s+CharacterDeployable\s*:\s*CharacterOwnedEntity",
            "deployable extension boundary", deployable_source)
    require(r"abstract\s+class\s+CharacterSummon\s*:\s*CharacterOwnedEntity",
            "summon extension boundary", summon_source)
    require(r"sealed\s+class\s+CharacterOwnedEntityRegistry",
            "per-character owned entity registry", registry_source)
    require(r"interface\s+IDamageable", "shared damage target interface", damageable_source)
    require(r"class\s+CharacterThrowable\s*:\s*CharacterOwnedEntity",
            "physical throwable extension boundary", throwable_source)
    for token in (
        "Rigidbody2D", "Collider2D", "Fusion.Addons.Physics.NetworkRigidbody2D", "fuseSeconds", "groundLayer",
        "CharacterThrowableFuseStartMode", "TickTimer", "OnCollisionEnter2D",
        "OnThrowableSpawnedAuthority", "OnFuseStartedAuthority", "OnFuseExpiredAuthority", "FuseExpired",
    ):
        if token not in throwable_source:
            raise AssertionError(f"character throwable contract missing: {token}")
    require(r"protected\s+sealed\s+override\s+void\s+OnOwnedEntitySpawnedAuthority",
            "throwable fuse initialization cannot be bypassed by subclasses", throwable_source)

    spawn_throwable_body = extract_method_body(SOURCE, "protected OwnedEntitySpawnResult<T> SpawnThrowable")
    if "SpawnOwnedEntity" not in spawn_throwable_body or "Runner.Spawn" in spawn_throwable_body:
        raise AssertionError("throwable spawn does not route through the owned entity facade")
    if "GetComponent<Fusion.Addons.Physics.NetworkRigidbody2D>()" not in spawn_throwable_body:
        raise AssertionError("throwable spawn accepts a prefab without network physics synchronization")
    if "body.bodyType != RigidbodyType2D.Dynamic" not in spawn_throwable_body:
        raise AssertionError("throwable spawn accepts a non-dynamic rigidbody")
    if "collider.isTrigger" not in spawn_throwable_body:
        raise AssertionError("throwable spawn accepts a trigger collider that cannot report ground collision")
    start_fuse_body = extract_method_body(SOURCE, "protected bool StartThrowableFuse(")
    if "ownedEntityRegistry.Contains" not in start_fuse_body or "TryStartFuse" not in start_fuse_body:
        raise AssertionError("manual throwable fuse start bypasses owner validation")

    for token in (
        "HealthDepleted", "LifetimeExpired", "LimitExceeded", "OwnerDied",
        "OwnerDespawned", "OwnerDisconnected", "SkillTriggered", "Manual",
        "RejectNew", "DestroyOldest", "DestroyNewest", "Unlimited",
        "HealthOrDuration", "AllowSelfDamage", "AllowFriendlyDamage",
    ):
        if token not in policies_source + owned_source:
            raise AssertionError(f"owned entity policy contract missing: {token}")

    for facade in (
        "SpawnOwnedEntity", "DestroyOwnedEntity", "GetOwnedEntities", "DestroyOwnedEntities",
    ):
        if facade not in SOURCE:
            raise AssertionError(f"CharacterBase owned entity facade missing: {facade}")

    spawn_body = extract_method_body(SOURCE, "protected OwnedEntitySpawnResult<T> SpawnOwnedEntity")
    if "Runner.Spawn(" not in spawn_body or "InitializeOwnedEntity" not in spawn_body:
        raise AssertionError("owned entity facade does not own spawn and initialization")
    if "ownedEntityRegistry" not in spawn_body:
        raise AssertionError("owned entity facade does not route through registry")
    spawn_position = spawn_body.find("Runner.Spawn(")
    replacement_position = spawn_body.find("replacement.RequestDestroy")
    if replacement_position < 0 or replacement_position < spawn_position:
        raise AssertionError("overflow replacement is retired before replacement spawn succeeds")
    if "Runner.Despawn(spawnedObject)" not in spawn_body:
        raise AssertionError("malformed owned entity spawn is not rolled back")

    destroy_body = extract_method_body(SOURCE, "protected bool DestroyOwnedEntity(")
    if "RequestDestroy" not in destroy_body:
        raise AssertionError("owned entity facade bypasses the entity destruction gate")

    query_source = (ROOT / "Runtime" / "Combat" / "CharacterCombatQuery2D.cs").read_text(encoding="utf-8")
    for facade, query in (
        ("FindDamageablesInCircle", "DamageablesInCircle"),
        ("FindDamageablesInBox", "DamageablesInBox"),
        ("FindDamageablesInLine", "DamageablesInLine"),
        ("FindDamageablesInArc", "DamageablesInArc"),
    ):
        if facade not in SOURCE:
            raise AssertionError(f"CharacterBase damageable query facade missing: {facade}")
        if query not in query_source:
            raise AssertionError(f"shared damageable query missing: {query}")

    require(
        r"DamageablesInCircle\s*\(\s*CharacterBase\s+owner\s*,",
        "damageable query receives the attacking character",
        query_source,
    )
    require(
        r"target\s*==\s*owner",
        "damageable query excludes the attacking character but not its owned entities",
        query_source,
    )

    for token in (
        "StateAuthorityChanged", "NetworkObjectFlags.DestroyWhenStateAuthorityLeaves",
        "NetworkObjectFlags.AllowStateAuthorityOverride", "RequestStateAuthority",
        "pendingOwnerDisconnect", "RequestDestroy(OwnedEntityDestroyReason.OwnerDisconnected)",
        "OwnerDisconnected", "cachedOwnerCharacterId", "cachedGroup", "cachedOwner",
        "ConfirmOwnedEntityDamage",
    ):
        if token not in owned_source:
            raise AssertionError(f"owned entity lifecycle contract missing: {token}")
    if "Rpc_ConfirmOwnedEntityDamage" not in SOURCE:
        raise AssertionError("owned entity applied damage is not acknowledged to the source authority")
    if "OnOwnedEntityDamageDealt(targetId, appliedDamage, source)" not in SOURCE:
        raise AssertionError("owned entity damage confirmation lacks a despawn-safe NetworkId hook")

CSHARP_NON_CODE = re.compile(
    r'''(?:
        \$?@"(?:""|[^"])*" |
        @\$"(?:""|[^"])*" |
        \$?"(?:\\.|[^"\\])*" |
        '(?:\\.|[^'\\])*' |
        //[^\r\n]* |
        /\*.*?\*/
    )''',
    re.DOTALL | re.VERBOSE,
)


def require(pattern: str, label: str, source: str = SOURCE) -> None:
    if not re.search(pattern, source, re.DOTALL):
        raise AssertionError(label)


def require_slow_expiry_contract(source: str) -> None:
    require(
        r"bool\s+ICharacterStatusStateStore\.IsSlowRunning\s*=>\s*Runner\s*!=\s*null\s*&&\s*NetSlowTimer\.IsRunning\s*;",
        "slow running preserves configured TickTimer state",
        source,
    )
    require(
        r"bool\s+ICharacterStatusStateStore\.IsSlowExpired\s*=>\s*Runner\s*!=\s*null\s*&&\s*NetSlowTimer\.IsRunning\s*&&\s*NetSlowTimer\.Expired\(Runner\)\s*;",
        "slow expiry is actionable while timer remains configured",
        source,
    )


def require_success_only_action_contract(source: str) -> None:
    start = source.find("private void TryExecute")
    end = source.find("private CharacterActionContext CreateActionContext", start)
    if start < 0 or end < 0:
        raise AssertionError("TryExecute body")

    body = source[start:end]
    can_use = body.find("CanUse(action)")
    execute = body.find("OnBasicAttack(context)")
    if can_use < 0 or execute < 0 or can_use >= execute:
        raise AssertionError("TryExecute checks CanUse before invoking the action")

    guard = re.search(r"if\s*\(\s*!executed\s*\)\s*return\s*;", body)
    if guard is None:
        raise AssertionError("TryExecute has a simple failed-action return guard")

    before_success = body[:guard.start()]
    success_body = body[guard.end():]
    if "if (!executed)" in success_body:
        raise AssertionError("TryExecute contains a failed-action branch after its success guard")

    for token in (
        "ConsumeCharge(action)",
        "ShouldStartCooldownAutomatically(action)",
        "StartCooldown(action)",
    ):
        if token in before_success:
            raise AssertionError(f"TryExecute mutates action state before success: {token}")

    positions = [
        success_body.find(token)
        for token in (
            "ConsumeCharge(action)",
            "ShouldStartCooldownAutomatically(action)",
            "StartCooldown(action)",
            "NetActionSequence++",
            "OnSkillExecuted(action)",
        )
    ]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        raise AssertionError("TryExecute success-only action order")


def require_movement_lock_action_contract(source: str) -> None:
    body = extract_method_body(source, "private void TryExecute")
    guard = re.search(
        r"if\s*\(\s*action\s*==\s*CharacterActionType\.Dash\s*&&\s*!NetMovementEnabled\s*\)\s*return\s*;",
        body,
    )
    if guard is None:
        raise AssertionError("TryExecute blocks Dash specifically while movement is locked")
    if body.count("NetMovementEnabled") != 1:
        raise AssertionError("TryExecute movement lock affects only the Dash action")
    if guard.start() > body.find("OnDash(context)"):
        raise AssertionError("TryExecute checks movement lock before entering OnDash")


def require_damage_pipeline_contract(source: str) -> None:
    require(r"class\s+CharacterDamagePipeline", "damage pipeline exists", source)
    apply_start = source.find("public void Apply")
    apply_end = source.find("\n        }\n    }", apply_start)
    if apply_start < 0 or apply_end < 0:
        raise AssertionError("damage pipeline Apply body")

    body = source[apply_start:apply_end]
    modify = body.find("modifyDamage")
    request = body.find("requestDamage(finalDamage)")
    notify = body.find("notifyDamageDealt(finalDamage)")
    if any(position < 0 for position in (modify, request, notify)) or not modify < request < notify:
        raise AssertionError("damage pipeline modifies, requests, then notifies")


def extract_method_body(source: str, declaration: str) -> str:
    code = sanitize_csharp_non_code(source)
    start = code.find(declaration)
    if start < 0:
        raise AssertionError(f"missing method declaration: {declaration}")

    opening_brace = code.find("{", start)
    if opening_brace < 0:
        raise AssertionError(f"missing method body: {declaration}")

    depth = 0
    for index in range(opening_brace, len(code)):
        token = code[index]
        if token == "{":
            depth += 1
        elif token == "}":
            depth -= 1
            if depth == 0:
                return code[opening_brace + 1:index]

    raise AssertionError(f"unterminated method body: {declaration}")


def require_damage_entry_point_contract(source: str) -> None:
    for declaration, damage_source in (
        ("protected void DealDamage(", "CharacterDamageSource.Direct"),
        ("internal void DealProjectileDamage(", "CharacterDamageSource.Projectile"),
    ):
        body = extract_method_body(source, declaration)
        helper_call = r"\bDealDamageThroughPipeline\s*\("
        expected_call = (
            rf"{helper_call}\s*target\s*,\s*amount\s*,\s*attacker\s*,\s*"
            rf"{re.escape(damage_source)}\s*\)"
        )
        if len(re.findall(helper_call, body)) != 1:
            raise AssertionError(f"{declaration} routes through the damage pipeline exactly once")
        if len(re.findall(expected_call, body)) != 1:
            raise AssertionError(f"{declaration} uses the exact {damage_source} pipeline route")
        if "target.RequestDamage" in body or "OnDamageDealt" in body:
            raise AssertionError(f"{declaration} bypasses the common damage pipeline")


def require_projectile_damage_contract(source: str) -> None:
    for token in (
        "NetSourceObjectId",
        "NetOwnerTeamId",
        "DealProjectileDamage",
        "IDamageable",
        "ResolveDamageable",
        "DamageRequest",
        "ProjectileDespawnReason.HitOwnedEntity",
        "Runner.TryFindObject(NetSourceObjectId, out NetworkObject sourceObject)",
    ):
        if token not in source:
            raise AssertionError(f"projectile damage contract missing: {token}")

    if source.count("IsSourceCharacter(target)") < 1 or source.count("IsSourceCharacter(candidateTarget)") < 1:
        raise AssertionError("projectile does not exclude only its source CharacterBase")

    code = sanitize_csharp_non_code(source)
    if code.count("target.RequestDamage(request);") != 1:
        raise AssertionError("projectile fallback damage request is not unique")

    require(
        r"private\s+void\s+DealDamage\s*\(\s*IDamageable\s+target\s*\)",
        "projectile deals damage through IDamageable",
        source,
    )
    require(
        r"private\s+static\s+IDamageable\s+ResolveDamageable\s*\(\s*Collider2D\s+collider\s*\)",
        "projectile resolves character and owned entity damage targets",
        source,
    )

    complete = extract_method_body(source, "private void Complete(")
    if not re.search(r"if\s*\(\s*consumed\s*\)\s*return\s*;", complete):
        raise AssertionError("projectile completion lacks consumed guard")
    if len(re.findall(r"\bconsumed\s*=\s*true\s*;", complete)) != 1:
        raise AssertionError("projectile completion does not consume exactly once")
    notify_call = (
        r"\bsource\s*\.\s*NotifyProjectileDespawned\s*"
        r"\(\s*this\s*,\s*reason\s*,\s*hitTarget\s*\)\s*;"
    )
    despawn_call = r"\bRunner\s*\.\s*Despawn\s*\(\s*Object\s*\)\s*;"
    if len(re.findall(notify_call, complete)) != 1:
        raise AssertionError("projectile completion source notification is not unique")
    if len(re.findall(despawn_call, complete)) != 1:
        raise AssertionError("projectile completion despawn is not unique")
    if len(re.findall(notify_call, code)) != 1:
        raise AssertionError("projectile source notification exists outside Complete")
    if len(re.findall(despawn_call, code)) != 1:
        raise AssertionError("projectile despawn exists outside Complete")

    fixed_update = extract_method_body(source, "public override void FixedUpdateNetwork(")
    if len(re.findall(r"\bComplete\s*\(", fixed_update)) != 2:
        raise AssertionError("FixedUpdateNetwork completion calls are not bounded to cast and lifetime")
    if not re.search(
        r"Complete\s*\(\s*hitTarget\s*\?\s*ResolveHitReason\s*\(\s*target\s*\)\s*:\s*ProjectileDespawnReason\.HitWall\s*,\s*hit\.point\s*,\s*hit\.normal\s*,\s*target\s+as\s+CharacterBase\s*,\s*true\s*\)\s*;",
        fixed_update,
    ):
        raise AssertionError("cast collision does not complete with damageable/wall reasons")
    if not re.search(
        r"if\s*\(\s*LifeTimer\.Expired\s*\(\s*Runner\s*\)\s*\)\s*Complete\s*\(\s*ProjectileDespawnReason\.LifetimeExpired\s*,\s*transform\.position\s*,\s*-NetDirection\s*,\s*null\s*,\s*false\s*\)\s*;",
        fixed_update,
    ):
        raise AssertionError("lifetime expiry does not use Complete")

    trigger = extract_method_body(source, "private void OnTriggerEnter2D(")
    if len(re.findall(r"\bComplete\s*\(", trigger)) != 2:
        raise AssertionError("trigger completion calls are not bounded to character and wall")
    if not re.search(
        r"Complete\s*\(\s*ResolveHitReason\s*\(\s*target\s*\)\s*,\s*hitPosition\s*,\s*hitNormal\s*,\s*target\s+as\s+CharacterBase\s*,\s*true\s*\)\s*;",
        trigger,
    ):
        raise AssertionError("trigger damageable hit does not use Complete")
    if not re.search(
        r"Complete\s*\(\s*ProjectileDespawnReason\.HitWall\s*,\s*hitPosition\s*,\s*hitNormal\s*,\s*null\s*,\s*true\s*\)\s*;",
        trigger,
    ):
        raise AssertionError("trigger wall hit does not use Complete")

    manual = extract_method_body(source, "internal void CompleteManually(")
    if len(re.findall(r"\bComplete\s*\(", manual)) != 1 or not re.search(
        r"Complete\s*\(\s*ProjectileDespawnReason\.Manual\s*,\s*transform\.position\s*,\s*-NetDirection\s*,\s*null\s*,\s*false\s*\)\s*;",
        manual,
    ):
        raise AssertionError("manual despawn does not use Complete with Manual reason")


def require_projectile_spawn_lifecycle_contract(source: str) -> None:
    code = sanitize_csharp_non_code(source)
    spawned = extract_method_body(source, "public override void Spawned(")
    if len(re.findall(r"\bconsumed\s*=\s*false\s*;", spawned)) != 1:
        raise AssertionError("Spawned does not reset projectile consumed state")
    if len(re.findall(r"\bconsumed\s*=\s*false\s*;", code)) != 1:
        raise AssertionError("projectile consumed state resets outside Spawned")


def require_manual_projectile_despawn_contract(source: str) -> None:
    body = extract_method_body(source, "protected void DespawnProjectile(")
    complete_manually = r"\bprojectile\s*\.\s*CompleteManually\s*\(\s*\)\s*;"
    if len(re.findall(complete_manually, body)) != 1:
        raise AssertionError("CharacterBase manual despawn does not call CompleteManually exactly once")
    if re.search(r"\b(?:projectile|Object|Runner)\s*\.\s*Despawn\s*\(", body):
        raise AssertionError("CharacterBase manual despawn bypasses projectile completion")
    if re.search(r"\bsource\s*\.\s*NotifyProjectileDespawned\s*\(", body):
        raise AssertionError("CharacterBase manual despawn notifies the source directly")


def require_legacy_projectile_initialize_contract(source: str) -> None:
    require(
        r"public\s+void\s+Initialize\s*\(\s*Vector2\s+direction\s*,\s*float\s+speed\s*,\s*float\s+damage\s*,\s*LayerMask\s+targetLayer\s*,\s*PlayerRef\s+owner\s*,\s*NetworkId\s+sourceObjectId\s*,\s*int\s+ownerTeamId\s*\)",
        "source-aware projectile initializer",
        source,
    )
    require(
        r'\[Obsolete\(\s*"Use CharacterBase\.SpawnProjectile or Initialize with a source NetworkId; legacy initialization cannot preserve damage callbacks\."\s*,\s*true\s*\)\]\s*public\s+void\s+Initialize\s*\(\s*Vector2\s+direction\s*,\s*float\s+speed\s*,\s*float\s+damage\s*,\s*LayerMask\s+targetLayer\s*,\s*PlayerRef\s+owner\s*\)',
        "legacy projectile initializer is an obsolete compile-time error",
        source,
    )


def require_common_source_files(paths: tuple[str, ...]) -> None:
    missing = [path for path in paths if not (ROOT / path).is_file()]
    if missing:
        raise AssertionError(f"common source file missing: {', '.join(missing)}")


def require_examples_use_base_infrastructure(example_sources: dict[str, str]) -> None:
    forbidden = (r"\[Networked\]", r"\[Rpc", r"\bRpc\w*\s*\(", r"\bRunner\.Spawn\s*\(")
    for name, source in example_sources.items():
        for pattern in forbidden:
            if re.search(pattern, source):
                raise AssertionError(f"character example uses Fusion primitive: {name}: {pattern}")


def sanitize_csharp_non_code(source: str) -> str:
    def keep_line_breaks(match: re.Match[str]) -> str:
        return re.sub(r"[^\r\n]", " ", match.group())

    return CSHARP_NON_CODE.sub(keep_line_breaks, source)


def require_no_direct_projectile_initialize(example_sources: dict[str, str]) -> None:
    """Forbid CharacterProjectile initialization only in Runtime/Examples.

    CharacterBase owns projectile spawning and initialization.  The rule uses
    sanitized source and only follows CharacterProjectile declarations plus the
    explicit cast/as-cast/GetComponent forms.  Framework/Core sources are not
    passed here, so their legitimate initialization remains outside this check.
    """
    alias_declaration = re.compile(
        r"\busing\s+([A-Za-z_]\w*)\s*=\s*(?:global\s*::\s*)?"
        r"(?:[A-Za-z_]\w*\s*(?:\.\s*|::\s*))*CharacterProjectile\s*;"
    )

    for name, source in example_sources.items():
        code = sanitize_csharp_non_code(source)
        aliases = set(alias_declaration.findall(code))
        qualified_type = (
            r"(?:global\s*::\s*)?"
            r"(?:[A-Za-z_]\w*\s*(?:\.\s*|::\s*))*CharacterProjectile"
        )
        type_options = [qualified_type, *(re.escape(alias) for alias in sorted(aliases))]
        projectile_type = rf"(?:{'|'.join(type_options)})"

        projectile_declaration = re.compile(
            rf"\b{projectile_type}\s+([A-Za-z_]\w*)\b"
        )
        get_component_initialize = re.compile(
            rf"\bGetComponent(?:InChildren|InParent)?\s*<\s*{projectile_type}\s*>\s*"
            r"\(\s*\)\s*(?:\.\s*|\?\s*\.\s*)Initialize\s*\("
        )
        get_component_receiver = re.compile(
            rf"\bvar\s+([A-Za-z_]\w*)\s*=\s*[^;]*\bGetComponent(?:InChildren|InParent)?"
            rf"\s*<\s*{projectile_type}\s*>"
        )
        cast_initialize = re.compile(
            rf"\(+\s*{projectile_type}\s*\)\s*[A-Za-z_]\w*\s*\)*\s*"
            r"(?:\.\s*|\?\s*\.\s*)Initialize\s*\("
        )
        as_cast_initialize = re.compile(
            rf"\(+\s*[A-Za-z_]\w*\s+as\s+{projectile_type}\s*\)+\s*"
            r"(?:\.\s*|\?\s*\.\s*)Initialize\s*\("
        )
        cast_assigned_receiver = re.compile(
            rf"\bvar\s+([A-Za-z_]\w*)\s*=\s*\(\s*{projectile_type}\s*\)\s*[^;]+"
        )
        as_assigned_receiver = re.compile(
            rf"\bvar\s+([A-Za-z_]\w*)\s*=\s*[^;]*?\bas\s+{projectile_type}\b"
        )

        if get_component_initialize.search(code) or cast_initialize.search(code) or as_cast_initialize.search(code):
            raise AssertionError(f"character example initializes CharacterProjectile directly: {name}")

        receivers = set(projectile_declaration.findall(code))
        receivers.update(get_component_receiver.findall(code))
        receivers.update(cast_assigned_receiver.findall(code))
        receivers.update(as_assigned_receiver.findall(code))
        for receiver in receivers:
            if re.search(
                rf"(?:\bthis\s*\.\s*)?\b{re.escape(receiver)}\s*(?:\.\s*|\?\s*\.\s*)Initialize\s*\(",
                code,
            ):
                raise AssertionError(f"character example initializes CharacterProjectile directly: {name}")


def require_projectile_spawn_route(source: str, example_sources: dict[str, str]) -> None:
    body = extract_method_body(source, "protected void SpawnProjectile(")
    if "Runner.Spawn(" not in body:
        raise AssertionError("CharacterBase SpawnProjectile does not spawn through Runner")
    if not re.search(
        r"Initialize\s*\(\s*normalized\s*,\s*speed\s*,\s*damage\s*,\s*targetLayer\s*,\s*Object\.InputAuthority\s*,\s*Object\.Id\s*,\s*DamageTeamId\s*\)",
        body,
    ):
        raise AssertionError("CharacterBase SpawnProjectile does not use source-aware initialization")
    if re.search(r"Initialize\s*\([^)]*Object\.InputAuthority\s*\)", body):
        raise AssertionError("CharacterBase SpawnProjectile uses legacy five-argument initialization")

    require_no_direct_projectile_initialize(example_sources)


def require_template_override_examples(source: str) -> None:
    require(
        r"protected\s+override\s+float\s+ModifyOutgoingDamage\s*\(\s*CharacterBase\s+target\s*,\s*float\s+damage\s*,\s*CharacterDamageSource\s+source\s*\)",
        "template ModifyOutgoingDamage override example",
        source,
    )
    require(
        r"protected\s+override\s+void\s+OnProjectileDespawned\s*\(\s*CharacterProjectile\s+projectile\s*,\s*ProjectileDespawnReason\s+reason\s*,\s*CharacterBase\s+hitTarget\s*\)",
        "template OnProjectileDespawned override example",
        source,
    )


def require_new_character_docs(readme: str, guide: str) -> None:
    combined = readme + "\n" + guide
    for name in DOCUMENTED_FACADE_NAMES:
        if name not in combined:
            raise AssertionError(f"new-character docs omit facade API: {name}")

    for required_text, label in (
        ("CharacterBase", "CharacterBase ownership explanation"),
        ("직접 공격", "direct damage callback explanation"),
        ("투사체", "projectile damage callback explanation"),
        ("발사자", "projectile source callback explanation"),
        ("[Networked]", "network attribute prohibition"),
        ("[Rpc", "RPC attribute prohibition"),
        ("Runner.Spawn", "Runner spawn prohibition"),
        ("CharacterBase.SpawnProjectile", "approved projectile route"),
        ("5개", "legacy five-argument initializer explanation"),
        ("HitCharacter", "projectile hit despawn reason"),
        ("HitWall", "projectile wall despawn reason"),
        ("LifetimeExpired", "projectile lifetime despawn reason"),
        ("Manual", "projectile manual despawn reason"),
    ):
        if required_text not in combined:
            raise AssertionError(f"new-character docs omit {label}")


def audit_self_test() -> None:
    old_slow_adapter = SOURCE.replace(
        "NetSlowTimer.IsRunning",
        "NetSlowTimer.RemainingTime(Runner).GetValueOrDefault() > 0f",
        1,
    )
    try:
        require_slow_expiry_contract(old_slow_adapter)
    except AssertionError:
        pass
    else:
        raise AssertionError("slow-expiry audit accepts remaining-time running mapping")

    action_start = SOURCE.find("private void TryExecute")
    action_end = SOURCE.find("private CharacterActionContext CreateActionContext", action_start)
    failed_charge = SOURCE[:action_start] + SOURCE[action_start:action_end].replace(
        "actionState.ConsumeCharge(action);",
        "if (!executed)\n            {\n                actionState.ConsumeCharge(action);\n            }",
        1,
    ) + SOURCE[action_end:]
    try:
        require_success_only_action_contract(failed_charge)
    except AssertionError:
        pass
    else:
        raise AssertionError("TryExecute audit accepts charge consumption in failed branch")

    broad_movement_lock = SOURCE.replace(
        "if (action == CharacterActionType.Dash && !NetMovementEnabled)",
        "if (!NetMovementEnabled)",
        1,
    )
    if broad_movement_lock == SOURCE:
        raise AssertionError("movement-lock action mutation did not apply")
    try:
        require_movement_lock_action_contract(broad_movement_lock)
    except AssertionError:
        pass
    else:
        raise AssertionError("movement-lock audit accepts blocking non-movement actions")

    reordered_pipeline = PIPELINE_SOURCE.replace(
        "            requestDamage(finalDamage);\n            if (notifyDamageDealt != null)\n                notifyDamageDealt(finalDamage);",
        "            if (notifyDamageDealt != null)\n                notifyDamageDealt(finalDamage);\n            requestDamage(finalDamage);",
        1,
    )
    if reordered_pipeline == PIPELINE_SOURCE:
        raise AssertionError("damage-pipeline ordering mutation did not apply")
    try:
        require_damage_pipeline_contract(reordered_pipeline)
    except AssertionError:
        pass
    else:
        raise AssertionError("damage pipeline audit accepts notify-before-request ordering")

    direct_bypass = SOURCE.replace(
        "DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Direct);",
        "target.RequestDamage(amount, attacker);\n            OnDamageDealt(target, amount);",
        1,
    )
    if direct_bypass == SOURCE:
        raise AssertionError("direct damage bypass mutation did not apply")
    try:
        require_damage_entry_point_contract(direct_bypass)
    except AssertionError:
        pass
    else:
        raise AssertionError("damage-entry audit accepts direct RequestDamage bypass")

    projectile_bypass = SOURCE.replace(
        "DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Projectile);",
        "target.RequestDamage(amount, attacker);\n            OnDamageDealt(target, amount);",
        1,
    )
    if projectile_bypass == SOURCE:
        raise AssertionError("projectile damage bypass mutation did not apply")
    try:
        require_damage_entry_point_contract(projectile_bypass)
    except AssertionError:
        pass
    else:
        raise AssertionError("damage-entry audit accepts projectile RequestDamage bypass")

    wrong_direct_source = SOURCE.replace(
        "DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Direct);",
        "DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Projectile); // CharacterDamageSource.Direct",
        1,
    )
    if wrong_direct_source == SOURCE:
        raise AssertionError("direct damage source mutation did not apply")
    try:
        require_damage_entry_point_contract(wrong_direct_source)
    except AssertionError:
        pass
    else:
        raise AssertionError("damage-entry audit accepts a wrong direct damage source")

    projectile_mutations = (
        (
            "lifetime direct despawn",
            "Complete(ProjectileDespawnReason.LifetimeExpired, transform.position, -NetDirection, null, false);",
            "Runner.Despawn(Object); // ProjectileDespawnReason.LifetimeExpired",
        ),
        (
            "trigger damageable completion bypass",
            "Complete(ResolveHitReason(target), hitPosition, hitNormal, target as CharacterBase, true);",
            "Runner.Despawn(Object); // ResolveHitReason(target)",
        ),
        (
            "trigger wall completion bypass",
            "Complete(ProjectileDespawnReason.HitWall, hitPosition, hitNormal, null, true);",
            "Runner.Despawn(Object); // ProjectileDespawnReason.HitWall",
        ),
        (
            "manual completion bypass",
            "Complete(ProjectileDespawnReason.Manual, transform.position, -NetDirection, null, false);",
            "Runner.Despawn(Object); // ProjectileDespawnReason.Manual",
        ),
        (
            "misplaced source notification",
            "                DealDamage(target);",
            "                DealDamage(target);\n                source.NotifyProjectileDespawned(this, reason, hitTarget);",
        ),
        (
            "misplaced projectile despawn",
            "            AlignToDirection();",
            "            Runner.Despawn(Object);\n            AlignToDirection();",
        ),
    )
    for label, original, replacement in projectile_mutations:
        mutated = PROJECTILE_SOURCE.replace(original, replacement, 1)
        if mutated == PROJECTILE_SOURCE:
            raise AssertionError(f"{label} mutation did not apply")
        try:
            require_projectile_damage_contract(mutated)
        except AssertionError:
            pass
        else:
            raise AssertionError(f"projectile completion audit accepts {label}")

    whitespace_misplaced_calls = PROJECTILE_SOURCE.replace(
        "            AlignToDirection();",
        "            Runner . Despawn ( Object );\n            source . NotifyProjectileDespawned ( this, reason, hitTarget );\n            AlignToDirection();",
        1,
    )
    if whitespace_misplaced_calls == PROJECTILE_SOURCE:
        raise AssertionError("whitespace-form misplaced completion mutation did not apply")
    try:
        require_projectile_damage_contract(whitespace_misplaced_calls)
    except AssertionError:
        pass
    else:
        raise AssertionError("projectile completion audit accepts whitespace-form misplaced calls")

    manual_base_bypass = SOURCE.replace(
        "projectile.CompleteManually();",
        "Runner . Despawn ( projectile.Object ); // projectile.CompleteManually();",
        1,
    )
    if manual_base_bypass == SOURCE:
        raise AssertionError("CharacterBase manual despawn bypass mutation did not apply")
    try:
        require_manual_projectile_despawn_contract(manual_base_bypass)
    except AssertionError:
        pass
    else:
        raise AssertionError("manual projectile despawn audit accepts a direct Runner bypass")

    removed_spawn_reset = PROJECTILE_SOURCE.replace("            consumed = false;\n", "", 1)
    if removed_spawn_reset == PROJECTILE_SOURCE:
        raise AssertionError("projectile Spawned reset mutation did not apply")
    try:
        require_projectile_spawn_lifecycle_contract(removed_spawn_reset)
    except AssertionError:
        pass
    else:
        raise AssertionError("projectile lifecycle audit accepts a missing Spawned reset")

    non_code_completion_noise = PROJECTILE_SOURCE.replace(
        "            AlignToDirection();",
        "            string auditNoise = \"{ Runner.Despawn(Object); }\"; // source.NotifyProjectileDespawned(this, reason, hitTarget);\n            AlignToDirection();",
        1,
    )
    if non_code_completion_noise == PROJECTILE_SOURCE:
        raise AssertionError("projectile non-code mutation did not apply")
    try:
        require_projectile_damage_contract(non_code_completion_noise)
        require_projectile_spawn_lifecycle_contract(non_code_completion_noise)
    except AssertionError as error:
        raise AssertionError("projectile audit rejects completion tokens in comments or strings") from error

    try:
        require_common_source_files(("Runtime/Core/not-a-common-file.cs",))
    except AssertionError:
        pass
    else:
        raise AssertionError("common-file audit accepts a missing source file")

    for primitive, label in (
        ("[Networked] private int NetValue { get; set; }", "network attribute"),
        ("[Rpc] private void Rpc_Sync() { }", "RPC attribute"),
        ("private void Rpc_Sync() { }", "RPC method"),
        ("Runner.Spawn(projectilePrefab);", "Runner spawn"),
    ):
        bad_examples = dict(EXAMPLE_SOURCES)
        bad_examples["BadCharacter.cs"] = primitive
        try:
            require_examples_use_base_infrastructure(bad_examples)
        except AssertionError:
            pass
        else:
            raise AssertionError(f"example audit accepts a Fusion {label}")

    legacy_spawn = SOURCE.replace(
        """projectile?.Initialize(
                        normalized,
                        speed,
                        damage,
                        targetLayer,
                        Object.InputAuthority,
                        Object.Id,
                        DamageTeamId);""",
        "projectile?.Initialize(normalized, speed, damage, targetLayer, Object.InputAuthority);",
        1,
    )
    if legacy_spawn == SOURCE:
        raise AssertionError("legacy projectile spawn mutation did not apply")
    try:
        require_projectile_spawn_route(legacy_spawn, EXAMPLE_SOURCES)
    except AssertionError:
        pass
    else:
        raise AssertionError("projectile-route audit accepts legacy initialization")

    for forbidden_source, label in (
        ("CharacterProjectile fired; fired.Initialize(direction, speed, damage, layer, owner);", "typed local receiver"),
        ("CharacterProjectile fired; this.fired.Initialize(direction, speed, damage, layer, owner);", "typed member receiver"),
        ("void Arm(CharacterProjectile fired) { fired.Initialize(direction, speed, damage, layer, owner); }", "typed parameter receiver"),
        ("var fired = GetComponent<CharacterProjectile>(); fired.Initialize(direction, speed, damage, layer, owner);", "GetComponent receiver"),
        ("GetComponent<CharacterProjectile>()?.Initialize(direction, speed, damage, layer, owner);", "inline GetComponent receiver"),
        ("((CharacterProjectile)component).Initialize(direction, speed, damage, layer, owner);", "parenthesized cast receiver"),
        ("((component as CharacterProjectile)).Initialize(direction, speed, damage, layer, owner);", "as-cast receiver"),
        ("var fired = (CharacterProjectile)component; fired.Initialize(direction, speed, damage, layer, owner);", "cast-assigned receiver"),
        ("var fired = component as CharacterProjectile; fired?.Initialize(direction, speed, damage, layer, owner);", "as-assigned receiver"),
        ("using ProjectileAlias = ProjectMS.CharacterSystem.CharacterProjectile; ProjectileAlias fired; fired.Initialize(direction, speed, damage, layer, owner);", "explicit alias receiver"),
        ("using ProjectileAlias = ProjectMS.CharacterSystem.CharacterProjectile; var fired = (ProjectileAlias)component; fired.Initialize(direction, speed, damage, layer, owner);", "alias cast-assigned receiver"),
    ):
        bad_examples = dict(EXAMPLE_SOURCES)
        bad_examples["BadCharacter.cs"] = forbidden_source
        try:
            require_no_direct_projectile_initialize(bad_examples)
        except AssertionError:
            pass
        else:
            raise AssertionError(f"projectile-route audit accepts a direct {label}")

    comment_and_string_only = {
        "SafeCharacter.cs": (
            "// CharacterProjectile fired; fired.Initialize(direction, speed, damage, layer, owner);\n"
            "string note = \"GetComponent<CharacterProjectile>().Initialize(...)\";"
        )
    }
    try:
        require_no_direct_projectile_initialize(comment_and_string_only)
    except AssertionError as error:
        raise AssertionError("projectile-route audit rejects a comment or string") from error

    unrelated_initializer = {
        "SafeCharacter.cs": "SomeOtherComponent helper; helper.Initialize();"
    }
    try:
        require_no_direct_projectile_initialize(unrelated_initializer)
    except AssertionError as error:
        raise AssertionError("projectile-route audit rejects an unrelated helper initializer") from error

    missing_hook_template = TEMPLATE_SOURCE.replace("OnProjectileDespawned", "OnProjectileGone", 1)
    try:
        require_template_override_examples(missing_hook_template)
    except AssertionError:
        pass
    else:
        raise AssertionError("template audit accepts a missing projectile despawn hook")

    missing_damage_template = TEMPLATE_SOURCE.replace("ModifyOutgoingDamage", "ModifyDamage", 1)
    try:
        require_template_override_examples(missing_damage_template)
    except AssertionError:
        pass
    else:
        raise AssertionError("template audit accepts a missing damage modifier hook")

    missing_doc_api = (README_SOURCE + "\n" + GUIDE_SOURCE).replace("StartCooldown", "CooldownStartRemoved")
    try:
        require_new_character_docs("", missing_doc_api)
    except AssertionError:
        pass
    else:
        raise AssertionError("documentation audit accepts a missing facade API name")


def main() -> int:
    try:
        require(r"class\s+CharacterBase\s*:\s*NetworkBehaviour\s*,\s*ICharacterActionStateStore\s*,\s*ICharacterStatusStateStore", "CharacterBase explicitly implements state stores")
        require(r"private\s+const\s+int\s+ActionSlotCount\s*=\s*6\s*;", "six action slots")
        for name, value_type in (
            ("NetActionEnabled", "NetworkBool"),
            ("NetActionCharges", "int"),
            ("NetCooldownDurationOverrides", "float"),
            ("NetAutoCooldown", "NetworkBool"),
            ("NetCooldownTimers", "TickTimer"),
        ):
            require(rf"\[Networked\s*,\s*Capacity\(ActionSlotCount\)\]\s*private\s+NetworkArray<{value_type}>\s+{name}\s*=>\s*default\s*;", f"network action array {name}")

        for signature in (
            r"protected\s+void\s+SetActionEnabled\s*\(\s*CharacterActionType\s+action\s*,\s*bool\s+enabled\s*\)",
            r"protected\s+bool\s+IsActionEnabled\s*\(\s*CharacterActionType\s+action\s*\)",
            r"protected\s+void\s+SetActionCharges\s*\(\s*CharacterActionType\s+action\s*,\s*int\s+charges\s*\)",
            r"protected\s+void\s+AddActionCharges\s*\(\s*CharacterActionType\s+action\s*,\s*int\s+amount\s*\)",
            r"protected\s+int\s+GetActionCharges\s*\(\s*CharacterActionType\s+action\s*\)",
            r"protected\s+void\s+SetCooldownDuration\s*\(\s*CharacterActionType\s+action\s*,\s*float\s+seconds\s*\)",
            r"protected\s+void\s+ResetCooldownDuration\s*\(\s*CharacterActionType\s+action\s*\)",
            r"protected\s+void\s+SetAutoCooldown\s*\(\s*CharacterActionType\s+action\s*,\s*bool\s+enabled\s*\)",
            r"protected\s+void\s+StartCooldown\s*\(\s*CharacterActionType\s+action\s*\)",
            r"protected\s+void\s+StartCooldown\s*\(\s*CharacterActionType\s+action\s*,\s*float\s+seconds\s*\)",
            r"protected\s+void\s+ClearCooldown\s*\(\s*CharacterActionType\s+action\s*\)",
            r"protected\s+float\s+GetCooldownRemaining\s*\(\s*CharacterActionType\s+action\s*\)",
            r"protected\s+void\s+SetMovementEnabled\s*\(\s*bool\s+enabled\s*\)",
            r"protected\s+void\s+ApplySlow\s*\(\s*CharacterBase\s+target\s*,\s*float\s+slowRatio\s*,\s*float\s+duration\s*\)",
            r"protected\s+CharacterTimerHandle\s+ScheduleTimer\s*\(\s*float\s+seconds\s*,\s*Action\s+callback\s*\)",
            r"protected\s+bool\s+CancelTimer\s*\(\s*CharacterTimerHandle\s+handle\s*\)",
            r"public\s+int\s+FacingDirection\s*=>", r"public\s+bool\s+IsFacingRight\s*=>", r"public\s+bool\s+IsFacingLeft\s*=>",
            r"public\s+Vector2\s+AimDirection\s*=>\s*DirectionFromAngle\(NetAimAngle\)\s*;",
            r"protected\s+bool\s+IsBehindTarget\s*\(\s*CharacterBase\s+target\s*,\s*float\s+rearArcAngle\s*\)",
            r"protected\s+virtual\s+float\s+ModifyOutgoingDamage\s*\(",
            r"protected\s+virtual\s+void\s+OnProjectileDespawned\s*\(",
        ):
            require(signature, f"missing facade member: {signature}")

        require(r"protected\s+bool\s+IsMovementEnabled\s*=>", "movement enabled facade")
        require(r"public\s+float\s+SlowRatio\s*=>", "slow ratio facade")
        require(r"public\s+bool\s+IsSlowed\s*=>", "slow status facade")
        require(r"Rpc_RequestSlow", "slow request RPC")
        require(r"NetActionEnabled\.Set\(", "network enabled mutation uses Set")
        require(r"NetActionCharges\.Set\(", "network charges mutation uses Set")
        require(r"NetCooldownDurationOverrides\.Set\(", "network cooldown duration mutation uses Set")
        require(r"NetAutoCooldown\.Set\(", "network auto cooldown mutation uses Set")
        require(r"NetCooldownTimers\.Set\(", "network cooldown timer mutation uses Set")
        require(r"RemainingTime\(Runner\)", "TickTimer remaining time uses Runner")
        require(r"new\s+CharacterCooldownHandler\s*\(\s*actionState\s*\)", "legacy cooldown adapter shares network action state")

        require_slow_expiry_contract(SOURCE)
        require_success_only_action_contract(SOURCE)
        require_movement_lock_action_contract(SOURCE)
        require_damage_pipeline_contract(PIPELINE_SOURCE)
        require_damage_entry_point_contract(SOURCE)
        require_projectile_damage_contract(PROJECTILE_SOURCE)
        require_projectile_spawn_lifecycle_contract(PROJECTILE_SOURCE)
        require_manual_projectile_despawn_contract(SOURCE)
        require_legacy_projectile_initialize_contract(PROJECTILE_SOURCE)
        require_common_source_files(COMMON_SOURCE_FILES)
        require_owned_entity_base_contract()
        require_examples_use_base_infrastructure(EXAMPLE_SOURCES)
        require_projectile_spawn_route(SOURCE, EXAMPLE_SOURCES)
        require_template_override_examples(TEMPLATE_SOURCE)
        require_new_character_docs(README_SOURCE, GUIDE_SOURCE)
        audit_self_test()

        require(r"internal\s+void\s+NotifyProjectileDespawned\s*\(", "projectile despawn base hook")

        fixed_update_start = SOURCE.find("public override void FixedUpdateNetwork")
        fixed_update_end = SOURCE.find("public override void Render", fixed_update_start)
        if fixed_update_start < 0 or fixed_update_end < 0:
            raise AssertionError("FixedUpdateNetwork body")
        body = SOURCE[fixed_update_start:fixed_update_end]
        positions = [body.find(token) for token in ("input.ConsumeTick()", "status.Tick()", "SetMovementSpeedMultiplier", "movement.Tick", "timers.Tick", "HandleActionInputs", "OnPassiveTick")]
        if any(position < 0 for position in positions) or positions != sorted(positions):
            raise AssertionError("FixedUpdateNetwork required order")
    except AssertionError as error:
        print(f"FAIL verify_source_contracts: {error}", file=sys.stderr)
        return 1

    print("PASS verify_source_contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
