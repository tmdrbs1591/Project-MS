using UnityEngine;

/// <summary>
/// 슬라임 플레이어 캐릭터 예시다.
///
/// [역할]
///   - CharacterBase를 상속받아 실제 캐릭터의 평타, Q, E, 대시, 궁극기, 패시브를 구현한다.
///   - 후배들이 새 캐릭터를 만들 때 이 파일을 복사해서 클래스 이름과 스킬 내용을 바꾸면 된다.
///   - 공통 입력, 이동, 점프, 쿨타임, Photon 동기화는 CharacterBase와 Module 쪽에서 처리한다.
///   - 슬라임 전용 통통점프, 말랑말랑 몸통, 먼지 이펙트는 SlimeVisualController에 넘겨서 처리한다.
///
/// [필요한 것]
///   - Player 프리팹에 Rigidbody2D + Collider2D + PhotonView
///   - Player 프리팹에 SlimeVisualController를 함께 붙이고 Body Transform/Dust Effect를 연결
///   - PhotonView Observed Components에 이 SlimeCharacter 컴포넌트 등록
///   - 대시는 Movement.StartDefaultDash(this)를 호출해서 기본 대시를 사용
///
/// 프로토타입 단계에서는 Debug.Log로 동작을 확인하고, 이후 공격 판정/이펙트/데미지를 추가한다.
/// </summary>
public class SlimeCharacter : CharacterBase
{
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Visual")]
    [SerializeField] private SlimeVisualController visualController;

    protected override void Awake()
    {
        base.Awake();

        if (visualController == null)
            visualController = GetComponent<SlimeVisualController>();
    }

    protected override void BasicAttack()
    {
        Debug.Log("슬라임 평타");
        HitTargets(1);
    }

    protected override void SkillQ()
    {
        Debug.Log("슬라임 Q 스킬");
        HitTargets(2);
    }

    protected override void SkillE()
    {
        Debug.Log("슬라임 E 스킬");
    }

    protected override void Dash()
    {
        Debug.Log("슬라임 대시");
        Movement.StartDefaultDash(this);
    }

    protected override void Ultimate()
    {
        Debug.Log("슬라임 궁극기");
        HitTargets(5);
    }

    protected override void Passive()
    {
        // 패시브는 매 프레임 호출된다.
        // 예: 체력이 낮을 때 이동 속도 증가, 일정 시간마다 회복, 조건부 강화 등
    }

    protected override void OnCharacterJump()
    {
        if (visualController != null)
            visualController.PlayJumpStretch();
    }

    protected override void OnCharacterVisualTick(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity)
    {
        if (visualController != null)
            visualController.TickVisual(deltaTime, isGrounded, moveInput, velocity);
    }

    private void HitTargets(int damage)
    {
        if (attackPoint == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, targetLayer);
        foreach (Collider2D hit in hits)
        {
            Debug.Log($"{hit.name}에게 {damage} 데미지");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
