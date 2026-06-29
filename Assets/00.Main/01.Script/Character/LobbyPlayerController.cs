using UnityEngine;

/// <summary>
/// 로비(매칭 전) 전용 로컬 플레이어 컨트롤러. (네트워크 없음)
///
/// [왜 필요한가]
///   - 게임용 CharacterBase 는 Fusion 세션에 스폰된 뒤에만 움직인다.
///     따라서 로비에서 포탈까지 걸어가는 캐릭터는 네트워크 없이 로컬로 움직여야 한다.
///   - 이동/입력/비주얼은 기존 모듈(CharacterMovementHandler / CharacterInputHandler /
///     SlimeVisualController / SlimeMouseArmController)을 그대로 재사용한다.
///
/// [씬 설정]
///   - 로비 플레이어 프리팹에는 CharacterBase(SlimeCharacter) 대신 이 스크립트를 붙인다.
///     (NetworkObject 불필요. 로비 플레이어는 네트워크 객체가 아니다.)
///   - Rigidbody2D + Collider2D 필요. 비주얼/팔 컨트롤러는 있으면 자동 연동.
///   - 게임 씬으로 넘어가면 이 로비 플레이어는 사라지고, PlayerSpawner 가 네트워크
///     캐릭터를 새로 스폰한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LobbyPlayerController : MonoBehaviour
{
    [Header("Key Setting")]
    [SerializeField] private CharacterKeySetting keySetting = new CharacterKeySetting();

    [Header("Movement")]
    [SerializeField] private CharacterMovementData movementData = new CharacterMovementData();

    private Rigidbody2D rb;
    private Collider2D col;
    private CharacterInputHandler input;
    private CharacterMovementHandler movement;
    private SlimeVisualController visualController;
    private SlimeMouseArmController mouseArmController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        input = new CharacterInputHandler(keySetting);
        movement = new CharacterMovementHandler(rb, col, transform, movementData);

        visualController = GetComponent<SlimeVisualController>();
        mouseArmController = GetComponent<SlimeMouseArmController>();

        movement.Jumped += OnJumped;
    }

    private void OnDestroy()
    {
        if (movement != null)
            movement.Jumped -= OnJumped;
    }

    private void OnJumped()
    {
        if (visualController != null)
            visualController.PlayJumpStretch();
    }

    private void Update()
    {
        input.Read();

        // 로컬 마우스로 팔 조준 (로비는 나 혼자뿐이라 동기화 불필요)
        if (mouseArmController != null)
            mouseArmController.AimAtMouse();
    }

    private void FixedUpdate()
    {
        movement.TickFixed(input.ReadFixed(), Time.fixedDeltaTime);

        // 통통점프 물리
        if (visualController != null)
            visualController.NetworkHop(Time.fixedDeltaTime, movement.IsGrounded, movement.MoveInput);
    }

    private void LateUpdate()
    {
        // 스쿼시/먼지 등 시각 갱신
        if (visualController != null)
            visualController.TickVisual(Time.deltaTime, movement.IsGrounded, movement.MoveInput,
                                        movement.CurrentVelocity, movement.FacingDirection);
    }
}
