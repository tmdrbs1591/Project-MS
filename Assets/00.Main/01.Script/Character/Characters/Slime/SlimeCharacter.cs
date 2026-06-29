using Fusion;
using UnityEngine;

/// <summary>
/// 슬라임 플레이어 캐릭터. (Fusion 2 / Shared 모드)
///
/// [역할]
///   - CharacterBase 를 상속받아 평타/Q/E/대시/궁/패시브를 구현한다.
///   - 발사체는 Runner.Spawn 으로 네트워크 스폰되어 모두에게 보인다.
///   - 근접 스킬 데미지는 CharacterBase.TakeDamage 로 요청(피격자 권한에서 적용).
///   - 통통점프/스쿼시는 SlimeVisualController 에 위임한다.
///
/// 액션 함수들은 CharacterBase 가 "내 캐릭터(StateAuthority)" 에서만 호출하므로
/// 여기서 별도 권한 체크는 필요 없다.
/// </summary>
public class SlimeCharacter : CharacterBase
{
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private LayerMask projectileTargetLayer;

    // 팔 조준 동기화: 권한자가 마우스로 계산한 방향/각도를 원격이 그대로 재현한다.
    [Networked] private float NetAimAngle { get; set; }
    [Networked] private int NetArmDirection { get; set; }

    private SlimeVisualController visualController;
    private SlimeMouseArmController mouseArmController;

    // 발사 입력은 프레임에서 받고, 스폰은 네트워크 틱에서 처리(지연/지터 감소).
    private bool firePending;
    private CharacterActionType pendingFireAction;

    protected override void Awake()
    {
        base.Awake();

        if (visualController == null)
            visualController = GetComponent<SlimeVisualController>();

        if (mouseArmController == null)
            mouseArmController = GetComponent<SlimeMouseArmController>();
    }

    protected override void BasicAttack()
    {
        // 실제 스폰은 다음 네트워크 틱에서(OnNetworkTick) 처리해 틱과 정렬한다.
        firePending = true;
        pendingFireAction = CharacterActionType.BasicAttack;
    }

    protected override void SkillQ()
    {
        HitTargets(Stat.GetAttackDamage(CharacterActionType.SkillQ));
    }

    protected override void SkillE()
    {
        HitTargets(Stat.GetAttackDamage(CharacterActionType.SkillE));
    }

    protected override void Dash()
    {
        Movement.StartDefaultDash();
    }

    protected override void Ultimate()
    {
        HitTargets(Stat.GetAttackDamage(CharacterActionType.Ultimate));
    }

    protected override void Passive() { }

    protected override void OnCharacterJump()
    {
        if (visualController != null)
            visualController.PlayJumpStretch();
    }

    protected override void OnLocalUpdate()
    {
        // 내 캐릭터만 로컬 마우스로 팔을 조준한다.
        if (mouseArmController != null)
            mouseArmController.AimAtMouse();
    }

    protected override void OnNetworkTick(float deltaTime)
    {
        // 통통점프 물리는 네트워크 틱에서(권한자) 적용.
        if (visualController != null)
            visualController.NetworkHop(deltaTime, Movement.IsGrounded, Movement.MoveInput);

        // 팔 조준 값을 동기화(원격이 재현).
        if (mouseArmController != null)
        {
            NetAimAngle = mouseArmController.CurrentAngle;
            NetArmDirection = mouseArmController.ArmDirection;
        }

        // 틱 정렬된 발사체 스폰.
        if (firePending)
        {
            firePending = false;
            FireProjectile(pendingFireAction);
        }
    }

    protected override void OnCharacterVisualTick(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity)
    {
        if (visualController != null)
            visualController.TickVisual(deltaTime, isGrounded, moveInput, velocity, Movement.FacingDirection);

        // 원격 캐릭터의 팔은 동기화된 조준 값으로 재현한다(내 마우스가 아니라).
        if (!IsMine && mouseArmController != null)
            mouseArmController.ApplyAim(NetArmDirection, NetAimAngle);
    }

    private void FireProjectile(CharacterActionType actionType)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[SlimeCharacter] Projectile Prefab 이 연결되지 않았습니다.");
            return;
        }

        Transform firePoint = mouseArmController != null && mouseArmController.FirePoint != null
            ? mouseArmController.FirePoint
            : attackPoint;

        if (firePoint == null)
        {
            Debug.LogWarning("[SlimeCharacter] 발사 위치가 없습니다. Attack Point 또는 Fire Point 를 연결하세요.");
            return;
        }

        Vector2 direction = mouseArmController != null
            ? mouseArmController.GetAimDirection(Camera.main, firePoint)
            : (Movement.FacingDirection > 0 ? Vector2.right : Vector2.left);

        float damage = Stat.GetAttackDamage(actionType);
        Vector3 spawnPos = firePoint.position;

        // 네트워크 스폰: 발사자가 StateAuthority 가 되며, onBeforeSpawned 에서 방향/데미지 주입.
        Runner.Spawn(
            projectilePrefab,
            spawnPos,
            Quaternion.identity,
            Object.InputAuthority,
            (runner, obj) =>
            {
                Projectile p = obj.GetComponent<Projectile>();
                if (p != null)
                    p.Initialize(runner, spawnPos, direction, damage, projectileTargetLayer);
            });
    }

    private void HitTargets(float damage)
    {
        if (attackPoint == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, targetLayer);
        foreach (Collider2D hit in hits)
        {
            CharacterBase target = hit.GetComponentInParent<CharacterBase>();
            if (target != null && target != this)
                target.TakeDamage(damage);
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
