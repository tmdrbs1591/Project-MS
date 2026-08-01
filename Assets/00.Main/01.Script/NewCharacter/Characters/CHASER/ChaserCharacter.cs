using ProjectMS.CharacterSystem;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 체이서 캐릭터의 스킬 로직을 담당하는 클래스.
    /// </summary>
    public class ChaserCharacter : CharacterBase
    {
        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;

        /*
        
        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile basicAttackProjectilePrefab;
        [Min(0f)] [SerializeField] private float basicAttackProjectileDamageMultiplier = 0.3f;
        [Min(0f)] [SerializeField] private float basicAttackProjectileSpeed = 15f;
        [Min(0)] [SerializeField] private int basicAttackProjectileFireCount = 4;
        [Min(0f)] [SerializeField] private float basicAttackAngle = 45f;

        // Dual인데 한 발 발사로 적혀있어서 수정될 수도 있음
        // 2발 이상이 된다면 발사 간격도 변수로 있어야 할 것 같음
        [Header("Skill Q - Dual Revolver")]
        [Tooltip("미지정 시 자동으로 basicAttackProjectilePrefab 사용")] 
        [SerializeField] private CharacterProjectile dualRevolverProjectilePrefab; // 필요 없으면 삭제해도?
        [SerializeField] private float dualRevolverProjectileSpeed = 25f;
        [SerializeField] private float dualRevolverProjectileDamageMultiplier = 1.5f;
        
         */

        // 현재 코드 구조에서 개발이 어려움
        [Header("Skill E - Technical Jump")]
        [Tooltip("바닥 기준, 0 ~ 180 사이로 입력")]
        [Min(0f)] [SerializeField] private float techJumpAngle = 60f;
        [Min(0f)] [SerializeField] private float techJumpPower = 30f;
        [Min(0f)] [SerializeField] private float techJumpDuration = 0.08f;
        [SerializeField] private CharacterProjectile techJumpFlashBangPrefab;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSpeed = 8f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangDamageMultiplier = 2f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangRange = 0.8f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSlowTime = 0.5f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSlowValue = 0.4f;

        [Header("Skill R - Dead Eye")]
        [SerializeField] private CharacterProjectile deadEyeProjectilePrefab;
        [Min(0f)] [SerializeField] private float deadEyeProjectileSpeed = 30f;
        [Min(0f)] [SerializeField] private float deadEyeProjectileFireCooltime = 0.75f;
        [Min(0f)] [SerializeField] private float deadEyeProjectileDamageMultiplier = 2f;
        [Min(0)] [SerializeField] private int deadEyeProjectileFireCount = 3;
        [Min(0f)] [SerializeField] private float deadEyeSnipingTimeLimit = 7.5f;

        [Header("Passive - Dirty Carnival")]
        [Tooltip("캐릭터 등 기준, 0 ~ 360 사이로 입력")]
        [Min(0f)] [SerializeField] private float carnivalBackAttackCriterionAngle = 90f;
        [Min(1f)] [SerializeField] private float carnivalBackAttackAdditionalDamageMultiplier = 2f; // 추후 CharacterBase에 들어가면 좋을 듯함

        private bool isSniping = false;
        private float deadEyeSnipingTimeTimer;
        private int deadEyeCurrentFireCount = 0;

        private float backAngleDecimal;

        private RigidbodyType2D rigidBodyTypeBeforeDeadEye;

        private CharacterBase target;

        private bool isFirstGetPassiveAppliedDamage = true;
        private bool isFirstTechJump = true;


        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (isSniping)
            {
                deadEyeCurrentFireCount++;

                float finalDamage = GetPassiveAppliedDamage(context.Damage * deadEyeProjectileDamageMultiplier);

                SpawnProjectile(
                    deadEyeProjectilePrefab,
                    ProjectileOrigin.position,
                    context.AimDirection,
                    deadEyeProjectileSpeed,
                    finalDamage,
                    targetLayer);

                PlayActionEffect(CharacterActionType.Ultimate, EffectOrigin.position, context.AimAngle);

                if (deadEyeCurrentFireCount >= deadEyeProjectileFireCount)
                {
                    ChangeSnipingMode(false);
                }

                return true;
            }

            return false;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            if (isSniping) return false;

            // Projectile로 ResolveDualRevolverPrefab(basicAttackProjectilePrefab) 쓰세용

            return false;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            if (isSniping) return false;
            
            if (isFirstTechJump)
            {
                isFirstTechJump = false;
                if (techJumpAngle > 180f)
                {
                    Debug.LogError("[ChaserCharacter] 전술 도약(E) 스킬의 점프 각도는 180도를 초과하면 안됩니다.");
                    techJumpAngle = 180f;
                }
            }

            float finalDamage = GetPassiveAppliedDamage(context.Damage * techJumpFlashBangDamageMultiplier);

            SpawnProjectile(
                techJumpFlashBangPrefab,
                AttackOrigin.position,
                Vector2.down,
                techJumpFlashBangSpeed,
                finalDamage,
                targetLayer);

            // 빗변 길이 1 기준 연산 - FacingDirection은 1 혹은 1 * -1로 반전용
            float jumpDirectionX = -Movement.FacingDirection * Mathf.Cos(techJumpAngle * Mathf.Deg2Rad);
            float jumpDirectionY = Mathf.Sin(techJumpAngle * Mathf.Deg2Rad);
            
            Vector2 jumpDirection = new Vector2(jumpDirectionX, jumpDirectionY).normalized;

            Movement.StartDash(jumpDirection, techJumpPower, techJumpDuration);

            // 섬광탄이 터지며 범위 내 슬로우(CharacterProjectile 수정 필요)

            PlayActionEffect(CharacterActionType.SkillE, AttackOrigin.position, AimAngle);
            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            if (!Movement.IsGrounded) return false;

            ChangeSnipingMode();
            
            // 테스트용 코드 - 이럴 경우 시간으로 인해 혹은 총알 소진으로 인해 종료될 경우 쿨타임 적용 X
            // 쿨타임 강제 시작 메서드 필요
            if (isSniping) return false; 

            // FUN으로 궁극기 켜져있으면 남은 시간 타이머 깎기 (CharacterBase에서 수정 필요)

            return true;
        }
        
        private void SettingPassive()
        {
            if (carnivalBackAttackCriterionAngle > 360)
            {
                Debug.LogError("[ChaserCharacter] 비열한 거리(패시브) 스킬의 백어택 판정 각도는 360도를 초과하면 안됩니다.");
                carnivalBackAttackCriterionAngle = 360f;
            }

            backAngleDecimal = -((carnivalBackAttackCriterionAngle / 2) / 90);
            bool isNotCriterionInCharacterBack = backAngleDecimal >= 1;

            if (isNotCriterionInCharacterBack)
                backAngleDecimal = -(backAngleDecimal - 1f);

            foreach (CharacterBase c in All)
            {
                if (c == LocalPlayer) continue;

                target = c;
            }
        }


        // TODO : 저격 모드에 따라 바뀌는 것들 바꾸기
        /* 
         * 바꿔야 할 것 목록
         * - 이동 가능 여부
         * - 궁극기 쿨타임
         * - basicAttack의 잔탄 수 (못바꾸면 지금 상태 유지하지만 중간에 장전 되는 거 꺼야함)
         * - basicAttack의 공격 쿨타임 (못바꾸면 변수 삭제)
         * - (선택적) UI에 스킬 사용 불가 표시 및 평타, 궁극기 스킬 아이콘 변경
         */
        private bool ChangeSnipingMode()
        {
            bool newIsSniping = !isSniping;

            ResetSnipingSetting(newIsSniping);

            isSniping = newIsSniping;
            return isSniping;
        }

        private bool ChangeSnipingMode(bool newIsSniping)
        {
            ResetSnipingSetting(newIsSniping);

            isSniping = newIsSniping;
            return isSniping;
        }

        private void ResetSnipingSetting(bool newIsSniping)
        {
            deadEyeSnipingTimeTimer = deadEyeSnipingTimeLimit;
            deadEyeCurrentFireCount = 0;
        }

        /*
        // 시간이 다 됐는지 반환
        private bool CheckInSnipingTime()
        {
            if (!isSniping) return false;

            deadEyeSnipingTimeTimer -= Runner.DeltaTime;

            if (deadEyeSnipingTimeTimer <= 0f) return true;

            return false;
        }
        */

        // 후에 1:1이 아니게 된다면 데미지를 입히기 직전 데미지를 줄 대상, 데미지가 매개변수로 호출되는 메서드가 필요함
        private float GetPassiveAppliedDamage(float damage)
        {
            if (isFirstGetPassiveAppliedDamage)
            {
                isFirstGetPassiveAppliedDamage = false;
                SettingPassive();
            }

            // 만약 좀 더 직관적으로 단순히 앞뒤 구분이 필요하다면 y 값을 0으로 설정 (x축으로만 비교)
            // 대신 이 경우 캐릭터의 뒤 기준을 정하는 각도는 쓰이지 않게 됨 (변수 삭제 필요)
            Vector2 directionTargetToThis = (transform.position - target.transform.position).normalized;
            Vector2 targetFacing = target.transform.right;

            float angleTargetToThis = Vector2.Dot(targetFacing, directionTargetToThis);

            bool isThisOnTargetBack = angleTargetToThis <= backAngleDecimal;

            return isThisOnTargetBack ? damage * carnivalBackAttackAdditionalDamageMultiplier : damage;
        }


        /*
        private CharacterProjectile ResolveDualRevolverPrefab(CharacterProjectile fallback)
        {
            return dualRevolverProjectilePrefab != null ? dualRevolverProjectilePrefab : fallback;
        }

        */
    }
}
