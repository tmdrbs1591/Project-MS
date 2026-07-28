using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    [Serializable]
    public sealed class CharacterSockets
    {
        [Tooltip("근접 공격과 범위 판정의 기준 위치입니다. 비어 있으면 캐릭터 루트를 사용합니다.")]
        [SerializeField] private Transform attackOrigin;

        [Tooltip("투사체 생성 위치입니다. 비어 있으면 Attack Origin을 사용합니다.")]
        [SerializeField] private Transform projectileOrigin;

        [Tooltip("검, 총, 지팡이 등 무기 표현의 부모입니다. 선택 항목입니다.")]
        [SerializeField] private Transform weaponRoot;

        [Tooltip("공격 VFX의 기본 위치입니다. 비어 있으면 Attack Origin을 사용합니다.")]
        [SerializeField] private Transform effectOrigin;

        public Transform AttackOrigin => attackOrigin;
        public Transform ProjectileOrigin => projectileOrigin;
        public Transform WeaponRoot => weaponRoot;
        public Transform EffectOrigin => effectOrigin;

        public Transform ResolveAttackOrigin(Transform fallback)
        {
            return attackOrigin != null ? attackOrigin : fallback;
        }

        public Transform ResolveProjectileOrigin(Transform fallback)
        {
            return projectileOrigin != null ? projectileOrigin : ResolveAttackOrigin(fallback);
        }

        public Transform ResolveEffectOrigin(Transform fallback)
        {
            return effectOrigin != null ? effectOrigin : ResolveAttackOrigin(fallback);
        }
    }
}
