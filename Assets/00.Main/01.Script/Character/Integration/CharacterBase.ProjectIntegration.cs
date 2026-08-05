using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// Project-MS-specific integration kept separate from the reusable character framework.
    /// </summary>
    public abstract partial class CharacterBase
    {
        // 증강 종류당 1비트. AugmentType이 32개를 넘어가면 long으로 바꿔야 한다.
        [Networked] private int NetAugmentFlags { get; set; }

        // 이번 AugmentSelect 구간에서 아직 고르지 못한 픽 수(승자 1 / 패자 2). MatchManager가
        // Rpc_BeginAugmentSelect로 배정하고, AugmentSelectUI가 이 값을 폴링해 선택 UI를 띄운다.
        [Networked] private int NetAugmentPicksRemaining { get; set; }

        public int AugmentPicksRemaining => NetAugmentPicksRemaining;

        public static readonly List<CharacterBase> All = new List<CharacterBase>();

        public static CharacterBase LocalPlayer
        {
            get
            {
                foreach (CharacterBase character in All)
                {
                    if (character != null && character.IsLocalPlayer)
                        return character;
                }

                return null;
            }
        }

        private static bool lobbyControlLocked;

        private static bool IsProjectInputLocked => lobbyControlLocked;

        private static bool IsProjectGameplayLocked =>
            MatchManager.Instance != null && MatchManager.Instance.Phase != MatchPhase.Fighting;

        public static void SetLobbyControlLocked(bool locked)
        {
            lobbyControlLocked = locked;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProjectIntegrationStatics()
        {
            lobbyControlLocked = false;
            All.Clear();
        }

        private void RegisterProjectIntegration()
        {
            if (!All.Contains(this))
                All.Add(this);
        }

        private void BindProjectHud()
        {
            if (IsLocalPlayer)
                CooldownHUD.Instance?.Bind(this);
        }

        private void UnregisterProjectIntegration()
        {
            All.Remove(this);

            if (IsLocalPlayer)
                CooldownHUD.Instance?.Unbind();
        }

        /// <summary>해당 증강을 이미 보유하고 있는지. 스킬 스크립트(예: SlimeCharacter)에서
        /// 분기 조건으로 쓴다.</summary>
        public bool HasAugment(AugmentType type) => (NetAugmentFlags & (1 << (int)type)) != 0;

        /// <summary>이번 AugmentSelect 구간에 이 캐릭터가 고를 수 있는 픽 수를 배정한다.
        /// 각 클라이언트가 자기 캐릭터에 대해서만 호출해야 한다(MatchManager의 RPC 브로드캐스트 참고).</summary>
        public void SetAugmentPicksRemaining(int count)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            NetAugmentPicksRemaining = Mathf.Max(0, count);
        }

        /// <summary>증강을 하나 확정한다. 남은 픽이 없으면 무시된다.</summary>
        public void GrantAugment(AugmentType type)
        {
            if (Object == null || !Object.HasStateAuthority || NetAugmentPicksRemaining <= 0)
                return;

            NetAugmentFlags |= 1 << (int)type;
            NetAugmentPicksRemaining--;
        }

        /// <summary>매치 시작 시 팩 선택 단계가 없는 동안은 전체 팩의 증강을 그대로 폴로 쓴다.</summary>
        public List<AugmentPoolEntry> GetAugmentPool()
        {
            List<AugmentPoolEntry> pool = new List<AugmentPoolEntry>();

            foreach (AugmentPackData pack in AugmentPackManager.AllPacks)
            {
                if (pack == null || pack.augments == null)
                    continue;

                foreach (AugmentData data in pack.augments)
                {
                    if (data != null)
                        pool.Add(new AugmentPoolEntry(data, pack));
                }
            }

            return pool;
        }

        /// <summary>제한시간 안에 다 못 고른 픽을 채우는 최종 안전장치(연결 끊김 등으로 클라이언트의
        /// AugmentSelectUI가 아예 돌지 않는 극단적인 경우 대비). 정상적으로는 AugmentSelectUI가
        /// 시간 초과 전에 화면에 뜬 첫 번째 슬롯을 이미 선택해뒀을 것이므로 대부분 아무 일도 하지
        /// 않는다. 여기서도 같은 "첫 번째" 원칙으로 폴 순서상 가장 앞의 미보유 증강을 고른다.</summary>
        public void AutoFinishAugmentPicks()
        {
            if (Object == null || !Object.HasStateAuthority || NetAugmentPicksRemaining <= 0)
                return;

            List<AugmentPoolEntry> pool = GetAugmentPool();

            while (NetAugmentPicksRemaining > 0)
            {
                AugmentPoolEntry chosen = pool.Find(entry => entry.Data != null && !HasAugment(entry.Data.type));
                if (chosen.Data == null)
                    break;

                GrantAugment(chosen.Data.type);
            }

            NetAugmentPicksRemaining = 0;
        }
    }
}
