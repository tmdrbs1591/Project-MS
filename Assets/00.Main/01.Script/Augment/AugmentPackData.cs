using UnityEngine;

/// <summary>
/// 증강 팩 하나를 정의하는 데이터. 매치 시작 시 플레이어가 팩 단위로 고르면,
/// 그 팩에 담긴 증강들이 라운드 종료 후 증강 선택 폴에 들어간다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Augment Pack", fileName = "NewAugmentPack")]
public class AugmentPackData : ScriptableObject
{
    [Tooltip("팩을 구분하는 비트마스크 인덱스(0~31). 팩마다 고유해야 한다.")]
    public int packIndex;

    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    public AugmentData[] augments;
}
