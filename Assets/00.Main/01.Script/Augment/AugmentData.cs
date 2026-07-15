using UnityEngine;

/// <summary>
/// 증강 하나의 표시용 데이터(이름/설명/아이콘) + 실제 효과와 연결되는 AugmentType.
/// 여러 팩(AugmentPackData)이 같은 AugmentData 에셋을 공유해서 넣을 수 있다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Augment Data", fileName = "NewAugmentData")]
public class AugmentData : ScriptableObject
{
    public AugmentType type;
    public string title;
    [TextArea] public string description;
    public Sprite icon;
}
