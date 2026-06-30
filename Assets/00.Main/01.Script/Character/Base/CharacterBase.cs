using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 모든 플레이어 캐릭터가 상속받는 부모 클래스다. (Fusion 2 / Shared 모드)
///
/// [역할]
///   - StateAuthority(= 내 캐릭터)인 클라만 입력을 읽고 물리를 시뮬레이션한다.
///   - 위치/회전/속도 동기화는 프리팹에 붙인 Fusion 의 NetworkRigidbody2D 가 담당한다.
///   - 방향/이동량/접지 같은 "시각용 상태"는 [Networked] 로 동기화해 원격 캐릭터의
///     애니메이션(스쿼시/먼지 등)을 똑같이 재현한다.
///   - 현재 체력은 [Networked] 단일 진실 소스이며, 데미지/힐은 그 캐릭터의
///     StateAuthority 에서만 RPC 로 적용된다 → 호스트/클라가 다를 수 없다.
///
/// [로컬(로비) 모드]
///   - 같은 캐릭터를 로비(매칭 전)에서도 그대로 쓸 수 있다.
///   - 아직 Fusion 세션에 스폰되지 않았으면(=Spawned 미호출) "로컬 모드" 로 동작한다:
///     네트워크 없이 일반 Update/FixedUpdate/LateUpdate 로 이동·비주얼만 구동하고,
///     [Networked] 프로퍼티는 일절 건드리지 않으며 전투/쿨다운도 돌지 않는다.
///   - 스폰되면 자동으로 네트워크 모드로 전환된다. (별도 로비 전용 스크립트 불필요)
///
/// [필요 컴포넌트 (프리팹)]
///   - NetworkObject (Fusion 이 자동 추가)
///   - Rigidbody2D + Collider2D
///   - Fusion 의 NetworkRigidbody2D (Physics 애드온) — 물리 위치 동기화
///   - 이 클래스를 상속한 캐릭터 스크립트(SlimeCharacter 등)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class CharacterBase : NetworkBehaviour
{
    [Header("Stat")]
    [SerializeField] private CharacterStatData statData = new CharacterStatData();

    [Header("Key Setting")]
    [SerializeField] private CharacterKeySetting keySetting = new CharacterKeySetting();

    [Header("Movement")]
    [SerializeField] private CharacterMovementData movementData = new CharacterMovementData();

    [Header("Cooldown")]
    [SerializeField] private CharacterCooldownData cooldownData = new CharacterCooldownData();

    /// <summary>현재 씬의 모든 캐릭터(카메라 등에서 사용).</summary>
    public static readonly List<CharacterBase> All = new List<CharacterBase>();

    // ---- 네트워크 동기화 상태 ----
    [Networked] private float NetHealth { get; set; }
    [Networked] private int NetFacing { get; set; }
    [Networked] private float NetMoveInput { get; set; }
    [Networked] private Vector2 NetVelocity { get; set; }
    [Networked] private NetworkBool NetGrounded { get; set; }
    [Networked] private NetworkBool NetIsDead { get; set; }

    public CharacterHealth Health { get; private set; }

    protected Rigidbody2D Rb { get; private set; }
    protected Collider2D Col { get; private set; }
    protected CharacterStat Stat { get; private set; }
    protected CharacterMovementHandler Movement { get; private set; }

    /// <summary>이 캐릭터를 내가 조종하는가(= StateAuthority).</summary>
    protected bool IsMine => Object != null && Object.HasStateAuthority;

    /// <summary>네트워크 세션에 스폰되어 동기화 중인가. false 면 로비(로컬) 모드.</summary>
    protected bool IsNetworked => isNetworked;

    private CharacterInputHandler input;
    private CharacterCooldownHandler cooldown;
    private bool ready;
    private bool isNetworked;   // Spawned() 가 호출되면 true (= Fusion 시뮬레이션의 일부)
    private float localHealth;  // 로컬 모드의 체력 백킹 스토어 ([Networked] 대용)
    private ChangeDetector changeDetector;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();

        input = new CharacterInputHandler(keySetting);
        cooldown = new CharacterCooldownHandler(cooldownData);
        Stat = new CharacterStat(statData);

        // 현재 체력 백킹 스토어: 네트워크 모드면 [Networked] NetHealth, 로컬 모드면 localHealth.
        // (NetHealth 는 스폰 전 접근하면 예외가 나므로 isNetworked 가 true 일 때만 읽고 쓴다.)
        Health = new CharacterHealth(Stat,
            () => isNetworked ? NetHealth : localHealth,
            v => { if (isNetworked) NetHealth = v; else localHealth = v; });
        Health.Died += OnDead;

        Movement = new CharacterMovementHandler(Rb, Col, transform, movementData);
        Movement.Jumped += OnCharacterJump;
    }

    public override void Spawned()
    {
        // 스폰됨 → 네트워크 모드로 전환. (이 시점부터 [Networked] 프로퍼티 접근 가능)
        isNetworked = true;

        if (!All.Contains(this))
            All.Add(this);

        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        // StateAuthority(내 캐릭터)만 체력을 초기화한다.
        if (Object.HasStateAuthority)
            Health.Initialize();

        ready = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    protected virtual void OnDestroy()
    {
        All.Remove(this);

        if (Movement != null)
            Movement.Jumped -= OnCharacterJump;
    }

    private void Update()
    {
        if (!isNetworked)
        {
            // 로컬(로비) 모드: 전투/쿨다운 없이 입력 읽기 + 마우스 조준만.
            input.Read();
            OnLocalUpdate();
            return;
        }

        if (!ready || !IsMine)
            return;

        // 입력(엣지 감지)은 프레임 기준으로 읽는다. 점프는 다음 네트워크 틱에서 소비.
        CharacterInputState inputState = input.Read();
        HandleActionInput(inputState);
        cooldown.Tick(Time.deltaTime);
        Passive();
        OnLocalUpdate();
    }

    private void FixedUpdate()
    {
        // 네트워크 모드에서는 FixedUpdateNetwork 가 물리를 담당하므로 여기선 건너뛴다.
        if (isNetworked)
            return;

        // 로컬(로비) 모드: 일반 물리 틱으로 이동/통통점프를 구동한다.
        Movement.TickFixed(input.ReadFixed(), Time.fixedDeltaTime);
        OnSimulationTick(Time.fixedDeltaTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsMine)
            return;

        // 내 캐릭터만 시뮬레이션. dt 는 네트워크 틱 간격.
        Movement.TickFixed(input.ReadFixed(), Runner.DeltaTime);
        OnSimulationTick(Runner.DeltaTime);

        // 원격 클라가 시각을 재현할 수 있도록 시각용 상태를 동기화.
        NetFacing = Movement.FacingDirection;
        NetMoveInput = Movement.MoveInput;
        NetGrounded = Movement.IsGrounded;
        NetVelocity = Rb.linearVelocity;
    }

    public override void Render()
    {
        if (!ready)
            return;

        // 사망 등 네트워크 값 변화 감지 (모든 클라에서)
        DetectChanges();

        if (!IsMine)
        {
            // 원격 캐릭터: 동기화된 시각 상태를 모듈에 주입해 애니메이션을 재현.
            // 속도도 동기화된 값을 써야 한다(원격 Rigidbody 는 kinematic 이라 velocity 가 부정확).
            Movement.ApplyNetworkVisualState(NetFacing, NetMoveInput, NetGrounded, NetVelocity);
        }

        OnCharacterVisualTick(Time.deltaTime, Movement.IsGrounded, Movement.MoveInput, Movement.CurrentVelocity);
    }

    private void LateUpdate()
    {
        // 네트워크 모드의 시각 갱신은 Render() 가 담당한다.
        if (isNetworked)
            return;

        // 로컬(로비) 모드: 스쿼시/먼지 등 시각 갱신.
        OnCharacterVisualTick(Time.deltaTime, Movement.IsGrounded, Movement.MoveInput, Movement.CurrentVelocity);
    }

    private void DetectChanges()
    {
        if (changeDetector == null)
            return;

        foreach (string change in changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetIsDead) && NetIsDead)
                OnDeadVisual();
        }
    }

    public void SetKeySetting(CharacterKeySetting newKeySetting)
    {
        keySetting = newKeySetting;
        input.SetKeySetting(newKeySetting);
    }

    private void HandleActionInput(CharacterInputState inputState)
    {
        TryUseAction(CharacterActionType.BasicAttack, inputState.BasicAttackPressed, BasicAttack);
        TryUseAction(CharacterActionType.SkillQ, inputState.SkillQPressed, SkillQ);
        TryUseAction(CharacterActionType.SkillE, inputState.SkillEPressed, SkillE);
        TryUseAction(CharacterActionType.Dash, inputState.DashPressed, Dash);
        TryUseAction(CharacterActionType.Ultimate, inputState.UltimatePressed, Ultimate);
    }

    private void TryUseAction(CharacterActionType actionType, bool pressed, System.Action action)
    {
        if (!pressed || !cooldown.CanUse(actionType))
            return;

        cooldown.Start(actionType);
        action?.Invoke();
    }

    // ---------------- 데미지 / 힐 (권한 측에서만 적용) ----------------

    /// <summary>어디서든 호출 가능. 실제 적용은 이 캐릭터의 StateAuthority 에서 일어난다.</summary>
    public void TakeDamage(float damage)
    {
        if (Object == null)
            return;

        if (Object.HasStateAuthority)
            ApplyDamage(damage);
        else
            Rpc_TakeDamage(damage);
    }

    public void RequestHealByMaxHealthPercent(float percent)
    {
        if (Object == null)
            return;

        if (Object.HasStateAuthority)
            Health.HealByMaxHealthPercent(percent);
        else
            Rpc_HealPercent(percent);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_TakeDamage(float damage)
    {
        ApplyDamage(damage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_HealPercent(float percent)
    {
        Health.HealByMaxHealthPercent(percent);
    }

    private void ApplyDamage(float damage)
    {
        Health.TakeDamage(damage);
        if (Health.IsDead)
            NetIsDead = true;
    }

    // ---------------- 가상 메서드 ----------------

    protected virtual void OnCharacterJump() { }

    /// <summary>매 프레임(내 캐릭터, 권한자) 호출. 마우스 조준 등 프레임 단위 로컬 처리용.</summary>
    protected virtual void OnLocalUpdate() { }

    /// <summary>
    /// 물리 틱마다 호출되는 시뮬레이션 훅. 슬라임 통통점프 등 물리 영향 로직용.
    /// 네트워크 모드면 FixedUpdateNetwork(권한자)에서, 로컬 모드면 FixedUpdate 에서 호출된다.
    /// 구현부에서 [Networked] 프로퍼티를 쓸 때는 반드시 IsNetworked 로 가드해야 한다.
    /// </summary>
    protected virtual void OnSimulationTick(float deltaTime) { }

    protected virtual void OnCharacterVisualTick(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity) { }

    /// <summary>StateAuthority 에서 사망 처리되었을 때(체력 0).</summary>
    protected virtual void OnDead()
    {
        Debug.Log("캐릭터 사망");
    }

    /// <summary>모든 클라에서 사망이 동기화됐을 때(연출용).</summary>
    protected virtual void OnDeadVisual() { }

    protected abstract void BasicAttack();
    protected abstract void SkillQ();
    protected abstract void SkillE();
    protected abstract void Dash();
    protected abstract void Ultimate();
    protected abstract void Passive();
}
