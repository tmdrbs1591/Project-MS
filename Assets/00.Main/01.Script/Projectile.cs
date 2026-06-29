using UnityEngine;

/// <summary>
/// 캐릭터가 발사하는 기본 2D 발사체다.
///
/// [역할]
///   - 지정된 방향으로 날아간다.
///   - 대상 레이어와 충돌하면 CharacterBase를 찾아 데미지를 준다.
///   - 일정 시간이 지나면 자동으로 사라진다.
///
/// [필요한 것]
///   - 발사체 프리팹에 Collider2D를 붙이고 Is Trigger를 켠다.
///   - Rigidbody2D를 붙이면 물리 속도로 이동하고, 없으면 Transform으로 이동한다.
///   - SlimeCharacter의 Projectile Prefab 칸에 이 스크립트가 붙은 프리팹을 연결한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private float damage;
    private LayerMask targetLayer;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized || rb != null)
            return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    public void Initialize(Vector2 shootDirection, float projectileDamage, LayerMask projectileTargetLayer)
    {
        direction = shootDirection.sqrMagnitude > 0.001f ? shootDirection.normalized : Vector2.right;
        damage = projectileDamage;
        targetLayer = projectileTargetLayer;
        initialized = true;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayer.value) == 0)
            return;

        CharacterBase target = other.GetComponentInParent<CharacterBase>();
        if (target != null)
            target.TakeDamage(damage);

        Destroy(gameObject);
    }
}
