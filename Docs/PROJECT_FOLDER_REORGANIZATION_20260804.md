## 한눈에 보기

```text
캐릭터 시스템     01.Script/NewCharacter
               → 01.Script/Character

공통 프레임워크   NewCharacter/Character
               → Character/Framework

캐릭터 스크립트   프리팹·데이터와 혼재
               → Character/Characters/CHASER, SPARK

캐릭터 프리팹     01.Script/NewCharacter/Characters
               → 03.Prefabs/Character/CHASER, SPARK

테스트 프리팹     01.Script/NewCharacter/Test
               → 03.Prefabs/Character/Test

캐릭터 데이터     캐릭터 스크립트 폴더에 혼재
               → 10.Data/Character/CHASER, SPARK, Test

SPARK 물리 재질   SPARK 스크립트 폴더
               → 02.Art/PhysicsMaterials/Character/SPARK

연동 코드         NewCharacter/Integration
               → Character/Integration

이전 비주얼 코드  NewCharacter/LegacyVisual
               → Character/Legacy

캐릭터 문서       Unity Assets 내부
               → Docs/Character
```

## 변경 후 주요 구조

```text
Assets/00.Main/
├─ 01.Script/
│  └─ Character/
│     ├─ Framework/
│     │  ├─ Runtime/
│     │  └─ Editor/
│     ├─ Characters/
│     │  ├─ CHASER/
│     │  └─ SPARK/
│     ├─ Integration/
│     └─ Legacy/
│
├─ 02.Art/
│  └─ PhysicsMaterials/
│     └─ Character/SPARK/
│
├─ 03.Prefabs/
│  └─ Character/
│     ├─ Common/
│     ├─ CHASER/
│     ├─ SPARK/
│     └─ Test/
│
└─ 10.Data/
   └─ Character/
      ├─ CHASER/
      ├─ SPARK/
      └─ Test/

Docs/
└─ Character/
   ├─ CharacterFramework.md
   └─ CharacterBase.ProjectIntegration.md
```

## 이름 오타 수정

```text
Vilige                                  → Village
Untimate                                → Ultimate
ElectricNodeMatarial.physicsMaterial2D  → ElectricNodeMaterial.physicsMaterial2D
09.TimeLine                             → 09.Timeline
```
