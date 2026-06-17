using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

/// <summary>
/// 슬라임처럼 통통 튀는 캐릭터 컨트롤러.
///
/// 조작
///   A / D   : 좌우 이동
///   Space   : 점프
///
/// 핵심은 "스쿼시 & 스트레치(Squash & Stretch)" 연출이다.
/// 몸통 스프라이트(bodyTransform) 단 한 장만 있으면 되고,
/// 애니메이션 클립을 따로 만들 필요 없이 코드가 매 프레임 스케일을 계산한다.
///   - 점프 순간 : 위로 쭉! (세로로 늘어남)
///   - 공중 상승 : 속도에 따라 길쭉하게
///   - 공중 하강 : 살짝 늘어진 채 떨어짐
///   - 착지 순간 : 밑부분이 납작하게 퍼졌다가(스프링) 통통 튕겨 원래대로
///   - 가만히/걸을 때 : 미세하게 말랑말랑 호흡(흔들림)
///
/// 동그라미(원형) 캐릭터의 생동감을 위해 부피 보존 느낌으로
/// 세로가 늘어나면 가로가 줄고, 세로가 눌리면 가로가 퍼지도록 했다.
///
/// [필요한 컴포넌트]
///   - Rigidbody2D (이 오브젝트)
///   - Collider2D  (이 오브젝트, 바닥 판정용)
///   - 자식으로 스프라이트(SpriteRenderer)를 둔 Transform 하나 → bodyTransform 에 연결
///     (스케일을 흔들어도 콜라이더는 안 흔들리도록 비주얼은 자식에 두는 것을 권장)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeCharacterController : MonoBehaviour, IPunObservable
{
    [Header("이동")]
    [Tooltip("좌우 이동 속도 (units/sec)")]
    [SerializeField] private float moveSpeed = 7f;

    [Tooltip("점프 힘 (높을수록 높이 뜀)")]
    [SerializeField] private float jumpForce = 14f;

    [Tooltip("땅에서 방향을 바꿀 때 얼마나 빠르게 가속/감속할지")]
    [SerializeField] private float groundAccel = 60f;

    [Tooltip("공중에서의 좌우 조작 감도 (땅보다 둔하게)")]
    [SerializeField] private float airAccel = 25f;

    [Header("점프 감각 (살짝 무게감)")]
    [Tooltip("떨어질 때 추가로 붙는 중력 배수 (낙하가 둔하지 않게)")]
    [SerializeField] private float fallGravityMultiplier = 2.2f;

    [Tooltip("점프 버튼을 짧게 떼면 점프를 줄여주는 배수 (가변 점프 높이)")]
    [SerializeField] private float lowJumpMultiplier = 2.5f;

    [Tooltip("착지 직전에 미리 눌러도 점프가 먹히는 시간(초) — 점프 씹힘 방지")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Tooltip("땅에서 막 떨어진 직후에도 점프를 허용하는 시간(초) — 점프 씹힘 방지")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("플레이어 최대 속도 제한. 박스 충돌 등으로 멀리 날아가는 것(튕겨나감)을 막는다")]
    [SerializeField] private float maxSpeed = 18f;

    [Header("통통 점프 (이동 중에만 자동 폴짝)")]
    [Tooltip("켜면 '움직일 때만' 슬라임처럼 통통 튄다 (가만히 있으면 안 뜀)")]
    [SerializeField] private bool autoHop = true;

    [Tooltip("자동 폴짝 사이의 간격(초). 작을수록 빠르게 통통통통")]
    [SerializeField] private float hopInterval = 0.12f;

    [Tooltip("이동 중 폴짝 뛰는 힘 (작게! 낮게 살짝만)")]
    [SerializeField] private float hopForce = 1.6f;

    [Tooltip("폴짝마다 늘어나는 스쿼시 양 (작을수록 애니메이션이 차분)")]
    [SerializeField] private float hopStretch = 0.12f;

    [Header("바닥 판정")]
    [Tooltip("바닥으로 인식할 레이어")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Tooltip("발밑으로 쏘는 바닥 감지 거리")]
    [SerializeField] private float groundCheckDistance = 0.08f;

    [Header("말랑말랑 비주얼")]
    [Tooltip("스케일을 출렁이게 만들 몸통(자식 스프라이트) Transform. 비우면 자기 자신을 사용")]
    [SerializeField] private Transform bodyTransform;

    [Tooltip("스프라이트 밑면을 땅에 고정한 채로 늘어나고 눌리게 할지 (슬라임 느낌의 핵심)")]
    [SerializeField] private bool anchorToBottom = true;

    [Tooltip("스프링 강성 (클수록 빠르게 원래대로 / 탱탱함)")]
    [SerializeField] private float squashStiffness = 320f;

    [Tooltip("스프링 감쇠 (작을수록 더 오래 출렁출렁 튕김)")]
    [SerializeField] private float squashDamping = 14f;

    [Tooltip("점프 순간 위로 늘어나는 양")]
    [SerializeField] private float jumpStretch = 0.35f;

    [Tooltip("착지 시 눌리는 최대 양 (충돌 속도에 비례)")]
    [SerializeField] private float landSquash = 0.45f;

    [Tooltip("공중에서 수직 속도에 따라 늘어나는 정도")]
    [SerializeField] private float airStretchFactor = 0.03f;

    [Tooltip("가만히 서 있을 때 숨 쉬듯 말랑이는 크기 (아이들 애니)")]
    [SerializeField] private float idleWobbleAmount = 0.07f;

    [Tooltip("아이들 호흡 속도 (작을수록 느긋하게 숨 쉼)")]
    [SerializeField] private float idleWobbleSpeed = 3f;

    [Header("이펙트")]
    [Tooltip("땅에서 달릴 때만 재생할 먼지 파티클 (점프/공중에선 자동으로 꺼짐)")]
    [SerializeField] private ParticleSystem dustEffect;

    [Header("네트워크 (Photon)")]
    [Tooltip("원격 캐릭터를 '과거 시점'으로 살짝 지연 재생해 끊김 없이 보간하는 시간(초). " +
             "예측을 안 하므로 땅을 안 뚫고 매끄럽다. 클수록 더 부드럽지만 상대가 살짝 늦게 보임. 0.1 권장")]
    [SerializeField] private float interpolationDelay = 0.1f;

    // --- 내부 상태 ---
    private Rigidbody2D rb;
    private Collider2D col;

    // --- 네트워크 (스냅샷 보간) ---
    private PhotonView pv;             // 없으면(싱글 테스트) 자동으로 로컬로 동작
    private bool isLocal = true;       // 내가 조종하는 캐릭터인가
    private Vector2 syncedVelocity;    // 보간된 속도 (스쿼시 연출용)

    // 받은 상태를 시간순으로 모아두고, '과거 시점'을 두 스냅샷 사이로 보간한다.
    private struct NetState
    {
        public double time;     // 보낸 시점(서버 시간)
        public Vector3 pos;
        public Vector2 vel;
        public int facing;
        public float move;
        public bool grounded;
    }
    private readonly System.Collections.Generic.List<NetState> buffer =
        new System.Collections.Generic.List<NetState>(32);

    private float moveInput;          // -1 ~ 1
    private bool jumpPressed;         // 이번 프레임에 점프를 눌렀는가
    private bool jumpHeld;            // 점프를 누르고 있는가
    private bool isGrounded;
    private bool wasGrounded;
    private int facing = 1;           // 바라보는 방향 (1: 오른쪽, -1: 왼쪽)
    private float hopTimer;           // 자동 폴짝 박자 타이머
    private bool playerJumping;       // 스페이스로 한 "진짜 점프"인지 (가변 점프 컷 대상)
    private float jumpBufferTimer;    // 점프 입력 버퍼 남은 시간
    private float coyoteTimer;        // 코요테 타임 남은 시간

    // 스쿼시 스프링: stretch>0 이면 세로로 늘어남, <0 이면 납작하게 눌림
    private float stretch;
    private float stretchVel;

    private Vector3 bodyBaseLocalPos; // 몸통의 기준 로컬 위치
    private float halfHeight;         // 스프라이트 절반 높이 (밑면 고정 계산용)
    private Vector3 rootBaseScale;    // 루트 오브젝트의 기준 스케일 (방향전환용)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // 회전으로 넘어지지 않게 고정 (원형 슬라임이 굴러다니면 곤란)
        rb.freezeRotation = true;

        // 네트워크 소유권 판별: PhotonView 가 없으면 싱글플레이로 보고 로컬 취급
        pv = GetComponent<PhotonView>();
        isLocal = pv == null || pv.IsMine;

        if (!isLocal)
        {
            // 원격 캐릭터는 우리 물리로 움직이지 않는다. 위치는 네트워크로만 갱신.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
            col.enabled = false; // 상대 캐릭터끼리 밀치지 않게
        }

        if (bodyTransform == null)
            bodyTransform = transform;

        bodyBaseLocalPos = bodyTransform.localPosition;
        rootBaseScale = transform.localScale;

        // 밑면 고정에 쓸 스프라이트 절반 높이를 구해 둔다
        var sr = bodyTransform.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            halfHeight = sr.sprite != null ? sr.sprite.bounds.extents.y : sr.bounds.extents.y;

        // 먼지는 항상 재생 상태로 두고, emission 만 켰다 껐다 하며 자연스럽게 제어한다
        if (dustEffect != null)
        {
            var emission = dustEffect.emission;
            emission.enabled = false;
            dustEffect.Play();
        }
    }

    private void Update()
    {
        if (isLocal)
            ReadInput();
        else
            NetworkInterpolate(); // 원격 캐릭터를 부드럽고 즉각적으로 따라붙임
    }

    private void FixedUpdate()
    {
        if (isLocal)
        {
            // 내 캐릭터: 입력/물리 직접 시뮬레이션
            GroundCheck();
            Move();
            HandleJump();
            HandleAutoHop();
            ApplyBetterGravity();

            // 박스 충돌 등으로 폭발적으로 튕겨 날아가는 것 방지: 속도 상한
            if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        else
        {
            // 원격 캐릭터: isGrounded/moveInput/속도는 네트워크로 받은 값 사용.
            // 착지 스쿼시만 시각적으로 따라 재생한다.
            ApplyLandingSquash();
        }

        // 비주얼(먼지/스쿼시)은 양쪽 모두에서 돈다 (로컬은 실제 값, 원격은 동기화 값 기반)
        UpdateDustEffect();
        UpdateSquash(Time.fixedDeltaTime);

        wasGrounded = isGrounded;
    }

    /// <summary>스쿼시 계산에 쓸 현재 속도 (로컬은 물리, 원격은 동기화 값).</summary>
    private Vector2 CurrentVelocity => isLocal ? rb.linearVelocity : syncedVelocity;

    /// <summary>신규 Input System 으로 키 입력을 읽는다.</summary>
    private void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return; // 키보드가 없는 환경 방어

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        moveInput = x;

        // 바라보는 방향 갱신 → 루트 오브젝트의 scale.x 를 ±1 로 뒤집어 방향전환
        if (x > 0.01f) facing = 1;
        else if (x < -0.01f) facing = -1;
        ApplyFacing();

        jumpHeld = kb.spaceKey.isPressed;
        if (kb.spaceKey.wasPressedThisFrame)
            jumpPressed = true; // FixedUpdate 에서 소비
    }

    /// <summary>발밑으로 박스 캐스트를 쏴서 바닥에 닿았는지 판정.</summary>
    private void GroundCheck()
    {
        Bounds b = col.bounds;
        // 콜라이더 폭보다 살짝 좁은 박스를 발밑으로 내려쏜다
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.05f);
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.02f);

        RaycastHit2D hit = Physics2D.BoxCast(
            origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);

        // 위로 올라가는 중에는 땅으로 치지 않는다 (점프 직후 오판 방지)
        isGrounded = hit.collider != null && rb.linearVelocity.y <= 0.01f;
    }

    /// <summary>좌우 이동. 땅/공중에 따라 가속도를 다르게 줘서 조작감을 낸다.</summary>
    private void Move()
    {
        float targetX = moveInput * moveSpeed;
        float accel = isGrounded ? groundAccel : airAccel;

        float newX = Mathf.MoveTowards(
            rb.linearVelocity.x, targetX, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    /// <summary>
    /// 점프 처리 + 착지 감지(착지 스쿼시 트리거).
    /// 점프 버퍼링 + 코요테 타임으로 입력이 씹히지 않게 한다.
    /// </summary>
    private void HandleJump()
    {
        float dt = Time.fixedDeltaTime;

        // 막 착지한 순간: 떨어지던 속도에 비례해 납작하게 눌러준다
        ApplyLandingSquash();

        // 코요테 타임: 땅에 있으면 가득 채우고, 떨어지면 줄어든다
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;

        // 점프 버퍼: 누른 순간 채우고, 시간이 지나면 줄어든다
        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= dt;
        jumpPressed = false;

        // 버퍼 안에 입력이 있고 + 코요테 타임이 남아 있으면 점프
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            playerJumping = true; // 이건 "진짜 점프" → 가변 점프 컷 대상

            // 입력/코요테 모두 소비해서 한 번만 점프
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            // 점프 순간 위로 쭉 늘어나는 연출
            stretchVel += jumpStretch * 60f;
        }
    }

    /// <summary>
    /// 막 착지한 순간 낙하 속도에 비례해 납작하게 눌러주는 스쿼시.
    /// 로컬/원격 모두에서 호출되어 같은 착지 연출이 나오게 한다.
    /// </summary>
    private void ApplyLandingSquash()
    {
        if (isGrounded && !wasGrounded)
        {
            float impact = Mathf.Abs(CurrentVelocity.y); // 직전 낙하 속도
            float amount = Mathf.Clamp01(impact / 18f) * landSquash;
            stretchVel -= amount * 60f; // 스프링이 출렁이며 튕기게
            playerJumping = false;
        }
    }

    /// <summary>
    /// 이동 중일 때만 박자에 맞춰 작게 통통 튄다.
    /// 가만히 서 있으면 뛰지 않는다.
    /// </summary>
    private void HandleAutoHop()
    {
        // 땅에 없거나 / 꺼져 있거나 / 가만히 있으면 폴짝 안 함
        if (!autoHop || !isGrounded || Mathf.Abs(moveInput) < 0.01f)
        {
            hopTimer = 0f; // 멈춰 있으면 박자 리셋 → 움직이는 순간 바로 첫 폴짝
            return;
        }

        hopTimer += Time.fixedDeltaTime;
        if (hopTimer < hopInterval) return;

        hopTimer = 0f;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * hopForce, ForceMode2D.Impulse);

        playerJumping = false;          // 자동 폴짝은 가변 점프 컷 대상 아님
        stretchVel += hopStretch * 45f; // 작은 통통 스트레치
    }

    /// <summary>점프를 더 쫀쫀하게: 하강은 빠르게, 짧게 누르면 낮게.</summary>
    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // 떨어질 때 중력 가중
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && playerJumping)
        {
            // 올라가는 중에 버튼을 떼면 점프 컷 (스페이스로 한 진짜 점프에만 적용,
            // 자동 통통 폴짝은 잘리지 않게 한다)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// 먼지 이펙트는 "땅에서 달릴 때"만 나오게 한다.
    /// 가만히 있거나 점프(공중)하면 emission 을 꺼서 자연스럽게 사라진다.
    /// </summary>
    private void UpdateDustEffect()
    {
        if (dustEffect == null) return;

        bool running = isGrounded && Mathf.Abs(moveInput) > 0.01f;
        var emission = dustEffect.emission;
        if (emission.enabled != running)
            emission.enabled = running;
    }

    /// <summary>
    /// 스쿼시 & 스트레치 핵심.
    /// 감쇠 조화 진동(스프링)으로 stretch 값을 0으로 되돌리되,
    /// 점프/착지 충격과 공중 속도, 그리고 아이들 호흡을 더해 말랑하게 만든다.
    /// </summary>
    private void UpdateSquash(float dt)
    {
        // 1) 공중에서는 수직 속도에 따라 길게 늘어진다 (원격은 동기화된 속도 사용)
        float airStretch = 0f;
        if (!isGrounded)
            airStretch = Mathf.Clamp(CurrentVelocity.y * airStretchFactor, -0.25f, 0.5f);

        // 2) 스프링: stretch 를 0(평상시 모양)으로 끌어당기되 충격이 들어오면 출렁인다
        float force = -squashStiffness * stretch - squashDamping * stretchVel;
        stretchVel += force * dt;
        stretch += stretchVel * dt;

        // 3) 가만히 서 있을 때의 아이들 애니: 천천히 숨 쉬듯 말랑말랑 (땅 + 정지일 때만)
        //    걷거나 공중일 땐 안 섞이게 해서 또렷하게 보이도록 한다.
        float wobble = 0f;
        bool idle = isGrounded && Mathf.Abs(moveInput) < 0.01f;
        if (idle)
            wobble = Mathf.Sin(Time.time * idleWobbleSpeed) * idleWobbleAmount;

        // 최종 늘어남 = 스프링 + 공중 스트레치 + 호흡
        float s = stretch + airStretch + wobble;

        // 부피 보존 느낌: 세로가 늘면 가로는 줄고, 세로가 눌리면 가로는 퍼진다
        float scaleY = Mathf.Max(0.2f, 1f + s);
        float scaleX = Mathf.Max(0.2f, 1f - s * 0.6f);

        // 방향전환은 루트 오브젝트 scale.x 로 처리하므로 여기선 facing 을 빼고 순수 스쿼시만
        bodyTransform.localScale = new Vector3(scaleX, scaleY, 1f);

        // 4) 밑면을 땅에 고정 → 늘어나면 위로 자라고, 눌리면 밑이 퍼지는 슬라임 느낌
        if (anchorToBottom && halfHeight > 0f)
        {
            float offsetY = halfHeight * (scaleY - 1f);
            bodyTransform.localPosition = bodyBaseLocalPos + new Vector3(0f, offsetY, 0f);
        }
    }

    /// <summary>현재 facing 값으로 루트 오브젝트의 scale.x 부호를 뒤집어 방향전환.</summary>
    private void ApplyFacing()
    {
        transform.localScale = new Vector3(
            Mathf.Abs(rootBaseScale.x) * facing, rootBaseScale.y, rootBaseScale.z);
    }

    // ---------------- 네트워크 동기화 ----------------

    /// <summary>
    /// 스냅샷 보간: 원격 캐릭터를 "현재 시간 - interpolationDelay" 시점으로 그린다.
    /// 그 시점을 감싸는 두 스냅샷 사이를 단순 보간하므로, 예측이 없어
    /// 땅을 뚫지도 끊기지도 않고 실제 지나간 경로를 매끄럽게 재현한다.
    /// </summary>
    private void NetworkInterpolate()
    {
        if (buffer.Count == 0) return;

        double renderTime = PhotonNetwork.Time - interpolationDelay;

        // 가장 최신 스냅샷보다도 미래를 그려야 하면(패킷 끊김) 최신 상태로 정지 유지
        NetState newest = buffer[buffer.Count - 1];
        if (renderTime >= newest.time)
        {
            ApplyState(newest, newest.pos);
            return;
        }

        // 가장 오래된 것보다 과거면(아직 버퍼가 덜 참) 그 상태로
        if (renderTime <= buffer[0].time)
        {
            ApplyState(buffer[0], buffer[0].pos);
            return;
        }

        // renderTime 을 감싸는 두 스냅샷 [i], [i+1] 을 찾아 그 사이를 보간
        for (int i = 0; i < buffer.Count - 1; i++)
        {
            NetState a = buffer[i];
            NetState b = buffer[i + 1];
            if (renderTime < a.time || renderTime > b.time) continue;

            double span = b.time - a.time;
            float t = span > 0.0 ? (float)((renderTime - a.time) / span) : 0f;

            Vector3 pos = Vector3.Lerp(a.pos, b.pos, t);
            syncedVelocity = Vector2.Lerp(a.vel, b.vel, t);
            // 애니 상태값은 시점상 가까운 쪽을 사용
            NetState near = t < 0.5f ? a : b;
            ApplyState(near, pos);

            // 다 쓴 과거 스냅샷은 정리해 버퍼를 가볍게 유지
            if (i > 0) buffer.RemoveRange(0, i);
            return;
        }
    }

    /// <summary>보간 결과를 캐릭터에 반영 (위치 + 방향 + 애니 상태값).</summary>
    private void ApplyState(NetState s, Vector3 pos)
    {
        transform.position = pos;
        facing = s.facing;
        moveInput = s.move;
        isGrounded = s.grounded;
        ApplyFacing();
    }

    /// <summary>
    /// 위치·속도·방향·이동입력·접지상태를 주고받는다.
    /// 비주얼(스쿼시/먼지/팔)이 원격에서도 똑같이 보이도록 애니 상태값까지 함께 보낸다.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 캐릭터 → 남에게 전송
            stream.SendNext(transform.position);
            stream.SendNext(rb.linearVelocity);
            stream.SendNext((short)facing);
            stream.SendNext(moveInput);
            stream.SendNext(isGrounded);
        }
        else
        {
            // 남의 캐릭터 → 받은 스냅샷을 시간과 함께 버퍼에 쌓는다
            NetState s;
            s.time = info.SentServerTime;
            s.pos = (Vector3)stream.ReceiveNext();
            s.vel = (Vector2)stream.ReceiveNext();
            s.facing = (short)stream.ReceiveNext();
            s.move = (float)stream.ReceiveNext();
            s.grounded = (bool)stream.ReceiveNext();

            // 순서가 뒤바뀐(늦게 도착한 오래된) 패킷은 버린다
            if (buffer.Count > 0 && s.time <= buffer[buffer.Count - 1].time)
                return;

            buffer.Add(s);

            // 버퍼가 너무 커지지 않게 상한
            if (buffer.Count > 30)
                buffer.RemoveAt(0);
        }
    }

    // 씬 뷰에서 바닥 감지 박스를 눈으로 확인하기 위한 보조 표시
    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Bounds b = c.bounds;
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.05f);
        Vector3 center = new Vector3(b.center.x, b.min.y + 0.02f - groundCheckDistance, 0f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
