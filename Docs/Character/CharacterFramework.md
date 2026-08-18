# Project MS Character Framework

Photon Fusion 2 Shared Mode용 공통 캐릭터 프레임워크입니다.

## 목표

- 캐릭터 제작자는 스킬과 패시브만 구현합니다.
- Fusion 권한, RPC, 입력 버퍼링, 공통 비주얼 동기화는 `CharacterBase`가 처리합니다.
- 총, 검, 마법, 범위 공격을 동일한 Base API로 구현할 수 있습니다.
- 비주얼 관련 사용자 컴포넌트는 `CharacterVisualController` 하나만 사용합니다.

## 시작 위치

- 전체 가이드: [CharacterFramework.md](CharacterFramework.md)
- 프로젝트 연동 지점: [CharacterBase.ProjectIntegration.md](CharacterBase.ProjectIntegration.md)
- 신규 캐릭터 템플릿: [CharacterTemplate.cs](../../Assets/00.Main/01.Script/Character/Framework/Runtime/Examples/CharacterTemplate.cs)
- 총 캐릭터 예제: [GunCharacterExample.cs](../../Assets/00.Main/01.Script/Character/Framework/Runtime/Examples/GunCharacterExample.cs)
- 검 캐릭터 예제: [SwordCharacterExample.cs](../../Assets/00.Main/01.Script/Character/Framework/Runtime/Examples/SwordCharacterExample.cs)

## 새 캐릭터 코드에서 쓰는 공통 기능

새 캐릭터는 `CharacterBase`를 상속하고, 스킬 함수와 필요한 이벤트만 `override`합니다. 행동 사용 가능 여부, 잔탄, 쿨타임, 이동 제한, 슬로우, 타이머, 방향, 투사체 네트워크 처리는 `CharacterBase`가 이미 구현합니다.

- 행동 설정: `SetActionEnabled`, `IsActionEnabled`, `SetActionCharges`, `AddActionCharges`, `GetActionCharges`, `SetCooldownDuration`, `ResetCooldownDuration`, `SetAutoCooldown`, `StartCooldown`, `ClearCooldown`, `GetCooldownRemaining`
- 이동과 슬로우: `SetMovementEnabled`, `IsMovementEnabled`, `ApplySlow`, `SlowRatio`, `IsSlowed`
- 시간과 방향: `ScheduleTimer`, `CancelTimer`, `FacingDirection`, `IsFacingRight`, `IsFacingLeft`, `IsBehindTarget`
- 공격과 투사체: `SpawnProjectile`, `SpawnThrowable`, `StartThrowableFuse`, `DespawnProjectile`, `ModifyOutgoingDamage`, `OnDamageDealt`, `OnProjectileDespawned`
- 캐릭터 소유 오브젝트: `SpawnOwnedEntity`, `DestroyOwnedEntity`, `GetOwnedEntities`, `DestroyOwnedEntities`
- 캐릭터와 소유 오브젝트 통합 공격 조회: `FindDamageablesInCircle`, `FindDamageablesInBox`, `FindDamageablesInLine`, `FindDamageablesInArc`

`StartCooldown(action)`은 수치 파일 또는 `SetCooldownDuration`으로 정한 시간을 사용합니다. `StartCooldown(action, seconds)`은 그 한 번만 직접 지정한 시간으로 시작합니다.

투사체는 캐릭터 코드에서 `CharacterBase.SpawnProjectile`만 호출합니다. `CharacterProjectile.Initialize`을 직접 호출하지 않습니다. 예전 5개 인자 초기화 함수는 발사자 정보를 보장하지 못하므로 일부러 컴파일 오류가 나게 막아 두었습니다.

다른 캐릭터의 현재 조준 방향은 공개 읽기 전용 `CharacterBase.AimDirection`으로 확인합니다. 이 값은 동기화된 `NetAimAngle`에서 계산되므로 상대 캐릭터의 백어택 판정에도 사용할 수 있습니다. 상대 입력이나 네트워크 필드를 직접 읽지 않습니다.

