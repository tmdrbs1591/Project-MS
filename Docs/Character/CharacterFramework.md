# Project MS Character Framework

Photon Fusion 2 Shared Mode용 공통 캐릭터 프레임워크입니다.

## 목표

- 캐릭터 제작자는 스킬과 패시브만 구현합니다.
- Fusion 권한, RPC, 입력 버퍼링, 공통 비주얼 동기화는 `CharacterBase`가 처리합니다.
- 총, 검, 마법, 범위 공격을 동일한 Base API로 구현할 수 있습니다.
- 비주얼 관련 사용자 컴포넌트는 `CharacterVisualController` 하나만 사용합니다.

## 시작 위치

- 전체 가이드: `Docs/캐릭터_개발_가이드.pdf`
- Markdown 원문: `Docs/캐릭터_개발_가이드.md`
- 신규 캐릭터 템플릿: `Runtime/Examples/CharacterTemplate.cs`
- 총 캐릭터 예제: `Runtime/Examples/GunCharacterExample.cs`
- 검 캐릭터 예제: `Runtime/Examples/SwordCharacterExample.cs`

## 새 캐릭터 코드에서 쓰는 공통 기능

새 캐릭터는 `CharacterBase`를 상속하고, 스킬 함수와 필요한 이벤트만 `override`합니다. 행동 사용 가능 여부, 잔탄, 쿨타임, 이동 제한, 슬로우, 타이머, 방향, 투사체 네트워크 처리는 `CharacterBase`가 이미 구현합니다.

- 행동 설정: `SetActionEnabled`, `IsActionEnabled`, `SetActionCharges`, `AddActionCharges`, `GetActionCharges`, `SetCooldownDuration`, `ResetCooldownDuration`, `SetAutoCooldown`, `StartCooldown`, `ClearCooldown`, `GetCooldownRemaining`
- 이동과 슬로우: `SetMovementEnabled`, `IsMovementEnabled`, `ApplySlow`, `SlowRatio`, `IsSlowed`
- 시간과 방향: `ScheduleTimer`, `CancelTimer`, `FacingDirection`, `IsFacingRight`, `IsFacingLeft`, `IsBehindTarget`
- 공격과 투사체: `SpawnProjectile`, `DespawnProjectile`, `ModifyOutgoingDamage`, `OnDamageDealt`, `OnProjectileDespawned`
- 캐릭터 소유 오브젝트: `SpawnOwnedEntity`, `DestroyOwnedEntity`, `GetOwnedEntities`, `DestroyOwnedEntities`
- 캐릭터와 소유 오브젝트 통합 공격 조회: `FindDamageablesInCircle`, `FindDamageablesInBox`, `FindDamageablesInLine`, `FindDamageablesInArc`

`StartCooldown(action)`은 수치 파일 또는 `SetCooldownDuration`으로 정한 시간을 사용합니다. `StartCooldown(action, seconds)`은 그 한 번만 직접 지정한 시간으로 시작합니다.

투사체는 캐릭터 코드에서 `CharacterBase.SpawnProjectile`만 호출합니다. `CharacterProjectile.Initialize`을 직접 호출하지 않습니다. 예전 5개 인자 초기화 함수는 발사자 정보를 보장하지 못하므로 일부러 컴파일 오류가 나게 막아 두었습니다.

`OnDamageDealt`는 직접 공격과 발사자 `CharacterBase`를 찾을 수 있는 투사체 공격 모두에서 호출됩니다. `OnProjectileDespawned`의 `reason`은 `HitCharacter`, `HitOwnedEntity`, `HitWall`, `LifetimeExpired`, `Manual` 중 하나입니다.

캐릭터 스크립트에는 `[Networked]`, `[Rpc]`, `Runner.Spawn`을 추가하지 않습니다. 공통 기능으로 만들 수 없는 경우에는 공통 시스템 담당자에게 기능을 요청합니다.

## 캐릭터 소유 오브젝트 API

`CharacterOwnedEntity`는 캐릭터 스킬이 전투 중 생성하는 오브젝트의 공통 기반입니다. 맵에 원래 배치되는 `StructureBase`와는 별개입니다.

- `CharacterDeployable`: 노드, 지뢰, 장판, 포탑처럼 필드에 설치하는 오브젝트
- `CharacterSummon`: 향후 펫, 분신, AI 소환수를 위한 확장 기반

캐릭터 제작자는 Fusion의 생성·제거·권한 코드를 직접 작성하지 않습니다. `CharacterBase`의 API를 통해서만 생성하고 제거합니다.

