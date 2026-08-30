using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class PopupErrorPopupDeployable : CharacterDeployable
    {
        [SerializeField] private Vector2 popupOffset;

        private CharacterBase target;

        private float duration;
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

            transform.position = target.transform.position + (Vector3)popupOffset;
        }

        public void Initialize(CharacterBase _target, float _duration, float _totalDamage, int _damageTimes)
        {
            target = _target;
            duration = _duration;
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
            if (currentDealedCount >= damageTimes)
                return;

            if (!HasStateAuthority)
                return;

            timers.Schedule(duration / damageTimes, () =>
            {
                DealContinuosDamage();
                SetContinuousDamage();
            });
        }

    }
}
