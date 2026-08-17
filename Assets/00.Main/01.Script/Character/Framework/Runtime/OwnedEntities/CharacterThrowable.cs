using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 수류탄, 섬광탄, 연막탄처럼 Rigidbody2D 물리로 포물선을 그리는 캐릭터 소유 오브젝트다.
    /// 충돌은 이동을 끝내거나 디스폰하지 않으며, 설정된 조건에서 퓨즈만 시작한다.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Fusion.Addons.Physics.NetworkRigidbody2D))]
    public class CharacterThrowable : CharacterOwnedEntity
    {
        [Header("Fuse")]
        [SerializeField] private CharacterThrowableFuseStartMode fuseStartMode =
            CharacterThrowableFuseStartMode.OnGroundContact;
        [Min(0f)] [SerializeField] private float fuseSeconds = 1f;
        [Tooltip("OnGroundContact 모드에서 퓨즈를 시작할 바닥·지형 레이어")]
        [SerializeField] private LayerMask groundLayer;

        [Networked] private NetworkBool NetFuseStarted { get; set; }
        [Networked] private NetworkBool NetFuseTriggered { get; set; }
        [Networked] private TickTimer NetFuseTimer { get; set; }

        public CharacterThrowableFuseStartMode FuseStartMode => fuseStartMode;
        public float FuseSeconds => Mathf.Max(0f, fuseSeconds);
        public float RemainingFuseTime => NetFuseTimer.RemainingTime(Runner) ?? 0f;
        public bool IsFuseStarted => NetFuseStarted;
        public bool HasFuseTriggered => NetFuseTriggered;

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (Object == null || !Object.HasStateAuthority || IsDestroying ||
                !NetFuseStarted || NetFuseTriggered || !NetFuseTimer.Expired(Runner))
            {
                return;
            }

            NetFuseTriggered = true;
            OnFuseExpiredAuthority();
            RequestDestroy(OwnedEntityDestroyReason.FuseExpired);
        }

        internal bool TryStartFuse(CharacterThrowableFuseTrigger trigger)
        {
            if (Object == null || !Object.HasStateAuthority || IsDestroying ||
                NetFuseStarted || !CharacterThrowableFuseRules.CanStart(fuseStartMode, trigger))
            {
                return false;
            }

            NetFuseStarted = true;
            NetFuseTriggered = false;
            NetFuseTimer = TickTimer.CreateFromSeconds(Runner, FuseSeconds);
            OnFuseStartedAuthority(trigger);
            return true;
        }

        protected sealed override void OnOwnedEntitySpawnedAuthority()
        {
            NetFuseStarted = false;
            NetFuseTriggered = false;
            NetFuseTimer = TickTimer.None;
            TryStartFuse(CharacterThrowableFuseTrigger.Spawn);
            OnThrowableSpawnedAuthority();
        }

        protected virtual void OnThrowableSpawnedAuthority() { }
        protected virtual void OnFuseStartedAuthority(CharacterThrowableFuseTrigger trigger) { }
        protected virtual void OnFuseExpiredAuthority() { }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null)
                return;

            int layerBit = 1 << collision.collider.gameObject.layer;
            if ((groundLayer.value & layerBit) != 0)
                TryStartFuse(CharacterThrowableFuseTrigger.GroundContact);
        }
    }
}
