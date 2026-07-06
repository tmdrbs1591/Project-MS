using Fusion;
using UnityEngine;

/// <summary>
/// 포물선으로 던져지는 수류탄이다. (Fusion 2 / Shared 모드, 실제 Rigidbody2D 물리)
///
/// [동작]
///   - 던지는 순간의 초기 속도만 권한자가 Rigidbody2D에 적용하고, 그 이후는 Unity 물리
///     (중력/충돌/마찰)가 자연스럽게 포물선으로 날아가 착지하고 굴러가는 움직임을 만든다.
///     회전/바운스 같은 자연스러운 느낌이 필요해서 총알과 달리 물리 시뮬레이션을 그대로 쓴다.
///   - 위치/속도/회전은 NetworkRigidbody2D(Fusion Physics 애드온)가 동기화한다.
///     (참고: Shared 모드는 전송 빈도가 낮아 권한 없는 클라에서는 빠른 움직임이 권한자보다
///      약간 덜 매끄럽게 보일 수 있다 — PushableStructure와 같은 트레이드오프.)
///   - 스폰 후 fuseDuration(기본 2초)이 지나면 자동 폭발.
///   - 총알에 맞으면 즉시 폭발(퓨즈 대기 스킵).
///   - 폭발 판정/데미지는 던진 사람(StateAuthority)만 수행한다.
///
/// [필요 컴포넌트 (프리팹)]
///   - NetworkObject + Fusion의 NetworkRigidbody2D(Physics 애드온)
///   - Rigidbody2D (Body Type: Dynamic)
///   - CircleCollider2D 권장 — 구르는 모양이 자연스럽게 보이려면 원형이어야 한다.
///     Is Trigger는 켜지 않는다(꺼둔 채로 둔다). 총알 쪽 콜라이더가 Trigger이기 때문에,
///     이 콜라이더가 Trigger가 아니어도 총알에 맞으면 OnTriggerEnter2D가 정상적으로 발생하고,
///     동시에 바닥/캐릭터와는 평범한 물리 충돌로 반응해 착지/굴러가는 게 가능해진다.
///   - 착지 시 살짝 튀는 느낌을 원하면 Physics Material 2D(Bounciness > 0)를 콜라이더에 연결.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Grenade : NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float fuseDuration = 2f;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private GameObject explosionVfxPrefab;

    [Networked] private TickTimer FuseTimer { get; set; }

    private Rigidbody2D rb;

    // 던진 사람(권한)에서만 쓰는 값 — 복제 불필요(초기 속도 적용 후에는 NetworkRigidbody2D가 동기화).
    private Vector2 initialVelocity;
    private float damage;
    private LayerMask targetLayer;

    private bool exploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>스폰 직전(onBeforeSpawned)에 던진 사람이 호출. 네트워크 초기 상태를 세팅한다.</summary>
    public void Initialize(NetworkRunner runner, Vector2 throwVelocity, float explosionDamage, LayerMask explosionTargetLayer)
    {
        initialVelocity = throwVelocity;
        FuseTimer = TickTimer.CreateFromSeconds(runner, fuseDuration);

        damage = explosionDamage;
        targetLayer = explosionTargetLayer;
    }

    public override void Spawned()
    {
        // 던진 사람만 초기 속도를 부여한다. 이후 움직임은 NetworkRigidbody2D가 동기화한다.
        if (Object.HasStateAuthority)
            rb.linearVelocity = initialVelocity;
    }

    public override void FixedUpdateNetwork()
    {
        if (exploded)
            return;

        if (Object.HasStateAuthority && FuseTimer.Expired(Runner))
            Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded || Object == null || !Object.HasStateAuthority)
            return;

        if (!other.CompareTag("Bullet"))
            return;

        Explode();
    }

    private void Explode()
    {
        if (exploded)
            return;
        exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);
        foreach (Collider2D hit in hits)
        {
            CharacterBase target = hit.GetComponentInParent<CharacterBase>();
            if (target == null)
                continue;

            // 던진 사람 본인은 맞지 않는다.
            if (target.Object != null && target.Object.InputAuthority == Object.InputAuthority)
                continue;

            target.TakeDamage(damage);
        }

        Rpc_PlayExplosionVfx(transform.position);
        Runner.Despawn(Object);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_PlayExplosionVfx(Vector2 position)
    {
        if (explosionVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(explosionVfxPrefab, position, Quaternion.identity);
        Destroy(vfx, 2f);
    }
}
