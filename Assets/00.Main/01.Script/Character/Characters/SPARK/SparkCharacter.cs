using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 신규 캐릭터 시작 템플릿.
    /// 필요한 스킬과 패시브 훅만 override하고 Fusion API는 직접 사용하지 않는다.
    /// </summary>
    public partial class SparkCharacter : CharacterBase
    {
        private const int NodeLinkerGroupId = 6;

        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;

        [Header("Basic Attack - Electric Gun")]
        [SerializeField] private CharacterProjectile gunProjectilePrefab;
        [Min(0f)][SerializeField] private float gunProjectileSpeed = 20f;

        [Header("Skill Q - Electric Node")]
        [SerializeField] private SparkNodeDeployable nodePrefab = null;
        [SerializeField] private SparkNodeLinkerDeployable nodeLinkerPrefab;
        [Min(0f)][SerializeField] private float nodeThrowSpeed = 10f;
        [SerializeField] private float nodeLinkerWidth = 0.8f;
        [SerializeField] private float nodeLinkerDamageInterval = 0.5f;

        [Header("Skill E - Overload")]
        [SerializeField] private float overloadRadius = 3.5f;
        [SerializeField] private float overloadSlowDuration = 1f;
        [SerializeField] private float overloadSlowRatio = 0.5f;
        [SerializeField] private GameObject overloadEffectPrefab;
        [SerializeField] private float overloadEffectDuration = 0.5f;

        [Header("Ultimate - Tesla Field")]
        [SerializeField] private float teslaRadius = 5f;
        [SerializeField] private float teslaDuration = 3f;
        [SerializeField] private float teslaExplosionInterval = 0.5f;
        [SerializeField] private float teslaSlowDuration = 0.3f;
        [SerializeField] private float teslaSlowRatio = 0.2f;
        [SerializeField] private GameObject teslaEffectPrefab;
        [SerializeField] private float teslaFirstEffectDuration = 0.6f;
        [SerializeField] private float teslaEffectDuration = 0.4f;
        
        [Header("Passive - Electrostatic Charge")]
        [SerializeField] private float electrostaticMaxCharge = 100f;
        [SerializeField] private float electrostaticChargeMultiplier = 1f;
        [SerializeField] private float electrostaticStunDuration = 0.5f;

        private CharacterProjectile gunEmpoweredProjectile;

        private float electrostaticCurrentCharge = 0f;
        private bool electrostaticIsCharged = false;
        private Vector2 lastPosition;
        
        // 현재 존재하는 모든 노드 목록 (노드가 실제로 아직 던져지고 있고 멈추지 않았어도 추가된다.)
        private List<SparkNodeDeployable> plantedElectricNodes = new List<SparkNodeDeployable>(2);
        private SparkNodeLinkerDeployable nodeLinker;
        
        private int TeslaCurrentExplosionCount { get; set; }
        private bool isTeslaFieldActive = false;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (gunProjectilePrefab == null)
                return false;

            // 패시브(정전기 충전)로 충전된 상태면 이 발이 강화된 기본공격임을 투사체 스프라이트로
            // 보여준다. empowered로 슬로우가 적용되는 건 아니다.
            // (스프라이트만 empowered 적용, 실제 슬로우는 OnProjectileDespawned에서)
            CharacterProjectile gunProjectile = SpawnProjectile(
                gunProjectilePrefab,
                ProjectileOrigin.position,
                context.AimDirection,
                gunProjectileSpeed,
                context.Damage,
                targetLayer,
                skillId: 0,
                empowered: electrostaticIsCharged);

            if (electrostaticIsCharged)
            {
                gunEmpoweredProjectile = gunProjectile;
                electrostaticIsCharged = false;
            }

            PlayActionEffect(context.Action, ProjectileOrigin.position, context.AimAngle);
            return true;
        }

        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            if (reason != ProjectileDespawnReason.HitCharacter || projectile != gunEmpoweredProjectile)
                return;

            ApplyControlSeal(hitTarget, CharacterControlType.All, electrostaticStunDuration);
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            if (nodePrefab == null)
                return false;

            SparkNodeDeployable node = SpawnOwnedEntity(
                nodePrefab,
                context.Action,
                ProjectileOrigin.position,
                maxCount: 2,
                initialVelocity: context.AimDirection * nodeThrowSpeed,
                initialize: (node) => node.Initialize(this));

            if (node != null)
                plantedElectricNodes.Add(node);

            if (plantedElectricNodes.Count >= 2)
                return true;

            return false;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            if (plantedElectricNodes.Count == 0)
                return false;

            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            IReadOnlyList<Vector3> nodePositions = GetActiveNodePositions();

            foreach (Vector3 nodePos in nodePositions)
            {
                // 리시뮬레이션 중엔 데미지 쿼리를 스킵
                if (HasStateAuthority && !Runner.IsResimulation)
                {
                    foreach (CharacterBase enemy in FindEnemiesInCircle(nodePos, overloadRadius, targetLayer))
                    {
                        DealDamage(enemy, context.Damage);
                        ApplySlow(enemy, overloadSlowRatio, overloadSlowDuration);
                    }
                }

                PlayActionEffect(context.Action, nodePos, 0f);

                //  E스킬 과부하 범위(overloadRadius) 크기에 맞춰 폭발 범위 이펙트 프리팹 출력
                PlayRangeEffect(RangeEffectKind.Overload, nodePos, overloadRadius, overloadEffectDuration);
            }

            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            TeslaCurrentExplosionCount = 0;

            isTeslaFieldActive = true;
            ScheduleTimer(teslaDuration, () => isTeslaFieldActive = false);

            ExplodeTeslaField();
            TryToSetContinousTeslaExplosion();

            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            //  궁극기 최초 시전 시 넓은 범위(teslaRadius)로 퍼지는 테슬라 필드 범위 이펙트 프리팹 출력
            PlayRangeEffect(RangeEffectKind.TeslaField, transform.position, teslaRadius, teslaFirstEffectDuration);

            return true;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            float moveDelta = Vector2.Distance(transform.position, lastPosition);

            electrostaticCurrentCharge += moveDelta * electrostaticChargeMultiplier;
            if (electrostaticCurrentCharge >= electrostaticMaxCharge)
            {
                electrostaticCurrentCharge = 0f;
                electrostaticIsCharged = true;
            }

            lastPosition = transform.position;
        }

        protected override void OnCharacterSpawned()
        {
            if (!HasStateAuthority)
                return;

            lastPosition = transform.position;

            if (nodeLinkerPrefab.DestroyWhenOwnerDies)
            {
                Debug.LogError("[SparkCharacter] NodeLinker의 DestroyWhenOwnerDies 옵션을 비활성화로 설정해주세요!");
                return;
            }

            OwnedEntitySpawnRequest linkerRequest = new(
                transform.position,
                Quaternion.identity,
                new OwnedEntityGroupId(NodeLinkerGroupId));

            OwnedEntitySpawnResult<SparkNodeLinkerDeployable> result = SpawnOwnedEntity(
                nodeLinkerPrefab,
                linkerRequest,
                initialize: (linker) => linker.Initialize(
                    nodeLinkerWidth, 
                    Definition.GetDamage(CharacterActionType.SkillQ), 
                    nodeLinkerDamageInterval));

            if (!result.Success)
            {
                Debug.LogError($"[SparkCharacter] NodeLinker 생성에 실패했습니다! 이유 : {result.FailureReason}");
                return;
            }

            nodeLinker = result.Entity;
        }

        protected override void OnResetCharacter()
        {
            // 타이머는 CharacterBase에서 모두 Stop 해준다.

            isTeslaFieldActive = false;

            // 라운드 종료 시 모든 노드를 지운다.
            DestroyOwnedEntities(CharacterActionType.SkillQ, reason: OwnedEntityDestroyReason.Manual);
            plantedElectricNodes.Clear();

            // NodeLinker가 써진 적이 있다면 보이지 않게 만든다.
            nodeLinker.SetLinkerActive(false);
            nodeLinker.OnResetCharacter();
        }

        protected override void OnCharacterDespawned()
        {
            if (!HasStateAuthority)
                return;

            nodeLinker.RequestDestroy(reason: OwnedEntityDestroyReason.OwnerDespawned);
        }

        protected override void OnOwnedEntityDestroyed(CharacterOwnedEntity entity, OwnedEntityDestroyReason reason)
        {
            plantedElectricNodes.RemoveAll(n => n == entity);
            
            if (plantedElectricNodes.Count < 2)
                nodeLinker.SetLinkerActive(false);
        }

        public void OnNodeStopped()
        {
            if (!HasStateAuthority || Runner.IsResimulation)
                return;

            if (plantedElectricNodes.Count < 2 || !plantedElectricNodes[0].IsStopped || !plantedElectricNodes[1].IsStopped)
                return;

            Vector2 nodeAPosition = plantedElectricNodes[0].EffectAnchor.position;
            Vector2 nodeBPosition = plantedElectricNodes[1].EffectAnchor.position;

            nodeLinker.SetNodes(nodeAPosition, nodeBPosition);
        }

        private void ExplodeTeslaField()
        {
            // 리시뮬레이션 중엔 데미지 쿼리를 스킵(중첩 계산 될 수 있음)
            if (!HasStateAuthority || Runner.IsResimulation)
                return;

            foreach (CharacterBase enemy in FindEnemiesInCircle(transform.position, teslaRadius, targetLayer))
            {
                DealDamage(enemy, Definition.GetDamage(CharacterActionType.Ultimate), CharacterDamageSource.Periodic);
                ApplySlow(enemy, teslaSlowRatio, teslaSlowDuration);
            }

            PlayActionEffect(CharacterActionType.Ultimate, transform.position, transform.eulerAngles.z);

            //  궁극기 범위(teslaRadius) 크기에 맞춰 자기장 범위 이펙트 프리팹 출력
            PlayRangeEffect(RangeEffectKind.TeslaField, transform.position, teslaRadius, teslaEffectDuration);

            TeslaCurrentExplosionCount++;
        }

        private void TryToSetContinousTeslaExplosion()
        {
            if (!isTeslaFieldActive)
                return;

            ScheduleTimer(teslaExplosionInterval, () =>
            {
                ExplodeTeslaField();
                TryToSetContinousTeslaExplosion();
            });
        }

        private IReadOnlyList<Vector3> GetActiveNodePositions()
        {
            List<Vector3> positions = new List<Vector3>();

            foreach (var node in plantedElectricNodes)
            {
                positions.Add(node.transform.position);
            }

            return positions;
        }
    }
}
