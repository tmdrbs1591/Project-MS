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

        private HashSet<CharacterBase> triggeringPlayers = new(2);
        private HashSet<CharacterBase> playersInDamageTimer = new(2);

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

        private void OnTriggerEnter2D(Collider2D collision)
        {
            bool canGetTarget = TryGetTarget(collision, out CharacterBase target);
            if (!canGetTarget)
                return;

            triggeringPlayers.Add(target);

            if (!playersInDamageTimer.Contains(target))
                DealContinousNodeLinkerDamage(target);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            bool canGetTarget = TryGetTarget(collision, out CharacterBase target);
            if (!canGetTarget)
                return;

            triggeringPlayers.Remove(target);
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

        private void DealContinousNodeLinkerDamage(CharacterBase target)
        {
            if (!triggeringPlayers.Contains(target))
            {
                playersInDamageTimer.Remove(target);
                return;
            }

            DealDamage(target, linkerDamage, CharacterDamageSource.Periodic);

            playersInDamageTimer.Add(target);

            // 재귀 형식
            timers.Schedule(linkerDamageInterval, () =>
            {
                DealContinousNodeLinkerDamage(target);
            });
        }

        private bool TryGetTarget(Collider2D collision, out CharacterBase target)
        {
            target = default;

            if (collision == OwnerCharacter)
                return false;

            bool isCollsisionPlayer = (targetLayer.value & (1 << collision.gameObject.layer)) != 0;
            if (!isCollsisionPlayer)
                return false;

            bool canGetCharacterBase = collision.TryGetComponent<CharacterBase>(out CharacterBase player);
            if (!canGetCharacterBase)
                return false;

            target = player;
            return true;
        }
    }
}
