using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

namespace ProjectMS.CharacterSystem.AI
{
    /// <summary>
    /// 지퍼 AI. 가까운 거리(2~5)에서 빠른 참격(기본공격)을 연사하며,
    ///   - Q(다이브, 충전 2회): 거리를 좁힐 때 적 쪽으로 돌진. 체력이 낮고 적이 붙으면 반대로 빠진다
    ///   - E(차원 장벽): 적 투사체가 날아오면 적 쪽에 장벽을 세워 막는다
    ///   - R(위상 돌파): 게이지가 차면 0.5초 뒤 적 위치를 예측해 그 자리로 순간이동하며 폭발
    /// </summary>
    public class ZipperBotBrain : CharacterBotBrain
    {
        private const float PreferredMinRange = 2f;
        private const float PreferredMaxRange = 5f;
        private const float SlashSpeed = 20f;          // ZipperCharacter.slashSpeed
        private const float UltimateDelay = 0.5f;      // ZipperCharacter.ultimateTeleportDelay
        private const float UltimateMaxRange = 10f;

        private float nextShotTime;
        private float nextSkillDecisionTime;
        private float nextDiveTime;
        private bool pendingDash;

        protected override void Think(ref CharacterInputSnapshot input, float deltaTime)
        {
            Vector2 me = SelfCenter;
            Vector2 targetPos = PerceivedTargetPosition;
            Vector2 targetVel = PerceivedTargetVelocity;
            float distance = Vector2.Distance(me, targetPos);
            bool lineOfSight = HasLineOfSight(me, targetPos);
            float toTarget = Mathf.Sign(targetPos.x - me.x);

            input.MoveDirection = DecideMovement(me, targetPos, distance, lineOfSight, toTarget);
            input.AimWorldPosition = targetPos;

            // E: 투사체가 날아오면 장벽으로 막는다. 장벽이 없으면 점프/대시로 피한다.
            if (ShouldDodgeProjectile())
            {
                if (IsReady(CharacterActionType.SkillE))
                {
                    input.AimWorldPosition = targetPos;
                    input.SkillEPressed = true;
                    OnSkillUsed();
                    return;
                }

                RequestJump(0.2f);
                if (IsReady(CharacterActionType.Dash))
                    pendingDash = true;
            }

            int ammo = Self.GetCurrentCharges(CharacterActionType.BasicAttack);
            if (ammo == 0 && !Self.IsReloading)
                input.ReloadPressed = true;

            if (pendingDash)
            {
                input.DashPressed = true;
                pendingDash = false;
            }

            if (Clock >= nextSkillDecisionTime)
            {
                // R: 0.5초 뒤 적이 있을 자리로 순간이동.
                if (distance <= UltimateMaxRange && IsUltimateReady() && !Roll(SkillHesitation))
                {
                    Vector2 landing = targetPos + targetVel * UltimateDelay;
                    input.AimWorldPosition = ApplyAimError(me, landing, 0.4f);
                    input.UltimatePressed = true;
                    nextSkillDecisionTime = Clock + Random.Range(2f, 3f);
                    return;
                }

                // Q: 위기일 땐 적 반대쪽으로 빠지고, 아니면 거리를 좁히며 돌진.
                if (IsReady(CharacterActionType.SkillQ) && Clock >= nextDiveTime)
                {
                    bool escape = Self.CurrentHealthPercent <= 0.3f && distance < 2.5f;
                    bool engage = lineOfSight && distance >= 2.5f && distance <= 6f;
                    if (escape || engage)
                    {
                        input.AimWorldPosition = escape
                            ? me + new Vector2(-toTarget * 4f, 1f)
                            : ApplyAimError(me, targetPos + targetVel * 0.15f, 0.3f);
                        input.SkillQPressed = true;
                        nextDiveTime = Clock + Random.Range(1.5f, 3f);
                        OnSkillUsed();
                        return;
                    }
                }

                nextSkillDecisionTime = Clock + Random.Range(0.25f, 0.6f);
            }

            TryBasicAttack(ref input, me, targetPos, targetVel, distance, lineOfSight);
        }

        // 근거리 캐릭터라 원거리 AI처럼 사거리 안에서 좌우로 흔들면 "왔다 갔다"만 하는 것처럼 보인다.
        // 다가가서 사거리에 들어오면 거의 멈춰 서서 공격하고, 가끔만 짧게 옆으로 비킨다.
        private float sidestepUntil;
        private float sidestepDirection;
        private float nextSidestepTime;

        private float DecideMovement(Vector2 me, Vector2 targetPos, float distance, bool lineOfSight, float toTarget)
        {
            float escape = EnemyGrenadeEscapeDirection();
            if (!Mathf.Approximately(escape, 0f))
            {
                float escapeMove = SafeMove(escape);
                if (Mathf.Approximately(escapeMove, 0f))
                    RequestJump(0.3f);
                return escapeMove;
            }

            float dy = targetPos.y - me.y;

            // 사거리 밖이거나 발판 등에 가려 안 보이면 다가간다. 막혀서 못 가거나 적이 위에 있으면 점프.
            if (!lineOfSight || distance > PreferredMaxRange)
            {
                float approach = SafeMove(toTarget);
                if (dy > 1.2f || (Mathf.Approximately(approach, 0f) && IsWallAhead(toTarget)))
                    RequestJump(0.35f);
                return approach;
            }

            if (distance < PreferredMinRange * 0.6f)
                return SafeMove(-toTarget);

            if (Clock < sidestepUntil)
                return SafeMove(sidestepDirection);

            if (Clock >= nextSidestepTime)
            {
                nextSidestepTime = Clock + Random.Range(1.5f, 3f);
                if (Roll(0.35f))
                {
                    sidestepDirection = Roll(0.5f) ? 1f : -1f;
                    sidestepUntil = Clock + Random.Range(0.15f, 0.3f);
                }
            }

            // 사거리 안: 너무 멀면 살짝 다가가고, 아니면 멈춰서 공격.
            return distance > PreferredMaxRange * 0.8f ? SafeMove(toTarget) * 0.6f : 0f;
        }

        private void TryBasicAttack(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos, Vector2 targetVel,
            float distance, bool lineOfSight)
        {
            if (!lineOfSight || distance > 8f || Clock < nextShotTime)
                return;

            Vector2 aim = PredictIntercept(origin, targetPos, targetVel, SlashSpeed);
            input.AimWorldPosition = ApplyAimError(origin, aim);
            input.BasicAttackPressed = true;
            nextShotTime = Clock + Random.Range(ShotDelayRange.x, ShotDelayRange.y);
        }

        private void OnSkillUsed()
        {
            nextShotTime = Mathf.Min(nextShotTime, Clock + 0.2f);
            nextSkillDecisionTime = Clock + Random.Range(0.4f, 0.8f);
        }
    }
}
