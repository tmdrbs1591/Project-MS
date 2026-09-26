using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

namespace ProjectMS.CharacterSystem.AI
{
    /// <summary>
    /// 거너 AI. 원거리 딜러답게 적정 거리를 유지하면서 쏘는 "카이팅" 스타일.
    ///
    /// [기본공격 - 6발 탄창 + 재장전]
    ///   이동 방향을 예측해 리드샷을 쏜다. 재장전 중엔 거리를 조금 더 벌린다.
    ///   수류탄이 날아가는 중엔 한 발을 아껴뒀다가 수류탄을 터뜨리는 데 쓴다.
    ///
    /// [Q - 관통 광선(즉발 사각형 판정)]
    ///   이동 시간이 없는 즉발기라 예측 없이 바로 겨냥한다. 사거리(사각형 길이) 안에 들어오면 사용.
    ///   벽을 무시하는 판정이라 시야가 막혀도 쓴다.
    ///
    /// [E - 수류탄 + "쏘면 폭발" 콤보]
    ///   포물선 궤도를 계산해 상대가 있을 자리에 던지고, 수류탄이 상대 근처에 가면
    ///   기본공격으로 수류탄을 쏴서 바로 터뜨린다(안 맞추면 퓨즈 시간 후 자동 폭발).
    ///   벽 너머 상대에게도 넘겨 던질 수 있어서, 시야가 막혔을 때 우선적으로 쓴다.
    ///
    /// [궁극기 - 바주카]
    ///   빗나가면 아까운 한 방이라 확실할 때만 쓴다: 상대가 경직 중이거나, 거의 멈춰 있거나,
    ///   체력이 낮을 때.
    ///
    /// [패시브 - 강화탄]
    ///   스킬을 쓰면 다음 기본공격이 강해지므로, 스킬 직후엔 쉬지 않고 바로 한 발 쏜다.
    /// </summary>
    public class GunnerBotBrain : CharacterBotBrain
    {
        // 거리 유지 (월드 단위)
        private const float PreferredMinRange = 3.5f;
        private const float PreferredMaxRange = 7.5f;
        private const float LowHealthMinRange = 5f;
        private const float BasicAttackRange = 14f;
        private const float UltimateRange = 12f;
        private const float GrenadeMinRange = 2.5f;

        private GunnerCharacter gunner;

        private float nextShotTime;
        private float strafeDirection;
        private float nextStrafeDecisionTime;
        private float nextSkillDecisionTime;
        private float grenadeThrownTime = -99f;
        private bool pendingDash;
        private int shotsInBurst;
        private int burstSize = 2;

        protected override void Initialize(CharacterBase self, BotDifficulty difficulty)
        {
            base.Initialize(self, difficulty);
            gunner = (GunnerCharacter)self;
        }

        protected override void Think(ref CharacterInputSnapshot input, float deltaTime)
        {
            Vector2 me = SelfCenter;
            Vector2 targetPos = PerceivedTargetPosition;
            Vector2 targetVel = PerceivedTargetVelocity;
            Vector2 attackOrigin = gunner.AttackOriginPosition;
            Vector2 projectileOrigin = gunner.ProjectileOriginPosition;

            float distance = Vector2.Distance(me, targetPos);
            bool lineOfSight = HasLineOfSight(projectileOrigin, targetPos);

            input.MoveDirection = DecideMovement(me, targetPos, distance, lineOfSight);
            input.AimWorldPosition = targetPos;

            if (ShouldDodgeProjectile())
                RequestJump(0.2f);

            // 싸움이 잠깐 끊겼을 때(안 보이거나 멀 때) 탄이 적으면 미리 재장전해둔다.
            int ammo = Self.GetCurrentCharges(CharacterActionType.BasicAttack);
            if (ammo >= 0 && ammo <= 2 && !Self.IsReloading && (!lineOfSight || distance > BasicAttackRange * 0.8f))
                input.ReloadPressed = true;

            // 대시는 이동 방향(=바라보는 방향)으로 나가므로, 이번 틱 이동 입력과 같이 누른다.
            if (pendingDash)
            {
                input.DashPressed = true;
                pendingDash = false;
            }

            // 스킬 사용: 한 틱에 하나만. 우선순위 순서대로 시도한다.
            if (TryGrenadeDetonation(ref input, projectileOrigin, targetPos))
                return;
            if (TryUltimate(ref input, projectileOrigin, targetPos, targetVel, distance, lineOfSight))
                return;
            if (TryPiercingLight(ref input, attackOrigin, targetPos))
                return;
            if (TryGrenadeThrow(ref input, projectileOrigin, targetPos, targetVel, distance, lineOfSight))
                return;
            TryBasicAttack(ref input, projectileOrigin, targetPos, targetVel, distance, lineOfSight);
        }

