using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 체이서 캐릭터의 스킬 로직을 담당하는 클래스.
    /// </summary>
    public class ChaserCharacter : CharacterBase
    {
        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;

        [Header("Basic Attack - Burst")]
        [SerializeField] private CharacterProjectile burstBulletProjectilePrefab;
        [Min(0f)][SerializeField] private float burstBulletProjectileSpeed = 15f;
        [Min(1)][SerializeField] private int burstBulletProjectileFireCount = 4;
        [Min(0f)][SerializeField] private float burstArcAngle = 45f;
        [Min(0f)][SerializeField] private float forkAngleStep = 12f;
        [Tooltip("켜면 기본탄과 갈래탄을 Burst Arc Angle 안에 균등하게 배치한다.")]
        [SerializeField] private bool useBurstArcForForkedProjectiles;
        [Min(0f)][SerializeField] private float burstReloadingDuration = 2.5f;
        [Min(1)][SerializeField] private int burstCharges = 4;

        [Header("Skill Q - Dual Revolver")]
        [Tooltip("비어있을 경우 Burst Bullet Projectile Prefab 사용")]
        [SerializeField] private CharacterProjectile dualRevolverProjectilePrefab; //없으면 기본 burstBulletProjectilePrefab사용
        [Min(0)][SerializeField] private float dualRevolverProjectileSpeed = 45f;
        [Tooltip("Q를 누르면 이 시간(초) 동안 기를 모았다가 쏜다. 그동안 마우스 방향으로 빨간 조준 레이저가 보인다.")]
        [Min(0f)][SerializeField] private float dualRevolverChargeTime = 1f;
        [SerializeField] private GameObject dualRevolverChargeVfx;
        [Min(0.01f)][SerializeField] private float dualRevolverChargeVfxScale = 0.35f;
        [SerializeField] private Color dualRevolverChargeTint = new Color(1f, 0.25f, 0.25f, 1f);
        [ColorUsage(true, true)][SerializeField] private Color dualRevolverLaserColor = new Color(2.2f, 0.12f, 0.15f, 1f);
        [Min(0f)][SerializeField] private float dualRevolverLaserLength = 26f;
        [SerializeField] private AudioClip dualRevolverChargeClip;
        [Range(0f, 2f)][SerializeField] private float dualRevolverChargeVolume = 0.9f;
        [Tooltip("모았던 기를 쏘는 순간의 소리(Q 기본 액션 소리는 누르는 순간이 아니라 이때 난다).")]
        [SerializeField] private AudioClip dualRevolverFireClip;
        [Range(0f, 2f)][SerializeField] private float dualRevolverFireVolume = 1f;
        [Tooltip("Q를 누르는 순간 점프 한 번 더 하듯 \"팍\" 뜬다 — 평소 점프 대비 배율(1 = 점프 한 번과 같은 높이).")]
        [Min(0f)][SerializeField] private float dualRevolverJumpScale = 0.75f;
        [Tooltip("기 모으는 동안, 올라갔다가 내려올 때의 중력 배율(작을수록 \"지이잉\" 천천히 내려온다). 쏘는 순간 원래대로.")]
        [Range(0f, 1f)][SerializeField] private float dualRevolverFloatGravity = 0.06f;
        [Min(0.1f)][SerializeField] private float dualRevolverFloatMaxFallSpeed = 0.7f;
        [Tooltip("기 모으는 동안 좌우 이동 속도 배율(작을수록 느리게).")]
        [Range(0f, 1f)][SerializeField] private float dualRevolverChargeMoveSpeed = 0.15f;

        [Header("Skill E - Technical Jump")]
        [Tooltip("바닥 기준, 0 ~ 180 사이로 입력")]
        [Min(0f)][SerializeField] private float techJumpAngle = 60f;
        [Min(0f)][SerializeField] private float techJumpPower = 30f;
        [Min(0f)][SerializeField] private float techJumpDuration = 0.08f;
        [SerializeField] private CharacterThrowable techJumpFlashBangPrefab;
        [Min(0f)][SerializeField] private float techJumpFlashBangSpeed = 8f;
        [Min(0f)][SerializeField] private float techJumpFlashBangRange = 0.8f;
        [Min(0f)][SerializeField] private float techJumpFlashBangSlowDuration = 0.5f;
        [Min(0f)][SerializeField] private float techJumpFlashBangSlowRatio = 0.4f;
        [Tooltip("섬광탄이 터질 때 그 자리에서 재생할 이펙트(모든 클라).")]
        [SerializeField] private GameObject techJumpFlashBangExplodeVfx;
        [Min(0.01f)][SerializeField] private float techJumpFlashBangExplodeVfxScale = 0.5f;
        [SerializeField] private AudioClip techJumpFlashBangExplodeClip;
        [Range(0f, 2f)][SerializeField] private float techJumpFlashBangExplodeVolume = 1f;
        [Tooltip("섬광 범위 안에 있던 플레이어 화면이 이만큼 하얗게 번쩍인다(눈부심).")]
        [Range(0f, 1f)][SerializeField] private float techJumpFlashBangBlindAlpha = 0.75f;

        [Header("Skill R - Dead Eye")]
        [SerializeField] private CharacterProjectile deadEyeProjectilePrefab;
        [Tooltip("저격 착탄 시 데미지를 입힐 원형 범위 반경(FindEnemiesInCircle에 사용).")]
        [Min(0f)][SerializeField] private float deadEyeRadius = 1f;
        [Min(0f)][SerializeField] private float deadEyeProjectileFireCooltime = 0.75f;
        [Min(0)][SerializeField] private int deadEyeProjectileCharges = 3;
        [Min(0f)][SerializeField] private float deadEyeSnipingTimeLimit = 7.5f;
        [Min(0f)][SerializeField] private float deadEyeReuseCooltime = 0.25f;
        [Tooltip("저격 모드가 끝난 뒤 다시 쓰기까지의 쿨타임(초). 무적이라 연달아 못 쓰게 한다.")]
        [Min(0f)][SerializeField] private float deadEyeCooldownAfterUse = 12f;
        [Tooltip("꾹 눌러 100%까지 모으는 데 걸리는 시간(초). 놓는 순간 그만큼의 위력으로 쏜다.")]
        [Min(0.05f)][SerializeField] private float deadEyeChargeTime = 1f;
        [Tooltip("0%에서 바로 놓았을 때의 위력 비율(100%면 1).")]
        [Range(0f, 1f)][SerializeField] private float deadEyeMinChargeDamageRatio = 0.35f;
        [Tooltip("저격 모드인 동안 몸을 감싸는 무적 표시 이펙트(계속 켜져 있다가 모드가 끝나면 사라진다).")]
        [SerializeField] private GameObject deadEyeInvulnerableVfx;
        [Min(0.01f)][SerializeField] private float deadEyeInvulnerableVfxScale = 0.4f;
        [Tooltip("무적 표시에 겹쳐 도는 반짝임(선택).")]
        [SerializeField] private GameObject deadEyeInvulnerableSparkleVfx;
        [Min(0.01f)][SerializeField] private float deadEyeInvulnerableSparkleVfxScale = 0.4f;
        [Tooltip("무적 동안 몸을 감싸고 맥박치는 빛 고리 색(코드로 그린다, 블룸이 걸리게 밝게).")]
        [ColorUsage(true, true)][SerializeField] private Color deadEyeInvulnerableRingColor = new Color(1f, 0.25f, 1f, 1f);
        [Min(0.1f)][SerializeField] private float deadEyeInvulnerableRingSize = 1.8f;
        [Tooltip("조준점이 적 위에 있다고 표시(락온)할 반경. 0 이하면 deadEyeRadius를 따릅니다.")]
        [Min(0f)][SerializeField] private float deadEyeLockRadius = 0f;

        [Header("Ultimate - Dead Eye Sounds")]
        [Tooltip("궁극기(저격 모드)를 켤 때 재생")]
        [SerializeField] private AudioClip deadEyeActivateClip;
        [Range(0f, 2f)][SerializeField] private float deadEyeActivateVolume = 1f;
        [Tooltip("저격 모드에서 한 발 쏠 때 재생(이때는 기본공격 사운드 대신 이 소리가 난다)")]
        [SerializeField] private AudioClip deadEyeShotClip;
        [Range(0f, 2f)][SerializeField] private float deadEyeShotVolume = 1f;

        [Header("Ultimate - Dead Eye Camera (내 화면에서만)")]
        [Tooltip("궁극기를 켤 때 화면 흔들림 세기(월드 단위)")]
        [Min(0f)][SerializeField] private float deadEyeShakeMagnitude = 0.6f;
        [Min(0f)][SerializeField] private float deadEyeShakeDuration = 0.35f;
        [Tooltip("마우스가 가리키는 지점 쪽으로 카메라를 추가로 당기는 최대 거리(월드 단위). 줌인 자체도 그 지점을 기준으로 한다.")]
        [Min(0f)][SerializeField] private float deadEyeZoomOffset = 1.2f;
        [Tooltip("줌인 비율(0.15 = 15% 가까이)")]
        [Range(0f, 0.5f)][SerializeField] private float deadEyeZoomRatio = 0.15f;
        [Tooltip("당겨졌다가 원래대로 돌아오는 데 걸리는 시간(초)")]
        [Min(0.05f)][SerializeField] private float deadEyeZoomDuration = 0.6f;

        [Header("Ultimate - Dead Eye Shot Camera (쏠 때, 내 화면에서만)")]
        [Min(0f)][SerializeField] private float deadEyeShotShakeMagnitude = 0.45f;
        [Min(0f)][SerializeField] private float deadEyeShotShakeDuration = 0.25f;
        [Min(0f)][SerializeField] private float deadEyeShotZoomOffset = 0.8f;
        [Range(0f, 0.5f)][SerializeField] private float deadEyeShotZoomRatio = 0.1f;
        [Min(0.05f)][SerializeField] private float deadEyeShotZoomDuration = 0.4f;

        // 사운드/조준경 연출을 모든 클라에서 똑같이 보이게 하기 위한 네트워크 상태.
        [Networked] private NetworkBool NetIsSniping { get; set; }
        [Networked] private int NetSnipeShotSequence { get; set; }
        [Networked] private TickTimer NetDualRevolverChargeTimer { get; set; }
        [Networked] private int NetDualRevolverFireSequence { get; set; }
        [Networked] private NetworkBool NetSnipeCharging { get; set; }
        [Networked] private int NetSnipeChargeStartTick { get; set; }
        [Networked] private Vector2 NetSnipeAim { get; set; }
        [Networked] private TickTimer NetSnipeEndTimer { get; set; }
        [Networked] private Vector2 NetSnipeShotPos { get; set; }
        [Networked] private float NetSnipeShotCharge { get; set; }

        private bool renderStateInitialized;
        private bool lastRenderedSniping;
        private int lastRenderedSnipeShotSequence;
        private int lastRenderedDualRevolverFireSequence;
        private bool renderedCharging;
        private GameObject chargeVfxInstance;
        private GameObject invulnerableVfxInstance;
        private GameObject invulnerableSparkleInstance;
        private SpriteRenderer invulnerableRing;
        private LineRenderer laserCore;
        private LineRenderer laserGlow;
        private CharacterTimerHandle dualRevolverChargeTimer;
        private bool dualRevolverFloating;
        private bool dualRevolverLeftGround;
        private float fireFlashTime = -10f;
        private Vector2 fireFlashAim;

        [Header("Passive - Dirty Carnival")]
        [Tooltip("캐릭터 등 기준, 0 ~ 360 사이로 입력")]
        [Min(0f)][SerializeField] private float carnivalBackAttackCriterionAngle = 90f;
        [Min(1f)][SerializeField] private float carnivalBackAttackAdditionalDamageMultiplier = 2f;

        private int EffectiveBurstCharges => Mathf.Max(1, Mathf.RoundToInt(burstCharges * MaxAmmoMultiplier));
        private float EffectiveBurstReloadDuration =>
            (burstReloadingDuration > 0f ? burstReloadingDuration : (Definition != null ? Definition.GetCooldown(CharacterActionType.BasicAttack) : 1f))
            * ReloadSpeedMultiplier;

        private bool isSniping = false;
        private CharacterTimerHandle deadEyeSnipingTimeTimer;

        /// <summary>데드아이(궁극기 저격 모드) 중인지. 조준경 UI(ChaserScopeUI) 등 연출용.</summary>
        public bool IsSniping => Object != null && NetIsSniping;
        protected override bool IsUltimateInProgress => IsSniping;
        // 데드아이(저격 모드) 동안은 무적.
        protected override bool IsInvulnerable => IsSniping;
        private bool IsChargingDualRevolver => Object != null && NetDualRevolverChargeTimer.IsRunning && !NetDualRevolverChargeTimer.Expired(Runner);

        /// <summary>지금 aimWorld를 쏘면 적이 맞는지(조준점 락온 표시용, 내 화면에서만 쓴다).</summary>
        public bool IsAimOnEnemy(Vector2 aimWorld)
        {
            float radius = deadEyeLockRadius > 0f ? deadEyeLockRadius : deadEyeRadius;
            return FindEnemiesInCircle(aimWorld, radius, targetLayer).Count > 0;
        }
        /// <summary>저격 모드에서 쏜 발 수(누적). 값이 바뀌면 한 발 쏜 것 — 조준경 반동 연출용.</summary>
        public int SnipeShotCount => Object != null ? NetSnipeShotSequence : 0;
        /// <summary>저격탄을 꾹 눌러 모으는 중인지.</summary>
        public bool IsSnipeCharging => Object != null && NetIsSniping && NetSnipeCharging;
        /// <summary>모은 정도 0~1(모으는 중이 아니면 0).</summary>
        public float SnipeChargeRatio => IsSnipeCharging && Runner != null
            ? Mathf.Clamp01((Runner.Tick - NetSnipeChargeStartTick) * Runner.DeltaTime / deadEyeChargeTime)
            : 0f;
        /// <summary>조준점(월드). 모든 클라에 동기화되므로 상대 화면에서도 조준 UI를 띄울 수 있다.</summary>
        public Vector2 SnipeAimWorld => Object != null ? NetSnipeAim : Vector2.zero;
        /// <summary>저격 모드 남은 시간 비율 1→0.</summary>
        public float SnipeTimeRemaining01 => IsSniping && Runner != null && deadEyeSnipingTimeLimit > 0f
            ? Mathf.Clamp01((NetSnipeEndTimer.RemainingTime(Runner) ?? 0f) / deadEyeSnipingTimeLimit)
            : 0f;
        public Vector2 LastSnipeShotPosition => Object != null ? NetSnipeShotPos : Vector2.zero;
        public float LastSnipeShotCharge => Object != null ? NetSnipeShotCharge : 0f;

        // ---- AI(ChaserBotBrain)가 판단에 쓰는 읽기 전용 수치 ----
        public float BurstBulletSpeed => burstBulletProjectileSpeed;
        public float DualRevolverSpeed => dualRevolverProjectileSpeed;
        public float DeadEyeRadius => deadEyeRadius;

        public Vector2 ProjectileOriginPosition => ProjectileOrigin.position;
        public Vector2 AttackOriginPosition => AttackOrigin.position;

        private bool isFirstBurst = true;

        private CharacterThrowable currentFlashBangEntity;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (isSniping)
                return OnSnipingBasicAttack(context);

            return OnNormalBasicAttack(context);
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            CharacterProjectile prefab = ResolveDualRevolverPrefab(burstBulletProjectilePrefab);

            if (prefab == null || IsChargingDualRevolver) return false;

            float damage = context.Damage;
            if (dualRevolverChargeTime <= 0f)
            {
                FireDualRevolver(prefab, damage);
                return true;
            }

            // 기 모으기: 위로 툭 떠서 둥실 체공하며 모은다. 그동안 평타/E는 못 쓰고, 다 모이면 그때의 마우스 방향으로 쏜다.
            Movement.Launch(Movement.JumpVelocity * dualRevolverJumpScale);
            dualRevolverFloating = true;
            dualRevolverLeftGround = false;
            NetDualRevolverChargeTimer = TickTimer.CreateFromSeconds(Runner, dualRevolverChargeTime);
            SetActionEnabled(CharacterActionType.BasicAttack, false);
            SetActionEnabled(CharacterActionType.SkillE, false);
            dualRevolverChargeTimer = ScheduleTimer(dualRevolverChargeTime, () =>
            {
                EndDualRevolverCharge();
                FireDualRevolver(prefab, damage);
            });
            return true;
        }

        private void FireDualRevolver(CharacterProjectile prefab, float damage)
        {
            Vector2 aim = AimDirection.sqrMagnitude > 0.0001f ? AimDirection.normalized : new Vector2(FacingDirection, 0f);
            SpawnProjectile(
                prefab,
                ProjectileOrigin.position,
                aim,
                dualRevolverProjectileSpeed,
                damage,
                targetLayer);

            PlayActionEffect(CharacterActionType.SkillQ, EffectOrigin.position, AimAngle);
            NetDualRevolverFireSequence++;
        }

        private void EndDualRevolverCharge()
        {
            NetDualRevolverChargeTimer = default;
            if (!isSniping)
            {
                SetActionEnabled(CharacterActionType.BasicAttack, true);
                SetActionEnabled(CharacterActionType.SkillE, true);
            }
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            CharacterThrowable flashBang = SpawnThrowable(
                techJumpFlashBangPrefab,
                CharacterActionType.SkillE,
                ProjectileOrigin.position,
                Vector2.down,
                techJumpFlashBangSpeed);

            if (flashBang == null)
            {
                Debug.LogWarning("[ChaserCharacter] 섬광탄 소환에 실패했습니다.");
                return false;
            }

            currentFlashBangEntity = flashBang;
            BackJump(context.AimDirection);

            PlayActionEffect(CharacterActionType.SkillE, AttackOrigin.position, AimAngle);
            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            ChangeSnipingMode();

            return true;
        }

        protected override float ModifyOutgoingDamage(CharacterBase target, float damage, CharacterDamageSource source)
        {
            bool isBackOfTarget = IsBehindTargetAimDirection(target, Mathf.Clamp(0f, carnivalBackAttackCriterionAngle, 360f));

            return isBackOfTarget ? damage * carnivalBackAttackAdditionalDamageMultiplier : damage;
        }

        protected override void OnOwnedEntityDestroyed(CharacterOwnedEntity entity, OwnedEntityDestroyReason reason)
        {
            if (reason != OwnedEntityDestroyReason.FuseExpired ||
                entity != currentFlashBangEntity) return;

            OnFlashBangExpired(currentFlashBangEntity);
        }

        protected override void OnResetCharacter()
        {
            TurnOffSnipingMode(isResettingCooldownUnnecessary: true);
            CancelTimer(dualRevolverChargeTimer);
            EndDualRevolverCharge();
            EndDualRevolverFloat();
            NetSnipeCharging = false;
            isFirstBurst = true;
        }

        private bool OnNormalBasicAttack(CharacterActionContext context)
        {
            if (isFirstBurst)
            {
                SetActionCharges(CharacterActionType.BasicAttack, EffectiveBurstCharges);
                isFirstBurst = false;
            }

            if (burstBulletProjectilePrefab == null) return false;
            Vector2 aim = context.AimDirection.sqrMagnitude > 0.0001f
                ? context.AimDirection.normalized
                : new Vector2(FacingDirection, 0f);

            float forkDamage = context.Damage * ForkedProjectileDamageMultiplier;

            int shotCount = burstBulletProjectileFireCount + (useBurstArcForForkedProjectiles ? ForkedProjectileCount : 0);
            int forkShotIndex = 0;

            for (int i = 0; i < shotCount; i++)
            {
                // 총알 발사 개수가 1개 일수도 있으므로 방어코드. 개수가 1개면 0.5, 아니면 나누기
                float angleRatio = shotCount == 1 ? 0.5f : (float)i / (shotCount - 1);
                float offset = -burstArcAngle * 0.5f + burstArcAngle * angleRatio;

                Vector2 dir = Rotate(aim, offset);

                // 갈래 마법 - BurstArc 적용 시
                // 갈래 탄환을 기존 탄환에 섞어 골고루 쏨
                // 쏴야할 모든 탄환을 ForkedProjectileCount로 나눠 묶음을 만들고, 해당 묶음의 가운데를 갈래 탄환으로 지정
                bool isForkShot = useBurstArcForForkedProjectiles && forkShotIndex < ForkedProjectileCount &&
                    i == (int)((forkShotIndex + 0.5f) * shotCount / ForkedProjectileCount);

                if (isForkShot)
                    forkShotIndex++;

                FireBurstBullet(dir, isForkShot ? forkDamage : context.Damage);
            }

            // 갈래 마법 - BurstArc 미적용 시
            if (!useBurstArcForForkedProjectiles && ForkedProjectileCount > 0)
            {
                for (int i = 0; i < ForkedProjectileCount; i++)
                {
                    float angleOffset = forkAngleStep * (i / 2 + 1) * (i % 2 == 0 ? 1f : -1f);
                    FireBurstBullet(Rotate(aim, angleOffset), forkDamage);
                }
            }

            PlayActionEffect(CharacterActionType.BasicAttack, EffectOrigin.position, context.AimAngle);

            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;
            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, EffectiveBurstReloadDuration);
                SetActionCharges(CharacterActionType.BasicAttack, EffectiveBurstCharges + 1);
                NotifyReloadStarted(EffectiveBurstReloadDuration);
            }
            else
                ResetCooldownDuration(CharacterActionType.BasicAttack);

            return true;
        }

        private void FireBurstBullet(Vector2 direction, float damage)
        {
            CharacterProjectile projectile = SpawnProjectile(
                burstBulletProjectilePrefab,
                ProjectileOrigin.position,
                direction,
                burstBulletProjectileSpeed,
                damage,
                targetLayer);

            if (projectile == null)
                return;

            // 바운스 마법 / 폭발 마법
            bool explosive = HasExplosiveProjectile;
            int bounces = explosive ? 0 : ProjectileBounceCount;
            projectile.ConfigureAugmentBehavior(bounces, explosive, ExplosiveProjectileDamageMultiplier);
        }

        // 저격 모드의 기본공격: 누르는 순간엔 모으기만 시작한다(탄/쿨타임 소모 없음).
        // 실제 발사는 버튼을 놓는 틱에 FixedUpdateNetwork에서 ReleaseSnipe로 처리한다.
        private bool OnSnipingBasicAttack(CharacterActionContext context)
        {
            if (NetSnipeCharging || GetActionCharges(CharacterActionType.BasicAttack) <= 0)
                return false;

            NetSnipeCharging = true;
            NetSnipeChargeStartTick = Runner.Tick;
            return false;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (Object == null || !Object.HasStateAuthority)
                return;

            TickDualRevolverFloat();

            if (!isSniping)
            {
                NetSnipeCharging = false;
                return;
            }

            NetSnipeAim = LastInput.AimWorldPosition;
            if (NetSnipeCharging && (!LastInput.BasicAttackHeld || IsDead))
                ReleaseSnipe();
            // 버튼을 미리 누르고 있었으면(발사 직후 재장전 중에 눌렀거나 모드 켜기 전부터 누르고 있던 경우)
            // 쏠 수 있게 되는 순간 바로 이어서 모으기 시작한다 — "누르는 순간"만 받으면 입력이 씹힌 것처럼 느껴진다.
            else if (!NetSnipeCharging && LastInput.BasicAttackHeld && !IsDead &&
                     GetCooldownRemaining(CharacterActionType.BasicAttack) <= 0f &&
                     GetActionCharges(CharacterActionType.BasicAttack) > 0)
            {
                NetSnipeCharging = true;
                NetSnipeChargeStartTick = Runner.Tick;
            }
        }

        private void ReleaseSnipe()
        {
            float ratio = Mathf.Clamp01((Runner.Tick - NetSnipeChargeStartTick) * Runner.DeltaTime / deadEyeChargeTime);
            NetSnipeCharging = false;
            if (IsDead)
                return;

            Vector2 aim = NetSnipeAim;
            float damage = Definition.GetDamage(CharacterActionType.Ultimate) * Mathf.Lerp(deadEyeMinChargeDamageRatio, 1f, ratio);

            List<CharacterBase> hitEnemies = FindEnemiesInCircle(aim, deadEyeRadius, targetLayer);
            foreach (CharacterBase enemy in hitEnemies)
            {
                DealDamage(enemy, damage);
            }

            PlayActionEffect(CharacterActionType.Ultimate, EffectOrigin.position, AimAngle);
            NetSnipeShotPos = aim;
            NetSnipeShotCharge = ratio;
            NetSnipeShotSequence++;

            int left = GetActionCharges(CharacterActionType.BasicAttack) - 1;
            if (left <= 0)
            {
                TurnOffSnipingMode(isTurnedOffByNoAmmo: true);
                return;
            }
            SetActionCharges(CharacterActionType.BasicAttack, left);
            StartCooldown(CharacterActionType.BasicAttack);
        }

        private void TickDualRevolverFloat()
        {
            if (!dualRevolverFloating)
                return;
            // 쏘고 나면(기 모으기 끝) 바로 원래 중력: 스킬이 끝났는데 느리게 내려오지 않게.
            if (IsDead || !IsChargingDualRevolver)
            {
                EndDualRevolverFloat();
                return;
            }
            // 기 모으는 동안엔(땅에 내려와도) 좌우로 아주 느리게만 움직인다.
            Movement.SetSkillSpeedMultiplier(dualRevolverChargeMoveSpeed);
            if (!Movement.IsGrounded)
                dualRevolverLeftGround = true;
            // 올라갈 땐 보통 중력으로 "팍" 짧게, 꼭대기를 지나 내려오기 시작하면 "지이잉" 천천히. 땅에 닿으면 보통 중력.
            bool landed = dualRevolverLeftGround && Movement.IsGrounded;
            if (!landed && Movement.VerticalVelocity <= 0f)
                Movement.SetGravityMultiplier(dualRevolverFloatGravity, dualRevolverFloatMaxFallSpeed);
            else
                Movement.SetGravityMultiplier(1f);
        }

        private void EndDualRevolverFloat()
        {
            dualRevolverFloating = false;
            dualRevolverLeftGround = false;
            Movement?.SetGravityMultiplier(1f);
            Movement?.SetSkillSpeedMultiplier(1f);
        }

        private void BackJump(Vector2 AimDirection)
        {
            float techJumpAngleRad = Mathf.Min(180, techJumpAngle) * Mathf.Deg2Rad;

            float jumpDirectionX = default;

            // 빗변 길이 1 기준 연산
            if (AimDirection.x <= 0)
                jumpDirectionX = Mathf.Cos(techJumpAngleRad);
            else
                jumpDirectionX = -1 * Mathf.Cos(techJumpAngleRad);

            float jumpDirectionY = Mathf.Sin(techJumpAngleRad);

            Vector2 jumpDirection = new Vector2(jumpDirectionX, jumpDirectionY).normalized;

            Movement.StartDash(jumpDirection, techJumpPower, techJumpDuration);
        }

        private void OnFlashBangExpired(CharacterThrowable flashBang)
        {
            List<CharacterBase> foundEnemies = FindEnemiesInCircle(flashBang.transform.position, techJumpFlashBangRange, targetLayer);
            Rpc_PlayFlashBang(flashBang.transform.position);

            foreach (CharacterBase foundEnemy in foundEnemies)
            {
                DealDamage(foundEnemy, Definition.GetDamage(CharacterActionType.SkillE));
                ApplySlow(foundEnemy, techJumpFlashBangSlowRatio, techJumpFlashBangSlowDuration);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayFlashBang(Vector2 position)
        {
            FxSpawner.Spawn(techJumpFlashBangExplodeVfx, position, Quaternion.identity, techJumpFlashBangExplodeVfxScale);
            SoundManager.Instance?.PlaySfx(techJumpFlashBangExplodeClip, techJumpFlashBangExplodeVolume);

            // 내 캐릭터가 섬광 범위 근처면(던진 쪽 제외) 화면이 하얗게 번쩍인다. 가까이 보는 것만으로도 살짝 번쩍.
            CharacterBase local = All.Find(c => c != null && c.IsLocalPlayer);
            if (local != null)
            {
                float d = Vector2.Distance(local.transform.position, position);
                bool caught = local != this && d <= techJumpFlashBangRange * 1.3f;
                ScreenFlash.Play(Color.white, caught ? techJumpFlashBangBlindAlpha : 0.18f, caught ? 0.12f : 0f, caught ? 0.6f : 0.2f);
            }
            if (TwoPlayerCamera.Instance != null)
                TwoPlayerCamera.Instance.Shake(0.35f, 0.25f);
        }

        private void ChangeSnipingMode()
        {
            if (isSniping) TurnOffSnipingMode();
            else TurnOnSnipingMode();
        }

        private void TurnOnSnipingMode()
        {
            isSniping = true;
            NetIsSniping = true;

            SetMoveAndSkillExceptUltimateCanUse(false);

            SetActionCharges(CharacterActionType.BasicAttack, deadEyeProjectileCharges);
            SetCooldownDuration(CharacterActionType.BasicAttack, deadEyeProjectileFireCooltime);

            SetCooldownDuration(CharacterActionType.Ultimate, deadEyeReuseCooltime);

            deadEyeSnipingTimeTimer = ScheduleTimer(deadEyeSnipingTimeLimit, () => TurnOffSnipingMode());
            NetSnipeEndTimer = TickTimer.CreateFromSeconds(Runner, deadEyeSnipingTimeLimit);
            NetSnipeCharging = false;
        }

        private void TurnOffSnipingMode(bool isTurnedOffByNoAmmo = false, bool isResettingCooldownUnnecessary = false)
        {
            isSniping = false;
            NetIsSniping = false;
            NetSnipeCharging = false;
            NetSnipeEndTimer = default;
            CancelTimer(deadEyeSnipingTimeTimer);

            SetMoveAndSkillExceptUltimateCanUse(true);

            SetActionCharges(
                CharacterActionType.BasicAttack,
                isTurnedOffByNoAmmo ? EffectiveBurstCharges + 1 : EffectiveBurstCharges);
            ResetCooldownDuration(CharacterActionType.BasicAttack);

            if (isResettingCooldownUnnecessary)
                StartCooldown(CharacterActionType.Ultimate);
            else
            {
                // 무적 저격 모드를 연달아 못 쓰게 끝난 뒤 긴 쿨타임을 건다.
                SetCooldownDuration(CharacterActionType.Ultimate, deadEyeCooldownAfterUse);
                StartCooldown(CharacterActionType.Ultimate);
            }
        }

        // 저격 모드에서 쏜 발은 일반 기본공격 사운드 대신 deadEyeShotClip 을 낸다(Render 에서 재생).
        // 마지막 탄을 쏘면 같은 틱에 저격 모드가 꺼지므로, 모드 플래그가 아니라 "이번에 저격탄 카운트가 올랐는지"로 판단한다.
        protected override bool ShouldPlayDefaultActionSound(CharacterActionType action)
        {
            if (action == CharacterActionType.SkillQ && dualRevolverChargeTime > 0f)
                return false;
            if (action == CharacterActionType.BasicAttack && renderStateInitialized &&
                NetSnipeShotSequence != lastRenderedSnipeShotSequence)
                return false;
            return base.ShouldPlayDefaultActionSound(action);
        }

        public override void Render()
        {
            base.Render();

            if (!renderStateInitialized)
            {
                // 늦게 들어온 클라가 이미 지난 소리를 재생하지 않도록 현재 값을 기준점으로 잡는다.
                renderStateInitialized = true;
                lastRenderedSniping = NetIsSniping;
                lastRenderedSnipeShotSequence = NetSnipeShotSequence;
                lastRenderedDualRevolverFireSequence = NetDualRevolverFireSequence;
                return;
            }

            RenderDualRevolver();
            RenderInvulnerable();

            if (NetIsSniping && !lastRenderedSniping)
            {
                SoundManager.Instance?.PlaySfx(deadEyeActivateClip, deadEyeActivateVolume);
                PlayCameraPunch(deadEyeShakeMagnitude, deadEyeShakeDuration,
                    deadEyeZoomOffset, deadEyeZoomRatio, deadEyeZoomDuration);
            }
            lastRenderedSniping = NetIsSniping;

            if (NetSnipeShotSequence != lastRenderedSnipeShotSequence)
            {
                lastRenderedSnipeShotSequence = NetSnipeShotSequence;
                SoundManager.Instance?.PlaySfx(deadEyeShotClip, deadEyeShotVolume);
                PlayCameraPunch(deadEyeShotShakeMagnitude, deadEyeShotShakeDuration,
                    deadEyeShotZoomOffset, deadEyeShotZoomRatio, deadEyeShotZoomDuration);
            }
        }

        // ───── Q: 기 모으기(빨간 조준 레이저 + 충전 이펙트 + 살짝 줌) → 팡! ─────
        private void RenderDualRevolver()
        {
            bool charging = IsChargingDualRevolver && !IsDead;
            if (charging && !renderedCharging)
            {
                chargeVfxInstance = FxSpawner.Spawn(dualRevolverChargeVfx, EffectOrigin.position, Quaternion.identity,
                    dualRevolverChargeVfxScale, 0f, FxSpawner.DefaultSortingOrder, EffectOrigin);
                FxSpawner.Tint(chargeVfxInstance, dualRevolverChargeTint);
                SoundManager.Instance?.PlaySfx(dualRevolverChargeClip, dualRevolverChargeVolume, 0f);
                if (IsLocalPlayer && TwoPlayerCamera.Instance != null)
                    TwoPlayerCamera.Instance.PunchZoomTowardsMouse(0.35f, 0.07f, dualRevolverChargeTime + 0.4f);
            }
            if (!charging && renderedCharging)
            {
                if (chargeVfxInstance != null)
                    Destroy(chargeVfxInstance);
                chargeVfxInstance = null;
            }
            renderedCharging = charging;
            UpdateLaser(charging);

            if (NetDualRevolverFireSequence != lastRenderedDualRevolverFireSequence)
            {
                lastRenderedDualRevolverFireSequence = NetDualRevolverFireSequence;
                SoundManager.Instance?.PlaySfx(dualRevolverFireClip, dualRevolverFireVolume, 0f);
                fireFlashTime = Time.time;
                fireFlashAim = AimDirection.sqrMagnitude > 0.0001f ? AimDirection.normalized : new Vector2(FacingDirection, 0f);
                if (TwoPlayerCamera.Instance != null)
                {
                    // 쏜 쪽은 크게, 맞는 쪽 화면도 살짝 흔들린다.
                    TwoPlayerCamera.Instance.Shake(IsLocalPlayer ? 0.7f : 0.3f, 0.32f);
                    if (IsLocalPlayer)
                    {
                        TwoPlayerCamera.Instance.PunchZoom(-fireFlashAim, 0.6f, 0.06f, 0.32f);
                        ScreenFlash.Play(new Color(1f, 0.25f, 0.25f), 0.18f, 0f, 0.18f);
                    }
                }
            }
        }

        private void UpdateLaser(bool charging)
        {
            // 쏜 직후 0.22초: 궤적 전체가 하얗게 번쩍였다가 빨갛게 사라진다(쏜 방향이 확실히 보이게).
            float sinceFire = Time.time - fireFlashTime;
            if (!charging && sinceFire < 0.22f)
            {
                if (laserCore == null)
                {
                    laserGlow = CreateLaserLine("Q Laser Glow");
                    laserCore = CreateLaserLine("Q Laser Core");
                }
                float k = 1f - sinceFire / 0.22f;
                Vector3 a0 = ProjectileOrigin.position;
                Vector3 b0 = a0 + (Vector3)(fireFlashAim * dualRevolverLaserLength * 1.3f);
                a0.z = b0.z = 0f;
                SetLaser(laserCore, a0, b0, 0.26f * k, new Color(1f, 0.85f, 0.85f, k));
                SetLaser(laserGlow, a0, b0, 1.1f * k, new Color(1f, 0.15f, 0.2f, 0.6f * k));
                return;
            }
            if (!charging)
            {
                if (laserCore != null) laserCore.enabled = laserGlow.enabled = false;
                return;
            }
            if (laserCore == null)
            {
                laserGlow = CreateLaserLine("Q Laser Glow");
                laserCore = CreateLaserLine("Q Laser Core");
            }

            float remaining = NetDualRevolverChargeTimer.RemainingTime(Runner) ?? 0f;
            float p = dualRevolverChargeTime > 0f ? Mathf.Clamp01(1f - remaining / dualRevolverChargeTime) : 1f;
            Vector2 aim = AimDirection.sqrMagnitude > 0.0001f ? AimDirection.normalized : new Vector2(FacingDirection, 0f);
            Vector3 start = ProjectileOrigin.position;
            Vector3 end = start + (Vector3)(aim * dualRevolverLaserLength);
            start.z = end.z = 0f;

            // 모일수록 굵고 진해지고, 끝 무렵엔 빠르게 깜빡여 "곧 쏜다"는 걸 알린다.
            float flicker = p > 0.75f ? (Mathf.Sin(Time.time * 60f) > 0f ? 1f : 0.45f) : 0.85f + 0.15f * Mathf.Sin(Time.time * 25f);
            float coreW = Mathf.Lerp(0.04f, 0.12f, p) * flicker;
            float glowW = Mathf.Lerp(0.16f, 0.6f, p) * flicker;
            Color core = Color.Lerp(dualRevolverLaserColor, new Color(2.5f, 1.2f, 1.2f, 1f), p * p);
            Color glow = new Color(dualRevolverLaserColor.r, dualRevolverLaserColor.g, dualRevolverLaserColor.b, Mathf.Lerp(0.15f, 0.45f, p));
            SetLaser(laserCore, start, end, coreW, core);
            SetLaser(laserGlow, start, end, glowW, glow);
        }

        private LineRenderer CreateLaserLine(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.numCapVertices = 4;
            lr.sharedMaterial = GetLaserMaterial();
            lr.sortingOrder = FxSpawner.DefaultSortingOrder - 5;
            lr.enabled = false;
            return lr;
        }

        private static Material laserMaterial;
        private static Material GetLaserMaterial()
        {
            if (laserMaterial != null)
                return laserMaterial;
            laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Q Laser (Additive)" };
            laserMaterial.SetFloat("_Surface", 1f);
            laserMaterial.SetFloat("_Blend", 2f);
            laserMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            laserMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            laserMaterial.SetFloat("_ZWrite", 0f);
            laserMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            laserMaterial.EnableKeyword("_BLENDMODE_ADD");
            laserMaterial.SetColor("_BaseColor", new Color(2f, 2f, 2f, 1f));
            laserMaterial.renderQueue = 3000;
            return laserMaterial;
        }

        private static void SetLaser(LineRenderer lr, Vector3 a, Vector3 b, float width, Color color)
        {
            lr.enabled = true;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.widthMultiplier = width;
            // 총구 쪽이 진하고 멀어질수록 옅어진다
            // 머티리얼은 두 줄이 같이 쓰므로 색은 선의 꼭짓점 색으로 준다(머티리얼 쪽 2배 밝기로 블룸이 걸린다).
            float m = Mathf.Max(1f, Mathf.Max(color.r, Mathf.Max(color.g, color.b)));
            Color c = new Color(color.r / m, color.g / m, color.b / m, 1f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a * 0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            lr.colorGradient = g;
        }

        // ───── 궁: 저격 모드 동안 무적 표시 ─────
        private void RenderInvulnerable()
        {
            bool on = NetIsSniping && !IsDead;
            if (on && invulnerableVfxInstance == null && deadEyeInvulnerableVfx != null)
            {
                invulnerableVfxInstance = FxSpawner.Spawn(deadEyeInvulnerableVfx, transform.position, Quaternion.identity,
                    deadEyeInvulnerableVfxScale, 0f, FxSpawner.DefaultSortingOrder - 20, transform);
                invulnerableSparkleInstance = FxSpawner.Spawn(deadEyeInvulnerableSparkleVfx, transform.position, Quaternion.identity,
                    deadEyeInvulnerableSparkleVfxScale, 0f, FxSpawner.DefaultSortingOrder - 10, transform);
            }
            UpdateInvulnerableRing(on);
            if (!on && invulnerableVfxInstance != null)
            {
                Destroy(invulnerableVfxInstance);
                invulnerableVfxInstance = null;
                if (invulnerableSparkleInstance != null)
                    Destroy(invulnerableSparkleInstance);
                invulnerableSparkleInstance = null;
            }
        }

        // 몸을 감싸는 빛 고리: 천천히 숨 쉬듯 커졌다 작아지며, 켜지는 순간엔 크게 퍼졌다가 자리 잡는다.
        private float ringOnTime;
        private void UpdateInvulnerableRing(bool on)
        {
            if (!on)
            {
                if (invulnerableRing != null) invulnerableRing.enabled = false;
                return;
            }
            if (invulnerableRing == null)
            {
                var go = new GameObject("Invulnerable Ring");
                go.transform.SetParent(transform, false);
                invulnerableRing = go.AddComponent<SpriteRenderer>();
                invulnerableRing.sprite = GetRingSprite();
                // 스프라이트 색이 그대로 나오도록 2D 스프라이트 Unlit 셰이더(조명 영향 없음)를 쓴다.
                Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (unlit != null)
                    invulnerableRing.sharedMaterial = new Material(unlit) { name = "Invulnerable Ring" };
                invulnerableRing.sortingOrder = FxSpawner.DefaultSortingOrder - 15;
            }
            if (!invulnerableRing.enabled)
            {
                invulnerableRing.enabled = true;
                ringOnTime = Time.time;
            }
            float since = Time.time - ringOnTime;
            float pop = Mathf.Max(0f, 1f - since / 0.35f);
            float breathe = 1f + 0.06f * Mathf.Sin(Time.time * 5f);
            float size = deadEyeInvulnerableRingSize * breathe * (1f + pop * 0.8f);
            invulnerableRing.transform.position = (Vector3)(Vector2)transform.position + new Vector3(0f, 0.45f, 0f);
            // 부모(캐릭터) 크기/좌우 반전과 상관없이 월드 기준 size 크기로 맞춘다.
            Vector3 parentScale = transform.lossyScale;
            invulnerableRing.transform.localScale = new Vector3(size / parentScale.x, size / parentScale.y, 1f);
            invulnerableRing.transform.Rotate(0f, 0f, 40f * Time.deltaTime, Space.Self);
            float m = Mathf.Max(1f, Mathf.Max(deadEyeInvulnerableRingColor.r, Mathf.Max(deadEyeInvulnerableRingColor.g, deadEyeInvulnerableRingColor.b)));
            float a = Mathf.Lerp(0.75f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 5f)) * (1f - pop * 0.3f);
            invulnerableRing.color = new Color(deadEyeInvulnerableRingColor.r / m, deadEyeInvulnerableRingColor.g / m, deadEyeInvulnerableRingColor.b / m, a);
        }

        private static Sprite ringSprite;
        private static Sprite GetRingSprite()
        {
            if (ringSprite != null)
                return ringSprite;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = new Vector2(x - (n - 1) * 0.5f, y - (n - 1) * 0.5f).magnitude / (n * 0.5f);
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.045f, 2f));           // 선명한 테두리
                float glow = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.1f, 2f)) * 0.45f;    // 테두리 둘레 번짐
                float fill = Mathf.SmoothStep(0f, 1f, r / 0.82f) * 0.12f * (r < 0.82f ? 1f : 0f); // 안쪽 옅은 막
                float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 0.99f, r));                     // 텍스처 가장자리에선 0(네모 자국 방지)
                // 테두리를 따라 끊긴 마디(회전하면 돌아가는 게 보이게)
                float ang = Mathf.Atan2(y - n * 0.5f, x - n * 0.5f);
                float dash = 0.75f + 0.25f * Mathf.Sign(Mathf.Sin(ang * 6f));
                float a = Mathf.Clamp01(ring * dash + glow + fill) * edge;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            ringSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return ringSprite;
        }

        // 궁극기 진입/저격탄 발사 연출: 한 번 흔들고, 조준(마우스) 방향으로 살짝 당겨 줌인했다가 돌아온다.
        // 쓰는 사람 화면에서만(상대 화면 카메라는 건드리지 않음).
        private void PlayCameraPunch(float shakeMagnitude, float shakeDuration, float zoomOffset, float zoomRatio, float zoomDuration)
        {
            if (!IsLocalPlayer || TwoPlayerCamera.Instance == null)
                return;

            TwoPlayerCamera.Instance.Shake(shakeMagnitude, shakeDuration);
            // 화면 중심 기준 "마우스가 가리키는 지점" 쪽으로 당긴다(캐릭터 기준 조준 방향으로 하면
            // 카메라 중심이 두 캐릭터 중간이라 엉뚱한 쪽으로 가 보일 수 있다).
            TwoPlayerCamera.Instance.PunchZoomTowardsMouse(zoomOffset, zoomRatio, zoomDuration);
        }

        // 휠 수동 재장전. 데드아이(저격 모드) 중엔 탄이 궁극기 탄이라 재장전 불가.
        protected override bool TryGetReloadInfo(out int magazine, out float duration)
        {
            magazine = EffectiveBurstCharges;
            duration = EffectiveBurstReloadDuration;
            return !isSniping;
        }

        private void SetMoveAndSkillExceptUltimateCanUse(bool canUse)
        {
            SetMovementEnabled(canUse);

            SetActionEnabled(CharacterActionType.SkillQ, canUse);
            SetActionEnabled(CharacterActionType.SkillE, canUse);
            SetActionEnabled(CharacterActionType.Dash, canUse);
        }

        protected bool IsBehindTargetAimDirection(CharacterBase target, float rearArcAngle)
        {
            if (target == null)
                return false;

            Vector2 targetForward = target.AimDirection.x >= 0 ? Vector2.right : Vector2.left;
            Vector2 targetToAttacker = ((Vector2)transform.position - (Vector2)target.transform.position).normalized;
            float rearHalfAngle = Mathf.Clamp(rearArcAngle, 0f, 360f) * 0.5f;
            return Vector2.Angle(-targetForward, targetToAttacker) <= rearHalfAngle;
        }

        private CharacterProjectile ResolveDualRevolverPrefab(CharacterProjectile fallback)
        {
            return dualRevolverProjectilePrefab != null ? dualRevolverProjectilePrefab : fallback;
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
