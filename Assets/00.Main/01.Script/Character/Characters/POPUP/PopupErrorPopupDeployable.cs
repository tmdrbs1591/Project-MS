using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class PopupErrorPopupDeployable : CharacterDeployable
    {
        [SerializeField] private Vector2 popupOffset;
        [Min(0.1f)][SerializeField] private float popupTabLeashRadius = 0.5f;
        private CharacterBase target;

        private float damageDuration;
        private float totalDamage;
        private int damageTimes;
        private Action onErrorPopupEndByTimeOver;

        private CharacterTimerHandler timers;

        private int currentDealedCount = 0;

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

        public void Initialize(CharacterBase _target, float _duration, float _totalDamage, int _damageTimes, Action _onErrorPopupEndByTimeOver)
        {
            // PopupCharacter에서 target에 행동 불가 상태 추가

            target = _target;
            damageDuration = _duration;
            totalDamage = _totalDamage;
            damageTimes = _damageTimes;
            onErrorPopupEndByTimeOver = _onErrorPopupEndByTimeOver;

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

        private void DealContinuosDamage()
        {
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
                DealContinuosDamage();
                SetContinuousDamage();
            });
        }

    }
}
