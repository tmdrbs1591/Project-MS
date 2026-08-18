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
        // AugmentType의 항목 수와 반드시 일치해야 한다(둘 다 늘어나면 같이 늘린다).
        private const int AugmentTypeCount = 13;

        // 증강 종류별 중첩 횟수. 인덱스 = (int)AugmentType. 단순 비트마스크(있다/없다)로는
        // Max_Stack(중첩)을 표현할 수 없어서 카운트 배열로 저장한다(NetActionCharges와 같은 패턴).
        [Networked, Capacity(AugmentTypeCount)]
        private NetworkArray<int> NetAugmentStacks => default;

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

        // ---------------- 증강 보유/획득 ----------------

        /// <summary>이 증강을 몇 번 중첩해서 갖고 있는지(0이면 없음).</summary>
        public int GetAugmentStack(AugmentType type)
        {
            return IsAugmentType(type) ? NetAugmentStacks.Get((int)type) : 0;
        }

        /// <summary>해당 증강을 하나라도 보유하고 있는지. 스킬 스크립트에서 분기 조건으로 쓴다.</summary>
        public bool HasAugment(AugmentType type) => GetAugmentStack(type) > 0;

        /// <summary>증강을 한 스택 확정한다. 남은 픽이 없거나, 이미 Max_Stack까지 찼거나,
        /// 유효한 풀 항목이 아니면 무시된다.</summary>
        public void GrantAugment(AugmentType type)
        {
            if (Object == null || !Object.HasStateAuthority || NetAugmentPicksRemaining <= 0 || !IsAugmentType(type))
                return;

            AugmentData data = FindAugmentData(type);
            if (data == null)
                return;

            int current = NetAugmentStacks.Get((int)type);
            if (current >= Mathf.Max(1, data.maxStack))
                return;

            NetAugmentStacks.Set((int)type, current + 1);
            NetAugmentPicksRemaining--;
        }

        /// <summary>이번 AugmentSelect 구간에 이 캐릭터가 고를 수 있는 픽 수를 배정한다.
        /// 각 클라이언트가 자기 캐릭터에 대해서만 호출해야 한다(MatchManager의 RPC 브로드캐스트 참고).</summary>
        public void SetAugmentPicksRemaining(int count)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            NetAugmentPicksRemaining = Mathf.Max(0, count);
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
        /// AugmentSelectUI가 아예 돌지 않는 극단적인 경우 대비). 아직 Max_Stack에 안 닿은 증강 중
        /// 폴 순서상 가장 앞의 것을 고른다(중첩형이면 여러 번 골라도 됨).</summary>
        public void AutoFinishAugmentPicks()
        {
            if (Object == null || !Object.HasStateAuthority || NetAugmentPicksRemaining <= 0)
                return;

            List<AugmentPoolEntry> pool = GetAugmentPool();

            while (NetAugmentPicksRemaining > 0)
            {
                AugmentPoolEntry chosen = pool.Find(entry =>
                    entry.Data != null && GetAugmentStack(entry.Data.type) < Mathf.Max(1, entry.Data.maxStack));
                if (chosen.Data == null)
                    break;

                GrantAugment(chosen.Data.type);
            }

            NetAugmentPicksRemaining = 0;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>테스트 전용 치트. AugmentPicksRemaining(라운드 승패로 버는 픽 수)과 무관하게
        /// 증강을 한 스택 즉시 지급한다(Max_Stack까진). 릴리즈 빌드에는 아예 포함되지 않는다 —
        /// AugmentCheatController가 이 메서드를 호출한다.</summary>
        public void DebugGrantAugment(AugmentType type)
        {
            if (Object == null || !Object.HasStateAuthority || !IsAugmentType(type))
                return;

            AugmentData data = FindAugmentData(type);
            if (data == null)
                return;

            int current = NetAugmentStacks.Get((int)type);
            if (current >= Mathf.Max(1, data.maxStack))
                return;

            NetAugmentStacks.Set((int)type, current + 1);
        }

        /// <summary>테스트 전용 치트. 보유한 증강을 전부 초기화한다.</summary>
        public void DebugClearAugments()
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            for (int i = 0; i < AugmentTypeCount; i++)
                NetAugmentStacks.Set(i, 0);
        }
