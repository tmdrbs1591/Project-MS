/// <summary>
/// 증강 선택 폴의 항목 하나. 어느 팩에서 나온 증강인지 UI에 표시하기 위해 출처 팩을 함께 들고 있다.
/// </summary>
public readonly struct AugmentPoolEntry
{
    public readonly AugmentData Data;
    public readonly AugmentPackData SourcePack;

    public AugmentPoolEntry(AugmentData data, AugmentPackData sourcePack)
    {
        Data = data;
        SourcePack = sourcePack;
    }
}
