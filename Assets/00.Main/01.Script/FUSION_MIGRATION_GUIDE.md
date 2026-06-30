# PUN → Fusion 2 (Shared 모드) 마이그레이션 가이드

코드는 모두 Fusion 2 Shared 모드로 전환됐습니다. 아래 **에디터 작업**은 코드로 할 수 없어
직접 해주셔야 동작합니다. 순서대로 진행하세요.

---

## 0. 전제 조건 (가장 먼저)

### 0-1. Fusion Physics 애드온 임포트 ⚠️ 필수
현재 프로젝트엔 Fusion 기본 패키지만 있고 **Physics 애드온이 없습니다.**
이게 없으면 `NetworkRigidbody2D` 가 없어 플레이어/박스 물리 동기화가 안 됩니다.

- Unity 상단 메뉴 **Tools ▸ Fusion ▸ Fusion Hub ▸ Add-ons** 에서 **Physics** 임포트
- 또는 Photon 사이트에서 `Fusion Physics Addon` 다운로드 후 임포트
- 임포트되면 `Fusion.Addons.Physics.NetworkRigidbody2D` 컴포넌트가 생깁니다.

### 0-2. Fusion App Id 설정 (PUN 것과 별개)
- **Tools ▸ Fusion ▸ Fusion Hub ▸ Welcome/App Setup**
- Photon 대시보드에서 **Fusion 타입** 앱을 만들고 App Id 복사 → 입력
- (PUN App Id 와 다릅니다. Fusion 전용 앱을 새로 만드세요.)

### 0-3. PUN 제거 (선택, 권장)
코드에서 PUN 의존은 전부 사라졌습니다. 혼동/충돌 방지를 위해 나중에
`Assets/Photon/PhotonUnityNetworking` 폴더를 삭제해도 됩니다.
(단, 컴파일이 한 번 깨끗이 통과하는 걸 확인한 뒤 삭제하세요.)

---

## 1. NetworkProjectConfig (물리 모드)

**Tools ▸ Fusion ▸ Network Project Config** 에서:
- **Peer Mode**: `Single` (한 프로세스 1세션. 일반적)
- **Physics ▸ Simulation 2D** 가 활성화되도록 Physics 애드온 가이드를 따릅니다.
  (Shared 모드에서 각 권한자가 자기 물체를 시뮬레이션하도록)

> 핵심: Fusion 이 물리를 틱 단위로 돌리므로, 모든 물리 코드는 `FixedUpdateNetwork()` 에 있습니다(이미 처리됨).

---

## 2. 로비 씬 설정

1. 빈 GameObject **"NetworkLauncher"** 생성 → `NetworkLauncher` 스크립트 추가
   (`NetworkRunner` 는 `[RequireComponent]` 로 자동 추가됨)
   - **Player Count**: 2
   - **Game Scene Name**: 게임 씬 이름 (예: `SampleScene 1`) — Build Settings 이름과 정확히 일치
2. 매칭 UI 오브젝트에 `MatchmakingManager` 추가 → **Match Button / Status Text** 연결
3. 포탈 오브젝트: `Portal` + `Is Trigger` 콜라이더 (기존 그대로)
4. **Build Settings 에 로비 씬과 게임 씬을 모두 등록** (File ▸ Build Settings)

> NetworkLauncher 는 `DontDestroyOnLoad` 로 씬 전환에도 유지됩니다(자동).

### 로비의 플레이어 이동 — 같은 `SlimeCharacter` 프리팹 그대로 사용
별도 로비 스크립트는 더 이상 없습니다. `CharacterBase` 가 **세션에 스폰되기 전**(로비)에는
자동으로 **로컬 모드**로 동작해, 네트워크 없이 일반 `Update/FixedUpdate/LateUpdate` 로
이동·비주얼만 구동합니다. 스폰되는 순간 네트워크 모드로 전환됩니다.

- 로비 씬에는 게임용과 **동일한 `SlimeCharacter` 프리팹**을 그대로 하나 놓으면 됩니다.
  - 로비에서는 이동/점프/마우스 팔 조준/통통점프만 동작하고, 전투·쿨다운·발사체·체력 동기화는
    돌지 않습니다(`[Networked]` 미접근).
  - 스폰되지 않은 상태의 `NetworkObject` / `NetworkRigidbody2D` 는 비활성(시뮬레이션 콜백 미호출)
    이라 로컬 이동을 방해하지 않습니다. 만약 로비에서 슬라임이 안 움직이면 `NetworkRigidbody2D`
    가 Rigidbody2D 를 Kinematic 으로 잡고 있는지 확인하세요(드물게 그럴 수 있음).
- 게임 씬으로 넘어가면 이 로비 인스턴스는 (씬 전환으로) 사라지고, `PlayerSpawner` 가
  네트워크 캐릭터를 새로 스폰합니다.

---

## 3. Player 프리팹 설정

기존 PhotonView 를 **제거**하고 다음을 구성:

| 컴포넌트 | 비고 |
|---|---|
| `NetworkObject` | Fusion 핵심. CharacterBase 추가 시 자동 부착됨 |
| `Rigidbody2D` + `Collider2D` | 기존 그대로 |
| `Fusion.Addons.Physics.NetworkRigidbody2D` | **물리 위치 동기화** (애드온, 0-1 필요) |
| `SlimeCharacter` (CharacterBase) | 그대로 |
| `SlimeVisualController` / `SlimeMouseArmController` | 그대로 |
| `PortalInteractor` | 로비에서 쓸 경우 |

