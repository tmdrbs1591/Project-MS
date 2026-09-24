using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비 전용 경량 캐릭터 컨트롤러. Fusion 없이 Rigidbody2D로만 동작한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LobbyCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [Tooltip("점프 시작 속도(월드 유닛/초). Rigidbody2D의 Gravity Scale이 4라 상승 높이는\n" +
        "대략 jumpForce² / 78 유닛이다(11이면 약 1.5유닛 = 캐릭터 키의 2.8배).")]
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = ~0; // 기본값: 전체 레이어

    [Header("Jump Feel")]
    [Tooltip("땅에서 떨어진 뒤에도 이만큼은 점프를 받아준다. 오토홉 때문에 걷는 동안 대부분\n" +
        "공중에 떠 있어서, 이게 없으면 스페이스바를 눌러도 씹히는 일이 잦다.")]
    [Min(0f)] [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("공중에서 미리 누른 점프를 이만큼 기억했다가 착지하는 순간 발동시킨다.")]
    [Min(0f)] [SerializeField] private float jumpBufferTime = 0.12f;
    [Tooltip("내려갈 때 중력 배율. 1보다 크면 붕 뜨지 않고 빠르게 떨어져 점프가 경쾌해진다.")]
    [Min(1f)] [SerializeField] private float fallGravityMultiplier = 2.2f;
    [Tooltip("올라가는 중에 점프 키를 떼면 적용되는 중력 배율. 짧게 누르면 낮게 뛴다.")]
    [Min(1f)] [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Auto Hop")]
    [Tooltip("이동 중 자동으로 통통 튀는 연출. 인게임(CharacterDefinition)의 Auto Hop과 같은 값/동작이다.\n" +
        "튀는 높이는 여기서, 몸이 늘어나는 세기는 Visual Profile의 Auto Hop Stretch Scale에서 조절한다.")]
    [SerializeField] private bool autoHop = true;
    [Min(0.01f)] [SerializeField] private float autoHopInterval = 0.1f;
    [Min(0f)] [SerializeField] private float autoHopForce = 5f;
    [Min(0f)] [SerializeField] private float autoHopMoveThreshold = 0.05f;

    [Header("Keys")]
    [SerializeField] private Key moveLeft = Key.A;
    [SerializeField] private Key moveRight = Key.D;
    [SerializeField] private Key jump = Key.Space;

    [Header("Visual (선택)")]
    [SerializeField] private CharacterVisualController visual;

    public void SetVisual(CharacterVisualController newVisual) => visual = newVisual;

    /// <summary>선택한 캐릭터의 CharacterDefinition.AutoHop을 로비에도 그대로 따르게 한다.
    /// (체이서/팝업처럼 걸을 때 튀지 않는 캐릭터가 로비에서만 튀는 일이 없도록)</summary>
    public void SetAutoHop(bool enabled)
    {
        autoHop = enabled;
        if (!enabled)
            autoHopTimer = 0f;
    }

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private bool wasGrounded;
    private int facing = 1;
    private float autoHopTimer;
    private float groundIgnoreTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool playerJumping;

    // 점프/오토홉 직후 몇 프레임은 아직 발이 땅에서 groundCheckDistance만큼 떨어지지 못해
    // CheckGround가 계속 "접지"로 읽는다. 그대로 두면 튀어오른 프레임마다 착지(PlayLanded)가
    // 터져서 착지음이 연사되므로, 아주 짧게 접지 판정을 무시한다.
    private const float GroundIgnoreAfterHop = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Fusion이 매치 중 Physics2D를 Script 모드로 전환한 채로 세션이 끝나버리면
        // Rigidbody2D가 자동 시뮬레이션되지 않는다. ResetStatics()의 복구는
        // RuntimeInitializeOnLoadMethod라 프로세스 시작 시 딱 1회만 실행되므로, 두 번째
        // 매치 이후 로비로 돌아올 때는 적용되지 않는다 — 로비 씬이 로드될 때마다(=이
        // 컨트롤러가 새로 생성될 때마다) 여기서도 매번 강제로 복원한다.
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        // lobbyLocked도 같은 이유로 매번 풀어줘야 한다 — MatchmakingManager.StartMatching()이
        // 매칭 시작 시 SetLocked(true)로 잠그는데, 정상적으로 매치가 끝나 ReturnToLobby()로
        // 돌아오는 경로는 CancelMatching()을 안 거치므로 아무도 다시 풀어주지 않는다. static이라
        // 씬을 새로 로드해도 값이 안 지워져서, 안 풀면 두 번째 매치부터 로비 캐릭터가 아예
        // 움직이지 못한다(포탈까지 걸어갈 수도 없어 재매칭 자체가 막힌 것처럼 보임).
        lobbyLocked = false;
    }

    private void Update()
    {
        CheckGround();

        if (lobbyLocked || chatTyping)
        {
            autoHopTimer = 0f;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            UpdateVisual(0f);
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float deltaTime = Time.deltaTime;

        float dir = 0f;
        if (kb[moveLeft].isPressed) dir -= 1f;
        if (kb[moveRight].isPressed) dir += 1f;

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        if (dir != 0f)
            facing = (int)Mathf.Sign(dir);

        // 코요테 타임 + 점프 버퍼. 오토홉이 0.1초마다 캐릭터를 띄우기 때문에 "땅에 있는
        // 프레임"이 얼마 안 된다 — isGrounded를 그 프레임에 직접 보고 판정하면 스페이스바가
        // 계속 씹힌다. 인게임(CharacterMovementHandler.Jump)과 같은 방식으로 받아준다.
        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - deltaTime;
        jumpBufferTimer = kb[jump].wasPressedThisFrame ? jumpBufferTime : jumpBufferTimer - deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            playerJumping = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            autoHopTimer = 0f;
            groundIgnoreTimer = GroundIgnoreAfterHop;
            visual?.PlayJump();
        }

        TickAutoHop(dir, deltaTime);
        ApplyBetterGravity(kb[jump].isPressed, deltaTime);

        if (!wasGrounded && isGrounded)
        {
            playerJumping = false;
            visual?.PlayLanded();
        }

        wasGrounded = isGrounded;

        UpdateVisual(dir);
    }

    private void UpdateVisual(float moveInput)
    {
        if (visual == null) return;

        Camera cam = Camera.main;
        Vector2 mouseWorld = cam != null
            ? (Vector2)cam.ScreenToWorldPoint(Mouse.current.position.ReadValue())
            : Vector2.zero;
        Vector2 aimDir = mouseWorld - (Vector2)transform.position;
        float aimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        int aimDirection = aimDir.x >= 0f ? 1 : -1;

        // 마우스 방향으로 캐릭터 몸 전체를 뒤집는다
        facing = aimDirection;

        visual.ApplyState(new CharacterVisualState(
            Time.deltaTime,
            isGrounded,
            moveInput,
            rb.linearVelocity,
            facing,
            aimDirection,
            aimAngle,
            false));
    }

    /// <summary>인게임 CharacterMovementHandler.ApplyBetterGravity와 같은 방식. 내려갈 때는 더 무겁게,
    /// 올라가는 중에 점프 키를 떼면 일찍 꺾이게 해서 점프가 붕 뜨지 않고 경쾌하게 느껴진다.
    /// (오토홉으로 뜬 것은 playerJumping이 false라 낮은 점프 보정을 받지 않는다.)</summary>
    private void ApplyBetterGravity(bool jumpHeld, float deltaTime)
    {
        float verticalVelocity = rb.linearVelocity.y;

        if (verticalVelocity < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * deltaTime;
        }
        else if (verticalVelocity > 0f && !jumpHeld && playerJumping)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * deltaTime;
        }
    }

    /// <summary>인게임 CharacterMovementHandler.TickAutoHop과 같은 규칙으로, 걸을 때마다
    /// 짧은 간격으로 위로 톡톡 튀어오르게 한다. 스쿼시 연출(PlayAutoHop)도 같이 재생하되
    /// 점프 효과음은 울리지 않는다.</summary>
    private void TickAutoHop(float moveInput, float deltaTime)
    {
        if (!autoHop || !isGrounded || Mathf.Abs(moveInput) < autoHopMoveThreshold)
        {
            autoHopTimer = 0f;
            return;
        }

        autoHopTimer += deltaTime;
        if (autoHopTimer < autoHopInterval)
            return;

        autoHopTimer = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * autoHopForce, ForceMode2D.Impulse);
        isGrounded = false;
        groundIgnoreTimer = GroundIgnoreAfterHop;
        visual?.PlayAutoHop();
    }

    private void CheckGround()
    {
        if (groundIgnoreTimer > 0f)
        {
            groundIgnoreTimer -= Time.deltaTime;
            isGrounded = false;
            return;
        }

        // 콜라이더 하단에서 아래로 쏴서 자기 자신을 맞추지 않는다
        Vector2 origin = col != null
            ? new Vector2(transform.position.x, col.bounds.min.y + 0.01f)
            : (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;
    }

    private static bool lobbyLocked;
    public static void SetLocked(bool locked) => lobbyLocked = locked;

    private static bool chatTyping;

    /// <summary>채팅 입력창에 포커스가 있는 동안 로비 이동을 막는다(안 막으면 "wasd"를 치는 순간
    /// 캐릭터가 같이 뛰어다닌다). 매칭 잠금(lobbyLocked)과는 별개의 플래그라 서로 덮어쓰지 않는다.</summary>
    public static void SetChatTyping(bool typing) => chatTyping = typing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lobbyLocked = false;
        chatTyping = false;
        // Fusion이 Physics2D를 Script 모드로 전환한 채 종료되면 다음 세션에서 Rigidbody2D가
        // 자동 시뮬레이션되지 않는다. 로비 진입 시 강제로 FixedUpdate 모드로 복원한다.
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }
}
