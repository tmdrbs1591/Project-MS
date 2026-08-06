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
        [SerializeField] private float teslaRadius = 5f; // 전자기장 범위 반지름
        [SerializeField] private float teslaDuration = 3f; // 전자기장 지속 시간 (초)
        [SerializeField] private float tickInterval = 0.5f; // 데미지 주기 (초)

        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [Min(0f)][SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private LayerMask targetLayer; // 피격 대상 레이어

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

        [Networked] private TickTimer TeslaTimer { get; set; }
        [Networked] private TickTimer TeslaNextTickTimer { get; set; }
        [Networked] private float TeslaDamagePerTick { get; set; }

        [Networked] private int TeslaTickCounter { get; set; }
        private int lastRenderedTickCount = -1;

        // 지정한 범위(반지름) 크기의 원형으로 퍼져나가는 범위 이펙트 메서드
        private void PlayRangeEffect(Vector3 position, float radius, Color startColor, float duration = 1.0f)
        {
            GameObject particleObj = new GameObject("SkillRangeEffect");
            particleObj.transform.position = position;

            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = duration;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.1f;
            main.startSize = 0.5f;
            main.startColor = startColor;
            main.maxParticles = 100;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 60));

            // 스킬의 실제 반경(Radius)에 맞춰 원형 테두리 형태로 파티클 생성
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0.1f; // 테두리 선 형태로 선명하게 표시

            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            ps.Play();
            Destroy(particleObj, duration + 0.5f);
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

            if (node != null && node.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = context.AimDirection.normalized * throwSpeed;
            }

            plantedElectricNodes.Add(node);

            if (plantedElectricNodes.Count == 2)
            {
                return true;
            }
            else if (plantedElectricNodes.Count >= 3)
            {
                if (plantedElectricNodes[0] != null && plantedElectricNodes[0].IsValid)
                {
                    Runner.Despawn(plantedElectricNodes[0]);
                }
                plantedElectricNodes.RemoveAt(0);
                return true;
            }
            return false;
        }

        private void UpdateElectricLine(float deltaTime)
        {
            plantedElectricNodes.RemoveAll(node => node == null || !node.IsValid);

            if (plantedElectricNodes != null && plantedElectricNodes.Count >= 2)
            {
                if (!HasStateAuthority) return;

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

        private void UpdateLineVisual()
        {
            if (lineRenderer == null || Runner == null) return;
            List<Transform> myNodes = new List<Transform>();
            foreach (var netObj in Runner.GetAllNetworkObjects())
            {
                // 자신이 인풋권한을 가지고 있는 SPARK_Q라면
                if (netObj != null && netObj.InputAuthority == Object.InputAuthority && netObj.name.Contains("SPARK_Q"))
                {
                    myNodes.Add(netObj.transform);
                }
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
        }

        private void UpdateTeslaVisual()
        {
            if (TeslaTimer.IsRunning && !TeslaTimer.Expired(Runner))
            {
                if (TeslaTickCounter > lastRenderedTickCount)
                {
                    lastRenderedTickCount = TeslaTickCounter;
                    PlayActionEffect(CharacterActionType.Ultimate, transform.position, transform.eulerAngles.z);

                    // 💡 궁극기 범위(teslaRadius) 크기에 맞춰 푸른색 원형 자기장 범위 이펙트 출력
                    PlayRangeEffect(transform.position, teslaRadius, Color.cyan, 0.4f);
                }
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
                if (HasStateAuthority)
                {
                    foreach (CharacterBase enemy in FindEnemiesInCircle(nodePos, overloadRadius, targetLayer))
                    {
                        DealDamage(enemy, context.Damage);
                    }
                }

                PlayActionEffect(context.Action, nodePos, 0f);

                // 💡 E스킬 과부하 범위(overloadRadius) 크기에 맞춰 노란색 폭발 범위 이펙트 출력
                PlayRangeEffect(nodePos, overloadRadius, Color.yellow, 0.5f);
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

            if (HasStateAuthority)
            {
                foreach (CharacterBase enemy in FindEnemiesInCircle(transform.position, teslaRadius, targetLayer))
                {
                    DealDamage(enemy, TeslaDamagePerTick);
                }
            }

            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            // 💡 궁극기 최초 시전 시 넓은 범위(teslaRadius)로 퍼지는 테슬라 필드 범위 이펙트 출력
            PlayRangeEffect(transform.position, teslaRadius, new Color(0f, 0.8f, 1f, 1f), 0.6f);

            return true;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            UpdateElectricLine(deltaTime);
            UpdateTeslaFieldLogic();
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