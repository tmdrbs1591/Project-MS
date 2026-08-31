using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class PopupErrorPopupDeployable : CharacterDeployable
    {
        [SerializeField] private Vector2 popupOffset;
        [Min(3f)][SerializeField] private float popupTabLeashRadius = 3f;
        private CharacterBase target;

        private float damageDuration;
        private float totalDamage;
        private int damageTimes;

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
            if(target != null)
            {
                Vector3 idealPosition = target.transform.position + (Vector3)popupOffset;
                    // idealPosition = 타겟에 팝업창이 붙어있을 때의 위치 (타겟 현재 위치 + 오프셋) --> 의도하는 위치
                    //팝업창은 자기 위치(transform.postion)를 그대로 유지하다가 
                    // idealPosition 에서 leash 범위보다 멀어졌을 때만 다시 타겟에 위치로 따라잡음.
                if (Vector2.Distance(transform.position, idealPosition) > popupTabLeashRadius)
                    transform.position = idealPosition;
            }
            timers.Tick(Runner.DeltaTime);
        }

        public void Initialize(CharacterBase _target, float _duration, float _totalDamage, int _damageTimes)
        {
            target = _target;
            damageDuration = _duration;
            totalDamage = _totalDamage;
            damageTimes = _damageTimes;

            SetContinuousDamage();
        }

        private void DealContinuosDamage()
        {
            DealDamage(target, totalDamage / damageTimes);
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