직접 공격과 발사자를 확인할 수 있는 투사체 공격은 모두 `OnDamageDealt`로 결과를 전달합니다. 캐릭터 스크립트에서는 `Runner.Spawn` 대신 `CharacterBase.SpawnProjectile`을 사용하며, 발사자 정보가 없는 예전 5개 인자 초기화 함수는 사용하지 않습니다. 투사체 제거 사유는 `HitCharacter`, `HitOwnedEntity`, `HitWall`, `LifetimeExpired`, `Manual`로 구분합니다.

캐릭터 고유 상태에 `[Networked]` 또는 `[Rpc]`가 필요해 보이면 먼저 공통 API로 구현할 수 있는지 확인합니다. 꼭 필요한 경우에만 공통 시스템 담당자 검토 후 최소 범위로 추가합니다.

## 설치물·소환체·물리 투척체 제작 순서

`CharacterOwnedEntity`는 캐릭터 스킬이 전투 중 생성하는 오브젝트의 공통 기반입니다. 맵에 원래 배치되는 `StructureBase`와는 별개입니다.

공통 스폰 오브젝트 스크립트는 역할에 따라 다음 폴더에 둡니다.

```text
Runtime/SpawnedObjects
├─ CharacterOwnedEntity.cs
├─ CharacterOwnedEntityRegistry.cs
├─ OwnedEntityGroupId.cs
├─ OwnedEntityPolicies.cs
├─ OwnedEntitySpawnRequest.cs
├─ OwnedEntitySpawnResult.cs
├─ Deployables/
├─ Summons/
└─ Throwables/
```

- `CharacterDeployable`: 노드, 지뢰, 장판, 포탑처럼 필드에 설치하는 오브젝트
- `CharacterSummon`: 펫, 분신, 드론처럼 스스로 이동하거나 행동하는 소환체
- `CharacterThrowable`: 수류탄, 섬광탄, 연막탄처럼 중력과 충돌을 사용하는 투척체

캐릭터 제작자는 Fusion의 생성·제거·권한 코드를 직접 작성하지 않습니다. `CharacterBase`의 API를 통해서만 생성하고 제거합니다.

1. 만들 오브젝트 종류에 맞는 기반 클래스를 고릅니다.
2. 프리팹 루트에 `NetworkObject`와 해당 파생 스크립트를 추가합니다.
3. Inspector에서 HP, 제한시간, 충돌 레이어를 설정합니다.
4. 캐릭터 스킬 함수에서 `SpawnOwnedEntity` 또는 `SpawnThrowable`을 호출합니다.
5. 공격, 연결, 이동, 폭발 같은 고유 동작은 프리팹의 파생 스크립트에 작성합니다.
6. 프리팹 검증 후 2인 Shared 환경에서 생성·동작·제거를 확인합니다.

### 설치물 예제: SPARK Q 형태의 전기 노드

이 예제는 노드를 던져 생성하고 최대 2개를 유지하는 부분만 보여 줍니다. 두 노드 연결과 선 데미지는 노드 스크립트에 별도로 추가합니다.

프리팹 루트에는 다음 컴포넌트를 둡니다.

- `NetworkObject`
- `SparkQNode : CharacterDeployable`
- Dynamic `Rigidbody2D`
- Trigger가 아닌 `Collider2D`
- Fusion Physics Addon의 `NetworkRigidbody2D`
- 외형 오브젝트

```csharp
[SerializeField] private SparkQNode nodePrefab;
[SerializeField] private float nodeThrowSpeed = 10f;

protected override bool OnSkillQ(CharacterActionContext context)
{
    SparkQNode node = SpawnOwnedEntity(
        nodePrefab,
        context.Action,
        ProjectileOrigin.position,
        maxCount: 2,
        initialVelocity: context.AimDirection * nodeThrowSpeed);

    return node != null;
}
```

`maxCount: 2`이면 세 번째 노드를 만들 때 가장 오래된 노드가 제거됩니다. `context.Action`을 전달하면 Q 스킬로 만든 오브젝트끼리 자동으로 묶이므로 별도 그룹 번호를 만들 필요가 없습니다.

