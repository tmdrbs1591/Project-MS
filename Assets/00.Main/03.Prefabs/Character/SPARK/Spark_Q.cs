using ProjectMS.CharacterSystem;
using UnityEngine;

public class SparkQNode : CharacterDeployable
{
    public bool IsValid => Object != null && Object.IsValid && !IsDestroying;

    [Header("Overlap Setting")]
    [SerializeField] private float overlapRadius = 0.1f;
    [SerializeField] private float stopOverlapRadius = 0.1f;
    [SerializeField] private Vector2 rightPos;
    [SerializeField] private Vector2 leftPos;
    [SerializeField] private Vector2 topPos;
    [SerializeField] private Vector2 stopPos;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public bool isRight;
    public bool isLeft;
    public bool isTop;
    public bool isStop;

    /// <summary>던져진 뒤 어딘가(바닥/벽/천장)에 붙어서 멈췄는지. 전선은 두 노드가 다 멈춰야 이어진다.
    /// 원격 클라에선 물리가 안 돌아 속도가 0으로 보일 수 있는데, 그땐 멈춘 걸로 봐도 무방하다.</summary>
    public bool IsSettled => isStop || isTop || isRight || isLeft || (rb != null && rb.linearVelocity.sqrMagnitude < 0.01f);

    private void Update()
    {
        isRight = Physics2D.OverlapCircle((Vector2)transform.position + rightPos, overlapRadius, LayerMask.GetMask("Ground"));
        isLeft = Physics2D.OverlapCircle((Vector2)transform.position + leftPos, overlapRadius, LayerMask.GetMask("Ground"));
        isTop = Physics2D.OverlapCircle((Vector2)transform.position + topPos, overlapRadius, LayerMask.GetMask("Ground"));
        isStop = Physics2D.OverlapCircle((Vector2)transform.position + stopPos, stopOverlapRadius, LayerMask.GetMask("Ground"));

        if (isRight)
        {
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        if (isLeft)
        {
            transform.rotation = Quaternion.Euler(0, 0, -90);
        }
        if (isTop)
        {
            transform.rotation = Quaternion.Euler(0, 0, 180);
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
        }
        if (isStop)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + rightPos, overlapRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftPos, overlapRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + topPos, overlapRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + stopPos, stopOverlapRadius);
    }
}