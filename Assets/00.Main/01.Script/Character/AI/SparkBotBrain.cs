using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

namespace ProjectMS.CharacterSystem.AI
{
    public class SparkBotBrain : CharacterBotBrain
    {
        private const float PreferredMinRange = 3.0f;
        private const float PreferredMaxRange = 7.0f;
        private const float UltimateRange = 3.5f;
        private const float NodeDetonationTriggerRange = 3.5f; // 플레이어가 노드로부터 이 거리 이내에 있으면 E 격발

        private SparkCharacter spark;

        private float nextShotTime;
        private float strafeDirection;
        private float nextStrafeDecisionTime;
        private float nextSkillDecisionTime;
        private float nextQSkillTime;
        private bool pendingDash;
        private int shotsInBurst;
        private int burstSize = 3;

        protected override void Initialize(CharacterBase self, BotDifficulty difficulty)
        {
            base.Initialize(self, difficulty);
            spark = (SparkCharacter)self;
        }

        protected override void Think(ref CharacterInputSnapshot input, float deltaTime)
        {
            Vector2 me = SelfCenter;
            Vector2 targetPos = PerceivedTargetPosition;
            Vector2 targetVel = PerceivedTargetVelocity;
            Vector2 projectileOrigin = me;

            float distance = Vector2.Distance(me, targetPos);
            bool lineOfSight = HasLineOfSight(projectileOrigin, targetPos);

            input.MoveDirection = DecideMovement(me, targetPos, distance, lineOfSight);
            input.AimWorldPosition = targetPos;

            if (ShouldDodgeProjectile())
            {
                RequestJump(0.2f);
                if (IsReady(CharacterActionType.Dash))
                    pendingDash = true;
            }

            int ammo = Self.GetCurrentCharges(CharacterActionType.BasicAttack);
            if (ammo >= 0 && ammo <= 2 && !Self.IsReloading && (!lineOfSight || distance > 9f || ammo == 0))
            {
                input.ReloadPressed = true;
            }

            if (pendingDash)
            {
                input.DashPressed = true;
                pendingDash = false;
            }

            // =========================================================================
            // [스킬 사용 로직]
            // =========================================================================

            // 1. Q 스킬 (전류 노드): 기본 쿨타임 외에 추가로 3초 텀(nextQSkillTime)을 두어 번갈아가며 설치
            if (lineOfSight && distance <= 9f && IsReady(CharacterActionType.SkillQ) && Clock >= nextQSkillTime)
            {
                Vector2 predictedTarget = targetPos + (PerceivedTargetVelocity * 0.2f);
                input.AimWorldPosition = ApplyAimError(projectileOrigin, predictedTarget, 0.3f);
                input.SkillQPressed = true;

                // Q를 던진 시점부터 3초 동안은 다음 Q를 아낌
                nextQSkillTime = Clock + 3.0f;
                OnSkillUsed();
                return;
            }

            // 2. E 스킬 (과부하 폭발): 플레이어가 설치된 노드 근처에 왔을 때만 터트림
            if (IsReady(CharacterActionType.SkillE) && IsTargetNearAnyNode(targetPos))
            {
                input.SkillEPressed = true;
                OnSkillUsed();
                return;
            }

            // 3. 궁극기 및 기타 판단 (딜레이 적용)
            if (Clock >= nextSkillDecisionTime)
            {
                // 궁극기 (R)
                if (lineOfSight && distance <= UltimateRange && FightTime >= 8f && (Target.CurrentHealthPercent <= 0.6f || Self.CurrentHealthPercent >= 0.3f))
                {
                    if ((distance <= 2.8f || Target.IsHitstunned) && IsReady(CharacterActionType.Ultimate))
                    {
                        input.UltimatePressed = true;
                        OnUltimateUsed();
                        return;
                    }
                }
            }

            TryBasicAttack(ref input, projectileOrigin, targetPos, targetVel, distance, lineOfSight);
        }

        /// <summary>
        /// 스파크가 설치한 전기 노드 중 하나라도 플레이어와 가까이 있는지 검사합니다.
        /// </summary>
        private bool IsTargetNearAnyNode(Vector2 targetPos)
        {
            if (spark == null) return false;

            var nodesField = typeof(SparkCharacter).GetField("plantedElectricNodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nodesField != null)
            {
                var nodesList = nodesField.GetValue(spark) as System.Collections.IList;
                if (nodesList != null && nodesList.Count > 0)
                {
                    foreach (var node in nodesList)
                    {
                        if (node is MonoBehaviour nodeBehaviour && nodeBehaviour != null)
                        {
                            float distToNode = Vector2.Distance(nodeBehaviour.transform.position, targetPos);
                            if (distToNode <= NodeDetonationTriggerRange)
                            {
                                return true; // 노드 근처에 적이 있음!
                            }
                        }
                    }
                }
            }

            return false;
        }

        private float DecideMovement(Vector2 me, Vector2 targetPos, float distance, bool lineOfSight)
        {
            float toTarget = Mathf.Sign(targetPos.x - me.x);
            float dy = targetPos.y - me.y;

            float escape = EnemyGrenadeEscapeDirection();
            if (!Mathf.Approximately(escape, 0f))
            {
                float escapeMove = SafeMove(escape);
                if (Mathf.Approximately(escapeMove, 0f))
                    RequestJump(0.3f);
                return escapeMove;
            }

            if (dy > 1.5f && Mathf.Abs(targetPos.x - me.x) < 4f)
                RequestJump(0.35f);

            if (Clock >= nextStrafeDecisionTime)
            {
                float r = Random.value;
                strafeDirection = r < 0.4f ? -1f : r < 0.8f ? 1f : 0.2f * toTarget;
                nextStrafeDecisionTime = Clock + Random.Range(0.5f, 1.2f);
            }

            if (!lineOfSight || distance > PreferredMaxRange)
            {
                return SafeMove(toTarget);
            }
            else if (distance < PreferredMinRange)
            {
                float back = SafeMove(-toTarget);
                if (IsReady(CharacterActionType.Dash) && Random.value < 0.4f)
                    pendingDash = true;
                return back;
            }

            return SafeMove(strafeDirection);
        }

        private void TryBasicAttack(ref CharacterInputSnapshot input, Vector2 origin, Vector2 targetPos, Vector2 targetVel,
            float distance, bool lineOfSight)
        {
            if (!lineOfSight || Clock < nextShotTime)
                return;

            Vector2 aim = PredictIntercept(origin, targetPos, targetVel, 18f);
            input.AimWorldPosition = ApplyAimError(origin, aim);
            input.BasicAttackPressed = true;

            shotsInBurst++;
            if (shotsInBurst >= burstSize)
            {
                shotsInBurst = 0;
                burstSize = Random.Range(2, 5);
                nextShotTime = Clock + Random.Range(0.25f, 0.5f);
            }
            else
            {
                nextShotTime = Clock + Random.Range(0.12f, 0.25f);
            }
        }

        private void OnSkillUsed()
        {
            nextShotTime = Mathf.Min(nextShotTime, Clock + 0.25f);
            nextSkillDecisionTime = Clock + Random.Range(0.1f, 0.3f);
        }

        private void OnUltimateUsed()
        {
            nextShotTime = Mathf.Min(nextShotTime, Clock + 0.25f);
            nextSkillDecisionTime = Clock + Random.Range(3.0f, 5.0f);
        }
    }
}