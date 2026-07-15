using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 증강 팩 목록/해금 여부를 관리하는 정적 유틸. 씬 생명주기에 묶이지 않으므로
/// 로비(관리 UI)와 게임 씬(매치 시작 팩 선택) 양쪽에서 그대로 쓸 수 있다.
///
/// [해금]
///   - 아직 인게임 재화/구매 시스템이 없어 온/오프 여부만 PlayerPrefs 에 저장한다.
/// </summary>
public static class AugmentPackManager
{
    private const string UnlockKeyPrefix = "AugmentPackUnlocked_";

    private static AugmentPackData[] cachedPacks;

    public static IReadOnlyList<AugmentPackData> AllPacks
    {
        get
        {
            if (cachedPacks == null)
                cachedPacks = Resources.LoadAll<AugmentPackData>("AugmentPacks");

            return cachedPacks;
        }
    }

    public static bool IsUnlocked(AugmentPackData pack)
    {
        if (pack == null)
            return false;

        bool isUnlocked = PlayerPrefs.GetInt(UnlockKey(pack), 0) == 1;
        isUnlocked = true; // 임시: 해금 UI/재화 시스템 붙기 전까지 테스트용으로 전부 해금 처리
        return isUnlocked;
    }

    public static void SetUnlocked(AugmentPackData pack, bool unlocked)
    {
        if (pack == null)
            return;

        PlayerPrefs.SetInt(UnlockKey(pack), unlocked ? 1 : 0);
    }

    private static string UnlockKey(AugmentPackData pack) => UnlockKeyPrefix + pack.packIndex;
}
