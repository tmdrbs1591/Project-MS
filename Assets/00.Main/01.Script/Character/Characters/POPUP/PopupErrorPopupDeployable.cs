using System;
using Fusion;
using TMPro;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class PopupErrorPopupDeployable : CharacterDeployable
    {
        [SerializeField] private Vector2 popupOffset;
        [Min(0.1f)][SerializeField] private float popupTabLeashRadius = 0.5f;

        [Header("Break Progress")]
        [Tooltip("연타 진행도(예: 3 / 10)를 그릴 텍스트. 비워두면 표시하지 않는다.")]
        [SerializeField] private TMP_Text breakProgressText;

        // target/damage 관련 값은 스폰한 쪽(시전자, 권한자)에서만 채워진다 — 원격 클라이언트는
        // 이 필드를 못 쓰므로, 화면 표시에 필요한 값은 [Networked]로 따로 복제한다.
        [Networked] private PlayerRef NetTargetPlayer { get; set; }
        [Networked] private int NetBreakProgress { get; set; }
        [Networked] private int NetBreakRequired { get; set; }

        private CharacterBase target;

        private float damageDuration;
        private float totalDamage;
        private int damageTimes;
        private Action onErrorPopupEndByTimeOver;

        private CharacterTimerHandler timers;

        private int currentDealedCount = 0;
        private int lastRenderedProgress = -1;

        private void Awake()
        {
            timers = new CharacterTimerHandler();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (!Object.HasStateAuthority)
                return;
            if (target != null)
            {
                // idealPosition = 타겟에 팝업창이 붙어있을 때의 위치 (타겟 현재 위치 + 오프셋) --> 의도하는 위치
                // 팝업창은 자기 위치(transform.postion)를 그대로 유지하다가
                // idealPosition 에서 leash 범위보다 멀어졌을 때만 다시 타겟에 위치로 따라잡음.

                Vector3 idealPosition = target.transform.position + (Vector3)popupOffset;

                if (Vector2.Distance(transform.position, idealPosition) > popupTabLeashRadius)
                    transform.position = idealPosition;
            }
            timers.Tick(Runner.DeltaTime);
        }

        public override void Render()
        {
            base.Render();
            RenderBreakProgress();
        }

        public void Initialize(CharacterBase _target, float _duration, float _totalDamage, int _damageTimes, int _breakRequired, Action _onErrorPopupEndByTimeOver)
        {
            // PopupCharacter에서 target에 행동 불가 상태 추가

            target = _target;
            damageDuration = _duration;
            totalDamage = _totalDamage;
            damageTimes = _damageTimes;
            onErrorPopupEndByTimeOver = _onErrorPopupEndByTimeOver;

            if (!Object.HasStateAuthority)
                return;

            // 팝업을 깨야 하는 당사자의 화면에서만 진행도를 보여주기 위해 대상 플레이어를 복제해둔다.
            NetTargetPlayer = target != null && target.Object != null
                ? target.Object.InputAuthority
                : PlayerRef.None;
            NetBreakRequired = Mathf.Max(0, _breakRequired);
            NetBreakProgress = 0;

            SetContinuousDamage();

            if (LifetimeMode != OwnedEntityLifetimeMode.Manual)
            {
                Debug.LogError($"[PopupErrorPopupDeployable] 팝업의 \'시스템 에러 팝업 등장!\'(궁극기)의 오류 팝업창은 Lifetime Mode가 Manual이여야 합니다! (현재 : {LifetimeMode})\nLifetime을 설정하고 싶다면 PopupCharacter 프리팹의 Popup Appear Error Popup Duration을 수정해주세요!", this);
                return;
            }

            // 시간에 0.05f 추가 이유 : 데미지를 주기 전에 Destory될 수 있어서
            timers.Schedule(damageDuration + 0.05f, () =>
            {
                onErrorPopupEndByTimeOver?.Invoke();
                RequestDestroy(OwnedEntityDestroyReason.LifetimeExpired);
            });
        }

        /// <summary>시전자가 센 연타 횟수를 반영한다(권한자만 기록). 표시는 대상 본인 화면에서만 된다.</summary>
        public void SetBreakProgress(int progress)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            NetBreakProgress = Mathf.Clamp(progress, 0, NetBreakRequired);
        }

        private void RenderBreakProgress()
        {
            if (breakProgressText == null)
                return;

            bool isTargetLocal = Runner != null &&
                                 NetTargetPlayer != PlayerRef.None &&
                                 NetTargetPlayer == Runner.LocalPlayer;

            if (breakProgressText.enabled != isTargetLocal)
                breakProgressText.enabled = isTargetLocal;
            if (!isTargetLocal)
                return;

            // 값이 바뀔 때만 문자열을 다시 만든다(매 프레임 할당 방지).
            int progress = NetBreakProgress;
            if (progress == lastRenderedProgress)
                return;

            lastRenderedProgress = progress;
            breakProgressText.text = $"{progress} / {NetBreakRequired}";
        }

        private void DealContinuosDamage()
        {
            // 파괴 요청(연타로 깨짐 등) 직후 스폰 해제까지 1틱이 남아 있어, 그 사이에 예약된 도트딜이
            // 한 번 더 들어가지 않도록 막는다.
            if (IsDestroying)
                return;

            DealDamage(target, totalDamage / damageTimes, CharacterDamageSource.Periodic);
            currentDealedCount++;
        }

        private void SetContinuousDamage()
        {
            if (!Object.HasStateAuthority)
                return;

            if (currentDealedCount >= damageTimes)
                return;

            timers.Schedule(damageDuration / damageTimes, () =>
            {
                if (IsDestroying)
                    return;

                DealContinuosDamage();
                SetContinuousDamage();
            });
        }

    }
}
