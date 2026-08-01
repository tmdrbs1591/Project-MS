using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectMS.CharacterSystem;
namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 신규 캐릭터 시작 템플릿.
    /// 필요한 스킬과 패시브 훅만 override하고 Fusion API는 직접 사용하지 않는다.
    /// </summary>
    public class SPARK : CharacterBase
    {
        [Header("E 스킬 (과부하)")]
        [SerializeField] private float overloadRadius = 3.5f; // 노드 중심 폭발 범위 반지름
        [SerializeField] private LayerMask targetLayer;      // 피격 대상 레이어

        [Header("궁극기 (테슬라 필드)")]
        [SerializeField] private float teslaRadius = 5f;       // 전자기장 범위 반지름
        [SerializeField] private float teslaDuration = 3f;     // 전자기장 지속 시간 (초)
        [SerializeField] private float tickInterval = 0.5f;    // 데미지 주기 (초)

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            // 예: FindEnemiesInArc 또는 SpawnProjectile을 사용해 평타를 구현한다.
            return false;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            return false;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            // 필드에 있는 모든 전류 노드의 위치 목록을 가져옴
            IReadOnlyList<Vector3> nodePositions = GetActiveNodePositions();

            // 각 노드의 위치 중심으로 폭발 범위 판정 및 데미지 적용
            foreach (Vector3 nodePos in nodePositions)
            {
                foreach (CharacterBase enemy in FindEnemiesInCircle(nodePos, overloadRadius, targetLayer))
                {
                    DealDamage(enemy, context.Damage);
                }
            }

            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            PlayActionEffect(context.Action, transform.position, context.AimAngle);
            StartCoroutine(TeslaFieldRoutine(context.Damage));

            return true;
        }

        private IEnumerator TeslaFieldRoutine(float damagePerTick)
        {
            float elapsed = 0f;

            while (elapsed < teslaDuration)
            {
                foreach (CharacterBase enemy in FindEnemiesInCircle(transform.position, teslaRadius, targetLayer))
                {
                    DealDamage(enemy, damagePerTick);
                }

                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;
            }
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            // 매 네트워크 Simulation 틱에 필요한 패시브만 구현한다.
        }

        // Q스킬 연동 전까지 위치를 제공해주는 헬퍼 메서드
        private IReadOnlyList<Vector3> GetActiveNodePositions()
        {
            return System.Array.Empty<Vector3>();
        }
    }
}