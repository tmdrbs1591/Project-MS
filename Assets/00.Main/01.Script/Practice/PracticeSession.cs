/// <summary>연습 봇의 움직임.</summary>
public enum PracticeDummyMode
{
    Stand,
    Walk,
    Jump,
}

/// <summary>
/// 지금 게임 씬이 연습장으로 열렸는지와 연습 옵션. 로비의 모드 선택에서 "연습"을 고르면
/// NetworkLauncher.StartPractice()가 켜고, 로비로 돌아갈 때(ReturnToLobby) 끈다.
/// 연습장은 항상 오프라인(Single 모드)이라 모든 캐릭터의 권한이 내 쪽에 있다.
/// </summary>
public static class PracticeSession
{
    public static bool IsActive { get; private set; }
    public static PracticeDummyMode DummyMode { get; set; } = PracticeDummyMode.Stand;
    /// <summary>켜면 스킬/대시/궁극기 쿨타임이 매 틱 초기화되고 궁극기 게이지도 가득 찬다.</summary>
    public static bool NoCooldown { get; set; }

    /// <summary>연습 시작. 이미 연습 중(오프라인 ↔ 매칭 세션 전환)이면 봇 움직임/쿨타임 옵션은 그대로 둔다.</summary>
    public static void Begin()
    {
        if (!IsActive)
        {
            DummyMode = PracticeDummyMode.Stand;
            NoCooldown = false;
        }
        IsActive = true;
    }

    public static void End()
    {
        IsActive = false;
        NoCooldown = false;
    }
}
