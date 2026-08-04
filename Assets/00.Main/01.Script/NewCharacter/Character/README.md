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

`StartCooldown(action)`은 수치 파일 또는 `SetCooldownDuration`으로 정한 시간을 사용합니다. `StartCooldown(action, seconds)`은 그 한 번만 직접 지정한 시간으로 시작합니다.

투사체는 캐릭터 코드에서 `CharacterBase.SpawnProjectile`만 호출합니다. `CharacterProjectile.Initialize`을 직접 호출하지 않습니다. 예전 5개 인자 초기화 함수는 발사자 정보를 보장하지 못하므로 일부러 컴파일 오류가 나게 막아 두었습니다.

`OnDamageDealt`는 직접 공격과 발사자 `CharacterBase`를 찾을 수 있는 투사체 공격 모두에서 호출됩니다. `OnProjectileDespawned`의 `reason`은 `HitCharacter`, `HitWall`, `LifetimeExpired`, `Manual` 중 하나입니다.

캐릭터 스크립트에는 `[Networked]`, `[Rpc]`, `Runner.Spawn`을 추가하지 않습니다. 공통 기능으로 만들 수 없는 경우에는 공통 시스템 담당자에게 기능을 요청합니다.

## Unity가 있는 환경에서 실행 확인

- [ ] 행동 함수가 `true`를 돌려준 경우에만 잔탄이 1 감소하고, `false`면 잔탄과 쿨타임이 바뀌지 않는지 확인한다.
- [ ] `SetAutoCooldown(action, false)` 뒤에도 필요한 시점에 `StartCooldown(action)`을 직접 호출해 쿨타임을 시작할 수 있는지 확인한다.
- [ ] `SetMovementEnabled(false)`일 때 수평 이동·점프·자동 점프·새 대시가 막히고 진행 중인 대시는 취소되는지 확인한다. 수평 속도는 즉시 `0`이 되고, 수직 낙하와 중력은 계속 적용되어야 한다.
- [ ] 슬로우는 더 강한 값만 교체하고, 같은 강도는 유지 시간을 새로 시작하며, 약한 값은 현재 슬로우를 바꾸지 않는지 확인한다.
- [ ] 직접 공격과 발사자 `CharacterBase`를 찾은 투사체 공격에서 `OnDamageDealt`가 각각 한 번만 호출되는지 확인한다.
- [ ] 투사체가 캐릭터 명중(`HitCharacter`), 벽 명중(`HitWall`), 시간 종료(`LifetimeExpired`), 수동 종료(`Manual`)될 때 `OnProjectileDespawned`가 각각 한 번만 호출되는지 확인한다.
- [ ] Host, Client, 중간 입장 화면에서 `FacingDirection`, 슬로우 상태, 행동 상태가 같은지 확인한다.

## 주의

이 구현은 현재 Project MS의 `GameMode.Shared`를 기준으로 합니다. Host/Server Mode로 변경할 경우 Fusion의 `INetworkInput` 수집 계층을 추가하고, 캐릭터 스킬 API는 그대로 유지하는 방식으로 확장해야 합니다.