```csharp
public sealed class SparkQNode : CharacterDeployable
{
}
```

### 설치물 예제: 일정 시간마다 공격하는 포탑

캐릭터는 포탑을 생성하고, 탐지와 공격은 포탑 스크립트가 처리합니다. 포탑 프리팹 루트에는 `NetworkObject`, `SimpleTurret`, 피격용 Collider를 둡니다.

```csharp
[SerializeField] private SimpleTurret turretPrefab;

protected override bool OnSkillE(CharacterActionContext context)
{
    SimpleTurret turret = SpawnOwnedEntity(
        turretPrefab,
        context.Action,
        context.AimWorldPosition);

    return turret != null;
}
```

```csharp
public sealed class SimpleTurret : CharacterDeployable
{
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackRadius = 4f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask targetLayer;

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!TryUseInterval(attackInterval))
            return;

        IDamageable target = FindFirstDamageableInCircle(
            transform.position,
            attackRadius,
            targetLayer);

        if (target != null)
            DealDamage(target, damage);
    }
}
```

`TryUseInterval`은 State Authority에서 공격 간격이 지났을 때만 `true`를 반환합니다. `FindFirstDamageableInCircle`은 범위 안의 첫 공격 대상을 찾고, `DealDamage`는 소유 캐릭터의 공통 피해 처리 경로를 사용합니다.

### 소환체 생성

소환체 프리팹은 `CharacterSummon` 파생 스크립트를 사용합니다. 생성 코드는 설치물과 같고, 이동·추적·공격은 소환체 스크립트에 작성합니다.

```csharp
CharacterSummon summon = SpawnOwnedEntity(
    summonPrefab,
    context.Action,
    context.AimWorldPosition,
    maxCount: 1);

return summon != null;
```

### 물리 투척체 생성

직선 총알은 `CharacterProjectile`, 중력과 충돌이 필요한 수류탄·섬광탄·연막탄은 `CharacterThrowable`을 사용합니다.

```csharp
MyFlashThrowable flash = SpawnThrowable(
    flashPrefab,
    context.Action,
    ProjectileOrigin.position,
    context.AimDirection,
    throwSpeed,
    maxCount: 1);

return flash != null;
```

투척체 프리팹 루트에는 다음 컴포넌트를 둡니다.

- `NetworkObject`
- `CharacterThrowable` 파생 컴포넌트
- Dynamic `Rigidbody2D`
- Trigger가 아닌 `Collider2D`
- Fusion Physics Addon의 `NetworkRigidbody2D`

초기 속도는 State Authority의 `Rigidbody2D.linearVelocity`에 한 번 적용됩니다. 이후 포물선, 낙하, 바운스와 구름은 Rigidbody2D와 Collider2D가 처리하고 `NetworkRigidbody2D`가 위치와 회전을 동기화합니다. `NetworkTransform`을 함께 붙이지 않습니다.

`CharacterThrowable` Inspector에서 퓨즈 시작 방식을 설정합니다.

- `OnSpawn`: 생성 직후 시작
- `OnGroundContact`: `Ground Layer`에 처음 접촉한 뒤 시작
- `Manual`: `StartThrowableFuse(throwable)`을 호출할 때 시작

`Fuse Seconds`가 지나면 `OnFuseExpiredAuthority`가 한 번 호출됩니다. 폭발, 섬광, 연막 효과는 투척체 파생 클래스에서 이 함수를 재정의해 구현합니다. `OnGroundContact`를 사용하면 `Ground Layer`를 반드시 설정합니다.

### 생성한 오브젝트 조회와 제거

같은 스킬로 만든 오브젝트는 행동 종류로 조회하거나 한 번에 제거할 수 있습니다.

```csharp
IReadOnlyList<SparkQNode> nodes =
    GetOwnedEntities<SparkQNode>(CharacterActionType.SkillQ);

if (nodes.Count > 0)
    DestroyOwnedEntity(nodes[0]);

DestroyOwnedEntities(CharacterActionType.SkillQ);
```

