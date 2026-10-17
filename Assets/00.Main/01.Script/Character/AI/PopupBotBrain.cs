using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

namespace ProjectMS.CharacterSystem.AI
{
    /// <summary>
    /// 팝업 AI. 중거리(3~7)를 유지하며 포물선 기본공격(에러 투척)을 던지고,
    ///   - Q(조준 버그, 느린 투사체): 중거리에서 예측 조준으로 던져 상대 기본공격을 봉인
    ///   - E(이동 버그, 전방 부채꼴): 상대가 가까이 붙으면 바로 사용
    ///   - R(오류 팝업): 게이지가 차고 가까우면 예측 조준으로 사용
    /// </summary>
    public class PopupBotBrain : CharacterBotBrain
    {
        private const float PreferredMinRange = 3f;
        private const float PreferredMaxRange = 7f;
        private const float HackingCdSpeed = 7f;   // PopupCharacter.aimingBugHackingCDSpeed
        private const float PopupGlitchSpeed = 6f; // PopupCharacter.popupAppearGlitchSpeed
        private const float ConeRange = 4.5f;      // 부채꼴 반경(6)보다 조금 안쪽에서 쓴다

        private float nextShotTime;
        private float nextSkillDecisionTime;
        private bool pendingDash;

        protected override void Think(ref CharacterInputSnapshot input, float deltaTime)
        {
            Vector2 me = SelfCenter;
            Vector2 targetPos = PerceivedTargetPosition;
            Vector2 targetVel = PerceivedTargetVelocity;
            float distance = Vector2.Distance(me, targetPos);
            bool lineOfSight = HasLineOfSight(me, targetPos);

            input.MoveDirection = MoveKeepingRange(me, targetPos, distance, lineOfSight,
                PreferredMinRange, PreferredMaxRange, 0.35f, out bool wantDash);
            input.AimWorldPosition = targetPos;
            pendingDash |= wantDash;

            if (ShouldDodgeProjectile())
            {
                RequestJump(0.2f);
                if (IsReady(CharacterActionType.Dash))
                    pendingDash = true;
            }

            int ammo = Self.GetCurrentCharges(CharacterActionType.BasicAttack);
            if (ammo >= 0 && ammo <= 1 && !Self.IsReloading && (!lineOfSight || ammo == 0))
                input.ReloadPressed = true;

            if (pendingDash)
            {
                input.DashPressed = true;
                pendingDash = false;
            }

            // E: 붙어 오면 부채꼴로 밀어낸다(느리게 만들고 피해).
            if (lineOfSight && distance <= ConeRange && Mathf.Abs(targetPos.y - me.y) < 2f && IsReady(CharacterActionType.SkillE))
            {
                input.AimWorldPosition = targetPos;
                input.SkillEPressed = true;
                OnSkillUsed();
                return;
            }

            if (Clock >= nextSkillDecisionTime)
            {
                // R: 게이지가 찼고 가까우면 오류 팝업.
                if (lineOfSight && distance <= 6f && IsUltimateReady())
                {
                    Vector2 aim = PredictIntercept(me, targetPos, targetVel, PopupGlitchSpeed);
                    input.AimWorldPosition = ApplyAimError(me, aim, 0.5f);
                    input.UltimatePressed = true;
                    nextSkillDecisionTime = Clock + Random.Range(2f, 3f);
                    return;
                }

                // Q: 중거리에서 해킹 CD(상대 기본공격 봉인).
                if (lineOfSight && distance >= 2.5f && distance <= 8f && IsReady(CharacterActionType.SkillQ) && !Roll(SkillHesitation))
                {
                    Vector2 aim = PredictIntercept(me, targetPos, targetVel, HackingCdSpeed);
                    input.AimWorldPosition = ApplyAimError(me, aim, 0.5f);
                    input.SkillQPressed = true;
                    OnSkillUsed();
                    return;
                }

                nextSkillDecisionTime = Clock + Random.Range(0.3f, 0.7f);
            }

            TryBasicAttack(ref input, me, targetPos, targetVel, lineOfSight);
        }

        // 포물선으로 던지는 투척이라 착지 시점의 위치를 대략 예측해서 그쪽을 노린다.
        private void TryBasicAttack(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos, Vector2 targetVel, bool lineOfSight)
        {
            if (!lineOfSight || Clock < nextShotTime)
                return;

            Vector2 aim = targetPos + targetVel * 0.35f;
            input.AimWorldPosition = ApplyAimError(origin, aim);
            input.BasicAttackPressed = true;
            nextShotTime = Clock + Random.Range(ShotDelayRange.x, ShotDelayRange.y) + 0.15f;
        }

        private void OnSkillUsed()
        {
            nextShotTime = Mathf.Min(nextShotTime, Clock + 0.25f);
            nextSkillDecisionTime = Clock + Random.Range(0.4f, 0.9f);
        }
    }
}
