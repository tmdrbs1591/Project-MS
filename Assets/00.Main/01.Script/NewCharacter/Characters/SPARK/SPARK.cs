using System.Collections;
using System.Collections.Generic;
using Fusion;
using ProjectMS.CharacterSystem;
using UnityEngine;

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

        [Header("궁극기 (테슬라 필드)")]
        [SerializeField] private float teslaRadius = 5f;       // 전자기장 범위 반지름
        [SerializeField] private float teslaDuration = 3f;     // 전자기장 지속 시간 (초)
        [SerializeField] private float tickInterval = 0.5f;    // 데미지 주기 (초)

        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [Min(0f)][SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private LayerMask targetLayer;      // 피격 대상 레이어
        private float bonusDamage = 0;
        private bool useSkill = false;

        [Header("Skill Q - ElectricNode")]
        [SerializeField] private GameObject electricNodePrefab = null;
        [SerializeField] private LineRenderer lineRenderer;
        [Min(0f)][SerializeField] private float throwSpeed = 10f;
        [SerializeField] private float electricLineWidth = 0.8f;
        [SerializeField] private float lineDamagePerSecond = 20f;
        private List<NetworkObject> plantedElectricNodes = new List<NetworkObject>();

        [SerializeField] private float lineTickInterval = 0.5f; // 데미지 주기 (0.5초)
        [SerializeField] private float lineTickDamage = 20f;
        private float lineDamageTimer = 0f;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (projectilePrefab == null)
                return false;
            bonusDamage = context.Damage * 0.3f;

            float finalDamage = useSkill ? context.Damage + bonusDamage : context.Damage;

            SpawnProjectile(
                projectilePrefab,
                ProjectileOrigin.position,
                context.AimDirection,
                projectileSpeed,
                finalDamage,
                targetLayer
            );

            PlayActionEffect(context.Action, ProjectileOrigin.position, context.AimAngle);
            useSkill = false;
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            if (electricNodePrefab == null)
                return false;

            NetworkObject node = Runner.Spawn(
                electricNodePrefab,
                ProjectileOrigin.position,
                Quaternion.identity,
                Object.InputAuthority
            );

            node.GetComponent<Rigidbody2D>().linearVelocity = context.AimDirection.normalized * throwSpeed;

            plantedElectricNodes.Add(node);

            if (plantedElectricNodes.Count == 2)
            {
                return true;
            }
            else if (plantedElectricNodes.Count >= 3)
            {
                Runner.Despawn(plantedElectricNodes[0]);
                plantedElectricNodes.RemoveAt(0);
                return true;
            }
            return false;
        }
        private void UpdateElectricLine(float deltaTime)
        {
            // 노드가 2개 이상 설치되어 있는지 확인
            if (plantedElectricNodes != null && plantedElectricNodes.Count >= 2)
            {
                // 파괴되거나 despawn된 노드가 없는지 예외 체크
                if (plantedElectricNodes[0] == null || plantedElectricNodes[1] == null)
                    return;
                // 두 노드의 실시간 위치 받아오기
                Vector2 posA = plantedElectricNodes[0].transform.position;
                Vector2 posB = plantedElectricNodes[1].transform.position;

                lineDamageTimer += deltaTime;
                if (lineDamageTimer >= lineTickInterval)
                {
                    lineDamageTimer = 0f;

                    Vector2 direction = (posB - posA).normalized;
                    float distance = Vector2.Distance(posA, posB);
                    // 선 범위 내 적 감지
                    List<CharacterBase> enemies = FindEnemiesInLine(posA, direction, distance, electricLineWidth, targetLayer);
                    // 0.5초마다 lineTickDamage 적용
                    foreach (CharacterBase enemy in enemies)
                    {
                        DealDamage(enemy, lineTickDamage);
                    }

                }
            }
        }
        public override void Render()
        {
            base.Render();

            UpdateLineVisual();
        }

        private void UpdateLineVisual()
        {
            if (lineRenderer == null || Runner == null) return;
            // 포톤(Runner)을 통해 씬에서 내가 생성한 SPARK_Q 노드 2개 찾아오기 (상대방 컴퓨터 포함)
            List<Transform> myNodes = new List<Transform>();
            foreach (var netObj in Runner.GetAllNetworkObjects())
            {
                if (netObj != null && netObj.InputAuthority == Object.InputAuthority && netObj.name.Contains("SPARK_Q"))
                {
                    myNodes.Add(netObj.transform);
                }
            }
            // Q 노드가 2개 있으면 상대방 화면을 포함해 모두 라인 표시
            if (myNodes.Count >= 2)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, myNodes[0].position);
                lineRenderer.SetPosition(1, myNodes[1].position);
            }
            else
            {
                lineRenderer.enabled = false;
            }
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

        protected override void OnSkillExecuted(CharacterActionType actionType)
        {
            base.OnSkillExecuted(actionType);
            useSkill = true;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            UpdateElectricLine(deltaTime);
        }

        // Q스킬 연동 전까지 위치를 제공해주는 헬퍼 메서드
        private IReadOnlyList<Vector3> GetActiveNodePositions()
        {
            return System.Array.Empty<Vector3>();
        }
    }
}