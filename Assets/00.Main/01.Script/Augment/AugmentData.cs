using UnityEngine;

/// <summary>
/// 증강 하나의 표시용 데이터(이름/설명/아이콘) + 실제 효과와 연결되는 AugmentType, 그리고
/// 효과 수치. 여러 팩(AugmentPackData)이 같은 AugmentData 에셋을 공유해서 넣을 수 있다.
///
/// [효과 수치가 증강마다 다른 이유]
///   증강 13종의 효과 형태가 제각각(피해%, 체력%, 반경 등)이라 필드 하나로 못 묶는다.
///   그래서 범용 필드 3개(percentValue/secondaryPercentValue/radius)를 두고, 각 증강이
///   자기한테 필요한 것만 쓴다 — CharacterBase의 배율 프로퍼티(AttackMultiplier 등)와
///   투사체/반사 로직이 AugmentType으로 분기해서 맞는 필드를 읽는다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Augment Data", fileName = "NewAugmentData")]
public class AugmentData : ScriptableObject
{
    public AugmentType type;
    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Header("중첩")]
    [Tooltip("최대 중첩 가능 횟수. 1이면 중첩 불가(1회성).")]
    [Min(1)] public int maxStack = 1;

    [Header("효과 수치 (증강마다 의미가 다름 — 설명 참고)")]
    [Tooltip("주 효과 비율(0.3 = 30%). 스택마다 이 값만큼 단순 덧셈으로 누적된다.\n" +
             "예: 대형 탄약집 0.3 → 3스택이면 공격력 +90%.\n" +
             "버서커는 이 값이 '체력 30% 이하일 때 추가되는 공격력 비율'이다.")]
    public float percentValue;

    [Tooltip("보조 효과 비율. 두 번째 수치가 필요한 증강에서만 쓴다.\n" +
             "유리 대포: 최대 체력 감소 비율. 버서커: 발동 체력 임계값(0.3 = 30% 이하).")]
    public float secondaryPercentValue;

    [Tooltip("반사(AUG_013)처럼 범위 판정이 필요한 증강의 반경(월드 단위). 0이면 기본값을 쓴다.")]
    [Min(0f)] public float radius;
}