모든 제거는 공통 파괴 경로를 통과합니다. 캐릭터 스크립트에서 `Runner.Spawn`과 `Runner.Despawn`을 직접 호출하지 않습니다.

## 상세 설정

### 개수 제한

- `RejectNew`: 제한에 도달하면 새 생성을 거부합니다. 기본 정책입니다.
- `DestroyOldest`: 가장 오래된 오브젝트를 제거한 뒤 생성합니다.
- `DestroyNewest`: 가장 최근 오브젝트를 제거한 뒤 생성합니다.
- `Unlimited`: 개수 제한을 사용하지 않습니다.

기본 생성 함수는 `replaceOldest: true`이므로 제한을 넘으면 가장 오래된 오브젝트를 교체합니다. 제한에 도달했을 때 생성을 실패시키려면 `replaceOldest: false`를 전달합니다.

한 행동에서 서로 다른 오브젝트 그룹을 따로 관리하거나 세부 실패 이유가 필요할 때만 `OwnedEntityGroupId`, `OwnedEntitySpawnRequest`, `OwnedEntitySpawnResult`를 사용하는 상세 생성 함수를 사용합니다.

### HP와 제한시간

`CharacterOwnedEntity` Inspector에서 생존 방식을 선택합니다.

- `Manual`: 스킬 또는 소유자 정책으로만 제거
- `Health`: HP가 0이 되면 제거
- `Duration`: 제한시간이 끝나면 제거
- `HealthOrDuration`: HP 소진과 시간 만료 중 먼저 발생한 조건으로 제거

같은 네트워크 틱에 HP 소진과 시간 만료가 함께 확인되면 `HealthDepleted`가 우선합니다. 시간은 클라이언트의 `Time.time`이 아니라 Fusion의 `TickTimer`로 판정합니다.

파괴 사유는 `HealthDepleted`, `LifetimeExpired`, `FuseExpired`, `LimitExceeded`, `OwnerDied`, `OwnerDespawned`, `OwnerDisconnected`, `SkillTriggered`, `Manual`로 구분됩니다. 캐릭터별 후속 효과는 직접 디스폰하지 말고 파괴 사유를 기준으로 처리합니다.

소유 캐릭터도 `OnOwnedEntityDestroyed(CharacterOwnedEntity entity, OwnedEntityDestroyReason reason)`에서 공통 파괴 결과를 받을 수 있습니다. 충전 반환이나 스킬 상태 정리는 이 훅에서 처리합니다.

### 피해 처리

캐릭터와 캐릭터 소유 오브젝트는 모두 `IDamageable`로 조회할 수 있습니다. 소유 오브젝트의 기본 피해 정책은 다음과 같습니다.

- 적 피해 허용
- 자가 피해 차단
- 아군 피해 차단

자가 피해와 아군 피해는 각 프리팹의 `Allow Self Damage`, `Allow Friendly Damage` 설정으로 개별 허용할 수 있습니다. 실제 HP 변경과 파괴는 해당 오브젝트의 State Authority에서 한 번만 처리됩니다.

투사체의 `targetLayer`에는 피해를 받을 설치물 레이어도 포함해야 합니다. 투사체는 `IDamageable`을 찾아 `DamageRequest`를 전달하며, 설치물 명중 시 `ProjectileDespawnReason.HitOwnedEntity`를 사용합니다.

근접기와 범위기는 통합 조회 API를 사용합니다.

```csharp
foreach (IDamageable target in FindDamageablesInCircle(
             context.Origin,
             3f,
             targetLayer))
{
    DealDamage(target, context.Damage, CharacterDamageSource.Area);
}
```

`DamageRequest`에는 공격자, 공격 오브젝트, 팀, 피해 종류, 선택적인 스킬 ID와 충돌 정보가 포함됩니다. 설치물에서 다음 훅을 필요한 만큼만 재정의할 수 있습니다.