        // ================= 이동 =================

        private float DecideMovement(Vector2 me, Vector2 targetPos, float distance, bool lineOfSight)
        {
            float toTarget = Mathf.Sign(targetPos.x - me.x);
            float dy = targetPos.y - me.y;

            // 적 수류탄이 근처에 있으면 무조건 피한다.
            float escape = EnemyGrenadeEscapeDirection();
            if (!Mathf.Approximately(escape, 0f))
            {
                float escapeMove = SafeMove(escape);
                if (Mathf.Approximately(escapeMove, 0f))
                    RequestJump(0.3f);
                return escapeMove;
            }

            // 상대가 위층에 있고 가까우면 점프로 따라 올라간다.
            if (dy > 1.5f && Mathf.Abs(targetPos.x - me.x) < 4f)
                RequestJump(0.35f);

            bool lowHealth = Self.CurrentHealthPercent < 0.35f && Self.CurrentHealth < Target.CurrentHealth;
            bool reloading = !IsReady(CharacterActionType.BasicAttack);
            float minRange = lowHealth || reloading ? LowHealthMinRange : PreferredMinRange;

            float desired;
            if (!lineOfSight || distance > PreferredMaxRange)
            {
                desired = toTarget;
            }
            else if (distance < minRange)
            {
                // 너무 가까우면 뒤로 빠진다. 뒤가 낭떠러지면 대시로 상대를 넘어가거나 그냥 버틴다.
                desired = -toTarget;
                float back = SafeMove(desired);
                if (Mathf.Approximately(back, 0f))
                {
                    // 등 뒤가 낭떠러지 → 붙어 있으면 상대 쪽으로 대시해서 넘어간다.
                    if (distance < 2f && IsReady(CharacterActionType.Dash))
                    {
                        pendingDash = true;
                        return SafeMove(toTarget);
                    }
                    return 0f;
                }

                if (distance < 2f && IsReady(CharacterActionType.Dash) && !Roll(SkillHesitation))
                    pendingDash = true;
                return back;
            }
            else
            {
                // 적정 거리에선 좌우로 조금씩 흔들어서 쉽게 안 맞게 한다.
                if (Clock >= nextStrafeDecisionTime)
                {
                    float r = Random.value;
                    strafeDirection = r < 0.35f ? -1f : r < 0.7f ? 1f : 0f;
                    nextStrafeDecisionTime = Clock + Random.Range(0.4f, 1.1f);
                }
                desired = strafeDirection;
            }

            return SafeMove(desired);
        }

        // ================= 스킬 =================

        // 수류탄이 상대 근처에 가면 기본공격으로 쏴서 터뜨린다.
        private bool TryGrenadeDetonation(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos)
        {
            GunnerGrenadeProjectile grenade = gunner.ActiveGrenade;
            if (grenade == null || !IsReady(CharacterActionType.BasicAttack))
                return false;

            Vector2 grenadePos = grenade.transform.position;
            float blastRadius = gunner.GrenadeExplosionRadius * 0.85f;
            if ((grenadePos - targetPos).sqrMagnitude > blastRadius * blastRadius)
                return false;
            if (!HasLineOfSight(origin, grenadePos))
                return false;

            Vector2 aim = PredictIntercept(origin, grenadePos, grenade.Velocity, gunner.BulletSpeed);
            input.AimWorldPosition = ApplyAimError(origin, aim, 0.5f);
            input.BasicAttackPressed = true;
            nextShotTime = Clock + ShotDelayRange.x;
            return true;
        }

        private bool TryUltimate(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos, Vector2 targetVel,
            float distance, bool lineOfSight)
        {
            if (!IsReady(CharacterActionType.Ultimate) || !lineOfSight || distance > UltimateRange)
                return false;
            if (Self.IsUltimateGaugeMode && Self.UltimateGaugeCurrent < Self.UltimateGaugeMax)
                return false;

            float rocketDamage = Self.Definition != null ? Self.Definition.GetDamage(CharacterActionType.Ultimate) : 0f;
            bool finisher = Target.CurrentHealth <= rocketDamage * 1.1f;

            // 라운드 시작하자마자 궁을 날리지 않는다(막타 각이면 예외). 싸움이 좀 진행된 뒤,
            // 상대가 경직 중이거나(확정타) 체력이 꽤 빠진 채로 멈춰 있을 때만 쓴다.
            if (!finisher)
            {
                if (FightTime < 8f)
                    return false;

                bool stunned = Target.IsHitstunned;
                bool slowAndHurt = Target.IsGrounded && Mathf.Abs(targetVel.x) < 1.5f && Target.CurrentHealthPercent < 0.6f;
                if (!stunned && !slowAndHurt)
                    return false;
            }
            if (!SkillDecisionPasses())
                return false;

            Vector2 aim = PredictIntercept(origin, targetPos, targetVel, gunner.RocketSpeed);
            input.AimWorldPosition = ApplyAimError(origin, aim, 0.5f);
            input.UltimatePressed = true;
            OnSkillUsed();
            return true;
        }

