using Fusion;
using ProjectMS.CharacterSystem;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 거너 캐릭터. 기본 총격 + 관통형 Q(루시안식 피어싱 라이트) + 조기폭발형 수류탄 E +
    /// 게이지형 궁극기(바주카 로켓)로 구성된 원거리 딜러.
    ///
    /// [패시브 - 강화탄]
    ///   Q/E/궁 중 아무거나 쓰면 "강화탄" 상태가 되고, 유지시간 제한 없이 다음 기본공격 한 발이
    ///   나갈 때까지 그대로 유지된다(여러 번 스킬을 다시 써도 중첩되지 않고 그냥 갱신됨).
    ///   강화탄 상태의 기본공격은 데미지가 30% 증가한다.
    ///   총에 빨간 이펙트를 씌우는 연출은 아직 아트가 없어서, empoweredGunVisual을 비워두면
    ///   조용히 생략된다 — 나중에 무기 소켓 밑에 이펙트 오브젝트를 만들어 연결하면 된다.
    ///
    /// [궁극기 게이지]
    ///   CharacterDefinition.UltimateUsesGauge를 켜서 사용한다 — 시간 쿨타임이 아니라 적에게
    ///   입힌 데미지만큼 차는 게이지로 동작한다(프레임워크 공용 기능, CharacterBase 참고).
    ///
    /// [수류탄(E) 조기 폭발]
    ///   실제 충돌/타이머 로직은 GunnerGrenadeProjectile이 갖고 있고, 폭발 시 이 클래스의
    ///   DetonateGrenade()를 호출해 범위 데미지 계산을 위임한다(전투 쿼리 헬퍼가 CharacterBase
    ///   하위에서만 protected로 열려있어서).
    /// </summary>
    public class GunnerCharacter : CharacterBase
    {
        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;

        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile bulletProjectilePrefab;
        [Min(0f)] [SerializeField] private float bulletProjectileSpeed = 25f;
        [Min(1)] [SerializeField] private int magazineSize = 6;
        [Min(0f)] [SerializeField] private float reloadDuration = 1.5f;

        [Header("Skill Q - Piercing Light")]
        [Tooltip("투사체가 아니라 즉발 판정이다 — 정면 사각형 범위 안의 적 전부에게 그 자리에서 바로 데미지를 준다.")]
        [SerializeField] private Vector2 piercingLightBoxSize = new Vector2(6f, 1.5f);
        [Tooltip("사각형 중심을 캐릭터 위치에서 조준 방향으로 얼마나 띄울지(보통 사각형 길이의 절반).")]
        [Min(0f)] [SerializeField] private float piercingLightForwardOffset = 3f;

        [Header("Skill E - Grenade")]
        [SerializeField] private GunnerGrenadeProjectile grenadePrefab;
        [Min(0f)] [SerializeField] private float grenadeThrowSpeed = 12f;
        [Min(0f)] [SerializeField] private float grenadeExplosionRadius = 2f;

        [Header("Ultimate - Bazooka")]
        [SerializeField] private CharacterProjectile rocketProjectilePrefab;
        [Min(0f)] [SerializeField] private float rocketSpeed = 18f;

        [Header("Passive - Empowered Round")]
        [Range(1f, 5f)] [SerializeField] private float empoweredDamageMultiplier = 1.3f;
        [Tooltip("무기 소켓 밑에 미리 붙여둔 강화 상태 표시용 오브젝트(빨간 이펙트 등). 평소엔 비활성화해두면\n이 스크립트가 상태에 맞춰 켜고 끈다. 아직 아트가 없다면 비워둬도 안전함.")]
        [SerializeField] private GameObject empoweredGunVisual;

        [Networked] private NetworkBool NetEmpowered { get; set; }

        private bool isFirstBasicAttack = true;
        private bool lastRenderedEmpowered;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (bulletProjectilePrefab == null)
                return false;

            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;

            if (isFirstBasicAttack)
            {
                SetActionCharges(CharacterActionType.BasicAttack, magazineSize);
                isFirstBasicAttack = false;
            }

            bool empowered = NetEmpowered;
            float damage = empowered ? context.Damage * empoweredDamageMultiplier : context.Damage;

            SpawnProjectile(
                bulletProjectilePrefab,
                ProjectileOrigin.position,
                context.AimDirection,
                bulletProjectileSpeed,
                damage,
                targetLayer);

            if (empowered)
                NetEmpowered = false;

            PlayActionEffect(CharacterActionType.BasicAttack, EffectOrigin.position, context.AimAngle);

            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, reloadDuration);
                SetActionCharges(CharacterActionType.BasicAttack, magazineSize + 1);
            }
            else
            {
                ResetCooldownDuration(CharacterActionType.BasicAttack);
            }

            return true;
        }

        /// <summary>즉발 판정. 투사체가 날아가는 게 아니라, 캐스팅 순간 정면 사각형 범위 안의
        /// 적 전부에게 그 자리에서 바로 데미지를 주고 짧게 번쩍이는 이펙트만 재생한다
        /// (루시안 Q처럼 보이되, 실제로는 이동 시간이 없는 즉발기).</summary>
        protected override bool OnSkillQ(CharacterActionContext context)
        {
            Vector2 aimDirection = context.AimDirection.sqrMagnitude > 0.0001f
                ? context.AimDirection.normalized
                : new Vector2(FacingDirection, 0f);

            Vector2 boxCenter = (Vector2)AttackOrigin.position + aimDirection * piercingLightForwardOffset;

            // 리시뮬레이션 중엔 데미지 쿼리를 스킵(SPARK의 동일 패턴과 같은 이유 —
            // 같은 입력에 DealDamage가 여러 번 불리는 걸 막는다).
            if (HasStateAuthority && !Runner.IsResimulation)
            {
                foreach (CharacterBase enemy in FindEnemiesInBox(boxCenter, piercingLightBoxSize, context.AimAngle, targetLayer))
                {
                    DealDamage(enemy, context.Damage);
                }
            }

            PlayActionEffect(CharacterActionType.SkillQ, boxCenter, context.AimAngle);
            ActivatePassive();
            return true;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            if (grenadePrefab == null)
                return false;

            Vector2 direction = context.AimDirection.sqrMagnitude > 0.0001f
                ? context.AimDirection.normalized
                : new Vector2(FacingDirection, 0f);

            Vector2 throwVelocity = direction * grenadeThrowSpeed;
            float damage = context.Damage;

            Runner.Spawn(
                grenadePrefab,
                ProjectileOrigin.position,
                Quaternion.identity,
                Object.InputAuthority,
                (_, spawnedObject) =>
                {
                    GunnerGrenadeProjectile grenade = spawnedObject.GetComponent<GunnerGrenadeProjectile>();
                    grenade?.Initialize(throwVelocity, damage, Object.InputAuthority, Object.Id);
                });

            PlayActionEffect(CharacterActionType.SkillE, EffectOrigin.position, context.AimAngle);
            ActivatePassive();
            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            if (rocketProjectilePrefab == null)
                return false;

            SpawnProjectile(
                rocketProjectilePrefab,
                ProjectileOrigin.position,
                context.AimDirection,
                rocketSpeed,
                context.Damage,
                targetLayer);

            PlayActionEffect(CharacterActionType.Ultimate, EffectOrigin.position, context.AimAngle);
            ActivatePassive();
            return true;
        }

        /// <summary>GunnerGrenadeProjectile이 터질 때 호출한다(조기 폭발이든 자동 폭발이든 동일 경로).
        /// 던진 시점의 데미지(context.Damage)를 그대로 넘겨받아 범위 안의 적에게 적용한다.
        /// 폭발 이펙트는 여기서 안 그린다 — GunnerGrenadeProjectile.Rpc_PlayExplosionVfx가 따로
        /// 처리한다(던지는 연출용 PlayActionEffect(SkillE)와 슬롯이 겹치지 않게 분리함).</summary>
        public void DetonateGrenade(Vector2 position, float damage)
        {
            if (!HasStateAuthority || Runner.IsResimulation)
                return;

            foreach (CharacterBase enemy in FindEnemiesInCircle(position, grenadeExplosionRadius, targetLayer))
            {
                DealDamage(enemy, damage);
            }
        }

        protected override void OnResetCharacter()
        {
            NetEmpowered = false;
            isFirstBasicAttack = true;
        }

        public override void Render()
        {
            base.Render();

            if (empoweredGunVisual == null || lastRenderedEmpowered == NetEmpowered)
                return;

            lastRenderedEmpowered = NetEmpowered;
            empoweredGunVisual.SetActive(NetEmpowered);
        }

        private void ActivatePassive()
        {
            NetEmpowered = true;
        }
    }
}
