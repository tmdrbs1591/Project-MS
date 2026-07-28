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

## 주의

이 구현은 현재 Project MS의 `GameMode.Shared`를 기준으로 합니다. Host/Server Mode로 변경할 경우 Fusion의 `INetworkInput` 수집 계층을 추가하고, 캐릭터 스킬 API는 그대로 유지하는 방식으로 확장해야 합니다.
