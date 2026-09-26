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
        [Min(0f)] [SerializeField] private float burstBulletProjectileSpeed = 15f;
        [Min(1)] [SerializeField] private int burstBulletProjectileFireCount = 4;
        [Min(0f)] [SerializeField] private float burstArcAngle = 45f;
        [Min(0f)] [SerializeField] private float burstReloadingDuration = 2.5f;
        [Min(1)] [SerializeField] private int burstCharges = 4;

        [Header("Skill Q - Dual Revolver")]
        [Tooltip("비어있을 경우 Burst Bullet Projectile Prefab 사용")]
        [SerializeField] private CharacterProjectile dualRevolverProjectilePrefab; //없으면 기본 burstBulletProjectilePrefab사용
        [Min(0)] [SerializeField] private float dualRevolverProjectileSpeed = 45f;

        [Header("Skill E - Technical Jump")]
        [Tooltip("바닥 기준, 0 ~ 180 사이로 입력")]
        [Min(0f)] [SerializeField] private float techJumpAngle = 60f;
        [Min(0f)] [SerializeField] private float techJumpPower = 30f;
        [Min(0f)] [SerializeField] private float techJumpDuration = 0.08f;
        [SerializeField] private CharacterThrowable techJumpFlashBangPrefab;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSpeed = 8f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangRange = 0.8f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSlowDuration = 0.5f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSlowRatio = 0.4f;

        [Header("Skill R - Dead Eye")]
        [SerializeField] private CharacterProjectile deadEyeProjectilePrefab;
        [Min(0f)] [SerializeField] private float deadEyeProjectileFireCooltime = 0.75f;
        [Min(0)] [SerializeField] private int deadEyeProjectileCharges = 3;
        [Min(0f)] [SerializeField] private float deadEyeSnipingTimeLimit = 7.5f;
        [Min(0f)] [SerializeField] private float deadEyeReuseCooltime = 0.25f;

        [Header("Ultimate - Dead Eye Sounds")]
        [Tooltip("궁극기(저격 모드)를 켤 때 재생")]
        [SerializeField] private AudioClip deadEyeActivateClip;
        [Range(0f, 2f)] [SerializeField] private float deadEyeActivateVolume = 1f;
        [Tooltip("저격 모드에서 한 발 쏠 때 재생(이때는 기본공격 사운드 대신 이 소리가 난다)")]
        [SerializeField] private AudioClip deadEyeShotClip;
        [Range(0f, 2f)] [SerializeField] private float deadEyeShotVolume = 1f;

        [Header("Ultimate - Dead Eye Camera (내 화면에서만)")]
        [Tooltip("궁극기를 켤 때 화면 흔들림 세기(월드 단위)")]
        [Min(0f)] [SerializeField] private float deadEyeShakeMagnitude = 0.6f;
        [Min(0f)] [SerializeField] private float deadEyeShakeDuration = 0.35f;
        [Tooltip("마우스가 가리키는 지점 쪽으로 카메라를 추가로 당기는 최대 거리(월드 단위). 줌인 자체도 그 지점을 기준으로 한다.")]
        [Min(0f)] [SerializeField] private float deadEyeZoomOffset = 1.2f;
        [Tooltip("줌인 비율(0.15 = 15% 가까이)")]
        [Range(0f, 0.5f)] [SerializeField] private float deadEyeZoomRatio = 0.15f;
        [Tooltip("당겨졌다가 원래대로 돌아오는 데 걸리는 시간(초)")]
        [Min(0.05f)] [SerializeField] private float deadEyeZoomDuration = 0.6f;

        [Header("Ultimate - Dead Eye Shot Camera (쏠 때, 내 화면에서만)")]
        [Min(0f)] [SerializeField] private float deadEyeShotShakeMagnitude = 0.45f;
        [Min(0f)] [SerializeField] private float deadEyeShotShakeDuration = 0.25f;
        [Min(0f)] [SerializeField] private float deadEyeShotZoomOffset = 0.8f;
        [Range(0f, 0.5f)] [SerializeField] private float deadEyeShotZoomRatio = 0.1f;
        [Min(0.05f)] [SerializeField] private float deadEyeShotZoomDuration = 0.4f;

        // 사운드/조준경 연출을 모든 클라에서 똑같이 보이게 하기 위한 네트워크 상태.
        [Networked] private NetworkBool NetIsSniping { get; set; }
        [Networked] private int NetSnipeShotSequence { get; set; }

        private bool renderStateInitialized;
        private bool lastRenderedSniping;
        private int lastRenderedSnipeShotSequence;

        [Header("Passive - Dirty Carnival")]
        [Tooltip("캐릭터 등 기준, 0 ~ 360 사이로 입력")]
        [Min(0f)] [SerializeField] private float carnivalBackAttackCriterionAngle = 90f;
        [Min(1f)] [SerializeField] private float carnivalBackAttackAdditionalDamageMultiplier = 2f;

        private bool isSniping = false;
        private CharacterTimerHandle deadEyeSnipingTimeTimer;

        /// <summary>데드아이(궁극기 저격 모드) 중인지. 조준경 UI(ChaserScopeUI) 등 연출용.</summary>
        public bool IsSniping => Object != null && NetIsSniping;
        /// <summary>저격 모드에서 쏜 발 수(누적). 값이 바뀌면 한 발 쏜 것 — 조준경 반동 연출용.</summary>
        public int SnipeShotCount => Object != null ? NetSnipeShotSequence : 0;

        private bool isFirstBurst = true;

        private CharacterThrowable currentFlashBangEntity;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;

            if (isSniping)
            {
                SpawnProjectile(
                    deadEyeProjectilePrefab,
                    context.AimWorldPosition,
                    context.AimDirection,
                    0f,
                    context.Damage,
                    targetLayer);

                PlayActionEffect(CharacterActionType.Ultimate, EffectOrigin.position, context.AimAngle);
                NetSnipeShotSequence++;

                if (shouldReload)
                {
                    TurnOffSnipingMode(isTurnedOffByNoAmmo: true);
                }

                return true;
            }

            if (isFirstBurst)
            {
                SetActionCharges(CharacterActionType.BasicAttack, burstCharges);
                isFirstBurst = false;
            }

            if (burstBulletProjectilePrefab == null) return false;
            Vector2 aim = context.AimDirection;
            
            for (int i = 0; i < burstBulletProjectileFireCount; i++)
            {
                // 총알 발사 개수가 1개 일수도 있으므로 방어코드. 개수가 1개면 0.5, 아니면 나누기
                float angleRatio = (burstBulletProjectileFireCount == 1) ? 0.5f : (float)i / (burstBulletProjectileFireCount - 1);
                float offset = -burstArcAngle * 0.5f + burstArcAngle * angleRatio;
                
                Vector2 dir = Rotate(aim, offset);
                
                SpawnProjectile(
                    burstBulletProjectilePrefab,
                    ProjectileOrigin.position,
                    dir,
                    burstBulletProjectileSpeed,
                    context.Damage,
                    targetLayer);
            }
            
            PlayActionEffect(CharacterActionType.BasicAttack, EffectOrigin.position, context.AimAngle);
            
            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, burstReloadingDuration);
                SetActionCharges(CharacterActionType.BasicAttack, burstCharges + 1);
                NotifyReloadStarted(burstReloadingDuration);
            }
            else
                ResetCooldownDuration(CharacterActionType.BasicAttack);
            
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            CharacterProjectile prefab = ResolveDualRevolverPrefab(burstBulletProjectilePrefab);
            
            if (prefab == null) return false;

            SpawnProjectile(
                prefab,
                ProjectileOrigin.position,
                context.AimDirection,
                dualRevolverProjectileSpeed,
                context.Damage,
                targetLayer);

            PlayActionEffect(CharacterActionType.SkillQ, EffectOrigin.position, context.AimAngle);
            
            return true;
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

            foreach (CharacterBase foundEnemy in foundEnemies)
            {
                DealDamage(foundEnemy, Definition.GetDamage(CharacterActionType.SkillE));
                ApplySlow(foundEnemy, techJumpFlashBangSlowRatio, techJumpFlashBangSlowDuration);
            }
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
        }

        private void TurnOffSnipingMode(bool isTurnedOffByNoAmmo = false)
        {
            isSniping = false;
            NetIsSniping = false;
            CancelTimer(deadEyeSnipingTimeTimer);

            SetMoveAndSkillExceptUltimateCanUse(true);

            SetActionCharges(
                CharacterActionType.BasicAttack, 
                isTurnedOffByNoAmmo ? burstCharges + 1 : burstCharges);
            ResetCooldownDuration(CharacterActionType.BasicAttack);

            ResetCooldownDuration(CharacterActionType.Ultimate);
            StartCooldown(CharacterActionType.Ultimate);
        }

        // 저격 모드에서 쏜 발은 일반 기본공격 사운드 대신 deadEyeShotClip 을 낸다(Render 에서 재생).
        // 마지막 탄을 쏘면 같은 틱에 저격 모드가 꺼지므로, 모드 플래그가 아니라 "이번에 저격탄 카운트가 올랐는지"로 판단한다.
        protected override bool ShouldPlayDefaultActionSound(CharacterActionType action)
        {
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
                return;
            }

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
            magazine = burstCharges;
            duration = burstReloadingDuration;
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
