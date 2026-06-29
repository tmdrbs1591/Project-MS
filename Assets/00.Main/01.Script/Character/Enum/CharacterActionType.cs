using System;

/// <summary>
/// 캐릭터 행동 종류를 구분하는 enum이다.
///
/// [역할]
///   - 쿨타임 시스템에서 어떤 행동의 쿨타임인지 구분한다.
///   - CharacterBase가 입력을 읽은 뒤 해당 행동을 실행할 수 있는지 확인할 때 사용한다.
///
/// 새 스킬 버튼이 늘어나면 여기에 행동 종류를 추가하고 쿨타임 데이터도 함께 확장한다.
/// </summary>
public enum CharacterActionType
{
    BasicAttack,
    SkillQ,
    SkillE,
    Dash,
    Ultimate
}
