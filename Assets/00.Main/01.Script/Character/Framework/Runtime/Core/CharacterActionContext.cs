using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public readonly struct CharacterActionContext
    {
        public CharacterActionContext(
            CharacterActionType action,
            Vector2 origin,
            Vector2 aimDirection,
            Vector2 aimWorldPosition,
            float damage)
        {
            Action = action;
            Origin = origin;
            AimDirection = aimDirection;
            AimWorldPosition = aimWorldPosition;
            Damage = damage;
        }

        public CharacterActionType Action { get; }
        public Vector2 Origin { get; }
        public Vector2 AimDirection { get; }
        public Vector2 AimWorldPosition { get; }
        public float Damage { get; }
        public float AimAngle => Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
    }
}
