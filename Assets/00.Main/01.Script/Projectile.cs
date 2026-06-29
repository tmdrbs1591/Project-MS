using Fusion;
using UnityEngine;

/// <summary>
/// 캐릭터가 발사하는 네트워크 2D 발사체다. (Fusion 2 / 결정론적 이동)
///
/// [지연 없는 이동의 핵심]
///   - 발사 시작점/방향/속도/발사틱(FireTick)을 [Networked] 로 한 번만 보내고,
///     이후 위치는 "모든 클라가 똑같이 계산"한다: pos = start + dir * speed * elapsed.
///   - 그래서 NetworkTransform 의 보간 버퍼 지연("몇 박자 느림")이 없다.
///     각자 로컬에서 동일 공식을 돌리므로 항상 같은 위치에 그려진다.
///   - 충돌/데미지/소멸 판정만 발사자(StateAuthority)가 담당한다.
///
/// [필요 컴포넌트 (프리팹)]
///   - NetworkObject  (NetworkTransform 은 필요 없음 — 위치를 직접 계산하므로)
///   - Collider2D (Is Trigger 체크)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Projectile : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;

    [Networked] private Vector2 StartPosition { get; set; }
    [Networked] private Vector2 Dir { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int FireTick { get; set; }
    [Networked] private TickTimer Life { get; set; }

    // 발사자(권한)에서만 쓰는 값 — 복제 불필요.
    private float damage;
    private LayerMask targetLayer;

    /// <summary>스폰 직전(onBeforeSpawned)에 발사자가 호출. 네트워크 초기 상태를 세팅한다.</summary>
    public void Initialize(NetworkRunner runner, Vector3 startPos, Vector2 shootDirection,
                           float projectileDamage, LayerMask projectileTargetLayer)
    {
        Vector2 dir = shootDirection.sqrMagnitude > 0.001f ? shootDirection.normalized : Vector2.right;

        StartPosition = startPos;
        Dir = dir;
        Speed = speed;
        FireTick = runner.Tick;
        Life = TickTimer.CreateFromSeconds(runner, lifeTime);

        damage = projectileDamage;
        targetLayer = projectileTargetLayer;
    }

    public override void Spawned()
    {
        // 방향에 맞춰 회전(모든 클라). Dir 은 네트워크로 동기화되어 있다.
        float angle = Mathf.Atan2(Dir.y, Dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        UpdatePosition();
    }

    public override void FixedUpdateNetwork()
    {
        // 만료 시 발사자가 소멸시킨다.
        if (Life.Expired(Runner))
        {
            if (Object.HasStateAuthority)
                Runner.Despawn(Object);
            return;
        }

        UpdatePosition();
    }

    public override void Render()
    {
        // 틱 사이 프레임도 부드럽게: 같은 공식으로 보간 없이 외삽.
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float elapsed = (Runner.Tick - FireTick) * Runner.DeltaTime;
        transform.position = StartPosition + Dir * (Speed * elapsed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌 판정과 데미지는 발사자(권한)에서만.
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (((1 << other.gameObject.layer) & targetLayer.value) == 0)
            return;

        CharacterBase target = other.GetComponentInParent<CharacterBase>();
        if (target != null)
            target.TakeDamage(damage);

        Runner.Despawn(Object);
    }
}
