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
    public class SparkCharacter : CharacterBase
    {
        [Header("E 스킬 (과부하)")]
        [SerializeField] private float overloadRadius = 3.5f; // 노드 중심 폭발 범위 반지름
        [SerializeField] private GameObject overloadEffectPrefab; // 과부하 폭발 범위 이펙트 프리팹

        [Header("궁극기 (테슬라 필드)")]
        [SerializeField] private float teslaRadius = 5f; // 전자기장 범위 반지름
        [SerializeField] private float teslaDuration = 3f; // 전자기장 지속 시간 (초)
        [SerializeField] private float tickInterval = 0.5f; // 데미지 주기 (초)
        [SerializeField] private GameObject teslaFieldEffectPrefab; // 테슬라 필드 범위 이펙트 프리팹

        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [Min(0f)][SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private LayerMask targetLayer; // 피격 대상 레이어

        [Header("Skill Q - ElectricNode")]
        [SerializeField] private SparkQNode electricNodePrefab = null;
        [SerializeField] private GameObject electricLinkPrefab; // 두 노드를 잇는 연결 비주얼 프리팹 (X축 스케일로 신축시켜 연결)
        [Min(0f)][SerializeField] private float throwSpeed = 10f;
        [SerializeField] private float electricLineWidth = 0.8f;
        [SerializeField] private float lineDamagePerSecond = 20f;
        private List<SparkQNode> plantedElectricNodes = new List<SparkQNode>();

        private Transform electricLinkInstance; // electricLinkPrefab을 한 번만 생성해 재사용
        private float electricLinkBaseWidth = 1f; // 프리팹 원본(스케일 1) 기준 가로 폭

        [SerializeField] private float lineTickInterval = 0.5f; // 데미지 주기 (0.5초)
        [SerializeField] private float lineTickDamage = 20f;
        private float lineDamageTimer = 0f;

        [Header("Passive - Electrostatic Charge")]
        [SerializeField] private float totalCharge = 100f;
        private float maxCharge;
        private Vector2 lastPosition;
        private bool isCharging = false;

        [Networked] private TickTimer TeslaTimer { get; set; }
        [Networked] private TickTimer TeslaNextTickTimer { get; set; }
        [Networked] private float TeslaDamagePerTick { get; set; }

        [Networked] private int TeslaTickCounter { get; set; }
        private int lastRenderedTickCount = -1;
        private bool wasTeslaRunning; // UpdateTeslaVisual에서 "새 시전 시작"을 모든 클라에서 감지하기 위한 상태

        // GameObject 프리팹 참조는 RPC로 직렬화해서 보낼 수 없어서, 어떤 프리팹을 쓸지는 enum으로만 보내고
        // 각 클라가 자기 SPARK 인스펙터에 연결된 프리팹을 로컬에서 그대로 사용한다(양쪽 클라에 프리팹이
        // 동일하게 세팅돼 있어야 한다).
        private enum RangeEffectKind : byte
        {
            Overload,
            TeslaField
        }

        // effectKind에 해당하는 프리팹을 모든 클라에서 생성하고, 스킬의 실제 반경(radius)에 맞춰 크기를 맞추는
        // 범위 이펙트 메서드. RPC로 브로드캐스트하므로 상대(관전) 클라에서도 보인다.
        private void PlayRangeEffect(RangeEffectKind effectKind, Vector3 position, float radius, float duration = 1.0f)
        {
            if (Object != null && Object.HasStateAuthority)
                Rpc_PlayRangeEffect(effectKind, position, radius, duration);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayRangeEffect(RangeEffectKind effectKind, Vector3 position, float radius, float duration)
        {
            GameObject effectPrefab = effectKind switch
            {
                RangeEffectKind.Overload => overloadEffectPrefab,
                RangeEffectKind.TeslaField => teslaFieldEffectPrefab,
                _ => null
            };

            if (effectPrefab == null) return;

            GameObject instance = Instantiate(effectPrefab, position, Quaternion.identity);

            if (instance.TryGetComponent<ParticleSystem>(out var ps))
            {
                // 파티클 프리팹이면 Shape Radius를 직접 range 반경에 맞춤
                var shape = ps.shape;
                shape.radius = radius;
                ps.Play();
            }
            else
            {
                // 스프라이트 기반 프리팹이면 원본(스케일 1) 가로 폭 대비 스케일을 계산해서 지름(radius*2)에 맞춤
                SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
                float baseDiameter = (sr != null && sr.sprite != null && sr.sprite.bounds.size.x > 0f)
                    ? sr.sprite.bounds.size.x
                    : 1f;
                instance.transform.localScale = Vector3.one * ((radius * 2f) / baseDiameter);
            }

            Destroy(instance, duration);
        }

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (projectilePrefab == null)
                return false;

            SpawnProjectile(
                projectilePrefab,
                ProjectileOrigin.position,
                context.AimDirection,
                projectileSpeed,
                context.Damage,
                targetLayer
            );

            PlayActionEffect(context.Action, ProjectileOrigin.position, context.AimAngle);
            return true;
        }
        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            base.OnProjectileDespawned(projectile, reason, hitTarget);
            if (reason == ProjectileDespawnReason.HitCharacter && isCharging == true)
            {
                ApplySlow(hitTarget, 1f, 0.5f);
                isCharging = false;
            }
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            if (electricNodePrefab == null)
                return false;

            plantedElectricNodes.RemoveAll(n => n == null || !n.IsValid);

            SparkQNode node = SpawnOwnedEntity<SparkQNode>(
                electricNodePrefab,
                context.Action,
                ProjectileOrigin.position,
                maxCount: 2,
                initialVelocity: context.AimDirection * throwSpeed
            );

            if (node != null)
            {
                plantedElectricNodes.Add(node);
            }

            if (plantedElectricNodes.Count >= 2) return true;

            return false;
        }

        private void UpdateElectricLine(float deltaTime)
        {
            plantedElectricNodes.RemoveAll(node => node == null || !node.IsValid);

            if (plantedElectricNodes != null && plantedElectricNodes.Count >= 2)
            {
                // 리시뮬레이션 중엔 스킵 — 상대(원격 오브젝트) 위치 기준 물리 쿼리는 리시뮬레이션마다
                // 결과가 달라질 수 있어서, 가드 없이 두면 같은 타격에 DealDamage가 여러 번 불릴 수 있다
                // (UpdateTeslaFieldLogic의 기존 가드와 동일한 이유).
                if (!HasStateAuthority || Runner.IsResimulation) return;

                if (plantedElectricNodes[0] == null || plantedElectricNodes[1] == null)
                    return;

                Vector2 posA = plantedElectricNodes[0].transform.position;
                Vector2 posB = plantedElectricNodes[1].transform.position;

                lineDamageTimer += deltaTime;
                if (lineDamageTimer >= lineTickInterval)
                {
                    lineDamageTimer = 0f;

                    Vector2 direction = (posB - posA).normalized;
                    float distance = Vector2.Distance(posA, posB);

                    List<CharacterBase> enemies = FindEnemiesInLine(posA, direction, distance, electricLineWidth, targetLayer);

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

            if (Runner == null || !Object.IsValid) return;


            UpdateLineVisual();
            UpdateTeslaVisual();
        }

        // electricLinkPrefab을 최초 1회만 생성해서 electricLinkInstance에 캐싱
        private void EnsureElectricLinkInstance()
        {
            if (electricLinkInstance != null || electricLinkPrefab == null) return;

            GameObject obj = Instantiate(electricLinkPrefab);
            electricLinkInstance = obj.transform;

            // 스프라이트의 원본(스케일 1) 가로 폭을 기준 폭으로 저장 -> 이후 거리에 맞춰 X축 스케일 계산에 사용
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            electricLinkBaseWidth = (sr != null && sr.sprite != null && sr.sprite.bounds.size.x > 0f)
                ? sr.sprite.bounds.size.x
                : 1f;

            obj.SetActive(false);
        }

        private void UpdateLineVisual()
        {
            if (electricLinkPrefab == null || Runner == null) return;

            EnsureElectricLinkInstance();

            List<Transform> myNodes = new List<Transform>();
            foreach (var netObj in Runner.GetAllNetworkObjects())
            {
                // 자신이 인풋권한을 가지고 있는 SPARK_Q라면
                if (netObj != null && netObj.InputAuthority == Object.InputAuthority && netObj.name.Contains("SPARK_Q"))
                {
                    myNodes.Add(netObj.transform);
                }
            }

            if (myNodes.Count >= 2 && myNodes[1].GetComponent<SparkQNode>().isStop == true)
            {
                Vector2 posA = myNodes[0].position;
                Vector2 posB = myNodes[1].position;
                Vector2 delta = posB - posA;
                float distance = delta.magnitude;

                electricLinkInstance.gameObject.SetActive(true);
                electricLinkInstance.position = (Vector3)((posA + posB) * 0.5f); // 두 노드의 중점
                electricLinkInstance.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); // 두 노드를 잇는 각도

                // 중점 기준으로 X축을 거리만큼 늘려서 양쪽 노드에 정확히 걸치도록 함
                Vector3 scale = electricLinkInstance.localScale;
                scale.x = distance / electricLinkBaseWidth;
                electricLinkInstance.localScale = scale;
            }
            else if (electricLinkInstance != null)
            {
                electricLinkInstance.gameObject.SetActive(false);
            }
        }

        private void UpdateTeslaVisual()
        {
            bool isRunning = TeslaTimer.IsRunning && !TeslaTimer.Expired(Runner);

            if (isRunning && !wasTeslaRunning)
            {
                // 새 궁극기 시전 시작을 모든 클라(상대/관전 포함)에서 감지해 카운터 기준점을 리셋한다.
                // OnUltimate의 리셋(lastRenderedTickCount = -1)은 StateAuthority 클라에서만 실행되므로,
                // 여기서 안 맞춰주면 두 번째 시전부터 TeslaTickCounter가 이전 시전보다 낮은 값으로
                // 리셋됐을 때 "새 값 > 마지막 렌더값" 비교가 계속 false가 되어 상대 화면에서
                // 이펙트가 다시는 안 보이게 된다.
                lastRenderedTickCount = -1;
            }
            wasTeslaRunning = isRunning;

            if (isRunning && TeslaTickCounter > lastRenderedTickCount)
            {
                lastRenderedTickCount = TeslaTickCounter;
                PlayActionEffect(CharacterActionType.Ultimate, transform.position, transform.eulerAngles.z);

                //  궁극기 범위(teslaRadius) 크기에 맞춰 자기장 범위 이펙트 프리팹 출력
                PlayRangeEffect(RangeEffectKind.TeslaField, transform.position, teslaRadius, 0.4f);
            }
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            plantedElectricNodes.RemoveAll(node => node == null || !node.IsValid);

            if (plantedElectricNodes.Count == 0)
                return false;

            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            IReadOnlyList<Vector3> nodePositions = GetActiveNodePositions();

            foreach (Vector3 nodePos in nodePositions)
            {
                // 리시뮬레이션 중엔 데미지 쿼리를 스킵(UpdateElectricLine과 동일한 이유).
                if (HasStateAuthority && !Runner.IsResimulation)
                {
                    foreach (CharacterBase enemy in FindEnemiesInCircle(nodePos, overloadRadius, targetLayer))
                    {
                        DealDamage(enemy, context.Damage);
                        ApplySlow(enemy, 0.5f, 1f); // 50% 느려짐, 1초 유지
                    }
                }

                PlayActionEffect(context.Action, nodePos, 0f);

                //  E스킬 과부하 범위(overloadRadius) 크기에 맞춰 폭발 범위 이펙트 프리팹 출력
                PlayRangeEffect(RangeEffectKind.Overload, nodePos, overloadRadius, 0.5f);
            }

            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            TeslaDamagePerTick = context.Damage;
            TeslaTimer = TickTimer.CreateFromSeconds(Runner, teslaDuration);
            TeslaNextTickTimer = TickTimer.CreateFromSeconds(Runner, tickInterval);

            TeslaTickCounter = 1;
            lastRenderedTickCount = -1;

            // 리시뮬레이션 중엔 데미지 쿼리를 스킵(UpdateElectricLine/UpdateTeslaFieldLogic과 동일한 이유).
            if (HasStateAuthority && !Runner.IsResimulation)
            {
                foreach (CharacterBase enemy in FindEnemiesInCircle(transform.position, teslaRadius, targetLayer))
                {
                    DealDamage(enemy, TeslaDamagePerTick);
                    ApplySlow(enemy, 0.2f, 0.3f); // 20% 느려짐, 0.3초 유지
                }
            }

            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            //  궁극기 최초 시전 시 넓은 범위(teslaRadius)로 퍼지는 테슬라 필드 범위 이펙트 프리팹 출력
            PlayRangeEffect(RangeEffectKind.TeslaField, transform.position, teslaRadius, 0.6f);

            return true;
        }

        public override void Spawned()
        {
            base.Spawned();
            maxCharge = totalCharge;
            lastPosition = transform.position;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            UpdateElectricLine(deltaTime);
            UpdateTeslaFieldLogic();
            float moveDelta = Vector2.Distance(transform.position, lastPosition);

            totalCharge -= moveDelta;
            if (totalCharge < 0)
            {
                totalCharge = maxCharge;
                isCharging = true;
            }
            lastPosition = transform.position;
        }

        private void UpdateTeslaFieldLogic()
        {
            if (!HasStateAuthority || Runner.IsResimulation) return;

            if (TeslaTimer.IsRunning && !TeslaTimer.Expired(Runner))
            {
                if (TeslaNextTickTimer.Expired(Runner))
                {
                    TeslaNextTickTimer = TickTimer.CreateFromSeconds(Runner, tickInterval);
                    TeslaTickCounter++;

                    foreach (CharacterBase enemy in FindEnemiesInCircle(transform.position, teslaRadius, targetLayer))
                    {
                        DealDamage(enemy, TeslaDamagePerTick);
                        ApplySlow(enemy, 0.2f, 0.3f); // 20% 느려짐, 0.3초 유지
                    }
                }
            }
        }

        public void StopUltimate()
        {
            TeslaTimer = TickTimer.None;
            TeslaNextTickTimer = TickTimer.None;
            TeslaTickCounter = 0;
            lastRenderedTickCount = -1;
        }

        private void OnDestroy()
        {
            // 로컬로 생성해둔 연결 비주얼 인스턴스 정리
            if (electricLinkInstance != null)
            {
                Destroy(electricLinkInstance.gameObject);
            }
        }

        private IReadOnlyList<Vector3> GetActiveNodePositions()
        {
            plantedElectricNodes.RemoveAll(node => node == null || !node.IsValid);
            List<Vector3> positions = new List<Vector3>();

            foreach (var node in plantedElectricNodes)
            {
                positions.Add(node.transform.position);
            }

            return positions;
        }
    }
}
