# 궁극기 게이지 시스템 변경 사항 (공유용)

대상 파일:
- `Assets/00.Main/01.Script/Character/Framework/Runtime/Data/CharacterDefinition.cs`
- `Assets/00.Main/01.Script/Character/Framework/Runtime/Core/CharacterBase.cs`

## 배경

기존엔 게이지형 궁극기(`UltimateUsesGauge`)가 "적에게 준 데미지 비례 충전" 한 가지만
있었고, 게이지가 **라운드가 바뀔 때(그리고 죽는 즉시) 0으로 리셋**되고 있었습니다.
아래 스펙에 맞춰 충전 방식 2개를 추가하고, 라운드 간 게이지 이월이 되도록 고쳤습니다.

| 필드명 | 기존 코드 필드명 | 설명 |
| --- | --- | --- |
| `Approach_Charge_Rate` | `ultimateGaugePerApproachDistance` | 적 방향(좌우, X축) 이동 거리 1당 충전량. 0이면 미적용 |
| `Damage_Charge_Rate` | `ultimateGaugePerDamageDealt` | 적에게 입힌 데미지 1당 충전량 (기존에 이미 있었음) |
| `Taken_Charge_Rate` | `ultimateGaugePerDamageTaken` | 맞은 데미지 1당 충전량. 0이면 미적용 |

## 1. `CharacterDefinition.cs` — 필드 추가

```csharp
[Header("Ultimate Gauge")]
[SerializeField] private bool ultimateUsesGauge;
[Min(1f)] [SerializeField] private float ultimateGaugeMax = 100f;
[Min(0f)] [SerializeField] private float ultimateGaugePerDamageDealt = 1f;
[Min(0f)] [SerializeField] private float ultimateGaugePerDamageTaken = 0f;          // 신규
[Min(0f)] [SerializeField] private float ultimateGaugePerApproachDistance = 0f;      // 신규
```

접근자도 동일하게 추가(`UltimateGaugePerDamageTaken`, `UltimateGaugePerApproachDistance`).
둘 다 **기본값 0 → 미적용**이라 기존 캐릭터 애셋엔 영향 없습니다(값을 안 채워두면 자동으로 0).

## 2. `CharacterBase.cs` — 피격 충전

`AddUltimateGaugeFromDamageDealt`(기존)와 대칭으로 신설, `ApplyDamage()`에서
`applied`(실제로 깎인 체력)만큼 자기 자신의 게이지를 채웁니다.

```csharp
private void AddUltimateGaugeFromDamageTaken(float damage)
{
    if (!HasStateAuthority || !IsUltimateGaugeMode || damage <= 0f)
        return;

    float next = NetUltimateGauge + damage * definition.UltimateGaugePerDamageTaken * UltimateGaugeRateMultiplier;
    NetUltimateGauge = Mathf.Clamp(next, 0f, definition.UltimateGaugeMax);
}
```

호출 위치 (`ApplyDamage(DamageRequest request)` 안, 데미지 확정 직후):
```csharp
ApplyAugmentReflect(applied);
AddUltimateGaugeFromDamageTaken(applied);   // 추가
```

## 3. `CharacterBase.cs` — 접근 이동 충전

`FixedUpdateNetwork()`마다 상대 캐릭터와의 **X축 거리만** 비교해서, 직전 틱보다
가까워진 만큼만 충전합니다(멀어지면 미충전). 히트스턴/사망 등 `gameplayLocked`
상태에서는 충전은 안 되지만, 기준 거리 자체는 계속 갱신해서 락이 풀렸을 때
그동안 벌어진 거리 변화가 한 번에 몰아서 충전되는 걸 방지합니다.

```csharp
private float lastApproachDistanceToEnemy = float.NaN;   // [Networked] 아님, 시뮬레이션 로컬 값

private void TickApproachGauge(bool accruingAllowed)
{
    if (!IsUltimateGaugeMode || definition.UltimateGaugePerApproachDistance <= 0f)
    {
        lastApproachDistanceToEnemy = float.NaN;
        return;
    }

    CharacterBase enemy = All.Find(c => c != null && c != this && c.Object != null);
    if (enemy == null)
    {
        lastApproachDistanceToEnemy = float.NaN;
        return;
    }

    float distance = Mathf.Abs(transform.position.x - enemy.transform.position.x);

    if (accruingAllowed && !float.IsNaN(lastApproachDistanceToEnemy))
    {
        float closedDistance = lastApproachDistanceToEnemy - distance;
        if (closedDistance > 0f)
        {
            float next = NetUltimateGauge + closedDistance * definition.UltimateGaugePerApproachDistance * UltimateGaugeRateMultiplier;
            NetUltimateGauge = Mathf.Clamp(next, 0f, definition.UltimateGaugeMax);
        }
    }

    lastApproachDistanceToEnemy = distance;
}
```

`FixedUpdateNetwork()`에서 호출 위치(`gameplayLocked` 계산 직후, 락 여부와 무관하게 매 틱 실행):
```csharp
NetVelocity = rigidbody2D.linearVelocity;

TickApproachGauge(accruingAllowed: !gameplayLocked);   // 추가

if (!gameplayLocked) { ... }
```

## 4. `CharacterBase.cs` — 라운드 이월 (가장 중요한 동작 변경)

`ResetCommonState()`가 호출되는 곳이 3군데인데, 그중 **라운드 전환**과 **사망 처리**
두 곳에서 게이지까지 0으로 지워버리고 있었습니다. 매치 전체를 통틀어 게이지가 진짜로
0이어야 하는 시점(캐릭터가 처음 스폰될 때/오브젝트가 파괴될 때)만 남기고, 나머지
두 곳은 게이지를 건드리지 않도록 파라미터를 추가했습니다.

```csharp
private void ResetCommonState(bool resetUltimateGauge = true)
{
    ...
    if (resetUltimateGauge)
        NetUltimateGauge = 0f;
    ...
}
```

| 호출부 | 위치 | 인자 | 의미 |
| --- | --- | --- | --- |
| `Spawned()` | 캐릭터 최초 스폰 | 기본값(`true`) | 매치 시작 시 0으로 초기화 |
| `OnDestroy()` | 오브젝트 파괴 | 기본값(`true`) | 어차피 파괴되므로 무관 |
| `ResetCharacter(Vector2)` | **라운드 전환**(`MatchManager`가 호출) | `false` | **게이지 유지** |
| `ApplyDamage()` 사망 분기 | **죽는 순간 즉시** | `false` | **게이지 유지** — 라운드 끝나는 시점에 지워버리면 이월 자체가 성립 안 해서 여기도 같이 고쳐야 했음 |

## 확인/공유 필요 사항

- 캐릭터 애셋(`.asset`) 기준으로 **`ultimateUsesGauge`가 현재 POPUP만 `true`이고
  Gunner/CHASER/SPARK/Zipper는 전부 `false`**로 되어 있습니다. 기획상 전 캐릭터가
  게이지형이라고 들었는데, 애셋 반영이 아직 안 된 건지 확인 부탁드립니다 — 꺼져있는
  캐릭터는 이번에 추가한 접근/피격 충전 로직이 있어도 `IsUltimateGaugeMode`가
  `false`라 아무 효과가 없습니다.
- `ultimateGaugePerApproachDistance` / `ultimateGaugePerDamageTaken` 값은 캐릭터별로
  아직 아무도 안 채워서 전부 0(미적용) 상태입니다 — 밸런스 값 넣어주셔야 실제로
  작동합니다.
- `dotnet build Project-MS.sln` 기준 컴파일 오류 0개 확인.
