# Character Control Effects Design

## Goal

`CharacterBase` 파생 캐릭터가 상대방의 키 배치 기준 입력을 감지하고, 일정 시간 동안 특정 행동을 봉인하거나 조준과 조준선 UI를 방해할 수 있는 공통 API를 제공한다.

## Scope

- 수정 범위는 `CharacterBase` 공통 프레임워크와 개발 가이드로 한정한다.
- SPARK, CHASER, Gunner 등 기존 캐릭터 스크립트와 프리팩은 수정하지 않는다.
- Unity Input System의 실제 키가 아닌 `Jump`, `BasicAttack`, `SkillQ` 같은 매핑된 기능을 기준으로 기록한다.
- 현재 프로젝트의 Fusion Shared Mode 권한 구조를 유지한다.

## Architecture

### Input observation

`CharacterBase` 대상은 매 Fusion 틱에 봉인 적용 전의 입력을 네트워크 상태로 기록한다. 이동 방향과 점프 유지는 현재 값을, 점프·기본 공격·Q·E·대시·궁극기는 누적 순번을 기록한다. 순번을 사용하므로 한 틱의 버튼 입력을 네트워크 표시 프레임 사이에서 놓치지 않는다.

`WasInputPressed(target, type)`는 호출한 캐릭터별로 마지막에 확인한 순번을 보관한다. 처음 관찰할 때는 기존 입력을 새 입력으로 오인하지 않고, 이후 순번이 변했을 때만 `true`를 반환한다.

### Control seals

`CharacterControlType`은 이동, 점프, 기본 공격, Q, E, 대시, 궁극기를 독립적으로 표현하는 플래그다. `AllActions`는 공격과 스킬만, `All`은 이동과 점프까지 포함한다.

각 기능은 독립 `TickTimer`를 가진다. 같은 기능에 여러 봉인이 들어오면 현재 남은 시간보다 긴 시간만 적용해 기존 효과가 짧아지지 않게 한다. 봉인된 입력도 관찰 기록에는 남지만 이동과 행동 실행 직전에서 제거된다.

### Aim effects

조준 반전은 운영체제 마우스 포인터를 움직이지 않고, 캐릭터에서 마우스로 향하는 벡터를 반대로 바꾼다. 조준 각도 변경은 반전을 적용한 뒤 지정한 각도만큼 추가로 회전한다. 변환된 조준점을 `AimDirection`, `AimWorldPosition`, 투사체 발사와 조준 연출이 모두 같이 사용한다.

### Crosshair presentation

`CharacterAimCursorPresenter`는 Input Authority를 가진 로컬 캐릭터에서만 생성된다. 네트워크에는 표시 여부, 시스템 커서 표시 여부, 화면 픽셀 오프셋과 남은 시간만 보관한다. Canvas와 조준선은 로컬 오브젝트이며 네트워크로 생성하지 않는다.

기본 상태에서는 현재 게임의 커서를 변경하지 않는다. `ReplaceCursorWithCrosshair` 또는 개별 API를 사용했을 때만 조준선과 커서 상태를 변경하고, 시간이 끝나면 적용 직전의 로컬 커서 표시 상태를 복구한다.

## Authority and validation

공격자 캐릭터의 State Authority만 방해 API를 요청할 수 있다. 대상이 다른 피어에 있으면 대상의 State Authority로 RPC를 보낸다. 대상은 RPC 발신자, 공격자의 `NetworkId`, 소유자와 팀 정보를 기존 피해·슬로우 검증과 같은 방식으로 검증한다. 자기 자신, 아군, 죽은 대상, 잘못된 시간과 숫자는 무시한다.

## Public API for character authors

```csharp
ApplyControlSeal(target, CharacterControlType.Jump, 2f);
ApplyControlSeal(target, CharacterControlType.AllActions, 3f);
ApplyAimInversion(target, 2f);
ApplyAimAngleOffset(target, 30f, 2f);
SetCrosshairOffset(target, new Vector2(100f, -50f), 2f);
SetCrosshairVisible(target, true, 2f);
SetSystemCursorVisible(target, false, 2f);
ReplaceCursorWithCrosshair(target, 2f);

if (WasInputPressed(target, CharacterInputType.Jump)) { }
float move = GetObservedMoveDirection(target);
bool jumpHeld = IsInputHeld(target, CharacterInputType.Jump);
```

## Reset and failure behavior

- 죽음, 라운드 리셋, 디스폰에서 모든 봉인과 조준·UI 방해 타이머를 초기화한다.
- 0초 이하, NaN, Infinity 시간은 무시한다.
- NaN이나 Infinity 각도·오프셋은 무시한다.
- 조준선 UI를 생성할 수 없어도 게임플레이 방해 효과는 계속 동작한다.

## Verification

- 순수 규칙 테스트: 플래그 확장, 긴 시간 유지, 조준 반전·각도 변환, 입력 필터링.
- Unity 컴파일: 런타임과 Editor 어셈블리 오류 0개.
- Unity EditMode 테스트: 공통 규칙 테스트 전체 통과.
- 2인 Shared 수동 검증: 원격 대상의 입력 기록, 봉인, 발사 방향, 로컬 UI, 효과 종료 복구.

