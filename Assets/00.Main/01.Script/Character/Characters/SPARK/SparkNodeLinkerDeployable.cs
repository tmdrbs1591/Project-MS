using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class SparkNodeLinkerDeployable : CharacterDeployable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private NetworkTransform netTransform;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private GameObject visualRoot;

        private CharacterTimerHandler timers;
        private float nodeLinkerBaseSpriteWidth;
        private float nodeLinkerWidth;
        private float linkerDamage;
        private float linkerDamageInterval;

        public void Initialize(float _nodeLinkerWidth, float _linkerDamage, float _linkerDamageInterval)
        {
            nodeLinkerWidth = _nodeLinkerWidth;
            linkerDamage = _linkerDamage;
            linkerDamageInterval = _linkerDamageInterval;

            timers = new CharacterTimerHandler();

            nodeLinkerBaseSpriteWidth = (spriteRenderer != null && spriteRenderer.sprite != null && spriteRenderer.sprite.bounds.size.x > 0f)
                ? spriteRenderer.sprite.bounds.size.x
                : 1f;

            SetLinkerActive(false);
        }

        public void SetNodes(Vector2 nodeAPosition, Vector2 nodeBPosition)
        {
            SetLinkerPosition(nodeAPosition, nodeBPosition);
            SetLinkerActive(true);

            timers.CancelAll();
            SetConinuousNodeLinkerDamage(nodeAPosition, nodeBPosition);
        }

        protected override void OnOwnedEntitySpawnedAuthority()
        {
            base.OnOwnedEntitySpawnedAuthority();
            SetLinkerActive(false);
        }

        public void OnResetCharacter()
        {
            timers.CancelAll();
        }

        public override void Render()
        {
            base.Render();

            visualRoot.SetActive(IsActive && !IsDestroying);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
                return;

            timers.Tick(Runner.DeltaTime);
        }

        public void SetLinkerActive(bool isActive)
        {
            if (!Object.HasStateAuthority)
                return;

            SetOwnedEntityActive(isActive);

            if (!isActive)
                timers?.CancelAll();
        }

        private void SetLinkerPosition(Vector2 posA, Vector2 posB)
        {
            Vector2 delta = posB - posA;
            float distance = delta.magnitude;

            Vector3 position = (Vector3)((posA + posB) * 0.5f); // 두 노드의 중점
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); // 두 노드를 잇는 각도

            // 중점 기준으로 X축을 거리만큼 늘려서 양쪽 노드에 정확히 걸치도록 함
            Vector3 scale = transform.localScale;
            scale.x = distance / nodeLinkerBaseSpriteWidth;
            transform.localScale = scale;

            netTransform.Teleport(position, rotation);
        }

        private void DealNodeLinkerDamage(Vector2 posA, Vector2 posB)
        {
            // 리시뮬레이션 중엔 스킵 — 상대(원격 오브젝트) 위치 기준 물리 쿼리는 리시뮬레이션마다
            // 결과가 달라질 수 있어서, 가드 없이 두면 같은 타격에 DealDamage가 여러 번 불릴 수 있다.
            if (!HasStateAuthority || Runner.IsResimulation)
                return;

            Vector2 direction = (posB - posA).normalized;
            float distance = Vector2.Distance(posA, posB);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 참고 : Owner는 자동으로 찾기 대상에서 제외된다.
            List<IDamageable> damageables = FindDamageablesInBox(transform.position, new Vector2(distance, nodeLinkerWidth), angle, targetLayer);

            foreach (IDamageable damageable in damageables)
            {
                DealDamage(damageable, linkerDamage, CharacterDamageSource.Periodic);
            }
        }

        private void SetConinuousNodeLinkerDamage(Vector2 posA, Vector2 posB)
        {
            if (!Object.HasStateAuthority || Runner.IsResimulation || !IsActive || IsDestroying || timers == null)
                return;

            DealNodeLinkerDamage(posA, posB);
            timers.Schedule(linkerDamageInterval, () =>
                SetConinuousNodeLinkerDamage(posA, posB));
        }
    }
}