        // Q 는 즉발 사각형 판정 → 예측 없이 지금 보이는 위치로 바로 긋는다. 벽도 무시한다.
        private bool TryPiercingLight(ref CharacterInputSnapshot input, Vector2 attackOrigin, Vector2 targetPos)
        {
            if (!IsReady(CharacterActionType.SkillQ))
                return false;

            float reach = gunner.PiercingLightForwardOffset + gunner.PiercingLightBoxSize.x * 0.5f;
            if (Vector2.Distance(attackOrigin, targetPos) > reach - 0.3f)
                return false;
            if (!SkillDecisionPasses())
                return false;

            input.AimWorldPosition = ApplyAimError(attackOrigin, targetPos, 0.5f);
            input.SkillQPressed = true;
            OnSkillUsed();
            return true;
        }

        private bool TryGrenadeThrow(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos, Vector2 targetVel,
            float distance, bool lineOfSight)
        {
            if (!IsReady(CharacterActionType.SkillE) || gunner.ActiveGrenade != null || distance < GrenadeMinRange)
                return false;

            // 시야가 트여 있으면 가끔만, 벽 너머면 적극적으로 넘겨 던진다.
            if (lineOfSight && !SkillDecisionPasses())
                return false;

            float gravity = Mathf.Abs(Physics2D.gravity.y) * gunner.GrenadeGravityScale;

            // 날아가는 동안 상대가 움직일 만큼 한 번 보정해서 다시 계산한다(지면 기준이라 y 속도는 무시).
            if (!SolveBallisticDirection(origin, targetPos, gunner.GrenadeThrowSpeed, gravity, out Vector2 direction, out float flightTime))
                return false;

            Vector2 landing = targetPos + new Vector2(targetVel.x * flightTime * 0.6f, 0f);
            if (SolveBallisticDirection(origin, landing, gunner.GrenadeThrowSpeed, gravity, out Vector2 refined, out _))
                direction = refined;

            input.AimWorldPosition = ApplyAimError(origin, origin + direction * 5f, 0.6f);
            input.SkillEPressed = true;
            grenadeThrownTime = Clock;
            OnSkillUsed();
            return true;
        }

        private void TryBasicAttack(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos, Vector2 targetVel,
            float distance, bool lineOfSight)
        {
            if (!lineOfSight || distance > BasicAttackRange || Clock < nextShotTime)
                return;
            if (!IsReady(CharacterActionType.BasicAttack))
                return;

            // 수류탄이 날아가는 중이면 마지막 한 발은 폭발용으로 아껴둔다.
            bool grenadeInFlight = gunner.ActiveGrenade != null && Clock - grenadeThrownTime < 2f;
            int ammo = Self.GetCurrentCharges(CharacterActionType.BasicAttack); // 첫 발 전엔 -1(미설정)
            if (grenadeInFlight && ammo >= 0 && ammo <= 1 && distance > PreferredMinRange)
                return;

            Vector2 aim = PredictIntercept(origin, targetPos, targetVel, gunner.BulletSpeed);
            input.AimWorldPosition = ApplyAimError(origin, aim);
            input.BasicAttackPressed = true;

            // 몇 발 몰아 쏘고 잠깐 쉰다(계속 맞히면 상대가 피격 경직에서 못 벗어남).
            shotsInBurst++;
            if (shotsInBurst >= burstSize)
            {
                shotsInBurst = 0;
                burstSize = Random.Range(BurstSizeRange.x, BurstSizeRange.y + 1);
                nextShotTime = Clock + Random.Range(BurstPauseRange.x, BurstPauseRange.y);
            }
            else
            {
                nextShotTime = Clock + Random.Range(ShotDelayRange.x, ShotDelayRange.y);
            }
        }

        // 스킬 사용 판단을 매 틱 굴리면 사실상 바로 쓰게 되니, 일정 간격으로만 망설임 확률을 굴린다.
        private bool SkillDecisionPasses()
        {
            if (Clock < nextSkillDecisionTime)
                return false;

            nextSkillDecisionTime = Clock + Random.Range(0.15f, 0.35f);
            return !Roll(SkillHesitation);
        }

        // 스킬을 쓰면 강화탄이 붙으니 다음 기본공격을 바로 쏠 수 있게 한다.
        private void OnSkillUsed()
        {
            nextShotTime = Mathf.Min(nextShotTime, Clock + ShotDelayRange.x);
        }
    }
}