#endif

        private static bool IsAugmentType(AugmentType type)
        {
            int index = (int)type;
            return index >= 0 && index < AugmentTypeCount && System.Enum.IsDefined(typeof(AugmentType), type);
        }

        private AugmentData FindAugmentData(AugmentType type)
        {
            AugmentPoolEntry match = GetAugmentPool().Find(entry => entry.Data != null && entry.Data.type == type);
            return match.Data;
        }

        /// <summary>type의 percentValue를 중첩 수만큼 단순히 더한 값(예: 0.3 × 3스택 = 0.9).
        /// 안 갖고 있으면 0.</summary>
        private float GetAugmentPercentTotal(AugmentType type)
        {
            int stack = GetAugmentStack(type);
            if (stack <= 0)
                return 0f;

            AugmentData data = FindAugmentData(type);
            return data != null ? stack * data.percentValue : 0f;
        }

        // ---------------- 증강 배율 (스탯에 적용) ----------------

        /// <summary>체력 30% 이하일 때 버서커(AUG_012)가 발동 중인지.</summary>
        private bool IsBerserkerActive
        {
            get
            {
                if (!HasAugment(AugmentType.Berserker))
                    return false;

                AugmentData data = FindAugmentData(AugmentType.Berserker);
                float threshold = data != null && data.secondaryPercentValue > 0f ? data.secondaryPercentValue : 0.3f;
                return CurrentHealthPercent <= threshold;
            }
        }

        /// <summary>공격력 배율. 대형 탄약집 + 유리 대포 + (조건부) 버서커를 단순 덧셈으로 합산한다.
        /// DealDamageThroughPipeline에서 캐릭터별 ModifyOutgoingDamage보다 먼저 적용된다.</summary>
        public float AttackMultiplier
        {
            get
            {
                float bonus = GetAugmentPercentTotal(AugmentType.LargeAmmoPouch)
                    + GetAugmentPercentTotal(AugmentType.GlassCannon);

                if (IsBerserkerActive)
                {
                    AugmentData data = FindAugmentData(AugmentType.Berserker);
                    bonus += data != null ? data.percentValue : 0f;
                }

                return 1f + bonus;
            }
        }

        /// <summary>최대 체력 배율. 방탄복은 더하고 유리 대포는 뺀다(둘 다 0.1 미만으로는 안 내려감).</summary>
        public float MaxHealthMultiplier
        {
            get
            {
                float bonus = GetAugmentPercentTotal(AugmentType.BulletproofVest);

                if (HasAugment(AugmentType.GlassCannon))
                {
                    AugmentData data = FindAugmentData(AugmentType.GlassCannon);
                    bonus -= data != null ? data.secondaryPercentValue : 0f;
                }

                return Mathf.Max(0.1f, 1f + bonus);
            }
        }

        /// <summary>이동 속도 배율(신속의 신발). 슬로우 배율(MovementSpeedMultiplier)과는 별개로
        /// 곱해진다 — CharacterMovementHandler가 둘을 함께 곱해서 최종 속도를 낸다.</summary>
        public float MoveSpeedMultiplier => 1f + GetAugmentPercentTotal(AugmentType.SwiftBoots);

        /// <summary>최대 탄약 수 배율(과충전 탄창). 탄창 방식 캐릭터가 자기 magazineSize 계산에 곱해서 쓴다.</summary>
        public float MaxAmmoMultiplier => 1f + GetAugmentPercentTotal(AugmentType.OverchargedMagazine);

        /// <summary>재장전 시간 배율(고속 재장전, 1보다 작을수록 빠름). 탄창 방식 캐릭터가
        /// reloadDuration 계산에 곱해서 쓴다.</summary>
        public float ReloadSpeedMultiplier => Mathf.Max(0.1f, 1f - GetAugmentPercentTotal(AugmentType.RapidReload));

        /// <summary>대시 쿨타임 배율(추진력 강화, 1보다 작을수록 빠름).</summary>
        public float DashCooldownMultiplier => Mathf.Max(0.1f, 1f - GetAugmentPercentTotal(AugmentType.DashBooster));

        /// <summary>궁극기 게이지 충전율 배율(터보 차지). AddUltimateGaugeFromDamageDealt에서 쓴다.</summary>
        public float UltimateGaugeRateMultiplier => 1f + GetAugmentPercentTotal(AugmentType.TurboCharge);

        /// <summary>기본기에 추가로 나가는 투사체 수(갈래 마법 스택 수)와, 그 추가 투사체의 피해 배율.</summary>
        public int ForkedProjectileCount => GetAugmentStack(AugmentType.ForkedMagic);

        public float ForkedProjectileDamageMultiplier
        {
            get
            {
                AugmentData data = FindAugmentData(AugmentType.ForkedMagic);
                return data != null && data.percentValue > 0f ? data.percentValue : 0.5f;
            }
        }

        /// <summary>기본기 투사체가 벽에 튕기는 횟수(바운스 마법 스택 수).</summary>
        public int ProjectileBounceCount => GetAugmentStack(AugmentType.BouncingMagic);

        /// <summary>기본기 투사체가 벽/바닥 명중 시 폭발하는지, 그 폭발 피해 배율(투사체 피해 대비).</summary>
        public bool HasExplosiveProjectile => HasAugment(AugmentType.ExplosiveMagic);

        public float ExplosiveProjectileDamageMultiplier
        {
            get
            {
                AugmentData data = FindAugmentData(AugmentType.ExplosiveMagic);
                return data != null && data.percentValue > 0f ? data.percentValue : 0.5f;
            }
        }

        /// <summary>피격 시 반사(AUG_013). ApplyDamage에서 실제로 체력이 깎인 뒤 호출한다.
        /// 받은 데미지의 일정 비율을 반경 안의 다른 캐릭터에게 되돌린다(1대1이라 사실상 상대방).
        /// 데미지가 아주 작아지면(연쇄 반사 등) 재귀가 자연히 끊기도록 최소 임계값을 둔다.</summary>
        private void ApplyAugmentReflect(float appliedDamage)
        {
            if (!HasStateAuthority || appliedDamage <= 0f)
                return;

            int stacks = GetAugmentStack(AugmentType.Reflect);
            if (stacks <= 0)
                return;

            AugmentData data = FindAugmentData(AugmentType.Reflect);
            if (data == null)
                return;

            float reflectRatio = stacks * (data.percentValue > 0f ? data.percentValue : 0.2f);
            float reflectDamage = appliedDamage * reflectRatio;

            const float minReflectDamage = 0.05f;
            if (reflectDamage < minReflectDamage)
                return;

            float radius = data.radius > 0f ? data.radius : 3f;

            foreach (CharacterBase other in All)
            {
                if (other == null || other == this)
                    continue;

                if (Vector2.Distance(other.transform.position, transform.position) > radius)
                    continue;

                DealDamage(other, reflectDamage);
            }
        }
    }
}