- `ModifyIncomingDamage`: 방어력 또는 특정 피해 면역
- `OnDamageReceived`: 피격 효과
- `OnOwnedEntityHealthChanged`: 체력 UI 또는 상태 표시
- `OnOwnedEntityDestroyed`: 파괴 사유별 후속 효과

훅 안에서 네트워크 오브젝트를 직접 제거하지 않습니다.

공격 캐릭터는 State Authority가 확정한 실제 적용 피해를 `OnOwnedEntityDamageDealt(NetworkId targetId, float appliedDamage, CharacterDamageSource source)`에서 받습니다. 대상이 치명 피해로 먼저 디스폰되어도 `NetworkId` 기반 훅은 호출됩니다. 살아 있는 대상 컴포넌트가 필요한 경우에만 `OnDamageableDealt`를 사용하며, 디스폰된 대상에는 호출되지 않을 수 있습니다.

캐릭터 대상의 `OnDamageDealt`와 `OnDamageableDealt`도 요청량이 아니라 State Authority가 확정한 실제 적용량으로 호출됩니다. `OnDamaged(CharacterDamageInfo)`에서는 팀, 피해 종류, 스킬 ID, 충돌 위치·방향까지 읽을 수 있습니다.

원격 파괴 연출이 필요한 소유 오브젝트는 `OnOwnedEntityDestroyedRendered`를 재정의합니다. BASE가 파괴 상태를 한 네트워크 틱 동기화한 뒤 디스폰하므로, 파괴 직전에 보낸 오브젝트 RPC에 의존하지 않습니다.

### 소유자와 팀

기본 팀 ID는 `PlayerRef.PlayerId`이므로 현재 1대1 규칙에서는 각 플레이어가 서로 다른 팀입니다. 팀전이 추가되면 캐릭터에서 `ResolveDamageTeamId`만 재정의하고, 설치물 및 투사체 API는 변경하지 않습니다.

소유자가 사라질 때의 정책은 다음과 같습니다.

- `Destroy`: Shared Mode 마스터가 State Authority를 인수한 뒤 `OwnerDisconnected` 사유로 제거
- `ExpireNormally`: 마스터가 State Authority를 인수하고 제한시간 또는 자동 시작 퓨즈가 끝날 때까지 유지. 종료 조건이 없는 `Manual` 오브젝트와 수동 퓨즈는 생성 거부
- `TransferStateAuthority`: 향후 권한 모델용 예약 값이며 현재는 생성 실패를 반환

### 프리팹 체크리스트

- [ ] 루트에 `NetworkObject`가 있는가?
- [ ] `CharacterDeployable` 또는 파생 클래스가 있는가?
- [ ] 피격이 필요하면 Collider와 대상 레이어가 설정되어 있는가?
- [ ] HP, 제한시간, 자가/아군 피해 정책이 의도와 맞는가?
- [ ] 생성 캐릭터가 `SpawnOwnedEntity`만 사용하는가?
- [ ] 캐릭터가 `GetOwnedEntities`로 그룹을 조회하는가?
- [ ] 파괴가 `DestroyOwnedEntity` 또는 오브젝트의 공통 파괴 경로를 통과하는가?

## 향후 확장 가이드

새 기능은 `CharacterOwnedEntity`에 모두 추가하지 말고, 필요한 능력을 작은 컴포넌트나 인터페이스로 조합합니다.

