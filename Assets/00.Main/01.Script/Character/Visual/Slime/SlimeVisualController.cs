using Photon.Pun;
using UnityEngine;

/// <summary>
/// 슬라임 캐릭터의 통통 움직임, 말랑말랑 몸통, 먼지 이펙트를 담당하는 전용 컨트롤러다.
///
/// [역할]
///   - 슬라임이 땅에서 이동 중이면 일정 간격마다 Rigidbody2D에 위쪽 힘을 줘서 통통 뛰게 만든다.
///   - 점프, 통통점프, 착지 순간에 몸통을 늘리거나 눌러서 Squash & Stretch 느낌을 만든다.
///   - 공중에서는 세로 속도에 따라 몸통이 길게 늘어나거나 납작해진다.
///   - 가만히 있을 때는 아주 작게 흔들리는 idle wobble을 만든다.
///   - 땅에서 이동 중일 때만 dustEffect의 emission을 켠다.
///
/// [필요한 것]
///   - SlimeCharacter가 붙은 Player 루트에 함께 붙인다.
///   - Rigidbody2D + Collider2D + PhotonView가 같은 루트에 있어야 한다.
///   - Body Transform에는 실제로 늘어나고 줄어들 몸통 Sprite Transform을 연결한다.
///   - Dust Effect에는 기존 Player 자식의 ParticleSystem을 연결한다.
///
/// 이 스크립트는 슬라임 전용 특성이므로 다른 캐릭터 프리팹에는 붙이지 않는다.
/// </summary>
[DefaultExecutionOrder(1)]
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeVisualController : MonoBehaviour
{
    [Header("Auto Hop")]
    [SerializeField] private bool autoHop = true;
    [SerializeField] private float hopInterval = 0.1f;
    [SerializeField] private float hopForce = 5f;
    [SerializeField] private float hopStretch = 0.12f;
    [SerializeField] private float movingVelocityThreshold = 0.05f;

    [Header("Body")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private bool anchorToBottom = true;

    [Header("Squash And Stretch")]
    [SerializeField] private float squashStiffness = 320f;
    [SerializeField] private float squashDamping = 14f;
    [SerializeField] private float jumpStretch = 0.35f;
    [SerializeField] private float landSquash = 0.45f;
    [SerializeField] private float airStretchFactor = 0.03f;
    [SerializeField] private float idleWobbleAmount = 0.04f;
    [SerializeField] private float idleWobbleSpeed = 6f;

    [Header("Effect")]
    [SerializeField] private ParticleSystem dustEffect;

    private Rigidbody2D rb;
    private PhotonView pv;

    private bool wasGrounded;
    private bool hasVisualState;
    private bool cachedIsGrounded;
    private float cachedMoveInput;
    private float hopTimer;
    private float stretch;
    private float stretchVelocity;
    private float halfHeight;
    private Vector3 bodyBaseLocalPosition;
    private Vector3 bodyBaseLocalScale;

    private bool IsMine => pv == null || pv.IsMine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pv = GetComponent<PhotonView>();

        if (bodyTransform == null)
            bodyTransform = transform;

        bodyBaseLocalPosition = bodyTransform.localPosition;
        bodyBaseLocalScale = bodyTransform.localScale;

        SpriteRenderer spriteRenderer = bodyTransform.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            halfHeight = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.extents.y : spriteRenderer.bounds.extents.y;

        if (dustEffect != null)
        {
            ParticleSystem.EmissionModule emission = dustEffect.emission;
            emission.enabled = false;
            dustEffect.Play();
        }
    }

    private void FixedUpdate()
    {
        if (!IsMine)
            return;

        HandleAutoHop();
    }

    public void PlayJumpStretch()
    {
        stretchVelocity += jumpStretch * 60f;
    }

    public void TickVisual(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity, int bodyDirection)
    {
        if (!hasVisualState)
        {
            wasGrounded = isGrounded;
            hasVisualState = true;
        }

        cachedIsGrounded = isGrounded;
        cachedMoveInput = moveInput;

        ApplyLandingSquash(isGrounded, velocity);
        UpdateDustEffect(isGrounded, moveInput);
        UpdateSquash(deltaTime, isGrounded, moveInput, velocity, bodyDirection);
        wasGrounded = isGrounded;
    }

    private void HandleAutoHop()
    {
        if (!autoHop || !cachedIsGrounded || Mathf.Abs(cachedMoveInput) < movingVelocityThreshold)
        {
            hopTimer = 0f;
            return;
        }

        hopTimer += Time.fixedDeltaTime;
        if (hopTimer < hopInterval)
            return;

        hopTimer = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * hopForce, ForceMode2D.Impulse);
        stretchVelocity += hopStretch * 45f;
    }

    private void ApplyLandingSquash(bool isGrounded, Vector2 velocity)
    {
        if (!isGrounded || wasGrounded)
            return;

        float impact = Mathf.Abs(velocity.y);
        float amount = Mathf.Clamp01(impact / 18f) * landSquash;
        stretchVelocity -= amount * 60f;
    }

    private void UpdateDustEffect(bool isGrounded, float moveInput)
    {
        if (dustEffect == null)
            return;

        bool running = isGrounded && Mathf.Abs(moveInput) > 0.01f;
        ParticleSystem.EmissionModule emission = dustEffect.emission;

        if (emission.enabled != running)
            emission.enabled = running;
    }

    private void UpdateSquash(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity, int bodyDirection)
    {
        float airStretch = 0f;
        if (!isGrounded)
            airStretch = Mathf.Clamp(velocity.y * airStretchFactor, -0.25f, 0.5f);

        float force = -squashStiffness * stretch - squashDamping * stretchVelocity;
        stretchVelocity += force * deltaTime;
        stretch += stretchVelocity * deltaTime;

        float wobble = 0f;
        bool idle = isGrounded && Mathf.Abs(moveInput) < 0.01f;
        if (idle)
            wobble = Mathf.Sin(Time.time * idleWobbleSpeed) * idleWobbleAmount;

        float finalStretch = stretch + airStretch + wobble;
        float scaleY = Mathf.Max(0.2f, 1f + finalStretch);
        float scaleX = Mathf.Max(0.2f, 1f - finalStretch * 0.6f);

        int direction = bodyDirection >= 0 ? 1 : -1;

        bodyTransform.localScale = new Vector3(
            Mathf.Abs(bodyBaseLocalScale.x) * scaleX * direction,
            bodyBaseLocalScale.y * scaleY,
            bodyBaseLocalScale.z
        );

        if (anchorToBottom && halfHeight > 0f)
        {
            float offsetY = halfHeight * (scaleY - 1f);
            bodyTransform.localPosition = bodyBaseLocalPosition + new Vector3(0f, offsetY, 0f);
        }
    }
}
