using System;
using Fusion;

/// <summary>
/// 권한자가 선택하고 모든 클라이언트가 공유하는 맵 선택 결과.
/// 같은 세션/라운드는 재사용하고, 변경 시 직전 맵을 제외한 후보에서 한 번만 뽑는다.
/// </summary>
public struct RoundMapSelection : INetworkStruct
{
    public NetworkBool HasValue;
    public NetworkString<_256> SessionName;
    public int RoundNumber;
    public int MapIndex;

    public bool TryGetMapIndex(string sessionName, int roundNumber, out int mapIndex)
    {
        mapIndex = -1;
        if (!HasValue || RoundNumber != roundNumber || !SessionName.Equals(sessionName ?? string.Empty))
            return false;

        mapIndex = MapIndex;
        return true;
    }

    /// <summary>StateAuthority에서만 호출한다. 이전 결과 자체를 전달하므로 권한 변경 후에도 재추첨하지 않는다.</summary>
    public RoundMapSelection GetOrSelect(string sessionName, int roundNumber, int mapCount,
        Func<int, int> nextIndex, int previousMapIndex = -1)
    {
        if (mapCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(mapCount));
        if (roundNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(roundNumber));

        if (TryGetMapIndex(sessionName, roundNumber, out _))
            return this;

        int previous = HasValue ? MapIndex : previousMapIndex;
        bool excludePrevious = mapCount > 1 && previous >= 0 && previous < mapCount;
        int candidateCount = excludePrevious ? mapCount - 1 : mapCount;
        int index = candidateCount == 1 ? 0 : nextIndex(candidateCount);
        if (index < 0 || index >= candidateCount)
            throw new ArgumentOutOfRangeException(nameof(nextIndex));
        if (excludePrevious && index >= previous)
            index++;

        return new RoundMapSelection
        {
            HasValue = true,
            SessionName = sessionName ?? string.Empty,
            RoundNumber = roundNumber,
            MapIndex = index
        };
    }
}