- 지뢰: `CharacterDeployable`을 상속하고 트리거 감지만 추가합니다. 소유권, HP, 시간, 파괴는 그대로 재사용합니다.
- 수류탄·섬광탄·연막탄: `CharacterThrowable`을 상속하고 `OnFuseExpiredAuthority`에 효과만 추가합니다. 투척, 물리, 퓨즈, 소유권은 그대로 재사용합니다.
- 장판: `Duration`을 사용하고 HP를 사용하지 않습니다. 주기 효과는 별도 컴포넌트로 둡니다.
- 포탑: `CharacterDeployable`에 타게팅과 공격 컴포넌트를 조합합니다. 타게팅 코드를 BASE에 넣지 않습니다.
- 펫·분신·AI 소환수: `CharacterSummon`에서 시작하고 이동, 타게팅, 공격을 각각 독립 모듈로 추가합니다.
- 방벽·회복 대상: 음수 피해로 회복시키지 말고 `IHealable` 같은 명시적인 능력 계약을 추가합니다.
- 팀전: `ResolveDamageTeamId`를 실제 팀 시스템에 연결합니다. 자가/아군/적 피해 판정 코드는 유지합니다.
- Host/Server Mode: 캐릭터용 API는 유지하고 BASE 내부의 요청 전달과 권한 처리만 교체합니다.
- 권한 이전: `TransferStateAuthority`의 소유자 이탈·재접속·중간 입장 테스트를 완성한 뒤 예약 정책을 활성화합니다.
- 안정적인 계정 소유권: 재접속 후에도 영구 소환물을 유지해야 할 때는 재사용될 수 있는 `PlayerRef` 대신 백엔드의 고유 사용자 ID 계약을 추가합니다.
- Shared Mode 보안 강화: 경쟁 환경에서 임의 권한 요청을 신뢰할 수 없다면 공용 API는 유지하고 소유 오브젝트를 Master/Server Authority로 이전합니다.

확장 기능을 추가할 때는 기존에 재사용하는 계약, 새로 추가하는 한 가지 능력, 변경하지 않아야 하는 BASE 경계를 개발 문서에 함께 기록합니다.

캐릭터별 조준 모드·변신·연속기 상태는 `OnResetCharacter`에서 반드시 초기화하고, 외부 이벤트 구독은 `OnCharacterDespawned`에서 해제합니다. 공통 리셋은 쿨타임·타이머·슬로우·이동·게임플레이 잠금을 초기화하지만 파생 클래스의 필드는 추측해서 지우지 않습니다.

## Unity가 있는 환경에서 실행 확인

- [ ] 행동 함수가 `true`를 돌려준 경우에만 잔탄이 1 감소하고, `false`면 잔탄과 쿨타임이 바뀌지 않는지 확인한다.
- [ ] `SetAutoCooldown(action, false)` 뒤에도 필요한 시점에 `StartCooldown(action)`을 직접 호출해 쿨타임을 시작할 수 있는지 확인한다.
- [ ] `SetMovementEnabled(false)`일 때 수평 이동·점프·자동 점프·새 대시가 막히고 진행 중인 대시는 취소되는지 확인한다. 수평 속도는 즉시 `0`이 되고, 수직 낙하와 중력은 계속 적용되어야 한다.
- [ ] 슬로우는 더 강한 값만 교체하고, 같은 강도는 유지 시간을 새로 시작하며, 약한 값은 현재 슬로우를 바꾸지 않는지 확인한다.
- [ ] 직접 공격과 발사자 `CharacterBase`를 찾은 투사체 공격에서 `OnDamageDealt`가 각각 한 번만 호출되는지 확인한다.
- [ ] 투사체가 캐릭터 명중(`HitCharacter`), 벽 명중(`HitWall`), 시간 종료(`LifetimeExpired`), 수동 종료(`Manual`)될 때 `OnProjectileDespawned`가 각각 한 번만 호출되는지 확인한다.
- [ ] 투사체가 캐릭터 소유 오브젝트에 명중할 때 `HitOwnedEntity`가 한 번만 발생하고 HP가 State Authority에서 한 번만 감소하는지 확인한다.
- [ ] 소유 오브젝트의 HP 소진, 제한시간 만료, 개수 초과, 소유자 사망·퇴장이 각각 올바른 파괴 사유로 한 번만 처리되는지 확인한다.
- [ ] Host, Client, 중간 입장 화면에서 `FacingDirection`, 슬로우 상태, 행동 상태가 같은지 확인한다.

## 주의

이 구현은 현재 Project MS의 `GameMode.Shared`를 기준으로 합니다. Host/Server Mode로 변경할 경우 Fusion의 `INetworkInput` 수집 계층을 추가하고, 캐릭터 스킬 API는 그대로 유지하는 방식으로 확장해야 합니다.