```csharp
private static readonly OwnedEntityGroupId NodeGroup = new OwnedEntityGroupId(1);

[SerializeField] private CharacterDeployable nodePrefab;

protected override bool OnSkillQ(CharacterActionContext context)
{
    OwnedEntitySpawnRequest request = new OwnedEntitySpawnRequest(
        context.Origin,
        Quaternion.identity,
        NodeGroup,
        maxCount: 2,
        overflowPolicy: OwnedEntityOverflowPolicy.DestroyOldest,
        ownerExitPolicy: OwnedEntityOwnerExitPolicy.Destroy,
        initialVelocity: context.AimDirection * 10f);

    OwnedEntitySpawnResult<CharacterDeployable> result =
        SpawnOwnedEntity(nodePrefab, request);
    return result.Success;
}
```

그룹 ID는 한 캐릭터 안에서 스킬별 소유 오브젝트를 구분하는 양의 정수입니다. 오브젝트 이름이나 `Runner.GetAllNetworkObjects()` 검색으로 소유물을 찾지 않습니다.

### 개수 제한

- `RejectNew`: 제한에 도달하면 새 생성을 거부합니다. 기본 정책입니다.
- `DestroyOldest`: 가장 오래된 오브젝트를 제거한 뒤 생성합니다.
- `DestroyNewest`: 가장 최근 오브젝트를 제거한 뒤 생성합니다.
- `Unlimited`: 개수 제한을 사용하지 않습니다.

```csharp
IReadOnlyList<CharacterDeployable> nodes =
    GetOwnedEntities<CharacterDeployable>(NodeGroup);

DestroyOwnedEntity(nodes[0], OwnedEntityDestroyReason.SkillTriggered);
DestroyOwnedEntities(NodeGroup, OwnedEntityDestroyReason.Manual);
```

모든 제거는 공통 파괴 경로를 통과합니다. `Runner.Despawn`을 직접 호출하지 않습니다.

### HP와 제한시간

`CharacterOwnedEntity` Inspector에서 생존 방식을 선택합니다.

- `Manual`: 스킬 또는 소유자 정책으로만 제거
- `Health`: HP가 0이 되면 제거
- `Duration`: 제한시간이 끝나면 제거
- `HealthOrDuration`: HP 소진과 시간 만료 중 먼저 발생한 조건으로 제거

같은 네트워크 틱에 HP 소진과 시간 만료가 함께 확인되면 `HealthDepleted`가 우선합니다. 시간은 클라이언트의 `Time.time`이 아니라 Fusion의 `TickTimer`로 판정합니다.

파괴 사유는 `HealthDepleted`, `LifetimeExpired`, `LimitExceeded`, `OwnerDied`, `OwnerDespawned`, `OwnerDisconnected`, `SkillTriggered`, `Manual`로 구분됩니다. 캐릭터별 후속 효과는 직접 디스폰하지 말고 파괴 사유를 기준으로 처리합니다.

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

### 소유자와 팀

기본 팀 ID는 `PlayerRef.PlayerId`이므로 현재 1대1 규칙에서는 각 플레이어가 서로 다른 팀입니다. 팀전이 추가되면 캐릭터에서 `ResolveDamageTeamId`만 재정의하고, 설치물 및 투사체 API는 변경하지 않습니다.

소유자가 사라질 때의 정책은 다음과 같습니다.

- `Destroy`: Shared Mode 마스터가 State Authority를 인수한 뒤 `OwnerDisconnected` 사유로 제거
- `ExpireNormally`: 마스터가 State Authority를 인수하고 기존 `TickTimer`의 남은 제한시간 동안 유지
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
- 장판: `Duration`을 사용하고 HP를 사용하지 않습니다. 주기 효과는 별도 컴포넌트로 둡니다.
- 포탑: `CharacterDeployable`에 타게팅과 공격 컴포넌트를 조합합니다. 타게팅 코드를 BASE에 넣지 않습니다.
- 펫·분신·AI 소환수: `CharacterSummon`에서 시작하고 이동, 타게팅, 공격을 각각 독립 모듈로 추가합니다.
- 방벽·회복 대상: 음수 피해로 회복시키지 말고 `IHealable` 같은 명시적인 능력 계약을 추가합니다.
- 팀전: `ResolveDamageTeamId`를 실제 팀 시스템에 연결합니다. 자가/아군/적 피해 판정 코드는 유지합니다.
- Host/Server Mode: 캐릭터용 API는 유지하고 BASE 내부의 요청 전달과 권한 처리만 교체합니다.
- 권한 이전: `TransferStateAuthority`의 소유자 이탈·재접속·중간 입장 테스트를 완성한 뒤 예약 정책을 활성화합니다.

확장 기능을 추가할 때는 기존에 재사용하는 계약, 새로 추가하는 한 가지 능력, 변경하지 않아야 하는 BASE 경계를 개발 문서에 함께 기록합니다.

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
