using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class PopupErrorPopupDeployable : CharacterDeployable
    {
        [SerializeField] private Vector2 popupOffset;

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

            transform.position = target.transform.position + (Vector3)popupOffset;
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
