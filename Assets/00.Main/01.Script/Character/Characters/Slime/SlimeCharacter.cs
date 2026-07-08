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
    [SerializeField] private GameObject muzzleVfxPrefab;
    [Tooltip("이펙트 프리팹의 회전 0도가 향하는 기본 방향에 대한 보정각.")]
    [SerializeField] private float muzzleVfxAngleOffset = 0f;
    [Tooltip("이펙트 생성 위치를 파이어포인트에서 발사 방향으로 밀어내는 거리.")]
    [SerializeField] private float muzzleVfxForwardOffset = 10f;

    [Header("Skill Q (관통 파워샷)")]
    [SerializeField] private float qRange = 10f;
    [Tooltip("판정 폭. 0에 가까울수록 완전히 얇은 직선이라 살짝만 빗나가도 안 맞는다.")]
    [SerializeField] private float qBeamWidth = 0.8f;
    [SerializeField] private GameObject qCastVfxPrefab;

    [Header("Skill E (수류탄)")]
    [SerializeField] private Grenade grenadePrefab;
    [SerializeField] private float grenadeThrowSpeed = 10f;
    [SerializeField] private float grenadeSpreadAngle = 8f;
    [Tooltip("증강 'ESkillTripleThrow' 적용 시 추가 투척 사이의 간격(초). '뿅뿅뿅' 타이밍.")]
    [SerializeField] private float grenadeBurstInterval = 0.15f;

    // 팔 조준 동기화: 권한자가 마우스로 계산한 방향/각도를 원격이 그대로 재현한다.
    [Networked] private float NetAimAngle { get; set; }
    [Networked] private int NetArmDirection { get; set; }

    private SlimeVisualController visualController;
    private SlimeMouseArmController mouseArmController;

    // 발사/투척 입력은 프레임에서 받고, 스폰은 네트워크 틱에서 처리(지연/지터 감소).
    private bool firePending;
    private CharacterActionType pendingFireAction;
    private bool grenadeThrowPending;

    // ESkillTripleThrow 증강의 추가 투척 예약(뿅뿅뿅).
    private int pendingGrenadeBurstsRemaining;
    private float grenadeBurstTimer;

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
        // 실제 스폰은 다음 물리 틱에서(OnSimulationTick) 처리해 틱과 정렬한다.
        firePending = true;
        pendingFireAction = CharacterActionType.BasicAttack;
    }

    protected override void SkillQ()
    {
        Transform firePoint = ResolveFirePoint();
        if (firePoint == null)
            return;

        float damage = Stat.GetAttackDamage(CharacterActionType.SkillQ);

        if (HasAugment(AugmentType.QSkillOctoDirection))
        {
            // 증강: 조준 방향 대신 8방향으로 동시에 발사.
            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = Rotate(Vector2.right, i * 45f);
                FireQBeam(firePoint.position, direction, damage);
            }
        }
        else
        {
            Vector2 direction = mouseArmController != null
                ? mouseArmController.GetAimDirection(Camera.main, firePoint)
                : (Movement.FacingDirection > 0 ? Vector2.right : Vector2.left);

            FireQBeam(firePoint.position, direction, damage);
        }
    }

    private void FireQBeam(Vector2 origin, Vector2 direction, float damage)
    {
        // 관통: 사거리 안의 모든 대상을 한 번에 판정(막히지 않고 다 맞음).
        // 두께 0인 Raycast는 살짝만 빗나가도 안 맞으므로, 폭이 있는 CircleCast로 판정한다.
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, qBeamWidth * 0.5f, direction, qRange, targetLayer);
        foreach (RaycastHit2D hit in hits)
        {
            CharacterBase target = hit.collider.GetComponentInParent<CharacterBase>();
            if (target != null && target != this)
                target.TakeDamage(damage);
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Rpc_PlayQCastVfx(origin, angle);
    }

    protected override void SkillE()
    {
        // 실제 스폰은 다음 물리 틱에서(OnSimulationTick) 처리해 틱과 정렬한다.
        grenadeThrowPending = true;
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

    protected override void OnDamagedVisual()
    {
        if (visualController != null)
            visualController.PlayHitReaction();
    }

    protected override void OnLocalUpdate()
    {
        // 내 캐릭터만 로컬 마우스로 팔을 조준한다.
        if (mouseArmController != null)
            mouseArmController.AimAtMouse();
    }

    protected override void OnSimulationTick(float deltaTime)
    {
        // 통통점프 물리는 매 물리 틱에 적용(네트워크/로컬 공통, 안전).
        if (visualController != null)
            visualController.NetworkHop(deltaTime, Movement.IsGrounded, Movement.MoveInput);

        // 팔 조준 값 동기화는 네트워크 모드에서만(원격이 재현). 로컬 모드면 건너뛴다.
        if (IsNetworked && mouseArmController != null)
        {
            NetAimAngle = mouseArmController.CurrentAngle;
            NetArmDirection = mouseArmController.ArmDirection;
        }

        // 틱 정렬된 발사체 스폰. (로비에서는 발사 입력이 없으므로 firePending 도 항상 false)
        if (firePending)
        {
            firePending = false;
            FireProjectile(pendingFireAction);
        }

        if (grenadeThrowPending)
        {
            grenadeThrowPending = false;
            ThrowGrenades();

            // 증강: 첫 투척 후 추가로 2번 더(총 3연발, "뿅뿅뿅").
            pendingGrenadeBurstsRemaining = HasAugment(AugmentType.ESkillTripleThrow) ? 2 : 0;
            grenadeBurstTimer = grenadeBurstInterval;
        }
        else if (pendingGrenadeBurstsRemaining > 0)
        {
            grenadeBurstTimer -= deltaTime;
            if (grenadeBurstTimer <= 0f)
            {
                ThrowGrenades();
                pendingGrenadeBurstsRemaining--;
                grenadeBurstTimer = grenadeBurstInterval;
            }
        }
    }

    protected override void OnCharacterVisualTick(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity)
    {
        if (visualController != null)
            visualController.TickVisual(deltaTime, isGrounded, moveInput, velocity, Movement.FacingDirection);

        // 원격 캐릭터의 팔은 동기화된 조준 값으로 재현한다(내 마우스가 아니라).
        // 로컬 모드에선 내가 직접 AimAtMouse 로 조준하므로 여기선 적용하지 않는다.
        if (IsNetworked && !IsMine && mouseArmController != null)
            mouseArmController.ApplyAim(NetArmDirection, NetAimAngle);
    }

    private Transform ResolveFirePoint()
    {
        Transform firePoint = mouseArmController != null && mouseArmController.FirePoint != null
            ? mouseArmController.FirePoint
            : attackPoint;

        if (firePoint == null)
            Debug.LogWarning("[SlimeCharacter] 발사 위치가 없습니다. Attack Point 또는 Fire Point 를 연결하세요.");

        return firePoint;
    }

    private void FireProjectile(CharacterActionType actionType)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[SlimeCharacter] Projectile Prefab 이 연결되지 않았습니다.");
            return;
        }

        Transform firePoint = ResolveFirePoint();
        if (firePoint == null)
            return;

        Vector2 direction = mouseArmController != null
            ? mouseArmController.GetAimDirection(Camera.main, firePoint)
            : (Movement.FacingDirection > 0 ? Vector2.right : Vector2.left);

        float damage = Stat.GetAttackDamage(actionType);
        Vector3 spawnPos = firePoint.position;

        Vector2 muzzleVfxPos = (Vector2)spawnPos + direction * muzzleVfxForwardOffset;
        float muzzleAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Rpc_PlayMuzzleVfx(muzzleVfxPos, muzzleAngle);

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

    private void ThrowGrenades()
    {
        if (grenadePrefab == null)
        {
            Debug.LogWarning("[SlimeCharacter] Grenade Prefab 이 연결되지 않았습니다.");
            return;
        }

        Transform firePoint = ResolveFirePoint();
        if (firePoint == null)
            return;

        Vector2 baseDirection = mouseArmController != null
            ? mouseArmController.GetAimDirection(Camera.main, firePoint)
            : (Movement.FacingDirection > 0 ? Vector2.right : Vector2.left);

        float damage = Stat.GetAttackDamage(CharacterActionType.SkillE);
        int count = Mathf.Max(Stat.GrenadeCount, 1);
        Vector3 spawnPos = firePoint.position;

        for (int i = 0; i < count; i++)
        {
            float angleOffset = (i - (count - 1) * 0.5f) * grenadeSpreadAngle;
            Vector2 velocity = Rotate(baseDirection, angleOffset) * grenadeThrowSpeed;

            Runner.Spawn(
                grenadePrefab,
                spawnPos,
                Quaternion.identity,
                Object.InputAuthority,
                (runner, obj) =>
                {
                    Grenade g = obj.GetComponent<Grenade>();
                    if (g != null)
                        g.Initialize(runner, velocity, damage, projectileTargetLayer);
                });
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_PlayQCastVfx(Vector2 position, float angle)
    {
        if (qCastVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(qCastVfxPrefab, position, Quaternion.Euler(0f, 0f, angle));
        Destroy(vfx, 2f);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_PlayMuzzleVfx(Vector2 position, float angle)
    {
        if (muzzleVfxPrefab == null)
            return;

        float finalAngle = angle + muzzleVfxAngleOffset;
        GameObject vfx = Instantiate(muzzleVfxPrefab, position, Quaternion.Euler(0f, 0f, finalAngle));

        // 이 프리팹의 파티클은 빌보드(View) 정렬이라 오브젝트의 transform 회전을 무시하고
        // 항상 카메라를 향해 그려진다. 화면상 회전(Roll)은 파티클 자체의 시작 회전값으로
        // 직접 지정해야 반영된다. 파티클 startRotation은 화면 좌표계(Y축 반전) 기준이라
        // 좌우(Y=0)는 그대로 맞지만 상하는 뒤집혀 보이므로 부호를 반전시켜 보정한다.
        float rotationRad = -finalAngle * Mathf.Deg2Rad;
        foreach (ParticleSystem ps in vfx.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = rotationRad;
        }

        Destroy(vfx, 2f);
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
