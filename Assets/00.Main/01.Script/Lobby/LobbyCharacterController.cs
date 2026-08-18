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
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = ~0; // 기본값: 전체 레이어

    [Header("Keys")]
    [SerializeField] private Key moveLeft = Key.A;
    [SerializeField] private Key moveRight = Key.D;
    [SerializeField] private Key jump = Key.Space;

    [Header("Visual (선택)")]
    [SerializeField] private CharacterVisualController visual;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private bool wasGrounded;
    private int facing = 1;

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

        if (lobbyLocked)
        {
            UpdateVisual(0f);
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float dir = 0f;
        if (kb[moveLeft].isPressed) dir -= 1f;
        if (kb[moveRight].isPressed) dir += 1f;

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        if (dir != 0f)
            facing = (int)Mathf.Sign(dir);

        if (kb[jump].wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            visual?.PlayJump();
        }

        if (!wasGrounded && isGrounded)
            visual?.PlayLanded();

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

    private void CheckGround()
    {
        // 콜라이더 하단에서 아래로 쏴서 자기 자신을 맞추지 않는다
        Vector2 origin = col != null
            ? new Vector2(transform.position.x, col.bounds.min.y + 0.01f)
            : (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;
    }

    private static bool lobbyLocked;
    public static void SetLocked(bool locked) => lobbyLocked = locked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lobbyLocked = false;
        // Fusion이 Physics2D를 Script 모드로 전환한 채 종료되면 다음 세션에서 Rigidbody2D가
        // 자동 시뮬레이션되지 않는다. 로비 진입 시 강제로 FixedUpdate 모드로 복원한다.
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }
}
