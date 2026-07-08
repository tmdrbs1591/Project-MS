/// <summary>
/// 매치 진행 단계를 구분하는 enum이다. MatchManager 의 [Networked] 상태로 동기화된다.
/// </summary>
public enum MatchPhase
{
    /// <summary>전투 중.</summary>
    Fighting,

    /// <summary>한쪽이 죽어 라운드가 막 끝난 직후. 결과 배너를 보여주는 동안의 대기 구간.</summary>
    RoundEnd,

    /// <summary>다음 라운드 전 증강 선택 구간. (증강 시스템은 추후 구현 — 지금은 대기 후 자동 진행)</summary>
    AugmentSelect,

    /// <summary>한쪽이 승수를 다 채워 매치가 끝남. 결과 화면을 띄운 채 대기한다.</summary>
    MatchEnd
}
