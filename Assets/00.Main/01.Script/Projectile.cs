using Fusion;
using ProjectMS.CharacterSystem;
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

    [Header("Hit")]
    [SerializeField] private GameObject hitVfxPrefab;
    [Tooltip("이펙트 프리팹의 회전 0도가 향하는 기본 방향에 대한 보정각. " +
             "예: 프리팹이 회전 0도일 때 위쪽(+Y)을 향해 터지도록 만들어졌다면 -90.")]
    [SerializeField] private float hitVfxAngleOffset = -90f;
    [Tooltip("이펙트 생성 위치를 충돌 지점에서 총알이 날아온 방향(플레이어 쪽)으로 당기는 거리.")]
    [SerializeField] private float hitVfxPositionPullback = 1f;

    [Networked] private Vector2 StartPosition { get; set; }
    [Networked] private Vector2 Dir { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private int FireTick { get; set; }
    [Networked] private TickTimer Life { get; set; }

    // 발사자(권한)에서만 쓰는 값 — 복제 불필요.
    private float damage;
    private LayerMask targetLayer;

    // 원격 클라의 비주얼 전용 타이머. Spawned() 시점부터 실제 경과 시간을 잰다.
    private float localSpawnTime = -1f;

    // OnTriggerEnter2D 같은 물리 콜백 시점은 Fusion 이 [Networked] 값 읽기를 허용하는
    // 구간이 아니어서, 이미 스폰된 오브젝트여도 그 순간엔 Dir 을 읽으면 예외가 날 수 있다.
    // 그래서 Dir 은 절대 안 바뀌는 값이니 Spawned() 시점(안전한 구간)에 일반 필드로
    // 캐싱해두고, 외부(TravelDirection)에는 이 캐시만 노출한다.
    private bool spawned;
    private Vector2 cachedDir;
    private float cachedSpeed;

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
        spawned = true;
        cachedDir = Dir;
        cachedSpeed = Speed;

        // 방향에 맞춰 회전(모든 클라). Dir 은 네트워크로 동기화되어 있다.
        float angle = Mathf.Atan2(Dir.y, Dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        localSpawnTime = Time.time;
        transform.position = StartPosition;
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
        if (!Object.HasStateAuthority && localSpawnTime >= 0f)
        {
            // 원격 클라: 틱 기준 대신 로컬 스폰 시간 기준으로 이동.
            // FireTick 기준을 쓰면 원격 수신 지연(n틱)만큼 총알이 총구 앞에서 시작해 어긋난다.
            float visualElapsed = Time.time - localSpawnTime;
            transform.position = StartPosition + Dir * (Speed * visualElapsed);
        }
        else
        {
            // 발사자(권한): 시뮬레이션과 동일한 틱 기준으로 외삽.
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        float elapsed = (Runner.Tick - FireTick) * Runner.DeltaTime;
        transform.position = StartPosition + Dir * (Speed * elapsed);
    }

    /// <summary>총알의 진행 방향. 구조물이 맞았을 때 밀려나는 방향 계산 등 외부에서 참조한다.
    /// Networked 값을 물리 콜백에서 직접 읽지 않도록 Spawned() 시점에 캐싱해둔 값을 반환한다.</summary>
    public Vector2 TravelDirection => cachedDir;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 충돌 판정과 데미지는 발사자(권한)에서만.
        if (!spawned || Object == null || !Object.HasStateAuthority)
            return;

        if (((1 << other.gameObject.layer) & targetLayer.value) == 0)
            return;

        CharacterBase target = other.GetComponentInParent<CharacterBase>();
        if (target != null)
        {
            // 쏜 사람 본인은 맞지 않는다. 발사체는 스폰 시 발사자의 InputAuthority를 그대로 물려받는다.
            if (target.Object != null && target.Object.InputAuthority == Object.InputAuthority)
                return;

            target.RequestDamage(damage, Object.InputAuthority);
        }

        (Vector2 hitPosition, Vector2 hitNormal) = ResolveHitSurface(other);
        hitPosition -= cachedDir * hitVfxPositionPullback;
        Rpc_PlayHitVfx(hitPosition, hitNormal);
        Runner.Despawn(Object);
    }

    /// <summary>맞은 지점의 정확한 좌표와 그 지점의 표면 노멀을 구한다.
    /// transform.position은 Auto Sync Transforms가 꺼져있어 실제 충돌 시점엔 이미
    /// 콜라이더 안쪽까지 들어와 있을 수 있으므로, 이번 프레임 이동분만큼 뒤로 물러난
    /// 지점에서 진행 방향으로 레이캐스트해 실제 충돌면을 찾는다.</summary>
    private (Vector2 position, Vector2 normal) ResolveHitSurface(Collider2D other)
    {
        float probeDistance = Mathf.Max(cachedSpeed * Runner.DeltaTime, 0.1f) * 2f;
        Vector2 origin = (Vector2)transform.position - cachedDir * probeDistance;

        RaycastHit2D hit = Physics2D.Raycast(origin, cachedDir, probeDistance * 2f, targetLayer);
        if (hit.collider == other)
            return (hit.point, hit.normal);

        // 콜라이더가 너무 작거나 겹쳐 있어 레이가 빗나간 경우의 폴백.
        Vector2 closestPoint = other.ClosestPoint(transform.position);
        return (closestPoint, -cachedDir);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_PlayHitVfx(Vector2 position, Vector2 surfaceNormal)
    {
        if (hitVfxPrefab == null)
            return;

        // 충돌면의 노멀 방향을 보도록 회전시킨다. 프리팹 기본 방향 보정(hitVfxAngleOffset) 포함.
        float angle = Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg + hitVfxAngleOffset;

        GameObject vfx = Instantiate(hitVfxPrefab, position, Quaternion.Euler(0f, 0f, angle));
        Destroy(vfx, 2f);
    }
}
