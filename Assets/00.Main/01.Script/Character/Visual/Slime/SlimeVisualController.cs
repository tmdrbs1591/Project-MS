using UnityEngine;

/// <summary>
/// 슬라임 캐릭터의 통통 움직임, 말랑말랑 몸통, 먼지 이펙트를 담당하는 전용 컨트롤러다.
///
/// [Fusion 이전 메모]
///   - 물리에 영향을 주는 통통점프(AddForce)는 네트워크 틱에서만 실행돼야 하므로
///     SlimeCharacter.OnSimulationTick → NetworkHop() 으로 호출된다(권한자/로컬 공통).
///   - 시각(스쿼시/먼지)은 모든 클라가 동기화된 상태로 재현하므로 TickVisual() 은
///     CharacterBase.Render → OnCharacterVisualTick 에서 모두 호출된다.
///   - 따라서 이 스크립트는 더 이상 PhotonView/IsMine 을 직접 다루지 않는다.
///
/// [필요한 것]
///   - SlimeCharacter 가 붙은 Player 루트에 함께 붙인다.
///   - Rigidbody2D 가 같은 루트에 있어야 한다.
///   - Body Transform 에 늘어나고 줄어들 몸통 Sprite Transform 을 연결한다.
///   - Dust Effect 에는 ParticleSystem 을 연결한다.
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

    private bool wasGrounded;
    private bool hasVisualState;
    private float hopTimer;
    private float stretch;
    private float stretchVelocity;
    private float halfHeight;
    private Vector3 bodyBaseLocalPosition;
    private Vector3 bodyBaseLocalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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

    public void PlayJumpStretch()
    {
        stretchVelocity += jumpStretch * 60f;
    }

    /// <summary>네트워크 틱마다(권한자) 호출. 통통점프 물리 적용.</summary>
    public void NetworkHop(float deltaTime, bool isGrounded, float moveInput)
    {
        if (!autoHop || !isGrounded || Mathf.Abs(moveInput) < movingVelocityThreshold)
        {
            hopTimer = 0f;
            return;
        }

        hopTimer += deltaTime;
        if (hopTimer < hopInterval)
            return;

        hopTimer = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * hopForce, ForceMode2D.Impulse);
        stretchVelocity += hopStretch * 45f;
    }

    /// <summary>매 프레임(모든 클라) 호출. 시각만 갱신.</summary>
    public void TickVisual(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity, int bodyDirection)
    {
        if (!hasVisualState)
        {
            wasGrounded = isGrounded;
            hasVisualState = true;
        }

        ApplyLandingSquash(isGrounded, velocity);
        UpdateDustEffect(isGrounded, moveInput);
        UpdateSquash(deltaTime, isGrounded, moveInput, velocity, bodyDirection);
        wasGrounded = isGrounded;
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
        // 스프링이 프레임 튐(로딩/렉으로 dt 급증)에서 폭주하지 않도록 dt 를 제한한다.
        // (explicit Euler + 높은 stiffness 는 큰 dt 에서 불안정해져 몸통이 무한히 늘어날 수 있다.)
        deltaTime = Mathf.Min(deltaTime, 0.0333f);

        float airStretch = 0f;
        if (!isGrounded)
            airStretch = Mathf.Clamp(velocity.y * airStretchFactor, -0.25f, 0.5f);

        float force = -squashStiffness * stretch - squashDamping * stretchVelocity;
        stretchVelocity += force * deltaTime;
        stretch += stretchVelocity * deltaTime;

        // 발산 방지용 안전 클램프.
        stretchVelocity = Mathf.Clamp(stretchVelocity, -50f, 50f);
        stretch = Mathf.Clamp(stretch, -0.6f, 0.8f);

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
