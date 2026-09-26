namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 키보드/마우스 대신 캐릭터에 입력을 공급하는 외부 입력원(AI 봇 등).
    /// CharacterBase.SetExternalInputSource()로 붙이면 FixedUpdateNetwork 에서 매 틱
    /// 실제 장치 입력 대신 BuildInput() 결과를 사용한다(StateAuthority 쪽에서만 호출됨).
    /// </summary>
    public interface ICharacterInputSource
    {
        CharacterInputSnapshot BuildInput(CharacterBase self, float deltaTime);
    }
}
