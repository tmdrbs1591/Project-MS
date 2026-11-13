using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 팝업 캐릭터의 스킬 로직을 담당하는 클래스.
    /// </summary>
    public class PopupCharacter : CharacterBase
    {
        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private int baseAttackPower;

        [Header("Basic Attack - Throw Error")]
        [SerializeField] private PopupErrorThrowable errorThrowablePrefab;
        [Min(1)][SerializeField] private int errorThrowableFireCount = 5;
        [Min(0f)][SerializeField] private float errorReloadingDuration = 1.2f;
        [Min(0f)][SerializeField] private float errorThrowableMaxVelocity = 3f;
        [Min(0.1f)][SerializeField] private int errorThrowableMaxCount = 5;
        [Tooltip("바운스 마법 증강 획득 시 반사 속력 배율 (1이면 던진 속도 그대로)")]
        [Min(0f)][SerializeField] private float errorThrowableBounceForce = 1f;
        [Min(0f)][SerializeField] private float errorThrowableForkAngleStep = 12.5f;

        [Header("Another Throw Error Settings")]
        [SerializeField] private bool useAnotherErrorThrow = true;
        [SerializeField] private float anotherErrorThrowableSpeed = 1f;

        [Header("Skill Q - Occur Aiming Bug")]
        [SerializeField] private CharacterProjectile aimingBugHackingCDProjectile;
        [SerializeField] private float aimingBugHackingCDSpeed = 7f;
        [SerializeField] private float aimingBugDuration = 1f;

        [Header("Skill E - Occur Moving Bug")]
        [Min(0f)][SerializeField] private float movingBugArcAngle = 180;
        [Min(0f)][SerializeField] private float movingBugArcRadius = 2.5f;
        [Min(0f)][SerializeField] private float movingBugSlowRatio = 0.3f;
        [Min(0f)][SerializeField] private float movingBugSlowDuration = 0.75f;
        [Tooltip("E 를 쓸 때 스킬이 나가는 방향(조준 방향)으로 회전해서 생성되는 이펙트")]
        [SerializeField] private GameObject movingBugVfxPrefab;
        [Tooltip("X 회전에 더할 보정각. 조준 방향과 어긋나 보이면 여기서 맞춘다.")]
        [SerializeField] private float movingBugVfxAngleOffset = 0f;
        [Tooltip("켜면 X 회전 방향을 반대로 돌린다(위로 조준했는데 이펙트가 아래로 가면 켠다).")]
        [SerializeField] private bool movingBugVfxInvertAngle = false;
        [Tooltip("고정 회전값 Y / Z. X 회전만 조준 방향에 맞춰 바뀐다.")]
        [SerializeField] private float movingBugVfxFixedY = 90f;
        [SerializeField] private float movingBugVfxFixedZ = -90f;
        [Tooltip("이펙트 재생 시간(초). 지나면 남은 파티클이 다 사라진 뒤 지운다.")]
        [Min(0f)][SerializeField] private float movingBugVfxLifetime = 1f;
        [Tooltip("켜면 이펙트가 캐릭터를 따라다닌다(캐릭터 자식으로 붙음).")]
        [SerializeField] private bool movingBugVfxFollowCharacter = false;

        [Header("Skill R - System Error Popup Appeared")]
        [SerializeField] private CharacterProjectile popupAppearGlitchProjectile;
        [Min(0f)][SerializeField] private float popupAppearGlitchSpeed = 6f;
        [SerializeField] private PopupErrorPopupDeployable popupAppearErrorPopupDeployable;
        [Tooltip("갇힌 동안 도트 데미지가 이 시간에 나눠 들어간다. 시간 제한이 아니다 — 연타로만 풀린다.")]
        [Min(0f)][SerializeField] private float popupAppearErrorPopupDuration = 3f;
        [Min(0)][SerializeField] private int popupAppearErrorPopupDamageTimes = 3;
        [Min(0)][SerializeField] private int popupAppearErrorPopupBreakAttemptsRequire = 10;

        [Header("Skill R - 연출")]
        [Tooltip("궁을 쏘는 순간 손(총구)에서 터지는 이펙트.")]
        [SerializeField] private SkillFxSlot[] ultimateCastFx;
        [Tooltip("궁이 상대에게 꽂혀 에러창에 가두는 순간 상대 위치에서 터지는 이펙트.")]
        [SerializeField] private SkillFxSlot[] ultimateTrapFx;
        [Tooltip("상대를 가둬 해킹하는 동안 팝업 머리 위에 띄울 이펙트(타이핑 말풍선 등). offset은 캐릭터 기준.")]
        [SerializeField] private SkillFxSlot hackingFx;

        [Fusion.Networked] private Fusion.NetworkBool NetHacking { get; set; }
        private GameObject hackingInstance;

        [Header("Passive - Glitch Occured")]
        [Min(0f)][SerializeField] private float glitchDuration = 3f;
        [Min(0f)][SerializeField] private float glitchDamageTimes = 3f;
        [Min(0f)][SerializeField] private float glitchDamageRatio = 0.3f;

        private int EffectiveErrorThrowableFireCount => Mathf.Max(1, Mathf.RoundToInt(errorThrowableFireCount * MaxAmmoMultiplier));
        private float EffectiveErrorReloadDuration =>
            (errorReloadingDuration > 0f ? errorReloadingDuration : (Definition != null ? Definition.GetCooldown(CharacterActionType.BasicAttack) : 1f))
            * ReloadSpeedMultiplier;

        private CharacterProjectile currentHackingCD;
        private CharacterProjectile currentPopupAppearGlitch;

        private bool isFirstThrowError = true;

        private float errorThrowableGravityScale = 1f;

        private int currentGlitchDealedCount;
        private CharacterBase currentGlitchTarget;
        private bool isGlitchOccuring = false;
        private bool isGlitchDamage = false;

        // 연타로 풀 때까지 봉인을 이 간격으로 다시 건다(한 번에 길게 걸지 않는 이유는 OnPassiveTick 참고).
        private const float ErrorPopupSealRefreshInterval = 0.25f;
        private const float ErrorPopupSealDuration = 0.6f;
        private float errorPopupSealRefreshTimer;

        private bool isErrorPopupEnabled = false;
        private int errorPopupBreakAttempts = 0;
        private CharacterBase errorPopupTarget;
        private PopupErrorPopupDeployable errorPopupDeployable;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (isFirstThrowError)
            {
                SetActionCharges(CharacterActionType.BasicAttack, EffectiveErrorThrowableFireCount);
                errorThrowableGravityScale = errorThrowablePrefab.GetComponent<Rigidbody2D>().gravityScale;
                isFirstThrowError = false;
            }

            Vector2 errorVelocity = default;

            if (useAnotherErrorThrow)
            {
                Vector2 throwDirection = Quaternion.Euler(0, 0, AimAngle) * Vector2.right;
                errorVelocity = throwDirection * anotherErrorThrowableSpeed;
            }
            else
            {
                errorVelocity = CalculateThrowVelocity(
                    ProjectileOrigin.position,
                    context.AimWorldPosition,
                    errorThrowableMaxVelocity,
                    errorThrowableGravityScale);
            }

            bool isSucceedToThrowError = ThrowError(errorVelocity, 0f, context.Damage);
            if (!isSucceedToThrowError)
                return false;

            if (ForkedProjectileCount > 0)
            {
                float forkDamage = context.Damage * ForkedProjectileDamageMultiplier;
                for (int i = 0; i < ForkedProjectileCount; i++)
                {
                    float angleOffset = errorThrowableForkAngleStep * (i / 2 + 1) * (i % 2 == 0 ? 1f : -1f);
                    bool isSucceedToThrowForkError = ThrowError(errorVelocity, angleOffset, forkDamage);

                    if (!isSucceedToThrowForkError)
                        return false;
                }
            }

            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;
            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, EffectiveErrorReloadDuration);
                SetActionCharges(CharacterActionType.BasicAttack, EffectiveErrorThrowableFireCount + 1);
                NotifyReloadStarted(EffectiveErrorReloadDuration);
            }
            else
                ResetCooldownDuration(CharacterActionType.BasicAttack);

            return true;
        }

        private bool ThrowError(Vector2 velocity, float zAngle, float damage)
        {
            Vector2 rotatedVelocity = Rotate(velocity, zAngle);

            OwnedEntitySpawnRequest request = new OwnedEntitySpawnRequest(
                ProjectileOrigin.position,
                Quaternion.identity,
                new OwnedEntityGroupId((int)CharacterActionType.BasicAttack),
                maxCount: errorThrowableMaxCount,
                overflowPolicy: OwnedEntityOverflowPolicy.DestroyOldest,
                initialVelocity: rotatedVelocity);

            OwnedEntitySpawnResult<PopupErrorThrowable> result = SpawnThrowable(
                errorThrowablePrefab,
                in request,
                initialize: (errorThrowable) => errorThrowable.Initialize(
                    damage,
                    ProjectileBounceCount,
                    errorThrowableBounceForce,
                    HasExplosiveProjectile,
                    ExplosiveProjectileDamageMultiplier));

            if (!result.Success)
            {
                Debug.LogWarning("[PopupCharacter] 에러 투럭!(기본 공격) 발사체를 소환하는데 실패했습니다!");
                return false;
            }

            return true;
        }

        // 휠 수동 재장전.
        protected override bool TryGetReloadInfo(out int magazine, out float duration)
        {
            magazine = EffectiveErrorThrowableFireCount;
            duration = EffectiveErrorReloadDuration;
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            currentHackingCD = SpawnProjectile(
                aimingBugHackingCDProjectile,
                ProjectileOrigin.position,
                context.AimDirection,
                aimingBugHackingCDSpeed,
                context.Damage,
                targetLayer);

            return true;
        }
        
        protected override bool OnSkillE(CharacterActionContext context)
        {
            List<CharacterBase> enimies = FindEnemiesInArc(
                AttackOrigin.position,
                context.AimDirection,
                movingBugArcRadius,
                movingBugArcAngle,
                targetLayer);

            foreach (CharacterBase enemy in enimies)
            {
                DealDamage(enemy, context.Damage);
                ApplySlow(enemy, movingBugSlowRatio, movingBugSlowDuration);
            }

            if (!Runner.IsResimulation)
                Rpc_PlayMovingBugVfx(AttackOrigin.position, context.AimAngle);

            return true;
        }

        // E 이펙트: 모든 클라에서 스킬 방향(조준 각도)으로 회전해서 생성한다.
        // 3D 파티클 프리팹이라 Y/Z 는 고정(90, -90)하고 X 회전만 바꾼다. 이 고정값에서 파티클의 +Z(뿜는 방향)는
        // 화면상 (cos x, -sin x) 를 향하므로, 조준 각도 θ 를 향하려면 X = -θ 다.
        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_PlayMovingBugVfx(Vector2 position, float angle)
        {
            if (movingBugVfxPrefab == null)
                return;

            float x = (movingBugVfxInvertAngle ? angle : -angle) + movingBugVfxAngleOffset;
            Quaternion rotation = Quaternion.Euler(x, movingBugVfxFixedY, movingBugVfxFixedZ);
            GameObject vfx = movingBugVfxFollowCharacter
                ? Instantiate(movingBugVfxPrefab, position, rotation, transform)
                : Instantiate(movingBugVfxPrefab, position, rotation);
            EffectAutoDestroy.Schedule(vfx, movingBugVfxLifetime);
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            currentPopupAppearGlitch = SpawnProjectile(
                popupAppearGlitchProjectile,
                ProjectileOrigin.position,
                context.AimDirection,
                popupAppearGlitchSpeed,
                context.Damage,
                targetLayer);

            if (HasStateAuthority && !Runner.IsResimulation)
                Rpc_PlayUltimateCast(ProjectileOrigin.position, context.AimAngle);
            return true;
        }

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_PlayUltimateCast(Vector2 origin, float angle)
        {
            SkillFxSlot.SpawnAll(ultimateCastFx, origin, angle);
            if (!IsLocalPlayer || TwoPlayerCamera.Instance == null)
                return;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            TwoPlayerCamera.Instance.Shake(0.35f, 0.22f);
            TwoPlayerCamera.Instance.PunchZoom(-dir, 0.45f, 0.04f, 0.25f);
        }

        // 궁이 꽂혀 상대를 가두는 순간 — 양쪽 화면 모두 크게 흔들리고 상대 자리에서 이펙트가 터진다.
        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_PlayHackTrap(Vector2 position)
        {
            SkillFxSlot.SpawnAll(ultimateTrapFx, position, 0f);
            TwoPlayerCamera.Instance?.Shake(0.55f, 0.35f);
        }

        public override void Render()
        {
            base.Render();

            bool hacking = Object != null && Object.IsValid && NetHacking && !IsDead;
            if (hacking && hackingInstance == null && hackingFx != null && hackingFx.prefab != null)
                hackingInstance = hackingFx.Spawn(transform.position, 0f);
            if (hackingInstance != null)
            {
                if (hacking)
                    hackingInstance.transform.position = transform.position + (Vector3)hackingFx.offset;
                else
                {
                    Destroy(hackingInstance);
                    hackingInstance = null;
                }
            }
        }

        private void OnDisable()
        {
            if (hackingInstance != null)
                Destroy(hackingInstance);
            hackingInstance = null;
        }

        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            if (reason != ProjectileDespawnReason.HitCharacter)
                return;

            if (projectile == currentHackingCD)
            {
                ApplyControlSeal(hitTarget, CharacterControlType.BasicAttack, aimingBugDuration);
                return;
            }

            if (projectile == currentPopupAppearGlitch)
            {
                errorPopupTarget = hitTarget;
                ChangeErrorPopupStatus(true);
                if (HasStateAuthority && !Runner.IsResimulation)
                    Rpc_PlayHackTrap(hitTarget.transform.position);

                errorPopupDeployable = SpawnOwnedEntity(
                    popupAppearErrorPopupDeployable,
                    CharacterActionType.Ultimate,
                    errorPopupTarget.transform.position,
                    maxCount: 1,
                    initialize: (popup) => popup.Initialize(
                        errorPopupTarget,
                        popupAppearErrorPopupDuration,
                        Definition.GetDamage(CharacterActionType.Ultimate),
                        popupAppearErrorPopupDamageTimes,
                        popupAppearErrorPopupBreakAttemptsRequire));

                return;
            }
        }

        // 패시브 틱이지만 시스템 오류 팝업 등장!(궁극기)의 팝업창 입력 감지에 사용
        protected override void OnPassiveTick(float deltaTime)
        {
            if (!isErrorPopupEnabled)
                return;

            // 시간 제한 없이 연타로만 풀린다. 다만 대상이 사라지거나 죽거나 팝업창이 없어지면(라운드 정리 등) 바로 푼다.
            if (errorPopupTarget == null || errorPopupTarget.Object == null || !errorPopupTarget.Object.IsValid ||
                errorPopupTarget.IsDead || errorPopupDeployable == null)
            {
                BreakErrorPopup();
                return;
            }

            // 봉인은 짧게 걸고 계속 갱신한다 — 팝업(시전자)이 게임을 나가 버려도 상대가 영영 갇히지 않게.
            errorPopupSealRefreshTimer -= deltaTime;
            if (errorPopupSealRefreshTimer <= 0f)
            {
                errorPopupSealRefreshTimer = ErrorPopupSealRefreshInterval;
                ApplyControlSeal(errorPopupTarget, CharacterControlType.All, ErrorPopupSealDuration);
            }

            // 점프(스페이스)와 기본공격(좌클릭)만 "팝업 깨기 시도"로 센다. 한 틱에 여러 번 눌려도
            // 누락 없이 더하도록 WasInputPressed(bool) 대신 새로 눌린 횟수를 받아온다.
            int newAttempts =
                ConsumeNewInputCount(errorPopupTarget, CharacterInputType.BasicAttack) +
                ConsumeNewInputCount(errorPopupTarget, CharacterInputType.Jump);

            if (newAttempts > 0)
            {
                errorPopupBreakAttempts += newAttempts;
                if (errorPopupDeployable != null)
                    errorPopupDeployable.SetBreakProgress(errorPopupBreakAttempts);
            }

            if (errorPopupBreakAttempts >= popupAppearErrorPopupBreakAttemptsRequire)
                BreakErrorPopup();
        }

        // 라운드 도중 리셋되면(상대 사망 등) 남아있는 팝업/봉인/도트딜 상태를 정리한다.
        protected override void OnResetCharacter()
        {
            if (isErrorPopupEnabled)
                BreakErrorPopup();

            isFirstThrowError = true;
        }

        // 팝업(시전자)이 죽으면 갇힌 상대도 풀어준다.
        protected override void OnDied(Fusion.PlayerRef attacker)
        {
            base.OnDied(attacker);
            if (isErrorPopupEnabled)
                BreakErrorPopup();
        }

        // 상대가 연타로 팝업을 깨거나, 대상/시전자가 사라지거나, 라운드 리셋될 때 쓴다(시간 제한은 없다).
        private void BreakErrorPopup()
        {
            // ChangeErrorPopupStatus(false)가 target/deployable 참조를 null로 지우므로, 그 전에
            // 봉인 해제와 팝업창 파괴에 쓸 값을 먼저 빼둔다(예전엔 지운 뒤에 null을 넘겨서
            // 팝업창이 안 부서지고 도트딜이 계속 들어갔다).
            CharacterBase target = errorPopupTarget;
            PopupErrorPopupDeployable deployable = errorPopupDeployable;

            ReleaseControlSeal(target, CharacterControlType.All);
            ChangeErrorPopupStatus(false);
            DestroyOwnedEntity(deployable, OwnedEntityDestroyReason.Manual);
        }

        protected override void OnDamageDealt(CharacterBase target, float requestedDamage)
        {
            if (isGlitchDamage)
            {
                isGlitchDamage = false; ;
                return;
            }

            currentGlitchTarget = target;
            currentGlitchDealedCount = 0;

            if (isGlitchOccuring) return;

            SetContinuousGlitch();
        }

        private Vector2 CalculateThrowVelocity(Vector2 startPosition, Vector2 targetPosition, float maxVelocity, float projectileGravityScale)
        {
            Vector2 delta = targetPosition - startPosition;

            float gravity = Mathf.Abs(Physics2D.gravity.y * projectileGravityScale);

            float x = Mathf.Abs(delta.x);
            float y = delta.y;

            float speedSquared = maxVelocity * maxVelocity;

            // 판별식
            float discriminant = speedSquared * speedSquared - gravity * (gravity * x * x + 2f * y * speedSquared);

            float directionX = Mathf.Sign(delta.x);

            bool canReachInMaxVelocity = discriminant >= 0f && x > 0.001f;
            if (canReachInMaxVelocity)
            {
                float tanTheta =
                    (speedSquared - Mathf.Sqrt(discriminant))
                    / (gravity * x);

                float cosTheta = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
                float sinTheta = tanTheta * cosTheta;

                return new Vector2(
                    directionX * cosTheta,
                    sinTheta
                ) * maxVelocity;
            }

            Vector2 fallbackDirection =
                new Vector2(directionX, 1f).normalized;

            return fallbackDirection * maxVelocity;
        }

        private void ChangeErrorPopupStatus(bool isEnable)
        {
            errorPopupBreakAttempts = 0;
            isErrorPopupEnabled = isEnable;
            NetHacking = isEnable;
            if (!isEnable)
            {
                errorPopupDeployable = null;
                errorPopupTarget = null;
            }

            // 팝업(시전자)은 묶이지 않는다 — 갇힌 상대를 마음껏 때릴 수 있다.
            if (isEnable)
            {
                // 이전에 눌렀던 입력이 "깨기 시도"로 세어지지 않게 관찰 기준을 지금으로 맞춘다.
                ResetInputObservation(errorPopupTarget, CharacterInputType.BasicAttack);
                ResetInputObservation(errorPopupTarget, CharacterInputType.Jump);

                errorPopupSealRefreshTimer = ErrorPopupSealRefreshInterval;
                ApplyControlSeal(errorPopupTarget, CharacterControlType.All, ErrorPopupSealDuration);
            }
            // 봉인 해제는 BreakErrorPopup()이 ReleaseControlSeal로 한다(연타 성공/대상·시전자 이탈/리셋).
        }

        private void DealGlitchDamage()
        {
            DealDamage(currentGlitchTarget, baseAttackPower * glitchDamageRatio / glitchDamageTimes, CharacterDamageSource.Periodic);
            currentGlitchDealedCount++;
            isGlitchDamage = true;
        }

        private void SetContinuousGlitch()
        {
            if (currentGlitchDealedCount >= glitchDamageTimes)
            {
                isGlitchOccuring = false;
                return;
            }

            isGlitchOccuring = true;

            ScheduleTimer(glitchDuration / glitchDamageTimes, () =>
            {
                DealGlitchDamage();
                SetContinuousGlitch();
            });
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                
                if(TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
                {
                    rb.AddForce(Vector2.up * 3f, ForceMode2D.Impulse);
                } 
            }
        }
    }
}
