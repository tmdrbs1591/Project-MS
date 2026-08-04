# CharacterBase.ProjectIntegration 설명

## 한 줄 요약

`CharacterBase.ProjectIntegration.cs`는 최신 캐릭터 공통 프레임워크와 기존 Project MS 시스템을 연결하는 프로젝트 전용 호환 계층이다.

이 파일은 새 캐릭터 프레임워크 원본에 포함된 파일이 아니다. 최신 프레임워크를 Project MS에 적용하는 과정에서 기존 UI, 카메라, 매치 시스템 및 캐릭터 코드가 계속 동작하도록 별도로 추가했다.

## 왜 필요한가

기존 Project MS 코드는 다음과 같은 `CharacterBase` 기능을 사용하고 있었다.

- `CharacterBase.All`: 현재 생성된 전체 캐릭터 목록
- `CharacterBase.LocalPlayer`: 현재 클라이언트가 조작하는 캐릭터
- `CharacterBase.SetLobbyControlLocked(...)`: 로비에서 캐릭터 입력을 잠그거나 해제하는 기능
- 로컬 캐릭터와 `CooldownHUD`의 자동 연결
- 매치가 전투 상태가 아닐 때 캐릭터 입력을 막는 처리

최신 공통 프레임워크의 `CharacterBase`에는 위 프로젝트 전용 기능이 포함되어 있지 않다. 프레임워크를 그대로 교체하면 카메라, HUD, 매치 관리, 캐릭터 선택 UI 및 CHASER 코드에서 컴파일 오류가 발생한다.

이 기능을 다시 공통 프레임워크 파일 안에 직접 섞으면 다음 프레임워크 업데이트 때 덮어쓰기와 충돌이 반복된다. 그래서 Project MS 전용 코드를 별도 파일로 분리했다.

## partial class 구조

두 파일은 모두 같은 클래스를 구성한다.

```text
Character/Runtime/Core/CharacterBase.cs
    공통 캐릭터 프레임워크

Integration/CharacterBase.ProjectIntegration.cs
    Project MS 전용 연동 기능
```

두 파일의 클래스 선언에는 `partial`이 사용된다.

```csharp
public abstract partial class CharacterBase
```

C# 컴파일러는 두 파일을 하나의 `CharacterBase` 클래스로 합친다. 상속 관계가 하나 더 생기는 것이 아니며, Unity 컴포넌트도 추가되지 않는다.

## 담당 기능

### 1. 전체 캐릭터 목록

```csharp
CharacterBase.All
```

캐릭터가 네트워크에 생성되면 목록에 등록하고, 제거되면 목록에서 해제한다.

현재 다음 시스템이 이 목록을 사용한다.

- 2인 카메라 추적
- 플레이어 HUD
- 매치 승패 및 라운드 초기화
- CHASER의 대상 검색

### 2. 로컬 플레이어 조회

```csharp
CharacterBase.LocalPlayer
```

`All` 목록에서 현재 클라이언트가 입력 권한을 가진 캐릭터를 찾아 반환한다.

캐릭터 선택 UI, 증강 UI, 결과 UI 등이 이 값을 사용한다.

### 3. 로비 입력 잠금

```csharp
CharacterBase.SetLobbyControlLocked(true);
CharacterBase.SetLobbyControlLocked(false);
```

매치메이킹 또는 로비 상태에서 캐릭터가 입력을 받지 않도록 제어한다.

### 4. 매치 상태에 따른 행동 제한

`MatchManager`의 현재 단계가 `MatchPhase.Fighting`이 아니면 이동과 공격 입력을 막는다.

캐릭터 공통 프레임워크의 네트워크 상태 처리는 유지하면서 Project MS의 라운드 진행 규칙만 연결한다.

### 5. CooldownHUD 연결

로컬 캐릭터가 생성되면 다음 연결을 수행한다.

```csharp
CooldownHUD.Instance?.Bind(this);
```

캐릭터가 제거되면 HUD 연결을 해제한다.

## 수정 범위

연동을 위해 공통 `CharacterBase.cs`에는 최소한의 연결 지점만 추가되어 있다.

- 클래스 선언을 `partial`로 변경
- 생성 시 프로젝트 연동 등록
- 제거 시 프로젝트 연동 해제
- 로비 및 매치 입력 잠금 상태 확인
- 로컬 캐릭터의 CooldownHUD 연결

실제 Project MS 전용 구현은 `CharacterBase.ProjectIntegration.cs`에 둔다.

## 삭제하면 발생하는 문제

이 파일만 삭제하면 다음 API가 사라진다.

- `CharacterBase.All`
- `CharacterBase.LocalPlayer`
- `CharacterBase.SetLobbyControlLocked(...)`

그 결과 카메라, UI, 매치 시스템과 CHASER 등에서 컴파일 오류가 발생한다. `CharacterBase.cs`에 남아 있는 연동 호출도 찾을 수 없게 되므로 함께 정리하지 않는 한 삭제하면 안 된다.

## 유지보수 원칙

- 캐릭터별 평타, 스킬, 패시브는 이 파일에 작성하지 않는다.
- Project MS 전체 시스템과 공통 프레임워크를 연결하는 기능만 둔다.
- 최신 캐릭터 프레임워크를 다시 교체할 때 이 파일은 보존한다.
- 새 `CharacterBase.cs`에 `partial` 선언과 연동 호출 지점이 유지되는지 확인한다.
- 공통 프레임워크 자체에 같은 기능이 정식 추가되면 중복 여부를 확인한 뒤 통합한다.

## 현재 검증 상태

- Unity 컴파일 오류 없음
- CHASER 프리팹 필수 구성 정상
- SPARK 프리팹 필수 구성 정상
- BasicChar 테스트 프리팹 필수 구성 정상
- 캐릭터 공통 모듈 테스트 통과
- 프레임워크 소스 계약 검사 통과

Fusion Host/Client 실플레이 검증은 별도로 진행해야 한다.