- 프리팹은 **Resources 폴더에 둘 필요 없음** (Fusion 은 NetworkProjectConfig 의
  Prefab 목록으로 관리. 프리팹 저장 시 자동 등록됨)
- 기존 PhotonView 의 "Observed Components" 설정은 더 이상 필요 없음

---

## 4. PlayerSpawner (게임 씬)

- 게임 씬에 빈 GameObject + `PlayerSpawner` 추가
- **Player Prefab**: 위 Player 프리팹의 `NetworkObject` 연결
- **Spawn Points**: (선택) 좌/우 스폰 위치. 비우면 PlayerId 로 자동 좌우 분배

---

## 5. 발사체(Projectile) 프리팹

| 컴포넌트 | 비고 |
|---|---|
| `NetworkObject` | |
| `Collider2D` (Is Trigger) | |
| `Projectile` | 그대로 |

- **NetworkTransform 불필요** — 발사체는 시작점/방향/발사틱으로 모든 클라가
  위치를 직접 계산하는 결정론적 방식이라 보간 지연("몇 박자 느림")이 없습니다.
- Rigidbody2D 도 필요 없음 (transform 으로 이동)
- SlimeCharacter 의 **Projectile Prefab** 칸에 이 프리팹 연결

---

## 6. 구조물 / 아이템

### PushableStructure / BreakableStructure (밀 수 있는/부서지는 박스)
| 컴포넌트 | 비고 |
|---|---|
| `NetworkObject` | |
| `Rigidbody2D` (Dynamic) + `Collider2D` | |
| `Fusion.Addons.Physics.NetworkRigidbody2D` | 물리 동기화 (애드온) |
| `PushableStructure` 또는 `BreakableStructure` | 그대로 |

- 씬에 미리 배치하는 경우, Fusion 이 씬 NetworkObject 를 자동 베이크합니다.
  (저장 시 경고가 뜨면 **"Bake"** 실행)
- 플레이어 태그 `"Player"`, 총알 태그 `"Bullet"` 설정 유지

### StaticStructure (정적)
- 네트워크 불필요. `Collider2D` 만. 양쪽 씬에 동일 배치되면 됨. NetworkObject 불필요.

### ItemBase / HealPack
| 컴포넌트 | 비고 |
|---|---|
| `NetworkObject` | |
| `Collider2D` (Is Trigger) | |
| `HealPack` | Visual / Item Collider 칸 연결 |

---

## 7. 동작 원리 요약 (왜 이제 동기화가 맞는지)

- **체력/데미지**: 현재 체력은 `[Networked]` 단일 값. 데미지는 **피격자의 StateAuthority**
  에서만 RPC 로 적용 → 모든 클라가 같은 체력. (예전엔 각자 로컬 계산이라 desync)
- **발사체**: `Runner.Spawn` 으로 네트워크 스폰. 발사자만 충돌/데미지 판정 후 동기 제거.
- **이동**: 권한자만 `FixedUpdateNetwork` 에서 시뮬, `NetworkRigidbody2D` 가 위치 동기화.
  방향/접지 등 시각 상태는 `[Networked]` 로 원격에서도 동일 재현.
- **박스/아이템**: 권한자만 상태 변경(밀기/소비/파괴), 나머지는 RPC 로 요청 → 충돌 결과 일치.
- **팔 조준**: 권한자만 로컬 마우스로 팔을 조준하고, 그 각도/방향을 `[Networked]` 로 동기화.
  원격은 그 값으로 팔을 재현 → 상대 팔이 "내 마우스"를 따라가던 버그 해결.
- **원격 애니메이션**: 속도(velocity)도 `[Networked]` 로 보내 원격의 스쿼시/먼지/공중
  늘어남이 정상 재생됨(예전엔 원격 Rigidbody 가 kinematic 이라 velocity 가 0/부정확했음).

### 부드러움 관련 권장 설정 (에디터)
- **Network Project Config ▸ Simulation ▸ Tick Rate**: 60 권장(프레임레이트와 맞추면 발사체/이동이 더 매끄러움).
- 캐릭터 위치가 원격에서 끊겨 보이면 `NetworkRigidbody2D` 의 **Interpolation Target/보간 설정**을 확인.

---

## 8. 검증 체크리스트

1. 컴파일 통과 (애드온 임포트 후). 에러가 나면 **메시지 그대로 알려주세요** —
   Fusion 버전별 API 차이가 있으면 즉시 맞춰 고치겠습니다.
2. 에디터에서 **Multiplayer Play Mode** (Tools ▸ Fusion ▸ ...) 또는 빌드 2개로 2인 테스트
3. 확인 항목:
   - 두 클라에서 서로 위치가 같게 보이는지
   - 한쪽이 때리면 양쪽에서 같은 체력으로 깎이는지
   - 박스 밀기/부수기/아이템 먹기가 양쪽 동일한지

> ⚠️ 본 코드는 Fusion **2.0.x** API 기준으로 작성했습니다. 설치된 정확한 빌드와
> 시그니처가 다른 부분이 있으면 컴파일 에러 메시지를 알려주세요. 바로 수정합니다.
